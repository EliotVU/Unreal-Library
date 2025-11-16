using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using UELib.Core;
using UELib.ObjectModel.Annotations;
using UELib.Services;

namespace UELib;

public enum RegisterUnrealClassesStrategy
{
    None,
    StandardClasses,
    AssemblyClasses,
}

public sealed class UnrealPackageEnvironment : IDisposable
{
    public readonly string Name;
    public readonly string[] Directories;
    public readonly UnrealObjectContainer ObjectContainer;

    public UnrealPackageEnvironment(
        string name,
        RegisterUnrealClassesStrategy classesStrategy,
        Assembly? classesAssembly = null) : this(name, [], new UnrealObjectContainer())
    {
        switch (classesStrategy)
        {
            case RegisterUnrealClassesStrategy.None:
                break;

            case RegisterUnrealClassesStrategy.StandardClasses:
                AddUnrealClasses();

                if (classesAssembly != null)
                {
                    AddUnrealClasses(classesAssembly);

                    break;
                }

                break;

            case RegisterUnrealClassesStrategy.AssemblyClasses:
                Contract.Assert(classesAssembly != null, $"{nameof(classesAssembly)} cannot be null when using {nameof(RegisterUnrealClassesStrategy.AssemblyClasses)}");
                AddUnrealClasses(classesAssembly);

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(classesStrategy));
        }
    }

    public UnrealPackageEnvironment(string name, string[] directories) : this(name, directories, new UnrealObjectContainer())
    {
    }

    public UnrealPackageEnvironment(string name, string[] directories, UnrealObjectContainer container)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(name, nameof(name));
        ArgumentNullException.ThrowIfNull(directories, nameof(directories));
        ArgumentNullException.ThrowIfNull(container, nameof(container));
#endif

        Name = name;
        Directories = directories;
        ObjectContainer = container;

        AddUnrealClasses([
            (typeof(UPackage), typeof(UPackage).GetCustomAttribute<UnrealRegisterClassAttribute>(false)),
            (typeof(UClass), typeof(UClass).GetCustomAttribute<UnrealRegisterClassAttribute>(false)),
        ]);
    }

    // TODO: Source generator
    private static IEnumerable<(Type, UnrealRegisterClassAttribute)> GetRegisteredClassAttributes(Assembly assembly)
    {
        var types = assembly
            .GetExportedTypes()
            .Where(type => type.IsClass && type is { IsAbstract: false, IsPublic: true })
            .Select(type => (type, type.GetCustomAttribute<UnrealRegisterClassAttribute>(false)))
            .Where(t => t.Item2 != null);

        return types;
    }

    // ReSharper disable once UnusedMember.Global
    public void AddUnrealClass<T>()
        where T : UObject
    {
        var internalClassType = typeof(T);
        var attribute = internalClassType.GetCustomAttribute<UnrealRegisterClassAttribute>(false);
        if (attribute == null)
        {
            throw new InvalidOperationException($"Type {internalClassType.FullName} is not marked with {nameof(UnrealRegisterClassAttribute)}");
        }

        AddUnrealClasses([(internalClassType, attribute)]);
    }

    public void AddUnrealClass<T>(
        string className, string classPackageName, string superClassName,
        InternalClassFlags internalClassFlags = InternalClassFlags.Default)
        where T : UObject
    {
        var internalClassType = typeof(T);
        var attribute = new UnrealClassAttribute(className, classPackageName, superClassName, internalClassFlags);

        var staticClass = ObjectContainer.Find<UClass>(UnrealName.Class) ?? throw new InvalidOperationException("Missing Class'Core.Class'");

        var intrinsicClass = attribute.CreateStaticClass(internalClassType, attribute.ClassName);
        intrinsicClass.Class = staticClass;
        intrinsicClass.Outer = ObjectContainer.Find<UPackage>(attribute.ClassPackageName) ?? throw new InvalidOperationException("Missing class package");
        intrinsicClass.Super = ObjectContainer.Find<UClass>(attribute.SuperClassName) ?? throw new InvalidOperationException("Missing super class");
        ObjectContainer.Add(intrinsicClass);
    }

    /// <summary>
    /// Adds all Unreal classes registered with the <see cref="UnrealRegisterClassAttribute"/> from the UELib assembly.
    /// </summary>
    public void AddUnrealClasses()
    {
        AddUnrealClasses(Assembly.GetExecutingAssembly());
    }

    /// <summary>
    /// Adds all Unreal classes registered with the <see cref="UnrealRegisterClassAttribute"/> from the given assembly to the environment.
    /// </summary>
    /// <param name="classesAssembly">the assembly to scan for attributed classes.</param>
    public void AddUnrealClasses(Assembly classesAssembly)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(classesAssembly, nameof(classesAssembly));
#endif

        var staticClassesInfo = GetRegisteredClassAttributes(classesAssembly).ToList();
        Debug.Assert(staticClassesInfo.Any());

        AddUnrealClasses(staticClassesInfo);
    }

    private void AddUnrealClasses(List<(Type, UnrealRegisterClassAttribute)> staticClassesInfo)
    {
        // Initialize the environment, setup in stages to workaround re-cursive dependency issues.
        var packageNames = new HashSet<UName>();
        var classes = new List<(UClass, UName?, UName)>(staticClassesInfo.Count);
        foreach (var (internalClassType, classAttribute) in staticClassesInfo)
        {
            // Inherit the flags from any parent declaration too.
            var classFlagsAttribute = internalClassType.GetCustomAttribute<UnrealClassFlagsAttribute>(true);

            string internalPackageName = internalClassType.Namespace != null
                ? internalClassType.Namespace!.Substring(internalClassType.Namespace!.LastIndexOf('.') + 1)
                : "Core";
            // Register all unique package names. (Should only expect one for 'Core' and 'Engine')
            var packageName = new UName(internalPackageName); // staticClassInfo.PackageName;
            packageNames.Add(packageName);

            var className = new UName(internalClassType.Name.Substring(1)); // staticClassInfo.ClassName

            // Create the static class for each internal class type.
            var staticClass = classFlagsAttribute == null
                ? classAttribute.CreateStaticClass(internalClassType, className)
                : classAttribute.CreateStaticClass(internalClassType, className,
                    classFlagsAttribute.InternalClassFlags,
                    classFlagsAttribute.ClassFlags);
            var superName = classAttribute.SuperClassName;
            classes.Add((staticClass, superName, packageName));
        }

        // Create a UPackage as the root for each unique package name.
        var packages = new Dictionary<UName, UPackage>(packageNames.Count);
        foreach (var packageName in packageNames)
        {
            var package = new UPackage
            {
                Name = packageName,
                // We need this to be null, so it can be used an indicator for a not-yet-linked root package.
                //Package = UnrealPackage.TransientPackage
            };
            packages.Add(packageName, package);
            ObjectContainer.Add(package);
        }

        // Register and link all static classes to their respective packages.
        foreach (var (staticClass, _, packageName) in classes)
        {
            var package = ObjectContainer.Find<UPackage>(packageName);
            Debug.Assert(package != null);
            staticClass.Outer = package;
            staticClass.Class = staticClass;
            ObjectContainer.Add(staticClass);
        }

        // Now that we have objects hashed by outer...
        foreach (var (staticClass, superName, _) in classes)
        {
            if (superName.HasValue == false || superName.Value.IsNone()) continue;

            // What if the super is within another package???
            staticClass.Super = ObjectContainer.Find<UClass>(superName.Value);
            Debug.Assert(staticClass.Super != null);
        }

        // Set up the static class for the static UPackage class.
        var packageStaticClass = ObjectContainer.Find<UClass>(UnrealName.Package);
        Debug.Assert(packageStaticClass != null);
        foreach (var pair in packages)
        {
            var pkg = pair.Value;
            pkg.Class = packageStaticClass;
        }

        foreach (var (staticClass, _, _) in classes)
        {
            Trace.WriteLine($"Created static class {staticClass.GetReferencePath()} for internal type '{staticClass.InternalType}'");
        }
    }

    public void Dispose()
    {
        foreach (var obj in ObjectContainer.Enumerate())
        {
            // Dispose of all archives (which may have an associated stream).
            if ((UnrealPackage?)obj.Package != null) // Can be null if the object was created manually.
            {
                obj.Package.Archive.Dispose();
            }
        }

        ObjectContainer.Dispose();
    }
}
