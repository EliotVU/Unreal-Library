using System.Diagnostics;
using System.Diagnostics.Contracts;
using UELib.Core;
using UELib.Flags;

namespace UELib;

public static class UnrealPackageBuilder
{
    public static PackageBuilder Operate(UnrealPackage package)
    {
        return new PackageBuilder(package);
    }

    public static PackageBuilder Builder(this UnrealPackage package)
    {
        return Operate(package);
    }
}

// VERY basic builder for UnrealPackage.
// We'll add to it as the samples develop.
public sealed class PackageBuilder(UnrealPackage package)
{
    public UnrealPackage Get()
    {
        return package;
    }

    public PackageBuilder AddName(in UName name)
    {
        if (package.Archive.NameIndices.ContainsKey(name.Index))
        {
            return this;
        }

        package.Names.Add(new UNameTableItem(name));
        package.Archive.NameIndices[name.Index] = package.Names.Count - 1;

        return this;
    }

    public PackageBuilder AddResources(params UObject[] objects)
    {
        foreach (var obj in objects)
        {
            AddResource(obj);
        }

        return this;
    }

    public PackageBuilder AddResource(UObject obj)
    {
        Contract.Assert(obj.PackageIndex.IsNull, "Cannot add an object that has already been indexed.");
        Contract.Assert(
            obj.EnumerateOuter().Last(outer => outer == obj.Package.RootPackage) != null,
            "Object must be part of the package's hierarchy."
        );

        if (obj.Package == package)
        {
            var resource = new UExportTableItem(obj)
            {
                Index = package.Exports.Count,
                Package = package,
                Object = obj,
            };
            package.Exports.Add(resource);
            obj.PackageIndex = new UPackageIndex(package.Exports.Count);
            obj.PackageResource = resource;
            Debug.Assert(package.Linker.IndexToObject<UObject>(obj.PackageIndex) == obj);

            // TODO: Figure this out.
            package.Dependencies.Add([]);

        }
        else
        {
            // TODO: Import all relevant resources as well!

            var resource = new UImportTableItem(obj)
            {
                Index = package.Imports.Count,
                Package = package,
                Object = obj,
            };
            package.Imports.Add(resource);
            obj.PackageIndex = new UPackageIndex(-package.Imports.Count);
            obj.PackageResource = resource;
            Debug.Assert(package.Linker.IndexToObject<UObject>(obj.PackageIndex) == obj);
        }

        return this;
    }
}

public static class UnrealObjectBuilder
{
    public static UEnumBuilder CreateEnum(UnrealPackage package)
    {
        var objectClass = package.Environment.GetStaticClass(UnrealName.Enum);
        return new UEnumBuilder(new UEnum
        {
            Package = package,
            PackageIndex = UPackageIndex.Null,
            Class = objectClass,
            Outer = package.RootPackage
        });
    }

    public static UEnumBuilder Operate(UEnum obj)
    {
        return new UEnumBuilder(obj);
    }

    public static UStructBuilder CreateStruct(UnrealPackage package)
    {
        var objectClass = package.Environment.FindObject<UClass?>(UnrealName.ScriptStruct)
                       ?? package.Environment.GetStaticClass(UnrealName.Struct);
        if (objectClass.Name == UnrealName.ScriptStruct)
        {
            return new UStructBuilder(new UScriptStruct
            {
                Package = package,
                PackageIndex = UPackageIndex.Null,
                Class = objectClass,
                Outer = package.RootPackage
            });
        }

        return new UStructBuilder(new UStruct
        {
            Package = package,
            PackageIndex = UPackageIndex.Null,
            Class = objectClass,
            Outer = package.RootPackage
        });
    }

    public static UStructBuilder Operate(UStruct obj)
    {
        return new UStructBuilder(obj);
    }

    public static UObjectBuilder CreatePackage(UnrealPackage package)
    {
        var objectClass = package.Environment.GetStaticClass(UnrealName.Package);
        return new UObjectBuilder(new UPackage
        {
            Package = package,
            PackageIndex = UPackageIndex.Null,
            Class = objectClass,
            Outer = package.RootPackage
        });
    }

    public static UObjectBuilder Operate(UPackage obj)
    {
        return new UObjectBuilder(obj);
    }

    public static UFunctionBuilder CreateFunction(UnrealPackage package)
    {
        var objectClass = package.Environment.GetStaticClass(UnrealName.Function);
        return new UFunctionBuilder(new UFunction
        {
            Package = package,
            PackageIndex = UPackageIndex.Null,
            Class = objectClass,
            Outer = package.RootPackage
        });
    }

    public static UFunctionBuilder Operate(UFunction obj)
    {
        return new UFunctionBuilder(obj);
    }

    public static UStateBuilder CreateState(UnrealPackage package)
    {
        var objectClass = package.Environment.GetStaticClass(UnrealName.State);
        return new UStateBuilder(new UState
        {
            Package = package,
            PackageIndex = UPackageIndex.Null,
            Class = objectClass,
            Outer = package.RootPackage
        });
    }

    public static UStateBuilder Operate(UState obj)
    {
        return new UStateBuilder(obj);
    }

    public static UClassBuilder CreateClass(UnrealPackage package)
    {
        var objectClass = package.Environment.GetStaticClass(UnrealName.Class);
        return new UClassBuilder(new UClass
        {
            Package = package,
            PackageIndex = UPackageIndex.Null,
            Class = objectClass,
            Outer = package.RootPackage
        });
    }

    public static UClassBuilder Operate(UClass obj)
    {
        return new UClassBuilder(obj);
    }
}

public abstract class UFieldBuilder<TObject, TBuilder>(TObject obj) : UObjectBuilder<TObject, TBuilder>(obj)
    where TObject : UField
    where TBuilder : UFieldBuilder<TObject, TBuilder>
{
    public TBuilder NextField(UField field)
    {
        obj.NextField = field;

        return (TBuilder)this;
    }
}

public abstract class UEnumBuilder<TObject, TBuilder>(TObject obj) : UFieldBuilder<TObject, TBuilder>(obj)
    where TObject : UEnum
    where TBuilder : UEnumBuilder<TObject, TBuilder>
{
}

public sealed class UEnumBuilder(UEnum obj) : UEnumBuilder<UEnum, UEnumBuilder>(obj);

public abstract class UStructBuilder<TObject, TBuilder>(TObject obj) : UFieldBuilder<TObject, TBuilder>(obj)
    where TObject : UStruct
    where TBuilder : UStructBuilder<TObject, TBuilder>
{
    public TBuilder AddField(UStruct field)
    {
        obj.AddField(field);

        return (TBuilder)this;
    }

    public TBuilder RemoveField(UStruct field)
    {
        obj.RemoveField(field);

        return (TBuilder)this;
    }

    public TBuilder StructFlags(params StructFlag[] flagIndices)
    {
        obj.StructFlags = new UnrealFlags<StructFlag>(
            obj.Package.Branch.EnumFlagsMap[typeof(StructFlag)],
            flagIndices);

        return (TBuilder)this;
    }

    public TBuilder WithStatements(params UStruct.UByteCodeDecompiler.Token[] statements)
    {
        Contract.Assert(statements.Last() is UStruct.UByteCodeDecompiler.EndOfScriptToken);

        var script = new UByteCodeScript(obj, statements.ToList());
        obj.Script = script;

        return (TBuilder)this;
    }
}

public sealed class UStructBuilder(UStruct obj) : UStructBuilder<UStruct, UStructBuilder>(obj);

public abstract class UFunctionBuilder<TObject, TBuilder>(TObject obj) : UStructBuilder<TObject, TBuilder>(obj)
    where TObject : UFunction
    where TBuilder : UFunctionBuilder<TObject, TBuilder>
{
    public TBuilder FriendlyName(in UName name)
    {
        if (obj.IsOperator())
        {
            var symbolMap = new Dictionary<char, string>
            {
                { ' ', "Spc" },
                { '!', "Not" },
                { '"', "DoubleQuote" },
                { '#', "Pound" },
                { '%', "Percent" },
                { '&', "And" },
                { '\'', "SingleQuote" },
                { '(', "OpenParen" },
                { ')', "CloseParen" },
                { '*', "Multiply" },
                { '+', "Add" },
                { ',', "Comma" },
                { '-', "Subtract" },
                { '.', "Dot" },
                { '/', "Divide" },
                { ':', "Colon" },
                { ';', "Semicolon" },
                { '<', "Less" },
                { '=', "Equal" },
                { '>', "Greater" },
                { '?', "Question" },
                { '@', "At" },
                { '[', "OpenBracket" },
                { '\\', "Backslash" },
                { ']', "CloseBracket" },
                { '^', "Xor" },
                { '_', "_" },
                { '{', "OpenBrace" },
                { '|', "Or" },
                { '}', "CloseBrace" },
                { '~', "Complement" }
            };

            string signature = "";
            foreach (var c in name.ToString())
            {
                if (symbolMap.TryGetValue(c, out string newString))
                {
                    signature += newString;

                    continue;
                }

                signature += c;
            }

            signature += "_";
            if (obj.IsPre())
            {
                signature += "Pre";
            }

            foreach (var param in obj
                .EnumerateFields<UProperty>()
                .Where(property => property.IsParm() && !property.HasPropertyFlag(PropertyFlag.ReturnParm)))
            {
                signature += (param.Type) switch
                {
                    Types.PropertyType.ObjectProperty => ((UObjectProperty)param).Object.Name,
                    Types.PropertyType.ClassProperty => ((UObjectProperty)param).Class.Name,
                    Types.PropertyType.InterfaceProperty => ((UInterfaceProperty)param).InterfaceClass.Name,
                    Types.PropertyType.StructProperty => ((UStructProperty)param).Struct.Name,
                    _ => param.Name.ToString().Replace("Property", "")
                };
            }
        }
        else
        {
            obj.Name = name;
        }

        obj.FriendlyName = name;

        return (TBuilder)this;
    }

    public TBuilder FunctionFlags(params FunctionFlag[] flagIndices)
    {
        obj.FunctionFlags = new UnrealFlags<FunctionFlag>(
            obj.Package.Branch.EnumFlagsMap[typeof(FunctionFlag)],
            flagIndices);

        return (TBuilder)this;
    }
}

public sealed class UFunctionBuilder(UFunction obj) : UFunctionBuilder<UFunction, UFunctionBuilder>(obj);

public abstract class UStateBuilder<TObject, TBuilder>(TObject obj) : UStructBuilder<TObject, TBuilder>(obj)
    where TObject : UState
    where TBuilder : UStateBuilder<TObject, TBuilder>
{
    public TBuilder StateFlags(params StateFlag[] flagIndices)
    {
        obj.StateFlags = new UnrealFlags<StateFlag>(
            obj.Package.Branch.EnumFlagsMap[typeof(StateFlag)],
            flagIndices);

        return (TBuilder)this;
    }
}

public sealed class UStateBuilder(UState obj) : UStateBuilder<UState, UStateBuilder>(obj);

public abstract class UClassBuilder<TObject, TBuilder>(TObject obj) : UStateBuilder<TObject, TBuilder>(obj)
    where TObject : UClass
    where TBuilder : UClassBuilder<TObject, TBuilder>
{
    public TBuilder ClassFlags(params ClassFlag[] flagIndices)
    {
        obj.ClassFlags = new UnrealFlags<ClassFlag>(
            obj.Package.Branch.EnumFlagsMap[typeof(ClassFlag)],
            flagIndices);

        return (TBuilder)this;
    }
}

public sealed class UClassBuilder(UClass obj) : UClassBuilder<UClass, UClassBuilder>(obj);

public abstract class UObjectBuilder<TObject, TBuilder>(TObject obj)
    where TObject : UObject
    where TBuilder : UObjectBuilder<TObject, TBuilder>
{
    public TObject Build()
    {
        obj.Package.Environment.AddObject(obj);

        return obj;
    }

    public TObject Build(out TObject outObj)
    {
        outObj = Build();

        return outObj;
    }

    public TBuilder Name(in UName name)
    {
        obj.Name = name;

        if (obj is UStruct uStruct)
        {
            uStruct.FriendlyName = name; // Ensure that it is initialized, even when unwanted.
        }

        return (TBuilder)this;
    }

    public TBuilder ObjectFlags(params ObjectFlag[] flagIndices)
    {
        obj.ObjectFlags = new UnrealFlags<ObjectFlag>(
            obj.Package.Branch.EnumFlagsMap[typeof(ObjectFlag)],
            flagIndices);

        return (TBuilder)this;
    }

    public TBuilder Class(UClass @class)
    {
        obj.Class = @class;

        return (TBuilder)this;
    }

    public TBuilder Outer(UObject? outer)
    {
        obj.Outer = outer;

        return (TBuilder)this;
    }

    public TBuilder Archetype(UObject? archetype)
    {
        obj.Archetype = archetype;

        return (TBuilder)this;
    }
}

public sealed class UObjectBuilder(UObject obj) : UObjectBuilder<UObject, UObjectBuilder>(obj);
