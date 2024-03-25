using System;
using System.Collections.Generic;
using System.Diagnostics;
using UELib.Annotations;
using UELib.Core.Tokens;
using UELib.Decoding;
using UELib.Tokens;

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

        // TODO: Re-factor this as a factory where we can retrieve the correct type-specific serializer.
        public IPackageSerializer Serializer;
        [CanBeNull] private TokenFactory _TokenFactory;

        /// <summary>
        /// Which flag enums do we need to map?
        /// See <see cref="DefaultEngineBranch"/> for an implementation.
        /// This field is essential to <seealso cref="UnrealStreamImplementations.ReadFlags32"/>
        /// </summary>
        public readonly Dictionary<Type, ulong[]> EnumFlagsMap = new Dictionary<Type, ulong[]>();

        protected readonly ulong[] PackageFlags = new ulong[(int)Flags.PackageFlag.Max];

        public EngineBranch()
        {
            Generation = BuildGeneration.Undefined;
        }

        public EngineBranch(BuildGeneration generation)
        {
            Generation = generation;
        }

        protected void SetupSerializer<T>()
            where T : IPackageSerializer
        {
            Debug.Assert(Serializer == null, $"{nameof(Serializer)} is already setup");
            Serializer = Activator.CreateInstance<T>();
        }

        protected void SetupTokenFactory<T>(
            TokenMap tokenMap,
            Dictionary<ushort, NativeTableItem> nativeTokenMap,
            byte extendedNative,
            byte firstNative)
            where T : TokenFactory
        {
            Debug.Assert(_TokenFactory == null, $"{nameof(_TokenFactory)} is already setup");
            _TokenFactory =
                (T)Activator.CreateInstance(typeof(T), tokenMap, nativeTokenMap, extendedNative, firstNative);
        }

        /// <summary>
        /// Called right after the EngineBranch has been constructed.
        /// </summary>
        public abstract void Setup(UnrealPackage linker);

        /// <summary>
        /// Provides an opportunity to swap the serializer instance based on any linker's condition.
        /// </summary>
        protected abstract void SetupSerializer(UnrealPackage linker);

        /// <summary>
        /// Provides an opportunity to swap the token factory instance based on any linker's condition.
        /// </summary>
        protected virtual void SetupTokenFactory(UnrealPackage linker)
        {
            SetupTokenFactory<TokenFactory>(
                BuildTokenMap(linker), 
                TokenFactory.FromPackage(linker.NTLPackage),
                (byte)ExprToken.ExtendedNative, 
                (byte)ExprToken.FirstNative);
        }

        [NotNull]
        public TokenFactory GetTokenFactory(UnrealPackage linker)
        {
            if (_TokenFactory != null) return _TokenFactory;
            SetupTokenFactory(linker);
            // Sanity check for derived branches
            Debug.Assert(_TokenFactory != null, "Branch.TokenFactory cannot be null");
            return _TokenFactory;
        }

        protected virtual TokenMap BuildTokenMap(UnrealPackage linker)
        {
            return new TokenMap();
        }

        /// <summary>
        /// Called right after the <see cref="UnrealPackage.PackageFileSummary"/> has been deserialized.
        /// </summary>
        /// <param name="linker"></param>
        /// <param name="stream">The open stream that deserialized the summary.</param>
        /// <param name="summary">A reference to the deserialized summary.</param>
        public virtual void PostDeserializeSummary(UnrealPackage linker,
            IUnrealStream stream,
            ref UnrealPackage.PackageFileSummary summary)
        {
            SetupSerializer(linker);
            stream.Serializer = Serializer;
        }

        /// <summary>
        /// Called right after the package's tables (Names, Imports, and Exports, etc) have been deserialized.
        /// </summary>
        /// <param name="linker"></param>
        /// <param name="stream">The open stream that deserialized the package's summary and tables.</param>
        public virtual void PostDeserializePackage(UnrealPackage linker, IUnrealStream stream)
        {
        }
    }
}