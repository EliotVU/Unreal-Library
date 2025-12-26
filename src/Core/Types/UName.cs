using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Hashing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace UELib.Core;

/// <summary>
///     Implements FName
///
///     A reference to a unique name and number in an Unreal package.
///     The reference itself does not store any text, but instead, registers and retrieves it from the global <see cref="IndexNameMap"/>
/// </summary>
[DebuggerDisplay("{Text}")]
[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 8)]
public readonly struct UName : IEquatable<UName>
{
    private const int NoNumber = 0;

    /// <summary>
    ///     The unique hash index in the <see cref="IndexNameMap"/>
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
    ///     Creates and registers a new name reference from a string and an optional number.
    /// </summary>
    public UName(string text, int number = NoNumber)
    {
        Index = IndexName.FromText(text).Index;
        Number = number;
    }

    /// <summary>
    ///     Creates and registers a new name reference from a string, a number, and fixed index.
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
        return this == UnrealName.None;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(UName other)
    {
        return Index == other.Index && Number == other.Number;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(UName a, UName b)
    {
        return Unsafe.As<UName, ulong>(ref a) == Unsafe.As<UName, ulong>(ref b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(UName a, UName b)
    {
        return Unsafe.As<UName, ulong>(ref a) != Unsafe.As<UName, ulong>(ref b);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(UName a, string text)
    {
        return string.Equals(a, text, StringComparison.OrdinalIgnoreCase);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(UName a, string text)
    {
        return !string.Equals(a, text, StringComparison.OrdinalIgnoreCase);
    }

    public static bool operator ==(UName a, int index)
    {
        return a.Index == index;
    }

    public static bool operator !=(UName a, int index)
    {
        return a.Index != index;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator string(UName name)
    {
        return name.ToString();
    }

    // No longer compatible, because UName no longer preserves the package index, but instead uses a global hash map.
    [Obsolete("Deprecated", true)]
    public static explicit operator int(UName name)
    {
        throw new NotImplementedException();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator bool(UName name)
    {
        return name.IsNone() == false;
    }

    public override bool Equals(object? obj)
    {
        if (obj is UName other)
        {
            return Equals(other);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return Index ^ Number;
    }

    public override string ToString()
    {
        return Number == NoNumber
            ? Text
            : $"{Text}_{Number - 1}";
    }

    [Obsolete("Use Text instead")]
    public string Name => Text;
}

internal static class IndexNameMap
{
    private static readonly IndexName s_noneIndex = new("None", 0);

    private static readonly ConcurrentDictionary<int, IndexName> s_byIndex = new(
        new Dictionary<int, IndexName>(4096)
        {
            // Ensure that non-initialized UName are always "None" with index 0.
            { 0, s_noneIndex },
            // Map the actual hash to the hardcoded 0 index.
            { IndexName.ToIndex("None"), s_noneIndex },
        }
    );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static IndexName GetByIndex(int index)
    {
        return s_byIndex[index];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Has(int index)
    {
        return s_byIndex.ContainsKey(index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Add(IndexName item)
    {
        s_byIndex.TryAdd(item.Index, item);
    }
}

[StructLayout(LayoutKind.Auto, CharSet = CharSet.Unicode)]
public sealed class IndexName(string text, int index)
{
    public int Index { get; } = index;
    public string Text { get; } = text;

    internal static int ToIndex(string text)
    {
        Span<byte> dest = stackalloc byte[4];
        Crc32.Hash(UnrealEncoding.ANSI.GetBytes(text), dest);
        int checksum = BitConverter.ToInt32(dest.ToArray(), 0);
        return checksum;
#if false
        return StringComparer.OrdinalIgnoreCase.GetHashCode(text);
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
}

public static class UnrealName
{
    public static readonly UName None = new(0); // See the IndexNameMap for the "None" entry.
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
    public static readonly UName Commandlet = new("Commandlet");
    public static readonly UName ObjectRedirector = new("ObjectRedirector");
    public static readonly UName OrderIndex = new("OrderIndex");
    public static readonly UName Tooltip = new("Tooltip");

    public static readonly UName Core = new("Core");
    public static readonly UName Engine = new("Engine");

    public static readonly UName System = new("System");
}
