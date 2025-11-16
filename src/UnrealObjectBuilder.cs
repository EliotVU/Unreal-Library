using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using UELib.Core;
using UELib.Flags;

namespace UELib;

public static class UnrealPackageBuilder
{
    public static PackageBuilder Operate(UnrealPackageLinker linker)
    {
        return new PackageBuilder(linker);
    }

    public static PackageBuilder Builder(this UnrealPackageLinker linker)
    {
        return Operate(linker);
    }
}

// VERY basic builder for UnrealPackage.
// We'll add to it as the samples develop.
public sealed class PackageBuilder(UnrealPackageLinker linker)
{
    public UnrealPackage Get()
    {
        return linker.Package;
    }

    public PackageBuilder AddName(UName name)
    {
        if (linker.Package.Archive.NameIndices.ContainsKey(name.Index))
        {
            return this;
        }

        linker.Package.Names.Add(new UNameTableItem(name));
        linker.Package.Archive.NameIndices[name.Index] = linker.Package.Names.Count - 1;

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
        Contract.Assert(((UPackageIndex)obj).IsNull, "Cannot add an object that has already been indexed.");

        if (obj.Package == linker.Package)
        {
            Contract.Assert(
                obj.EnumerateOuter().Last(outer => outer == linker.Package.RootPackage) != null,
                "Object must be part of the package's hierarchy."
            );

            var resource = new UExportTableItem(obj)
            {
                Index = linker.Package.Exports.Count,
                Package = linker.Package,
                Object = obj,
            };
            linker.Package.Exports.Add(resource);
            obj.PackageIndex = new UPackageIndex(linker.Package.Exports.Count);
            obj.PackageResource = resource;
            Debug.Assert(linker.IndexToObject<UObject>(obj.PackageIndex) == obj);

            // TODO: Figure this out.
            linker.Package.Dependencies.Add([]);

        }
        else
        {
            // TODO: Import all relevant resources as well!

            var resource = new UImportTableItem(obj)
            {
                Index = linker.Package.Imports.Count,
                Package = linker.Package,
                Object = obj,
            };
            linker.Package.Imports.Add(resource);
            obj.PackageIndex = new UPackageIndex(-linker.Package.Imports.Count);
            obj.PackageResource = resource;
            Debug.Assert(linker.IndexToObject<UObject>(obj.PackageIndex) == obj);
        }

        return this;
    }
}

public static class UnrealObjectBuilder
{
    public static UEnumBuilder CreateEnum(UnrealPackageLinker linker)
    {
        var objectClass = linker.GetStaticClass(UnrealName.Enum);
        return new UEnumBuilder(new UEnum
        {
            Package = linker.Package,
            PackageIndex = UPackageIndex.Null,
            Class = objectClass,
            Outer = linker.Package.RootPackage
        });
    }

    public static UEnumBuilder Operate(UEnum obj)
    {
        return new UEnumBuilder(obj);
    }

    public static UStructBuilder CreateStruct(UnrealPackageLinker linker)
    {
        var objectClass = linker.FindObject<UClass>(UnrealName.ScriptStruct) ?? linker.GetStaticClass(UnrealName.Struct);
        if (objectClass.Name == UnrealName.ScriptStruct)
        {
            return new UStructBuilder(new UScriptStruct
            {
                Package = linker.Package,
                PackageIndex = UPackageIndex.Null,
                Class = objectClass,
                Outer = linker.Package.RootPackage
            });
        }

        return new UStructBuilder(new UStruct
        {
            Package = linker.Package,
            PackageIndex = UPackageIndex.Null,
            Class = objectClass,
            Outer = linker.Package.RootPackage
        });
    }

    public static UStructBuilder Operate(UStruct obj)
    {
        return new UStructBuilder(obj);
    }

    public static UObjectBuilder CreatePackage(UnrealPackageLinker linker)
    {
        var objectClass = linker.GetStaticClass(UnrealName.Package);
        return new UObjectBuilder(new UPackage
        {
            Package = linker.Package,
            PackageIndex = UPackageIndex.Null,
            Class = objectClass,
            Outer = linker.Package.RootPackage
        });
    }

    public static UObjectBuilder Operate(UPackage obj)
    {
        return new UObjectBuilder(obj);
    }

    public static UFunctionBuilder CreateFunction(UnrealPackageLinker linker)
    {
        var objectClass = linker.GetStaticClass(UnrealName.Function);
        return new UFunctionBuilder(new UFunction
        {
            Package = linker.Package,
            PackageIndex = UPackageIndex.Null,
            Class = objectClass,
            Outer = linker.Package.RootPackage
        });
    }

    public static UFunctionBuilder Operate(UFunction obj)
    {
        return new UFunctionBuilder(obj);
    }

    public static UStateBuilder CreateState(UnrealPackageLinker linker)
    {
        var objectClass = linker.GetStaticClass(UnrealName.State);
        return new UStateBuilder(new UState
        {
            Package = linker.Package,
            PackageIndex = UPackageIndex.Null,
            Class = objectClass,
            Outer = linker.Package.RootPackage
        });
    }

    public static UStateBuilder Operate(UState obj)
    {
        return new UStateBuilder(obj);
    }

    public static UClassBuilder CreateClass(UnrealPackageLinker linker)
    {
        var objectClass = linker.GetStaticClass(UnrealName.Class);
        return new UClassBuilder(new UClass
        {
            Package = linker.Package,
            PackageIndex = UPackageIndex.Null,
            Class = objectClass,
            Outer = linker.Package.RootPackage
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
    public TBuilder FriendlyName(UName name)
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
        obj.Package.Linker.PackageEnvironment.ObjectContainer.Add(obj);

        return obj;
    }

    public TObject Build(out TObject outObj)
    {
        outObj = Build();

        return outObj;
    }

    public TBuilder Name(UName name)
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
