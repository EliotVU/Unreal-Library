using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace UELib.Core;

/// <summary>
///     Implements FName
///
///     A reference to a unique name and number in an Unreal package.
///     The reference itself does not store any text, but instead, registers and retrieves it from a global name map.
/// </summary>
[DebuggerDisplay("{Text}")]
public readonly struct UName : IEquatable<UName>
{
    public const int NoNumber = -1;

    /// <summary>
    ///     The unique index (hash) in the global NamesMap.
    /// </summary>
    public readonly int Index;

    /// <summary>
    ///     The unique number of the name, e.g. "Component_1"
    /// </summary>
    public readonly int Number;

    /// <summary>
    ///     Creates a new name reference from a package name entry.
    /// </summary>
    public UName(UNameTableItem item, int number = NoNumber) : this(item.IndexName, number)
    {
        Debug.Assert(IndexNameMap.Has(Index), "Attempted to construct a name reference to an unregistered name index.");
    }

    /// <summary>
    ///     Creates and registers a new name reference from text string and an optional number.
    /// </summary>
    public UName(string text, int number = NoNumber)
    {
        Index = IndexName.FromText(text).Index;
        Number = number;
    }

    /// <summary>
    ///     Creates and registers a new name reference from text string, a number, and fixed index.
    /// </summary>
    public UName(string text, int number, int index)
    {
        Index = IndexName.FromText(text, index).Index;
        Number = number;
    }

    /// <summary>
    ///     Creates a new name reference from a unique indexed name entry.
    /// </summary>
    public UName(IndexName item, int number = NoNumber)
    {
        Index = item.Index;
        Number = number;

        Debug.Assert(IndexNameMap.Has(Index), "Attempted to construct a name reference to an unregistered name index.");
    }

    /// <summary>
    ///     Creates a new name reference from an index and an optional number.
    /// </summary>
    public UName(int index, int number = NoNumber)
    {
        Index = index;
        Number = number;

        Debug.Assert(IndexNameMap.Has(Index), "Attempted to construct a name reference to an unregistered name index.");
    }

    public string Text => IndexNameMap.GetByIndex(Index).Text;
    public int Length => Text.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNone()
    {
        return Equals(UniqueName.None);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(UName other)
    {
        return Index == other.Index && Number == other.Number;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(UName a, UName b)
    {
        return a.Index == b.Index && a.Number == b.Number;
    }

    public static bool operator ==(UName a, UName? b)
    {
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(UName a, UName b)
    {
        return a.Index != b.Index || a.Number != b.Number;
    }

    public static bool operator !=(UName a, UName? b)
    {
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(UName a, string text)
    {
        return string.Equals(a, text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(UName a, string text)
    {
        return !string.Equals(a, text);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(UName a, int index)
    {
        return a.Index == index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(UName a, int index)
    {
        return a.Index != index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(UName name)
    {
        return name.ToString();
    }

    // FIXME: No longer compatible with uses if (int)NameRef because we no longer return the index into the NamesTable, but instead we return a hash of the name.
    // Therefor comparisons made using this operator will break.
    public static explicit operator int(UName name)
    {
        return name.GetHashCode();
    }

    public override bool Equals(object? obj)
    {
        return base.Equals(obj);
    }

    public override int GetHashCode()
    {
        return Index ^ Number;
    }

    public override string ToString()
    {
        return Number == NoNumber
            ? Text
            : $"{Text}_{Number}";
    }

    [Obsolete("Use Text instead")]
    public string Name => Text;
}

internal static class IndexNameMap
{
    private static readonly Dictionary<int, IndexName> ByIndex = new(1000)
    {
        // Ensure that non-initialized UName are always "None" with index 0.
        { 0, new IndexName("None", IndexName.ToIndex("None")) }
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndexName GetByIndex(int index)
    {
        return ByIndex[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has(int index)
    {
        return ByIndex.ContainsKey(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add(IndexName item)
    {
        ByIndex.Add(item.Index, item);
    }
}

public sealed class IndexName(string text, int index)
{
    public string Text { get; } = text;
    public int Index { get; } = index;

    internal static int ToIndex(string text)
    {
#if NETCOREAPP2_1_OR_GREATER
        return text.GetHashCode(StringComparison.OrdinalIgnoreCase);
#else
        return text.ToLowerInvariant().GetHashCode();
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndexName? FromIndex(int index)
    {
        return IndexNameMap.Has(index) ? IndexNameMap.GetByIndex(index) : null;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndexName FromText(string text)
    {
        int index = ToIndex(text);
        return FromText(text, index);
    }

    public static IndexName FromText(string text, int index)
    {
        if (IndexNameMap.Has(index))
        {
            return IndexNameMap.GetByIndex(index);
        }

        var item = new IndexName(text, index);
        IndexNameMap.Add(item);

        return item;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode()
    {
        return Index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(UName other)
    {
        return Index == other.Index;
    }
}

public static class UniqueName
{
    public static readonly UName None = new("None");
    public static readonly UName Package = new("Package");
    public static readonly UName Object = new("Object");
    public static readonly UName Field = new("Field");
    public static readonly UName Const = new("Const");
    public static readonly UName Enum = new("Enum");
    public static readonly UName Struct = new("Struct");
    public static readonly UName ScriptStruct = new("ScriptStruct");
    public static readonly UName Property = new("Property");
    public static readonly UName ArrayProperty = new("ArrayProperty");
    public static readonly UName FixedArrayProperty = new("FixedArrayProperty");
    public static readonly UName MapProperty = new("MapProperty");
    public static readonly UName StringProperty = new("StringProperty");
    public static readonly UName StrProperty = new("StrProperty");
    public static readonly UName BoolProperty = new("BoolProperty");
    public static readonly UName ByteProperty = new("ByteProperty");
    public static readonly UName IntProperty = new("IntProperty");
    public static readonly UName FloatProperty = new("FloatProperty");
    public static readonly UName NameProperty = new("NameProperty");
    public static readonly UName StructProperty = new("StructProperty");
    public static readonly UName ObjectProperty = new("ObjectProperty");
    public static readonly UName ComponentProperty = new("ComponentProperty");
    public static readonly UName InterfaceProperty = new("InterfaceProperty");
    public static readonly UName ClassProperty = new("ClassProperty");
    public static readonly UName DelegateProperty = new("DelegateProperty");
    public static readonly UName PointerProperty = new("PointerProperty");
    public static readonly UName Function = new("Function");
    public static readonly UName State = new("State");
    public static readonly UName Class = new("Class");
    public static readonly UName Interface = new("Interface");
    public static readonly UName TextBuffer = new("TextBuffer");
    public static readonly UName OrderIndex = new("OrderIndex");
    public static readonly UName Tooltip = new("Tooltip");

    public static readonly UName Core = new("Core");
    public static readonly UName Engine = new("Engine");
}