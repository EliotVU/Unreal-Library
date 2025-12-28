using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Reflection;
using UELib.Core;
using UELib.Engine;
using UELib.Flags;
using UELib.ObjectModel.Annotations;
using UELib.Services;

namespace UELib;

public enum RegisterUnrealClassesStrategy
{
    /// <summary>
    /// Register all essential classes, choose this for a minimal implementation.
    /// </summary>
    EssentialClasses,

    /// <summary>
    /// Register all standard classes, choose this to register all available classes in the UELib assembly.
    /// </summary>
    StandardClasses,
}

public sealed class UnrealPackageEnvironment : IDisposable
{
    public string Name { get; }

    private readonly UnrealObjectContainer _ObjectContainer;

    public UnrealPackageEnvironment(
        string name,
        RegisterUnrealClassesStrategy classesStrategy,
        Assembly? classesAssembly = null) : this(name, new UnrealObjectContainer())
    {
        switch (classesStrategy)
        {
            case RegisterUnrealClassesStrategy.EssentialClasses:
                AddUnrealClasses([
                    (typeof(UObject), typeof(UObject).GetCustomAttribute<UnrealRegisterClassAttribute>(false)),
                    (typeof(UField), typeof(UField).GetCustomAttribute<UnrealRegisterClassAttribute>(false)),
                    (typeof(UStruct), typeof(UStruct).GetCustomAttribute<UnrealRegisterClassAttribute>(false)),
                    (typeof(UState), typeof(UState).GetCustomAttribute<UnrealRegisterClassAttribute>(false)),
                    // UClass can work without its super classes, but it's nice to have it all linked up to satisfy the assertions.
                    (typeof(UClass), typeof(UClass).GetCustomAttribute<UnrealRegisterClassAttribute>(false)),
                    // We really do need this one for the RootPackage construction of the transient package and environment etc.
                    (typeof(UPackage), typeof(UPackage).GetCustomAttribute<UnrealRegisterClassAttribute>(false)),
                ]);
                break;

            case RegisterUnrealClassesStrategy.StandardClasses:
                AddUnrealClasses();

                if (classesAssembly != null)
                {
                    AddUnrealClasses(classesAssembly);
                }

                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(classesStrategy));
        }

        if (classesAssembly != null)
        {
            AddUnrealClasses(classesAssembly);
        }
    }

    public UnrealPackageEnvironment(string name, UnrealObjectContainer container)
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(container);
#endif

        Name = name;
        _ObjectContainer = container;
    }

    // TODO: Source generator
    private static IEnumerable<(Type, UnrealClassAttribute)> GetRegisteredClassAttributes(Assembly assembly)
    {
        var types = assembly
            .GetExportedTypes()
            .Where(type => type.IsClass && type is { IsAbstract: false, IsPublic: true })
            .Select(type => (type, (UnrealClassAttribute)type.GetCustomAttribute<UnrealRegisterClassAttribute>(false)))
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

        var staticClass = _ObjectContainer.Find<UClass>(UnrealName.Class) ?? throw new InvalidOperationException("Missing Class'Core.Class'");

        var intrinsicClass = attribute.CreateStaticClass(internalClassType, attribute.ClassName);
        intrinsicClass.Class = staticClass;
        intrinsicClass.Outer = _ObjectContainer.Find<UPackage>(attribute.ClassPackageName) ?? throw new InvalidOperationException("Missing class package");
        intrinsicClass.Super = _ObjectContainer.Find<UClass>(attribute.SuperClassName) ?? throw new InvalidOperationException("Missing super class");
        _ObjectContainer.Add(intrinsicClass);
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
        ArgumentNullException.ThrowIfNull(classesAssembly);
#endif

        var staticClassesInfo = GetRegisteredClassAttributes(classesAssembly).ToList();
        Debug.Assert(staticClassesInfo.Any());

        AddUnrealClasses(staticClassesInfo);
    }

    internal void AddUnrealClasses(List<(Type, UnrealClassAttribute)> staticClassesInfo)
    {
        // Initialize the environment, setup in stages to workaround re-cursive dependency issues.
        var packageNames = new HashSet<UName>();
        var classes = new List<(UClass, UName?, UName)>(staticClassesInfo.Count);
        foreach (var (internalClassType, classAttribute) in staticClassesInfo)
        {
            Debug.Assert(classAttribute != null, $"Missing UnrealRegisterClassAttribute on type '{internalClassType}'");
            Debug.Assert(typeof(UObject).IsAssignableFrom(internalClassType), $"A static class must inherit from internal type '{nameof(UObject)}'");

            // Inherit the flags from any parent declaration too.
            var classFlagsAttribute = internalClassType.GetCustomAttribute<UnrealClassFlagsAttribute>(true);

            // Register all unique package names. (Should only expect one for 'Core' and 'Engine')
            var packageName = classAttribute.ClassPackageName.IsNone()
                ? internalClassType.Namespace != null
                    ? new UName(internalClassType.Namespace.Substring(internalClassType.Namespace.LastIndexOf('.') + 1))
                    : UnrealName.Core
                : classAttribute.ClassPackageName;

            packageNames.Add(packageName);

            if (classAttribute.ClassName.IsNone())
            {
                if (typeof(AActor).IsAssignableFrom(internalClassType))
                {
                    Debug.Assert(internalClassType.Name.StartsWith("A"), "An 'Actor' derived static class must start with an uppercase 'A'");
                }
                else
                {
                    Debug.Assert(internalClassType.Name.StartsWith("U"), "A static class must start with an uppercase 'U'");
                }
            }

            var className = classAttribute.ClassName.IsNone()
                ? new UName(internalClassType.Name.Substring(1)) // staticClassInfo.ClassName
                : classAttribute.ClassName;

            var superClassName = classAttribute.SuperClassName.IsNone() && internalClassType != typeof(UObject)
                ? new UName(internalClassType.BaseType!.Name.Substring(1))
                : classAttribute.SuperClassName;

            // Create the static class for each internal class type.
            var staticClass = classFlagsAttribute == null
                ? classAttribute.CreateStaticClass(internalClassType, className)
                : classAttribute.CreateStaticClass(internalClassType, className,
                    classFlagsAttribute.InternalClassFlags,
                    classFlagsAttribute.ClassFlags);
            var superName = superClassName;
            classes.Add((staticClass, superName, packageName));
        }

        // Create a UPackage as the root for each unique package name.
        var packages = new Dictionary<UName, UPackage>(packageNames.Count);
        foreach (var packageName in packageNames)
        {
            // Let's not link any duplicates!
            if (_ObjectContainer.Find<UPackage?>(packageName) != null)
            {
                continue;
            }

            var package = new UPackage
            {
                Name = packageName,
                Package = UnrealPackage.TransientPackage
            };
            packages.Add(packageName, package);
            _ObjectContainer.Add(package);
        }

        // Register and link all static classes to their respective packages.
        foreach (var (staticClass, _, packageName) in classes)
        {
            var package = _ObjectContainer.Find<UPackage>(packageName);
            Debug.Assert(package != null);

            staticClass.Outer = package;
            staticClass.Class = staticClass;
            // Likely the TransientPackage, but in some cases the package may have already been constructed, in that case, link to that.
            staticClass.Package = package.Package;
            _ObjectContainer.Add(staticClass);
        }

        // Now that we have objects hashed by outer...
        foreach (var (staticClass, superName, _) in classes)
        {
            if (!superName.HasValue || superName.Value.IsNone()) continue;

            // What if the super is within another package???
            staticClass.Super = _ObjectContainer.Find<UClass?>(superName.Value);
            Debug.Assert(staticClass.Super != null, $"Couldn't link super '{superName}' for class {staticClass}");
        }

        // Set up the static class for the static UPackage class.
        var packageStaticClass = _ObjectContainer.Find<UClass>(UnrealName.Package);
        Debug.Assert(packageStaticClass != null, $"Couldn't find the static class for internal type '{nameof(UPackage)}'");

        foreach (var pair in packages)
        {
            var pkg = pair.Value;
            pkg.Class = packageStaticClass;
        }

        foreach (var (staticClass, _, _) in classes)
        {
            LibServices.Trace("Created static class {0} for internal type '{1}'", staticClass, staticClass.InternalType);
        }
    }

    public UClass GetStaticClass<T>()
        where T : UObject
    {
        // FIXME: Stupid workaround
        Debug.Assert(typeof(T).Name.StartsWith("U"), "A static class must start with an uppercase 'U'");
        return GetStaticClass(new UName(typeof(T).Name.Substring(1)));
        var classAttr = typeof(T).GetCustomAttribute<UnrealClassAttribute>();
        Debug.Assert(classAttr != null, "Internal type is missing the static class attribute.");

        var classPackage = FindObject<UPackage>(classAttr.ClassPackageName);
        var @class = FindObject<UClass>(classAttr.ClassName, classPackage);
        Debug.Assert(@class != null, $"Couldn't find the static class for internal type in package {classPackage}.");

        return @class;
    }

    public UClass GetStaticClass(in UName className)
    {
        return FindObject<UClass>(className) ?? throw new InvalidOperationException($"Couldn't find static class by name '{className}'");
    }

    public T CreateObject<T>(UnrealPackage package, in UName name, UClass @class, UObject? outer)
        where T : UObject, new() =>
        CreateObject<T>(package, name, @class, outer, [ObjectFlag.Public]);

    public T CreateObject<T>(UnrealPackage package, in UName name, UClass @class, UObject? outer, ObjectFlag[] objectFlags)
        where T : UObject, new()
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(@class, nameof(@class));
#endif

        var newObject = new T
        {
            // TODO: Doesn't actually map to anything without a branch link.
            ObjectFlags = new UnrealFlags<ObjectFlag>(new ulong[(int)ObjectFlag.Max], objectFlags),

            Package = package,
            PackageIndex = UPackageIndex.Null,

            Name = name,
            Class = @class,
            Outer = outer,
        };

        AddObject(newObject);

        return newObject;
    }

    internal void AddObject(UObject newObject)
    {
        _ObjectContainer.Add(newObject);
        // TODO: Generalize the event emitter.
        newObject.Package.Linker.EventEmitter?.OnAdded(newObject);
    }

    /// <summary>
    /// Enumerates over all objects in this environment.
    /// </summary>
    /// <returns>Enumerable objects</returns>
    public IEnumerable<UObject> EnumerateObjects()
    {
        return _ObjectContainer
            .Enumerate();
    }

    /// <summary>
    /// Enumerates over all objects in this environment and that belong to a particular object.
    /// </summary>
    /// <returns>Enumerable objects</returns>
    public IEnumerable<UObject> EnumerateObjects(UObject? outer)
    {
        return _ObjectContainer
            .Enumerate()
            .Where(obj => obj.Outer == outer);
    }

    /// <summary>
    /// Enumerates over all objects that belong to this environment.
    /// </summary>
    /// <typeparam name="T">The class type to constrain the search to.</typeparam>
    /// <returns>Enumerable objects</returns>
    public IEnumerable<T> EnumerateObjects<T>()
        where T : UObject
    {
        return EnumerateObjects().OfType<T>();
    }

    public T FindObject<T>(string name)
        where T : UObject?
    {
        return _ObjectContainer.Find<T>(IndexName.ToIndex(name));
    }

    public T FindObject<T>(in UName name)
        where T : UObject?
    {
        return _ObjectContainer.Find<T>(name);
    }

    public T FindObject<T>(string name, UClass @class)
        where T : UObject?
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(@class, nameof(@class));
#endif

        return _ObjectContainer.Find<T>(IndexName.ToIndex(name), @class);
    }

    public T FindObject<T>(in UName name, UObject? outer)
        where T : UObject?
    {
        return outer == null
            ? _ObjectContainer.Find<T>(name)
            : _ObjectContainer.Find<T>(name, outer.Name);
    }

    public T FindObject<T>(in UName name, UClass @class, UObject? outer)
        where T : UObject?
    {
#if NET8_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(@class, nameof(@class));
#endif

        return outer == null
            ? _ObjectContainer.Find<T>(name.GetHashCode(), @class)
            : _ObjectContainer.Find<T>(name.GetHashCode(), outer.Name.GetHashCode(), @class);
    }

    /// <summary>
    /// Disposes of all resources used by the environment.
    ///
    /// This includes all objects and their associated package's archive (this also includes disposing of the archive stream)
    /// </summary>
    public void Dispose()
    {
        // FIXME: Potential memory leak if a package's objects have been removed already.
        foreach (var obj in _ObjectContainer.Enumerate().OfType<UPackage>())
        {
            // Dispose of all archives (which may have an associated stream).
            if (obj.Outer == null && (UnrealPackage?)obj.Package != null) // Can be null if the object was created manually.
            {
                obj.Package.Archive.Dispose();
            }
        }

        _ObjectContainer.Dispose();
    }

    /// <summary>
    /// Disposes of all objects in this environment that belong to a package.
    ///
    /// This does not dispose of the package.
    /// </summary>
    /// <param name="package">The package with the objects to dispose of.</param>
    public void DisposeObjects(UnrealPackage package)
    {
        _ObjectContainer.Dispose(package);
    }
}
