using System;
using UELib.Branch.UE2.VG.Tokens;
using UELib.Branch.UE3.BL2.Tokens;
using UELib.Core;
using UELib.Core.Tokens;
using UELib.Flags;
using UELib.Tokens;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace UELib.Branch
{
    /// <summary>
    /// The default EngineBranch handles UE1, 2, and 3 packages, this helps us separate the UE3 or less specific code away
    /// from the <see cref="UE4.EngineBranchUE4" /> implementation.
    /// Note: The current implementation is incomplete, i.e. it does not map any other enum flags other than PackageFlags
    /// yet.
    /// Its main focus is to split UE3 and UE4 logic.
    /// </summary>
    public class DefaultEngineBranch : EngineBranch
    {
        [Flags]
        public enum PackageFlagsDefault : uint
        {
            /// <summary>
            /// UEX: Whether clients are allowed to download the package from the server.
            /// UE4: Displaced by "NewlyCreated"
            /// </summary>
            AllowDownload = 0x00000001U,

            /// <summary>
            /// Whether clients can skip downloading the package but still able to join the server.
            /// </summary>
            ClientOptional = 0x00000002U,

            /// <summary>
            /// Only necessary to load on the server.
            /// </summary>
            ServerSideOnly = 0x00000004U,

            /// <summary>
            /// UE4: Displaced by "CompiledIn"
            /// </summary>
            Unsecure = 0x00000010U,

            Protected = 0x80000000U
        }
#if TRANSFORMERS
        [Flags]
        public enum PackageFlagsHMS : uint
        {
            XmlFormat = 0x80000000U
        }
#endif
        [Flags]
        public enum PackageFlagsUE1 : uint
        {
            /// <summary>
            /// Whether the package has broken links.
            /// Runtime-only;not-serialized
            /// </summary>
            [Obsolete] BrokenLinks = 0x00000008U,

            /// <summary>
            /// Whether the client needs to download the package.
            /// Runtime-only;not-serialized
            /// </summary>
            [Obsolete] Need = 0x00008000U,

            /// <summary>
            /// The package is encrypted.
            /// <= UT
            /// Also attested in file UT2004/Packages.MD5 but it is not encrypted.
            /// </summary>
            Encrypted = 0x00000020U
        }

        [Flags]
        public enum PackageFlagsUE2 : uint
        {
        }

        [Flags]
        public enum PackageFlagsUE3 : uint
        {
            /// <summary>
            /// Whether the package has been cooked.
            /// </summary>
            Cooked = 0x00000008U,

            /// <summary>
            /// Whether the package contains a ULevel or UWorld object.
            /// </summary>
            ContainsMap = 0x00020000U,

            [Obsolete] Trash = 0x00040000,

            /// <summary>
            /// Whether the package contains a UClass or any UnrealScript types.
            /// </summary>
            ContainsScript = 0x00200000U,

            /// <summary>
            /// Whether the package contains debug info i.e. it was built with -Debug.
            /// </summary>
            ContainsDebugData = 0x00400000U,

            Imports = 0x00800000U,

            StoreCompressed = 0x02000000U,
            StoreFullyCompressed = 0x04000000U,

            /// <summary>
            /// Whether package has metadata exported(anything related to the editor).
            /// </summary>
            NoExportsData = 0x20000000U,

            /// <summary>
            /// Whether the package TextBuffers' have been stripped of its content.
            /// </summary>
            StrippedSource = 0x40000000U
        }

        public DefaultEngineBranch(BuildGeneration generation) : base(generation)
        {
        }

        public override void Setup(UnrealPackage linker)
        {
            SetupEnumPackageFlags(linker);
            EnumFlagsMap.Add(typeof(PackageFlags), PackageFlags);
        }

        protected virtual void SetupEnumPackageFlags(UnrealPackage linker)
        {
            PackageFlags[(int)Flags.PackageFlags.AllowDownload] = (uint)PackageFlagsDefault.AllowDownload;
            PackageFlags[(int)Flags.PackageFlags.ClientOptional] = (uint)PackageFlagsDefault.ClientOptional;
            PackageFlags[(int)Flags.PackageFlags.ServerSideOnly] = (uint)PackageFlagsDefault.ServerSideOnly;
#if UE1
            // FIXME: Version
            if (linker.Version > 61 && linker.Version <= 69) // <= UT99
                PackageFlags[(int)Flags.PackageFlags.Encrypted] = (uint)PackageFlagsUE1.Encrypted;
#endif

#if UE3
            // Map the new PackageFlags, but the version is nothing but a guess!
            if (linker.Version >= 180)
            {
                if (linker.Version >= UnrealPackage.PackageFileSummary.VCookerVersion)
                    PackageFlags[(int)Flags.PackageFlags.Cooked] = (uint)PackageFlagsUE3.Cooked;

                PackageFlags[(int)Flags.PackageFlags.ContainsMap] = (uint)PackageFlagsUE3.ContainsMap;
                PackageFlags[(int)Flags.PackageFlags.ContainsDebugData] = (uint)PackageFlagsUE3.ContainsDebugData;
                PackageFlags[(int)Flags.PackageFlags.ContainsScript] = (uint)PackageFlagsUE3.ContainsScript;
                PackageFlags[(int)Flags.PackageFlags.StrippedSource] = (uint)PackageFlagsUE3.StrippedSource;
            }
#endif
        }

        protected override void SetupSerializer(UnrealPackage linker)
        {
            SetupSerializer<DefaultPackageSerializer>();
        }

        /// <summary>
        /// Builds a tokens map for UE1, 2, and 3.
        /// The default byte-codes are correct for UE2 and are adjusted accordingly for UE1, and UE3.
        /// </summary>
        protected override TokenMap BuildTokenMap(UnrealPackage linker)
        {
            var tokenMap = new TokenMap((byte)ExprToken.ExtendedNative)
            {
                { 0x00, typeof(LocalVariableToken) },
                { 0x01, typeof(InstanceVariableToken) },
                { 0x02, typeof(DefaultVariableToken) },
                { 0x03, typeof(BadToken) },
                { 0x04, typeof(ReturnToken) },
                { 0x05, typeof(SwitchToken) },
                { 0x06, typeof(JumpToken) },
                { 0x07, typeof(JumpIfNotToken) },
                { 0x08, typeof(StopToken) },
                { 0x09, typeof(AssertToken) },
                { 0x0A, typeof(CaseToken) },
                { 0x0B, typeof(NothingToken) },
                { 0x0C, typeof(LabelTableToken) },
                { 0x0D, typeof(GotoLabelToken) },
                { 0x0E, typeof(EatReturnValueToken) },
                { 0x0F, typeof(LetToken) },
                
                // +UE2
                { 0x10, typeof(DynamicArrayElementToken) },
                
                // Bad expr in UE1 v61
                { 0x11, linker.Version < 62 
                    ? typeof(BadToken)
                    : typeof(NewToken)
                },
                
                { 0x12, typeof(ClassContextToken) },
                { 0x13, typeof(MetaClassCastToken) },

                {
                    0x14, linker.Version < 62
                        ? typeof(BeginFunctionToken)
                        : typeof(LetBoolToken)
                },

                {
                    0x15, linker.Version < 62
                        ? typeof(EatReturnValueToken)
                        // Attested in UE2 builds such as Unreal2 and Unreal2XMP, but not in any UE1 or UE2.5 builds, nor RS3 (UE2)
                        : linker.Build == BuildGeneration.UE2
                            ? typeof(LineNumberToken)
                            : typeof(BadToken)
                },

                { 0x16, typeof(EndFunctionParmsToken) },
                { 0x17, typeof(SelfToken) },
                { 0x18, typeof(SkipToken) },
                { 0x19, typeof(ContextToken) },
                { 0x1A, typeof(ArrayElementToken) },
                { 0x1B, typeof(VirtualFunctionToken) },
                { 0x1C, typeof(FinalFunctionToken) },
                { 0x1D, typeof(IntConstToken) },
                { 0x1E, typeof(FloatConstToken) },
                { 0x1F, typeof(StringConstToken) },
                { 0x20, typeof(ObjectConstToken) },
                { 0x21, typeof(NameConstToken) },
                { 0x22, typeof(RotationConstToken) },
                { 0x23, typeof(VectorConstToken) },
                { 0x24, typeof(ByteConstToken) },
                { 0x25, typeof(IntZeroToken) },
                { 0x26, typeof(IntOneToken) },
                { 0x27, typeof(TrueToken) },
                { 0x28, typeof(FalseToken) },
                { 0x29, typeof(NativeParameterToken) },
                { 0x2A, typeof(NoObjectToken) },

                {
                    0x2B, linker.Version < (uint)PackageObjectLegacyVersion.CastStringSizeTokenDeprecated
                        ? typeof(ResizeStringToken)
                        : typeof(BadToken)
                },

                { 0x2C, typeof(IntConstByteToken) },
                { 0x2D, typeof(BoolVariableToken) },
                { 0x2E, typeof(DynamicCastToken) },
                { 0x2F, typeof(IteratorToken) },
                { 0x30, typeof(IteratorPopToken) },
                { 0x31, typeof(IteratorNextToken) },
                { 0x32, typeof(StructCmpEqToken) },
                { 0x33, typeof(StructCmpNeToken) },
                
                // No unicode support in UE1 v61
                { 0x34, linker.Version < 62 
                    ? typeof(BadToken)
                    : typeof(UniStringConstToken)
                },
                
                {
                    0x35, linker.Build == BuildGeneration.UE2
                        ? typeof(RangeConstToken)
                        : typeof(BadToken)
                },
                
                { 0x36, typeof(StructMemberToken) },
                
                { 0x37, linker.Version < 62 
                    ? typeof(BadToken)
                    : typeof(DynamicArrayElementToken)
                },
                
                { 0x38, typeof(GlobalFunctionToken) },
                { 0x39, typeof(PrimitiveCastToken) },
                
                {
                    0x3A, linker.Version >= 420
                        ? typeof(DynamicArrayInsertToken)
                        : linker.Version >= (uint)PackageObjectLegacyVersion.PrimitiveCastTokenAdded
                            ? typeof(ReturnNothingToken)
                            : typeof(BadToken) // will be overriden down if UE1
                },

                // Added with UE2 (FIXME: version)
                { 0x3B, typeof(DelegateCmpEqToken) },
                { 0x3C, typeof(DelegateCmpNeToken) },
                { 0x3D, typeof(DelegateFunctionCmpEqToken) },
                { 0x3E, typeof(DelegateFunctionCmpNeToken) },
                { 0x3F, typeof(EmptyDelegateToken) },
                
                { 0x40, typeof(BadToken) },
                { 0x41, typeof(BadToken) },
                { 0x42, typeof(BadToken) },
                { 0x43, typeof(BadToken) },
                { 0x44, typeof(BadToken) },
                { 0x45, typeof(BadToken) },
                { 0x46, typeof(BadToken) },
                { 0x47, typeof(EndOfScriptToken) },
                { 0x48, typeof(ConditionalToken) },
                { 0x49, typeof(BadToken) },
                { 0x4A, typeof(BadToken) },
                { 0x4B, typeof(BadToken) },
                { 0x4C, typeof(BadToken) },
                { 0x4D, typeof(BadToken) },
                { 0x4E, typeof(BadToken) },
                { 0x4F, typeof(BadToken) },
                { 0x50, typeof(BadToken) },
                { 0x51, typeof(BadToken) },
                { 0x52, typeof(BadToken) },
                { 0x53, typeof(BadToken) },
                { 0x54, typeof(BadToken) },
                { 0x55, typeof(BadToken) },
                { 0x56, typeof(BadToken) },
                { 0x57, typeof(BadToken) },
                { 0x58, typeof(BadToken) },
                { 0x59, typeof(BadToken) },
                { 0x5A, typeof(BadToken) },
                { 0x5B, typeof(BadToken) },
                { 0x5C, typeof(BadToken) },
                { 0x5D, typeof(BadToken) },
                { 0x5E, typeof(BadToken) },
                { 0x5F, typeof(BadToken) },
            };
            
            if (linker.Version < (uint)PackageObjectLegacyVersion.PrimitiveCastTokenAdded)
                DowngradePrimitiveCasts(tokenMap);
#if UE3
            // RangeConst was deprecated to add new tokens, and as a result all op codes past it were shifted around.
            if (linker.Version >= (uint)PackageObjectLegacyVersion.RangeConstTokenDeprecated)
                UnshiftTokens3(tokenMap);
#endif
            // ReSharper disable once SwitchStatementMissingSomeEnumCasesNoDefault
            switch (linker.Build.Name)
            {
#if BORDERLANDS2
                case UnrealPackage.GameBuild.BuildName.Borderlands2:
                    tokenMap[0x4C] = typeof(LocalVariableToken<int>);
                    tokenMap[0x4D] = typeof(LocalVariableToken<float>);
                    tokenMap[0x4E] = typeof(LocalVariableToken<byte>);
                    tokenMap[0x4F] = typeof(LocalVariableToken<bool>);
                    tokenMap[0x50] = typeof(LocalVariableToken<UObject>);
                    tokenMap[0x51] = typeof(LocalVariableToken<dynamic>);
                    break;
#endif
#if BIOSHOCK
                case UnrealPackage.GameBuild.BuildName.BioShock:
                    tokenMap[0x49] = typeof(LogFunctionToken);
                    break;
#endif
#if MIRRORSEDGE
                case UnrealPackage.GameBuild.BuildName.MirrorsEdge:
                    tokenMap[0x4F] = typeof(UnresolvedToken);
                    break;
#endif
            }
            
            return tokenMap;
        }

        // TODO: Confirm if these are correct for UE1
        protected void DowngradePrimitiveCasts(TokenMap tokenMap)
        {
            var primitiveCastTokenType = typeof(PrimitiveInlineCastToken);
            // No implementation attested for (Vector/Rotator casts) in UE1 v61, but nonetheless accepted as a cast.
            tokenMap[(byte)CastToken.RotatorToVector] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.ByteToInt] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.ByteToBool] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.ByteToFloat] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.IntToByte] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.IntToBool] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.IntToFloat] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.BoolToByte] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.BoolToInt] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.BoolToFloat] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.FloatToByte] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.FloatToInt] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.FloatToBool] = primitiveCastTokenType;
            
            // Non-existent but accepted as a primitive cast...
            tokenMap[(byte)CastToken.ObjectToInterface] = primitiveCastTokenType;
            
            tokenMap[(byte)CastToken.ObjectToBool] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.NameToBool] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.StringToByte] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.StringToInt] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.StringToBool] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.StringToFloat] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.StringToVector] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.StringToRotator] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.VectorToBool] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.VectorToRotator] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.RotatorToBool] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.ByteToString] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.IntToString] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.BoolToString] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.FloatToString] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.ObjectToString] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.NameToString] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.VectorToString] = primitiveCastTokenType;
            tokenMap[(byte)CastToken.RotatorToString] = primitiveCastTokenType;
            
            // UE2+, later deprecated
            tokenMap[(byte)CastToken.DelegateToString] = primitiveCastTokenType; // StringToName in?
        }
#if UE3
        protected void UnshiftTokens3(TokenMap tokenMap)
        {
            tokenMap[0x15] = typeof(EndParmValueToken);

            tokenMap[0x35] = typeof(StructMemberToken);
            tokenMap[0x36] = typeof(DynamicArrayLengthToken);
            tokenMap[0x37] = typeof(GlobalFunctionToken);
            tokenMap[0x38] = typeof(PrimitiveCastToken);
            tokenMap[0x39] = typeof(DynamicArrayInsertToken);

            tokenMap[0x3A] = typeof(ReturnNothingToken);

            // 0x3B to 0x3F were not shifted.

            // These as well.
            tokenMap[0x40] = typeof(DynamicArrayRemoveToken);
            tokenMap[0x41] = typeof(DebugInfoToken);
            tokenMap[0x42] = typeof(DelegateFunctionToken);
            tokenMap[0x43] = typeof(DelegatePropertyToken);
            tokenMap[0x44] = typeof(LetDelegateToken);
            tokenMap[0x45] = typeof(ConditionalToken);
            tokenMap[0x46] = typeof(DynamicArrayFindToken);
            tokenMap[0x47] = typeof(DynamicArrayFindStructToken);
            tokenMap[0x48] = typeof(OutVariableToken);
            tokenMap[0x49] = typeof(DefaultParameterToken);
            tokenMap[0x4A] = typeof(EmptyParmToken);
            tokenMap[0x4B] = typeof(InstanceDelegateToken);
            //tokenMap[0x50] = typeof(BadToken);
            // Attested in GoW
            tokenMap[0x50] = typeof(UndefinedVariableToken);

            tokenMap[0x51] = typeof(InterfaceContextToken);
            tokenMap[0x52] = typeof(InterfaceCastToken);
            tokenMap[0x53] = typeof(EndOfScriptToken);
            tokenMap[0x54] = typeof(DynamicArrayAddToken);
            tokenMap[0x55] = typeof(DynamicArrayAddItemToken);
            tokenMap[0x56] = typeof(DynamicArrayRemoveItemToken);
            tokenMap[0x57] = typeof(DynamicArrayInsertItemToken);
            tokenMap[0x58] = typeof(DynamicArrayIteratorToken);
            tokenMap[0x59] = typeof(DynamicArraySortToken);

            // Added with a late UDK build.
            tokenMap[0x03] = typeof(StateVariableToken);
            tokenMap[0x5A] = typeof(FilterEditorOnlyToken);
        }
#endif
    }
}