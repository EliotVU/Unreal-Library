﻿using System;

namespace UELib.Core
{
    /// <summary>
    /// Implements FName. A data type that represents a string, usually acquired from a names table.
    /// </summary>
    public class UName : IComparable<UName>
    {
        private const string    None = "None";
        public const int        Numeric = 0;

        private UNameTableItem  _NameItem;

        /// <summary>
        /// Represents the number in a name, e.g. "Component_1"
        /// </summary>
        private readonly int    _Number;

        public int              Number => _Number;
        public string           Name => _NameItem.Name;
        private string          Text => _Number > Numeric ? $"{_NameItem.Name}_{_Number}" : _NameItem.Name;
        internal int            Index => _NameItem.Index;
        public int              Length => Text.Length;

        [Obsolete]
        public UName(IUnrealStream stream)
        {
            int index = stream.ReadNameIndex(out _Number);
            _NameItem = stream.Package.Names[index];
        }

        public UName(UNameTableItem nameItem, int number = Numeric)
{
            _NameItem = nameItem;
            _Number = number;
        }

        // FIXME: Ugly hack to create an UName from a raw string.
        public UName(string text)
        {
            var nameEntry = new UNameTableItem
            {
                Name = text
            };
            _NameItem = nameEntry;
            _Number = Numeric;
        }

        // FIXME: Ugly hack for writing purposes
        public UName(int index, int number = Numeric)
        {
            var nameEntry = new UNameTableItem
            {
                Index = index
            };
            _NameItem = nameEntry;
            _Number = Numeric;
        }

        public bool IsNone()
        {
            return _NameItem.Name.Equals(None, StringComparison.OrdinalIgnoreCase);
        }

        public int CompareTo(UName other) => string.Compare(other.Text, Text, StringComparison.OrdinalIgnoreCase);

        public override string ToString()
        {
            return Text;
        }

        public override int GetHashCode()
        {
            return Index ^ _Number;
        }

        public static bool operator ==(UName a, UName b)
        {
            if (b is null)
            {
                return Equals(a, null);
            }

            if (a is null)
            {
                return false;
            }
            
            return a.Index == b.Index && a._Number == b._Number;
        }

        public static bool operator !=(UName a, UName b)
        {
            return !(a == b);
        }

        public static bool operator ==(UName a, string b)
        {
            return string.Equals(a, b);
        }

        public static bool operator !=(UName a, string b)
        {
            return !string.Equals(a, b);
        }

        public static implicit operator string(UName a)
        {
            return a?.Text;
        }

        public static explicit operator int(UName a)
        {
            return a.Index;
        }
    }
}
