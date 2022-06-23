using System;
using System.Collections.Generic;
using UELib.Annotations;
using UELib.Decoding;

namespace UELib.Branch
{
    /// <summary>
    /// EngineBranch lets you override the common serialization methods to assist with particular engine branches or generations.
    /// Some games or engine generations may drift away from the main UE branch too much, therefor it is useful to separate specialized logic as much as possible.
    /// 
    /// For UE1, 2, and 3 see <see cref="DefaultEngineBranch"/>.
    /// For UE4 see <see cref="UE4.EngineBranchUE4"/>.
    /// </summary>
    public abstract class EngineBranch
    {
        public readonly BuildGeneration Generation;
        
        [CanBeNull] public IBufferDecoder Decoder;
        public IPackageSerializer Serializer;

        /// <summary>
        /// Which flag enums do we need to map?
        /// See <see cref="DefaultEngineBranch"/> for an implementation.
        /// This field is essential to <seealso cref="UnrealStreamImplementations.ReadFlags32"/>
        /// </summary>
        public readonly Dictionary<Type, ulong[]> EnumFlagsMap = new Dictionary<Type, ulong[]>();

        protected EngineBranch(UnrealPackage package)
        {
            Generation = package.Build.Generation;
        }

        public abstract void PostDeserializeSummary(IUnrealStream stream, ref UnrealPackage.PackageFileSummary summary);
        public abstract void PostDeserializePackage(IUnrealStream stream, UnrealPackage package);

        public override string ToString()
        {
            return base.ToString();
        }
    }
}