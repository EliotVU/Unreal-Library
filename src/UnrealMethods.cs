using System;

namespace UELib
{
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
        public DeserializationException()
        {
        }

        public DeserializationException(string message) : base(message)
        {
        }
    }

    [Serializable]
    public class DecompilationException : UnrealException
    {
        public DecompilationException()
        {
        }

        public DecompilationException(string message) : base(message)
        {
        }
    }

    [Obsolete]
    public class DeserializingObjectsException : UnrealException
    {
    }

    [Obsolete]
    public class ImportingObjectsException : UnrealException
    {
    }

    [Obsolete]
    public class LinkingObjectsException : UnrealException
    {
    }
}