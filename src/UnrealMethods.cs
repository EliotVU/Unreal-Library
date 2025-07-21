using System;
using System.Collections.Generic;
using System.Linq;

namespace UELib
{
    #region Exceptions

    [Serializable]
    public class UnrealException : Exception
    {
        protected UnrealException()
        {
        }

        public UnrealException(string message) : base(message)
        {
        }

        public UnrealException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    [Serializable]
    public class DeserializationException : UnrealException
    {
        public DeserializationException(string message) : base(message)
        {
        }

        public DeserializationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    [Serializable]
    public class SerializationException : UnrealException
    {
        public SerializationException(string message) : base(message)
        {
        }

        public SerializationException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }

    [Obsolete]
    public class DecompilingCastException : UnrealException
    {
    }

    [Obsolete]
    public class DecompilingHeaderException : UnrealException
    {
        [NonSerialized] private readonly string _Output;

        public DecompilingHeaderException()
        {
            _Output = "DecompilingHeaderException";
        }

        public DecompilingHeaderException(string output)
        {
            _Output = output;
        }

        public override string ToString()
        {
            return _Output + "\r\n" + base.ToString();
        }
    }

    [Obsolete]
    public class DeserializingObjectsException : UnrealException
    {
        public DeserializingObjectsException() : base("deserializing objects")
        {
        }
    }

    [Obsolete]
    public class ImportingObjectsException : UnrealException
    {
        public ImportingObjectsException() : base("importing objects")
        {
        }
    }

    [Obsolete]
    public class LinkingObjectsException : UnrealException
    {
        public LinkingObjectsException() : base("linking objects")
        {
        }
    }

    #endregion

    #region Static Methods

    /// <summary>
    /// Provides static methods for formating flags.
    /// </summary>
    [Obsolete("Use .ToString() on the target enum instead.")]
    public static class UnrealMethods
    {
        public static string FlagsListToString(List<string> flagsList)
        {
            var output = string.Empty;
            foreach (string s in flagsList)
            {
                output += s + (s != flagsList.Last() ? "\n" : string.Empty);
            }

            return output;
        }

        public static List<string> FlagsToList(Type flagEnum, uint flagsDWORD)
        {
            var flagsList = new List<string>();
            var flagValues = Enum.GetValues(flagEnum);
            foreach (uint flag in flagValues)
            {
                if ((flagsDWORD & flag) != flag)
                    continue;

                string eName = Enum.GetName(flagEnum, flag);
                if (flagsList.Contains(eName))
                    continue;

                flagsList.Add($"0x{flag:X8}:{eName}");
            }

            return flagsList;
        }

        public static List<string> FlagsToList(Type flagEnum, ulong flagsDWORD)
        {
            var flagsList = new List<string>();
            var flagValues = Enum.GetValues(flagEnum);
            foreach (ulong flag in flagValues)
            {
                if ((flagsDWORD & flag) != flag)
                    continue;

                string eName = Enum.GetName(flagEnum, flag);
                if (flagsList.Contains(eName))
                    continue;

                flagsList.Add($"0x{flag:X8}:{eName}");
            }

            return flagsList;
        }

        public static List<string> FlagsToList(Type flagEnum, Type flagEnum2, ulong flagsQWORD)
        {
            var list = FlagsToList(flagEnum, flagsQWORD);
            list.AddRange(FlagsToList(flagEnum2, flagsQWORD >> 32));
            return list;
        }

        public static string FlagToString(uint flags)
        {
            return $"0x{$"{flags:X4}".PadLeft(8, '0')}";
        }

        public static string FlagToString(ulong flags)
        {
            return $"{FlagToString((uint)(flags >> 32))}-{FlagToString((uint)flags)}";
        }
    }

    #endregion
}
