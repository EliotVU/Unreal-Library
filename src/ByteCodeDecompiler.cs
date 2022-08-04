//#define SUPPRESS_BOOLINTEXPLOIT

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using UELib.Annotations;
using UELib.Branch.UE2.DNF.Tokens;
using UELib.Flags;
using UELib.Tokens;

namespace UELib.Core
{
    using System.Linq;
    using System.Text;

    public partial class UStruct
    {
        /// <summary>
        /// Decompiles the bytecodes from the 'Owner'
        /// </summary>
        public partial class UByteCodeDecompiler : IUnrealDecompilable
        {
            /// <summary>
            /// The Struct that contains the bytecode that we have to deserialize and decompile!
            /// </summary>
            private readonly UStruct _Container;

            /// <summary>
            /// Pointer to the ObjectStream buffer of 'Owner'
            /// </summary>
            private UObjectStream Buffer => _Container.Buffer;

            private UnrealPackage Package => _Container.Package;

            /// <summary>
            /// A collection of deserialized tokens, in their correspondence stream order.
            /// </summary>
            public List<Token> DeserializedTokens { get; private set; }

            [System.ComponentModel.DefaultValue(-1)]
            public int CurrentTokenIndex { get; set; }

            public Token NextToken => DeserializedTokens[++CurrentTokenIndex];

            public Token PeekToken => DeserializedTokens[CurrentTokenIndex + 1];

            public Token PreviousToken => DeserializedTokens[CurrentTokenIndex - 1];

            public Token CurrentToken => DeserializedTokens[CurrentTokenIndex];

            public UByteCodeDecompiler(UStruct container)
            {
                _Container = container;
                SetupMemorySizes();
                SetupByteCodeMap();
            }

            #region Deserialize

            private byte _ExtendedNative = (byte)ExprToken.ExtendedNative;
            private byte _FirstNative = (byte)ExprToken.FirstNative;

            /// <summary>
            /// The current simulated-memory-aligned position in @Buffer.
            /// </summary>
            private int CodePosition { get; set; }

            /// <summary>
            /// Size of FName in memory (int Index, (> 500) int Number).
            /// </summary>
            private byte _NameMemorySize = sizeof(int);

            /// <summary>
            /// Size of a pointer to an UObject in memory.
            /// 32bit, 64bit as of version 587 (even on 32bit platforms)
            /// </summary>
            private byte _ObjectMemorySize = sizeof(int);

            private void SetupMemorySizes()
            {
#if BIOSHOCK
                if (Package.Build == UnrealPackage.GameBuild.BuildName.BioShock)
                {
                    _NameMemorySize = sizeof(int) + sizeof(int);
                    return;
                }
#endif
                const short vNameSizeTo8 = 500;
                if (Buffer.Version >= vNameSizeTo8) _NameMemorySize = sizeof(int) + sizeof(int);
#if TERA
                // Tera's reported version is false (partial upgrade?)
                if (Package.Build == UnrealPackage.GameBuild.BuildName.Tera) return;
#endif
                const short vObjectSizeTo8 = 587;
                if (Buffer.Version >= vObjectSizeTo8) _ObjectMemorySize = sizeof(long);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void AlignSize(int size)
            {
                CodePosition += size;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void AlignNameSize()
            {
                CodePosition += _NameMemorySize;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void AlignObjectSize()
            {
                CodePosition += _ObjectMemorySize;
            }

            // TODO: Retrieve the byte-codes from a NTL file instead.
            [CanBeNull] private Dictionary<byte, byte> _ByteCodeMap;
#if AA2
            private readonly Dictionary<byte, byte> ByteCodeMap_BuildAa2_8 = new Dictionary<byte, byte>
            {
                { 0x00, (byte)ExprToken.LocalVariable },
                { 0x01, (byte)ExprToken.InstanceVariable },
                { 0x02, (byte)ExprToken.DefaultVariable },
                { 0x03, (byte)ExprToken.Unused },
                { 0x04, (byte)ExprToken.Switch },
                { 0x05, (byte)ExprToken.ClassContext },
                { 0x06, (byte)ExprToken.Jump },
                { 0x07, (byte)ExprToken.GotoLabel },
                { 0x08, (byte)ExprToken.VirtualFunction },
                { 0x09, (byte)ExprToken.IntConst },
                { 0x0A, (byte)ExprToken.JumpIfNot },
                { 0x0B, (byte)ExprToken.LabelTable },
                { 0x0C, (byte)ExprToken.FinalFunction },
                { 0x0D, (byte)ExprToken.EatString },
                { 0x0E, (byte)ExprToken.Let },
                { 0x0F, (byte)ExprToken.Stop },
                { 0x10, (byte)ExprToken.New },
                { 0x11, (byte)ExprToken.Context },
                { 0x12, (byte)ExprToken.MetaCast },
                { 0x13, (byte)ExprToken.Skip },
                { 0x14, (byte)ExprToken.Self },
                { 0x15, (byte)ExprToken.Return },
                { 0x16, (byte)ExprToken.EndFunctionParms },
                { 0x17, (byte)ExprToken.Unused },
                { 0x18, (byte)ExprToken.LetBool },
                { 0x19, (byte)ExprToken.DynArrayElement },
                { 0x1A, (byte)ExprToken.Assert },
                { 0x1B, (byte)ExprToken.ByteConst },
                { 0x1C, (byte)ExprToken.Nothing },
                { 0x1D, (byte)ExprToken.DelegateProperty },
                { 0x1E, (byte)ExprToken.IntZero },
                { 0x1F, (byte)ExprToken.LetDelegate },
                { 0x20, (byte)ExprToken.False },
                { 0x21, (byte)ExprToken.ArrayElement },
                { 0x22, (byte)ExprToken.EndOfScript },
                { 0x23, (byte)ExprToken.True },
                { 0x24, (byte)ExprToken.Unused },
                { 0x25, (byte)ExprToken.FloatConst },
                { 0x26, (byte)ExprToken.Case },
                { 0x27, (byte)ExprToken.IntOne },
                { 0x28, (byte)ExprToken.StringConst },
                { 0x29, (byte)ExprToken.NoObject },
                { 0x2A, (byte)ExprToken.NativeParm },
                { 0x2B, (byte)ExprToken.Unused },
                { 0x2C, (byte)ExprToken.DebugInfo },
                { 0x2D, (byte)ExprToken.StructCmpEq },
                // FIXME: Verify IteratorNext/IteratorPop?
                { 0x2E, (byte)ExprToken.IteratorNext },
                { 0x2F, (byte)ExprToken.DynArrayRemove },
                { 0x30, (byte)ExprToken.StructCmpNE },
                { 0x31, (byte)ExprToken.DynamicCast },
                { 0x32, (byte)ExprToken.Iterator },
                { 0x33, (byte)ExprToken.IntConstByte },
                { 0x34, (byte)ExprToken.BoolVariable },
                // FIXME: Verify IteratorNext/IteratorPop?
                { 0x35, (byte)ExprToken.IteratorPop },
                { 0x36, (byte)ExprToken.UniStringConst },
                { 0x37, (byte)ExprToken.StructMember },
                { 0x38, (byte)ExprToken.Unused },
                { 0x39, (byte)ExprToken.DelegateFunction },
                { 0x3A, (byte)ExprToken.Unused },
                { 0x3B, (byte)ExprToken.Unused },
                { 0x3C, (byte)ExprToken.Unused },
                { 0x3D, (byte)ExprToken.Unused },
                { 0x3E, (byte)ExprToken.Unused },
                { 0x3F, (byte)ExprToken.Unused },
                { 0x40, (byte)ExprToken.ObjectConst },
                { 0x41, (byte)ExprToken.NameConst },
                { 0x42, (byte)ExprToken.DynArrayLength },
                { 0x43, (byte)ExprToken.DynArrayInsert },
                { 0x44, (byte)ExprToken.PrimitiveCast },
                { 0x45, (byte)ExprToken.GlobalFunction },
                { 0x46, (byte)ExprToken.VectorConst },
                { 0x47, (byte)ExprToken.RotationConst },
                { 0x48, (byte)ExprToken.Unused },
                { 0x49, (byte)ExprToken.Unused },
                { 0x4A, (byte)ExprToken.Unused },
                { 0x4B, (byte)ExprToken.Unused },
                { 0x4C, (byte)ExprToken.Unused },
                { 0x4D, (byte)ExprToken.Unused },
                { 0x4E, (byte)ExprToken.Unused },
                { 0x4F, (byte)ExprToken.Unused },
                { 0x50, (byte)ExprToken.Unused },
                { 0x51, (byte)ExprToken.Unused },
                { 0x52, (byte)ExprToken.Unused },
                { 0x53, (byte)ExprToken.Unused },
                { 0x54, (byte)ExprToken.Unused },
                { 0x55, (byte)ExprToken.Unused },
                { 0x56, (byte)ExprToken.Unused },
                { 0x57, (byte)ExprToken.Unused },
                { 0x58, (byte)ExprToken.Unused },
                { 0x59, (byte)ExprToken.Unused }
            };

            /// <summary>
            /// The shifted byte-code map for AAA 2.6
            /// </summary>
            private readonly Dictionary<byte, byte> ByteCodeMap_BuildAa2_6 = new Dictionary<byte, byte>
            {
                { 0x00, (byte)ExprToken.LocalVariable },
                { 0x01, (byte)ExprToken.InstanceVariable },
                { 0x02, (byte)ExprToken.DefaultVariable },
                { 0x03, (byte)ExprToken.Unused },
                { 0x04, (byte)ExprToken.Jump },
                { 0x05, (byte)ExprToken.Return },
                { 0x06, (byte)ExprToken.Switch },
                { 0x07, (byte)ExprToken.Stop },
                { 0x08, (byte)ExprToken.JumpIfNot },
                { 0x09, (byte)ExprToken.Nothing },
                { 0x0A, (byte)ExprToken.LabelTable },
                { 0x0B, (byte)ExprToken.Assert },
                { 0x0C, (byte)ExprToken.Case },
                { 0x0D, (byte)ExprToken.EatString },
                { 0x0E, (byte)ExprToken.Let },
                { 0x0F, (byte)ExprToken.GotoLabel },
                { 0x10, (byte)ExprToken.DynArrayElement },
                { 0x11, (byte)ExprToken.New },
                { 0x12, (byte)ExprToken.ClassContext },
                { 0x13, (byte)ExprToken.MetaCast },
                { 0x14, (byte)ExprToken.LetBool },
                { 0x15, (byte)ExprToken.EndFunctionParms },
                { 0x16, (byte)ExprToken.Skip },
                { 0x17, (byte)ExprToken.Unused },
                { 0x18, (byte)ExprToken.Context },
                { 0x19, (byte)ExprToken.Self },
                { 0x1A, (byte)ExprToken.FinalFunction },
                { 0x1B, (byte)ExprToken.ArrayElement },
                { 0x1C, (byte)ExprToken.IntConst },
                { 0x1D, (byte)ExprToken.FloatConst },
                { 0x1E, (byte)ExprToken.StringConst },
                { 0x1F, (byte)ExprToken.VirtualFunction },
                { 0x20, (byte)ExprToken.IntOne },
                { 0x21, (byte)ExprToken.VectorConst },
                { 0x22, (byte)ExprToken.NameConst },
                { 0x23, (byte)ExprToken.IntZero },
                { 0x24, (byte)ExprToken.ObjectConst },
                { 0x25, (byte)ExprToken.ByteConst },
                { 0x26, (byte)ExprToken.RotationConst },
                { 0x27, (byte)ExprToken.False },
                { 0x28, (byte)ExprToken.True },
                { 0x29, (byte)ExprToken.NoObject },
                { 0x2A, (byte)ExprToken.NativeParm },
                { 0x2B, (byte)ExprToken.Unused },
                { 0x2C, (byte)ExprToken.BoolVariable },
                { 0x2D, (byte)ExprToken.Iterator },
                { 0x2E, (byte)ExprToken.IntConstByte },
                { 0x2F, (byte)ExprToken.DynamicCast },
                { 0x30, (byte)ExprToken.Unused },
                { 0x31, (byte)ExprToken.StructCmpNE },
                { 0x32, (byte)ExprToken.UniStringConst },
                { 0x33, (byte)ExprToken.IteratorNext },
                { 0x34, (byte)ExprToken.StructCmpEq },
                { 0x35, (byte)ExprToken.IteratorPop },
                { 0x36, (byte)ExprToken.GlobalFunction },
                { 0x37, (byte)ExprToken.StructMember },
                { 0x38, (byte)ExprToken.PrimitiveCast },
                { 0x39, (byte)ExprToken.DynArrayLength },
                { 0x3A, (byte)ExprToken.Unused },
                { 0x3B, (byte)ExprToken.Unused },
                { 0x3C, (byte)ExprToken.Unused },
                { 0x3D, (byte)ExprToken.Unused },
                { 0x3E, (byte)ExprToken.Unused },
                { 0x3F, (byte)ExprToken.Unused },
                { 0x40, (byte)ExprToken.Unused },
                { 0x41, (byte)ExprToken.EndOfScript },
                { 0x42, (byte)ExprToken.DynArrayRemove },
                { 0x43, (byte)ExprToken.DynArrayInsert },
                { 0x44, (byte)ExprToken.DelegateFunction },
                { 0x45, (byte)ExprToken.DebugInfo },
                { 0x46, (byte)ExprToken.LetDelegate },
                { 0x47, (byte)ExprToken.DelegateProperty },
                { 0x48, (byte)ExprToken.Unused },
                { 0x49, (byte)ExprToken.Unused },
                { 0x4A, (byte)ExprToken.Unused },
                { 0x4B, (byte)ExprToken.Unused },
                { 0x4C, (byte)ExprToken.Unused },
                { 0x4D, (byte)ExprToken.Unused },
                { 0x4E, (byte)ExprToken.Unused },
                { 0x4F, (byte)ExprToken.Unused },
                { 0x50, (byte)ExprToken.Unused },
                { 0x51, (byte)ExprToken.Unused },
                { 0x52, (byte)ExprToken.Unused },
                { 0x53, (byte)ExprToken.Unused },
                { 0x54, (byte)ExprToken.Unused },
                { 0x55, (byte)ExprToken.Unused },
                { 0x56, (byte)ExprToken.Unused },
                { 0x57, (byte)ExprToken.Unused },
                { 0x58, (byte)ExprToken.Unused },
                { 0x59, (byte)ExprToken.Unused }
            };
#endif
#if APB
            private static readonly Dictionary<byte, byte> ByteCodeMap_BuildApb = new Dictionary<byte, byte>
            {
                { (byte)ExprToken.Return, (byte)ExprToken.LocalVariable },
                { (byte)ExprToken.LocalVariable, (byte)ExprToken.Return },
                { (byte)ExprToken.Jump, (byte)ExprToken.JumpIfNot },
                { (byte)ExprToken.JumpIfNot, (byte)ExprToken.Jump },
                { (byte)ExprToken.Case, (byte)ExprToken.Nothing },
                { (byte)ExprToken.Nothing, (byte)ExprToken.Case }
                //{ 0x48, (byte)ExprToken.OutVariable },
                //{ 0x53, (byte)ExprToken.EndOfScript }
            };
#endif
#if BIOSHOCK
            private static readonly Dictionary<byte, byte> ByteCodeMap_BuildBs = new Dictionary<byte, byte>
            {
                //{ (byte)ExprToken.OutVariable, (byte)ExprToken.LogFunction }
            };
#endif
#if DNF
            private static readonly Dictionary<byte, byte> ByteCodeMap_BuildDnf = new Dictionary<byte, byte>
            {
                //{ (byte)ExprToken.LocalVariable, (byte)ExprToken.LocalVariable },
                //{ (byte)ExprToken.InstanceVariable, (byte)ExprToken.InstanceVariable },
                //{ (byte)ExprToken.DefaultVariable, (byte)ExprToken.DefaultVariable },
                { 0x03, (byte)ExprToken.Unused },
                //{ 0x04, (byte)ExprToken.Unused },
                //{ 0x0C, (byte)ExprToken.LabelTable },
                //{ 0x0D, (byte)ExprToken.Unused },
                //{ 0x0E, (byte)ExprToken.Unused },
                //{ 0x12, (byte)ExprToken.ClassContext },
                //{ 0x13, (byte)ExprToken.MetaCast },
                { 0x15, (byte)ExprToken.Unused },
                //{ 0x16, (byte)ExprToken.EndFunctionParms },
                //{ 0x18, (byte)ExprToken.Skip },
                //{ 0x19, (byte)ExprToken.Context },
                //{ 0x1B, (byte)ExprToken.VirtualFunction },
                //{ 0x1C, (byte)ExprToken.FinalFunction },
                { 0x2B, (byte)ExprToken.Unused },
                //{ 0x2E, (byte)ExprToken.DynamicCast },
                { 0x35, (byte)ExprToken.Unused },
                //{ 0x36, (byte)ExprToken.StructMember },
                { 0x37/*DynArrayLength*/, (byte)ExprToken.DebugInfo },
                //{ 0x38, (byte)ExprToken.GlobalFunction },
                { 0x39/*PrimitiveCast*/, (byte)ExprToken.Unused },
                { 0x3A, (byte)ExprToken.Unused },
                { 0x3B, (byte)ExprToken.Unused },
                { 0x3C, (byte)ExprToken.Unused },
                { 0x3D, (byte)ExprToken.Unused },
                { 0x3E, (byte)ExprToken.Unused },
                { 0x3F, (byte)ExprToken.Unused },
                { 0x40, (byte)ExprToken.Unused },
                { 0x41, (byte)ExprToken.Unused },
                { 0x42, (byte)ExprToken.Unused },
                { 0x43, (byte)ExprToken.Unused },
                { 0x44, (byte)ExprToken.Unused },
                { 0x45, (byte)ExprToken.Unused },
                { 0x46, (byte)ExprToken.Unused },
                { 0x47, (byte)ExprToken.Unused },
                { 0x48, (byte)ExprToken.Unused },
                { 0x49, (byte)ExprToken.Unused },
                { 0x4A, (byte)ExprToken.Unused },
                { 0x4B, (byte)ExprToken.Unused },
                { 0x4C, (byte)ExprToken.Unused },
                { 0x4D, (byte)ExprToken.Unused },
                { 0x4E, (byte)ExprToken.Unused },
                { 0x4F, (byte)ExprToken.Unused },
                { 0x50, (byte)ExprToken.Unused },
                { 0x51, (byte)ExprToken.Unused },
                { 0x52, (byte)ExprToken.Unused },
                { 0x53, (byte)ExprToken.Unused },
                { 0x54, (byte)ExprToken.Unused },
                { 0x55, (byte)ExprToken.Unused },
                { 0x56, (byte)ExprToken.Unused },
                { 0x57, (byte)ExprToken.Unused },
                { 0x58, (byte)ExprToken.Unused },
                { 0x59, (byte)ExprToken.Unused },
                { 0x5A, (byte)ExprToken.DynArrayLength }, // keyword > expr
                { 0x5B, (byte)ExprToken.DynArrayInsert }, // expr > expr
                { 0x5C, (byte)ExprToken.DynArrayAdd }, // expr > expr
                { 0x5D, (byte)ExprToken.DynArrayRemove }, // expr > expr
                { 0x5E, (byte)ExprToken.DelegateFunction }, // object > name
                { 0x5F, (byte)ExprToken.DelegateProperty }, // name
                { 0x60, (byte)ExprToken.LetDelegate }, // expr > expr
                //{ 0x61, (byte)ExprToken.InternalUnresolved }, // void (VectorConstZero)
                //{ 0x62, (byte)ExprToken.InternalUnresolved }, // void (VectorConstUnitZ)
                //{ 0x63, (byte)ExprToken.InternalUnresolved }, // void (RotConstZero)
                //{ 0x64, (byte)ExprToken.InternalUnresolved }, // ushort (IntConstWord)
                //{ 0x65, (byte)ExprToken.InternalUnresolved }, // byte > byte > byte (RotConstBytes)
                //{ 0x66, (byte)ExprToken.InternalUnresolved }, // keyword > expr (DynArrayEmpty)
                //{ 0x67, (byte)ExprToken.InternalUnresolved }, // void (Breakpoint)
                { 0x68, (byte)ExprToken.Unused },
                //{ 0x69, (byte)ExprToken.InternalUnresolved }, // int (RotConstPitch)
                //{ 0x6A, (byte)ExprToken.InternalUnresolved }, // int (RotConstYaw)
                //{ 0x6B, (byte)ExprToken.InternalUnresolved }, // int (RotConstRoll)
                //{ 0x6C, (byte)ExprToken.InternalUnresolved }, // int (VectorX)
                //{ 0x6D, (byte)ExprToken.InternalUnresolved }, // int (VectorY)
                //{ 0x6E, (byte)ExprToken.InternalUnresolved }, // int (VectorZ)
                //{ 0x6F, (byte)ExprToken.InternalUnresolved }, // int > int (VectorXY)
                //{ 0x70, (byte)ExprToken.InternalUnresolved }, // int > int (VectorXZ)
                //{ 0x71, (byte)ExprToken.InternalUnresolved }, // int > int (VectorYZ)
            };
#endif
            private bool IsUsingInlinedPrimitiveCasting()
            {
#if DNF
                if (Package.Build == UnrealPackage.GameBuild.BuildName.DNF) return true;
#endif
                return Package.Version < VPrimitiveCastToken;
            }

            private void SetupByteCodeMap()
            {
#if AA2
                if (Package.Build == UnrealPackage.GameBuild.BuildName.AA2)
                {
                    if (Package.LicenseeVersion >= 33)
                    {
                        _ByteCodeMap = ByteCodeMap_BuildAa2_8;
                    }
                    else
                    {
                        // FIXME: The byte-code shifted as of V2.6, but there is no way to tell which game-version the package was compiled with.
                        // This hacky-solution also doesn't handle UState nor UClass cases.
                        // This can be solved by moving the byte-code maps to their own NTL files.

                        // Flags
                        //sizeof(uint) + 
                        // Oper
                        //sizeof(byte)
                        // NativeToken
                        //sizeof(ushort) + 
                        // EndOfScript ExprToken
                        //sizeof(byte), 
                        long functionBacktrackLength = -8;
                        if (_Container is UFunction function && function.HasFunctionFlag(Flags.FunctionFlags.Net))
                            // RepOffset
                            functionBacktrackLength -= sizeof(ushort);

                        Buffer.StartPeek();
                        Buffer.Seek(functionBacktrackLength, SeekOrigin.End);
                        byte valueOfEndOfScriptToken = Buffer.ReadByte();
                        Buffer.EndPeek();

                        // Shifted?
                        if (valueOfEndOfScriptToken != (byte)ExprToken.FunctionEnd)
                            _ByteCodeMap = ByteCodeMap_BuildAa2_6;
                    }

                    return;
                }
#endif
#if APB
                if (Package.Build == UnrealPackage.GameBuild.BuildName.APB &&
                    Package.LicenseeVersion >= 32)
                {
                    _ByteCodeMap = ByteCodeMap_BuildApb;
                    return;
                }
#endif
#if BIOSHOCK
                if (Package.Build == UnrealPackage.GameBuild.BuildName.BioShock)
                {
                    _ByteCodeMap = ByteCodeMap_BuildBs;
                    return;
                }
#endif
#if MOH
                if (Package.Build == UnrealPackage.GameBuild.BuildName.MOH)
                {
                    // TODO: Incomplete byte-code map
                    _ByteCodeMap = new Dictionary<byte, byte>
                    {
                        { 0x0C, (byte)ExprToken.EmptyParmValue },
                        { 0x1D, (byte)ExprToken.FinalFunction },
                        { 0x16, (byte)ExprToken.EndOfScript },
                        { 0x18, (byte)ExprToken.StringConst },
                        { 0x23, (byte)ExprToken.EndFunctionParms },
                        { 0x28, (byte)ExprToken.Nothing },
                        { 0x2C, (byte)ExprToken.Let },
                        { 0x31, (byte)ExprToken.LocalVariable },
                        { 0x34, (byte)ExprToken.JumpIfNot },
                        // Incremented by 1 to adjust to the UE3 shift
                        { 0x36, (byte)ExprToken.Return },
                        { 0x38, (byte)ExprToken.ReturnNothing },
                        { 0x40, (byte)ExprToken.PrimitiveCast },
                        { 0x47, (byte)ExprToken.StructMember },
                        { 0x4B, (byte)ExprToken.NativeParm }
                        //{ 0x4F, (byte)ExprToken.BoolVariable }
                    };
                    return;
                }
#endif
#if DNF
                if (Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
                {
                    _ExtendedNative = 0x80;
                    _FirstNative = 0x90;
                    _ByteCodeMap = ByteCodeMap_BuildDnf;
                }
#endif
#if UE1 || DNF
                if (IsUsingInlinedPrimitiveCasting())
                {
                    // Map all old CastTokens that were expressed as an ExprToken
                    var castBytesMap = new Dictionary<byte, byte>
                    {
                        // TODO: Confirm if these are correct for UE1
                        { (byte)CastToken.RotatorToVector, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.ByteToInt, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.ByteToBool, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.ByteToFloat, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.IntToByte, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.IntToBool, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.IntToFloat, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.BoolToByte, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.BoolToInt, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.BoolToFloat, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.FloatToByte, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.FloatToInt, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.FloatToBool, (byte)ExprToken.PrimitiveCast },

                        { (byte)CastToken.ObjectToInterface, (byte)ExprToken.PrimitiveCast }, // Actually StringToName

                        { (byte)CastToken.ObjectToBool, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.NameToBool, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.StringToByte, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.StringToInt, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.StringToBool, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.StringToFloat, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.StringToVector, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.StringToRotator, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.VectorToBool, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.VectorToRotator, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.RotatorToBool, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.ByteToString, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.IntToString, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.BoolToString, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.FloatToString, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.ObjectToString, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.NameToString, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.VectorToString, (byte)ExprToken.PrimitiveCast },
                        { (byte)CastToken.RotatorToString, (byte)ExprToken.PrimitiveCast },

                        { (byte)CastToken.DelegateToString, (byte)ExprToken.PrimitiveCast } // StringToName in HP2?
                    };

                    if (_ByteCodeMap == null)
                    {
                        _ByteCodeMap = castBytesMap;
                    }
                    else
                    {
                        foreach (var pair in castBytesMap)
                        {
                            _ByteCodeMap[pair.Key] = pair.Value;
                        }
                    }
                }
#endif
            }

            /// <summary>
            /// Fix the values of UE1/UE2 tokens to match the UE3 token values.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private byte FixToken(byte tokenCode)
            {
#if UE3
                // Adjust UE2 tokens to UE3
                // TODO: Use ByteCodeMap
                if (Package.Version >= 184
                    &&
                    (
                        (tokenCode >= (byte)ExprToken.RangeConst && tokenCode < (byte)ExprToken.ReturnNothing)
                        ||
                        (tokenCode > (byte)ExprToken.NoDelegate && tokenCode < (byte)ExprToken.ExtendedNative))
                   ) ++tokenCode;
#endif
                // TODO: Map directly to a Token type instead of a byte-code.
                if (_ByteCodeMap != null)
                    return _ByteCodeMap.TryGetValue(tokenCode, out byte newTokenCode)
                        ? newTokenCode
                        : tokenCode;
#if UE1
                if (Package.Version < 62)
                    switch (tokenCode)
                    {
                        //case (byte)ExprToken.LetBool:
                        //    return (byte)ExprToken.BeginFunction;

                        case (byte)ExprToken.EndParmValue:
                            return (byte)ExprToken.EatReturnValue;
                    }
#endif
                return tokenCode;
            }

            private bool _WasDeserialized;

            public void Deserialize()
            {
                if (_WasDeserialized)
                    return;

                _WasDeserialized = true;
                try
                {
                    _Container.EnsureBuffer();
                    Buffer.Seek(_Container.ScriptOffset, SeekOrigin.Begin);
                    CodePosition = 0;
                    int codeSize = _Container.ByteScriptSize;

                    CurrentTokenIndex = -1;
                    DeserializedTokens = new List<Token>();
                    _Labels = new List<ULabelEntry>();

                    while (CodePosition < codeSize)
                        try
                        {
                            DeserializeNext();
                        }
                        catch (EndOfStreamException error)
                        {
                            Console.WriteLine("Couldn't backup from this error! Decompiling aborted!");
                            break;
                        }
                        catch (SystemException e)
                        {
                            Console.WriteLine("Object:" + _Container.Name);
                            Console.WriteLine("Failed to deserialize token at position:" + CodePosition);
                            Console.WriteLine("Exception:" + e.Message);
                            Console.WriteLine("Stack:" + e.StackTrace);
                        }
                }
                finally
                {
                    _Container.MaybeDisposeBuffer();
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private void DeserializeDebugToken()
            {
                // Sometimes we may end up at the end of a script
                // -- and by coincidence pickup a DebugInfo byte-code outside of the script-boundary.
                if (CodePosition == _Container.ByteScriptSize) return;

                Buffer.StartPeek();
                byte tokenCode = FixToken(Buffer.ReadByte());
                Buffer.EndPeek();

                if (tokenCode == (byte)ExprToken.DebugInfo) DeserializeNext();
            }

            private NativeFunctionToken CreateNativeToken(ushort nativeIndex)
            {
                var nativeTableItem = _Container.Package.NTLPackage?.FindTableItem(nativeIndex) ?? new NativeTableItem
                {
                    Type = FunctionType.Function,
                    Name = $"__NFUN_{nativeIndex}__",
                    ByteToken = nativeIndex
                };
                return new NativeFunctionToken
                {
                    NativeItem = nativeTableItem,
                };
            }

            private Token DeserializeNext(byte tokenCode = byte.MaxValue)
            {
                int tokenPosition = CodePosition;
                if (tokenCode == byte.MaxValue)
                {
                    tokenCode = Buffer.ReadByte();
                    AlignSize(sizeof(byte));
                }

                byte serializedByte = tokenCode;
                Token token;
                if (tokenCode >= _ExtendedNative)
                {
                    if (tokenCode >= _FirstNative)
                    {
                        token = CreateNativeToken(tokenCode);
                    }
                    else
                    {
                        byte extendedByte = Buffer.ReadByte();
                        AlignSize(sizeof(byte));

                        var nativeIndex = (ushort)(((tokenCode - _ExtendedNative) << 8) | extendedByte);
                        Debug.Assert(nativeIndex < (ushort)ExprToken.MaxNative);
                        token = CreateNativeToken(nativeIndex);
                    }
                }
                else
                {
                    tokenCode = FixToken(tokenCode);
                    switch (tokenCode)
                    {
                        #region Cast

                        case (byte)ExprToken.DynamicCast:
                            token = new DynamicCastToken();
                            break;

                        case (byte)ExprToken.MetaCast:
                            token = new MetaClassCastToken();
                            break;

                        case (byte)ExprToken.InterfaceCast:
                            token = new InterfaceCastToken();
                            break;

                        case (byte)ExprToken.PrimitiveCast:
                            token = new PrimitiveCastToken();
                            break;

                        #endregion

                        #region Context

                        case (byte)ExprToken.ClassContext:
                            token = new ClassContextToken();
                            break;

                        case (byte)ExprToken.InterfaceContext:
                            token = new InterfaceContextToken();
                            break;

                        case (byte)ExprToken.Context:
                            token = new ContextToken();
                            break;

                        case (byte)ExprToken.StructMember:
                            token = new StructMemberToken();
                            break;

                        #endregion

                        #region Assigns

                        case (byte)ExprToken.Let:
                            token = new LetToken();
                            break;

                        case (byte)ExprToken.LetBool:
#if UE1
                            if (Buffer.Version < 62)
                            {
                                token = new BeginFunctionToken();
                                break;
                            }
#endif
                            token = new LetBoolToken();
                            break;

                        case (byte)ExprToken.EndParmValue:
#if UNREAL2
                            // FIXME: Not per se Unreal 2 specific, might be an old UE2 relict, however I cannot attest this token in UT99, RS3, nor UT2004.
                            if (Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.Unreal2 ||
                                Buffer.Package.Build == UnrealPackage.GameBuild.BuildName.Unreal2XMP)
                            {
                                token = new LineNumberToken();
                                break;
                            }
#endif
                            token = new EndParmValueToken();
                            break;

                        case (byte)ExprToken.LetDelegate:
                            token = new LetDelegateToken();
                            break;

                        case (byte)ExprToken.Conditional:
                            token = new ConditionalToken();
                            break;

                        case (byte)ExprToken.Eval:
                            if (Buffer.Version >= 300)
                                token = new DynamicArrayFindStructToken();
                            else
                                token = new ConditionalToken();

                            break;

                        #endregion

                        #region Jumps

                        case (byte)ExprToken.Return:
                            token = new ReturnToken();
                            break;

                        case (byte)ExprToken.ReturnNothing:
                            // definitely existed since GoW(490)
                            if (Buffer.Version > 420 && DeserializedTokens.Count > 0 &&
                                !(DeserializedTokens[DeserializedTokens.Count - 1] is
                                    ReturnToken)) // Should only be done if the last token wasn't Return
                                token = new DynamicArrayInsertToken();
                            else
                                token = new ReturnNothingToken();

                            break;

                        case (byte)ExprToken.GotoLabel:
                            token = new GoToLabelToken();
                            break;

                        case (byte)ExprToken.Jump:
                            token = new JumpToken();
                            break;

                        case (byte)ExprToken.JumpIfNot:
                            token = new JumpIfNotToken();
                            break;

                        case (byte)ExprToken.Switch:
                            token = new SwitchToken();
                            break;

                        case (byte)ExprToken.Case:
                            token = new CaseToken();
                            break;

                        case (byte)ExprToken.DynArrayIterator:
                            token = new ArrayIteratorToken();
                            break;

                        case (byte)ExprToken.Iterator:
                            token = new IteratorToken();
                            break;

                        case (byte)ExprToken.IteratorNext:
                            token = new IteratorNextToken();
                            break;

                        case (byte)ExprToken.IteratorPop:
                            token = new IteratorPopToken();
                            break;

                        case (byte)ExprToken.FilterEditorOnly:
                            token = new FilterEditorOnlyToken();
                            break;

                        #endregion

                        #region Variables

                        case (byte)ExprToken.NativeParm:
                            token = new NativeParameterToken();
                            break;

                        // Referenced variables that are from this function e.g. Local and params
                        case (byte)ExprToken.InstanceVariable:
                            token = new InstanceVariableToken();
                            break;

                        case (byte)ExprToken.LocalVariable:
                            token = new LocalVariableToken();
                            break;

                        case (byte)ExprToken.StateVariable:
                            token = new StateVariableToken();
                            break;

                        // Referenced variables that are default
                        case (byte)ExprToken.UndefinedVariable:
#if BORDERLANDS2
                            if (_Container.Package.Build == UnrealPackage.GameBuild.BuildName.Borderlands2)
                            {
                                token = new DynamicVariableToken();
                                break;
                            }
#endif
                            token = new UndefinedVariableToken();
                            break;

                        case (byte)ExprToken.DefaultVariable:
                            token = new DefaultVariableToken();
                            break;

                        case (byte)ExprToken.OutVariable:
#if BIOSHOCK
                            if (Package.Build == UnrealPackage.GameBuild.BuildName.BioShock)
                            {
                                token = new LogFunctionToken();
                                break;
                            }
#endif
                            token = new OutVariableToken();
                            break;

                        case (byte)ExprToken.BoolVariable:
                            token = new BoolVariableToken();
                            break;

                        case (byte)ExprToken.DelegateProperty:
                            token = new DelegatePropertyToken();
                            break;

                        case (byte)ExprToken.DefaultParmValue:
                            token = new DefaultParameterToken();
                            break;

                        #endregion

                        #region Misc

                        case (byte)ExprToken.DebugInfo:
                            token = new DebugInfoToken();
                            break;

                        case (byte)ExprToken.Nothing:
                            token = new NothingToken();
                            break;

                        case (byte)ExprToken.EndFunctionParms:
                            token = new EndFunctionParmsToken();
                            break;

                        case (byte)ExprToken.IntZero:
                            token = new IntZeroToken();
                            break;

                        case (byte)ExprToken.IntOne:
                            token = new IntOneToken();
                            break;

                        case (byte)ExprToken.True:
                            token = new TrueToken();
                            break;

                        case (byte)ExprToken.False:
                            token = new FalseToken();
                            break;

                        case (byte)ExprToken.NoDelegate:
                            token = new NoDelegateToken();
                            break;

                        case (byte)ExprToken.EmptyParmValue:
                            token = new NoParmToken();
                            break;

                        case (byte)ExprToken.NoObject:
                            token = new NoObjectToken();
                            break;

                        case (byte)ExprToken.Self:
                            token = new SelfToken();
                            break;

                        case (byte)ExprToken.Stop:
                            token = new StopToken();
                            break;

                        case (byte)ExprToken.Assert:
                            token = new AssertToken();
                            break;

                        case (byte)ExprToken.LabelTable:
                            token = new LabelTableToken();
                            break;

                        case (byte)ExprToken.EndOfScript:
                            token = new EndOfScriptToken();
                            break;

                        case (byte)ExprToken.Skip:
                            token = new SkipToken();
                            break;

                        case (byte)ExprToken.StructCmpEq:
                            token = new StructCmpEqToken();
                            break;

                        case (byte)ExprToken.StructCmpNE:
                            token = new StructCmpNeToken();
                            break;

                        case (byte)ExprToken.DelegateCmpEq:
                            token = new DelegateCmpEqToken();
                            break;

                        case (byte)ExprToken.DelegateFunctionCmpEq:
                            token = new DelegateFunctionCmpEqToken();
                            break;

                        case (byte)ExprToken.DelegateCmpNE:
                            token = new DelegateCmpNEToken();
                            break;

                        case (byte)ExprToken.DelegateFunctionCmpNE:
                            token = new DelegateFunctionCmpNEToken();
                            break;

                        case (byte)ExprToken.InstanceDelegate:
                            token = new InstanceDelegateToken();
                            break;

                        case (byte)ExprToken.EatReturnValue:
                            token = new EatReturnValueToken();
                            break;

                        case (byte)ExprToken.New:
                            token = new NewToken();
                            break;

                        case (byte)ExprToken.FunctionEnd:
                            if (Buffer.Version < 300)
                                token = new EndOfScriptToken();
                            else
                                token = new DynamicArrayFindToken();

                            break;

                        case (byte)ExprToken.VarInt:
                        case (byte)ExprToken.VarFloat:
                        case (byte)ExprToken.VarByte:
                        case (byte)ExprToken.VarBool:
                            //case (byte)ExprToken.VarObject:   // See UndefinedVariable
                            token = new DynamicVariableToken();
                            break;
#if UE1
                        case (byte)ExprToken.CastStringSize:
                            // FIXME: Version, just a safe guess.
                            if (Buffer.Version >= 70)
                            {
                                token = new UnresolvedToken();
                                break;
                            }

                            token = new CastStringSizeToken();
                            break;
#endif

                        #endregion

                        #region Constants

                        case (byte)ExprToken.IntConst:
                            token = new IntConstToken();
                            break;

                        case (byte)ExprToken.ByteConst:
                            token = new ByteConstToken();
                            break;

                        case (byte)ExprToken.IntConstByte:
                            token = new IntConstByteToken();
                            break;

                        case (byte)ExprToken.FloatConst:
                            token = new FloatConstToken();
                            break;

                        case (byte)ExprToken.ObjectConst:
                            token = new ObjectConstToken();
                            break;

                        case (byte)ExprToken.NameConst:
                            token = new NameConstToken();
                            break;

                        case (byte)ExprToken.StringConst:
                            token = new StringConstToken();
                            break;

                        case (byte)ExprToken.UniStringConst:
                            token = new UniStringConstToken();
                            break;

                        case (byte)ExprToken.RotationConst:
                            token = new RotationConstToken();
                            break;

                        case (byte)ExprToken.VectorConst:
                            token = new VectorConstToken();
                            break;

                        case (byte)ExprToken.RangeConst:
                            token = new RangeConstToken();
                            break;

                        #endregion

                        #region Functions

                        case (byte)ExprToken.FinalFunction:
                            token = new FinalFunctionToken();
                            break;

                        case (byte)ExprToken.VirtualFunction:
                            token = new VirtualFunctionToken();
                            break;

                        case (byte)ExprToken.GlobalFunction:
                            token = new GlobalFunctionToken();
                            break;

                        case (byte)ExprToken.DelegateFunction:
                            token = new DelegateFunctionToken();
                            break;

                        #endregion

                        #region Arrays

                        case (byte)ExprToken.ArrayElement:
                            token = new ArrayElementToken();
                            break;

                        case (byte)ExprToken.DynArrayElement:
                            token = new DynamicArrayElementToken();
                            break;

                        case (byte)ExprToken.DynArrayLength:
                            token = new DynamicArrayLengthToken();
                            break;

                        case (byte)ExprToken.DynArrayInsert:
                            token = new DynamicArrayInsertToken();
                            break;

                        case (byte)ExprToken.DynArrayInsertItem:
                            token = new DynamicArrayInsertItemToken();
                            break;

                        case (byte)ExprToken.DynArrayRemove:
                            token = new DynamicArrayRemoveToken();
                            break;

                        case (byte)ExprToken.DynArrayRemoveItem:
                            token = new DynamicArrayRemoveItemToken();
                            break;

                        case (byte)ExprToken.DynArrayAdd:
                            token = new DynamicArrayAddToken();
                            break;

                        case (byte)ExprToken.DynArrayAddItem:
                            token = new DynamicArrayAddItemToken();
                            break;

                        case (byte)ExprToken.DynArraySort:
                            token = new DynamicArraySortToken();
                            break;

                        #endregion

                        default:
#if DNF
                            // DNF has extended the ExtendNative tokens set.
                            // FIXME: Re-factor once we have a better ByteCodeToToken factory.
                            if (Package.Build == UnrealPackage.GameBuild.BuildName.DNF)
                            {
                                switch (tokenCode)
                                {
                                    case 0x61:
                                        token = new VectorConstZeroToken();
                                        break;
                                    
                                    case 0x62:
                                        token = new VectorConstUnitZToken();
                                        break;
                                    
                                    case 0x63:
                                        token = new RotConstZeroToken();
                                        break;
                                    
                                    case 0x64:
                                        token = new IntConstWordToken();
                                        break;
                                    
                                    case 0x65:
                                        token = new RotConstBytesToken();
                                        break;
                                    
                                    case 0x66:
                                        token = new DynamicArrayEmptyToken();
                                        break;
                                    
                                    case 0x67:
                                        token = new BreakpointToken();
                                        break;
                                    
                                    case 0x69:
                                        token = new RotConstPitchToken();
                                        break;
                                    
                                    case 0x6A:
                                        token = new RotConstYawToken();
                                        break;
                                    
                                    case 0x6B:
                                        token = new RotConstRollToken();
                                        break;

                                    case 0x6C:
                                        token = new VectorXToken();
                                        break;
                                    
                                    case 0x6D:
                                        token = new VectorYToken();
                                        break;
                                    
                                    case 0x6E:
                                        token = new VectorZToken();
                                        break;

                                    case 0x6F:
                                        token = new VectorXYToken();
                                        break;

                                    case 0x70:
                                        token = new VectorXZToken();
                                        break;

                                    case 0x71:
                                        token = new VectorYZToken();
                                        break;
                                    
                                    default:
                                        token = new UnresolvedToken();
                                        break;
                                }

                                goto assertToken;
                            }
#endif
                            token = new UnresolvedToken();
                            break;
                    }
                }

            assertToken:
                Debug.Assert(token != null);
                AddToken(token, serializedByte, tokenPosition);
                return token;
            }

            private void AddToken(Token token, byte tokenCode, int tokenPosition)
            {
                DeserializedTokens.Add(token);
                token.Decompiler = this;
                token.RepresentToken = tokenCode;
                token.Position = (uint)tokenPosition; // + (uint)Owner._ScriptOffset;
                token.StoragePosition = (uint)Buffer.Position - (uint)_Container.ScriptOffset - 1;
                token.Deserialize(Buffer);
                // Includes all sizes of followed tokens as well! e.g. i = i + 1; is summed here but not i = i +1; (not>>)i ++;
                token.Size = (ushort)(CodePosition - tokenPosition);
                token.StorageSize =
                    (ushort)(Buffer.Position - _Container.ScriptOffset - token.StoragePosition);
                token.PostDeserialized();
            }

            #endregion

#if DECOMPILE

            #region Decompile

            public class NestManager
            {
                public UByteCodeDecompiler Decompiler;

                public class Nest : IUnrealDecompilable
                {
                    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design",
                        "CA1008:EnumsShouldHaveZeroValue")]
                    public enum NestType : byte
                    {
                        Scope = 0,
                        If = 1,
                        Else = 2,
                        ForEach = 4,
                        Switch = 5,
                        Case = 6,
                        Default = 7,
                        Loop = 8
                    }

                    /// <summary>
                    /// Position of this Nest (CodePosition)
                    /// </summary>
                    public uint Position;

                    public NestType Type;
                    public Token Creator;

                    public virtual string Decompile()
                    {
                        return string.Empty;
                    }

                    public bool IsPastOffset(int position)
                    {
                        return position >= Position;
                    }

                    public override string ToString()
                    {
                        return $"Type:{Type} Position:0x{Position:X3}";
                    }
                }

                public class NestBegin : Nest
                {
                    public override string Decompile()
                    {
#if DEBUG_NESTS
                        return "\r\n" + UDecompilingState.Tabs + "//<" + Type + ">";
#else
                        return Type != NestType.Case && Type != NestType.Default
                            ? UnrealConfig.PrintBeginBracket()
                            : string.Empty;
#endif
                    }
                }

                public class NestEnd : Nest
                {
                    public JumpToken HasElseNest;

                    public override string Decompile()
                    {
#if DEBUG_NESTS
                        return "\r\n" + UDecompilingState.Tabs + "//</" + Type + ">";
#else
                        return Type != NestType.Case && Type != NestType.Default
                            ? UnrealConfig.PrintEndBracket()
                            : string.Empty;
#endif
                    }
                }

                public readonly List<Nest> Nests = new List<Nest>();

                public void AddNest(Nest.NestType type, uint position, uint endPosition, Token creator = null)
                {
                    creator = creator ?? Decompiler.CurrentToken;
                    Nests.Add(new NestBegin { Position = position, Type = type, Creator = creator });
                    Nests.Add(new NestEnd { Position = endPosition, Type = type, Creator = creator });
                }

                public NestBegin AddNestBegin(Nest.NestType type, uint position, Token creator = null)
                {
                    var n = new NestBegin { Position = position, Type = type };
                    Nests.Add(n);
                    n.Creator = creator ?? Decompiler.CurrentToken;
                    return n;
                }

                public NestEnd AddNestEnd(Nest.NestType type, uint position, Token creator = null)
                {
                    var n = new NestEnd { Position = position, Type = type };
                    Nests.Add(n);
                    n.Creator = creator ?? Decompiler.CurrentToken;
                    return n;
                }

                public bool TryAddNestEnd(Nest.NestType type, uint pos)
                {
                    foreach (var nest in Decompiler._Nester.Nests)
                        if (nest.Type == type && nest.Position == pos)
                            return false;

                    Decompiler._Nester.AddNestEnd(type, pos);
                    return true;
                }
            }

            private NestManager _Nester;

            // Checks if we're currently within a nest of type nestType in any stack!
            private NestManager.Nest IsWithinNest(NestManager.Nest.NestType nestType)
            {
                for (int i = _NestChain.Count - 1; i >= 0; --i)
                    if (_NestChain[i].Type == nestType)
                        return _NestChain[i];

                return null;
            }

            // Checks if the current nest is of type nestType in the current stack!
            // Only BeginNests that have been decompiled will be tested for!
            private NestManager.Nest IsInNest(NestManager.Nest.NestType nestType)
            {
                int i = _NestChain.Count - 1;
                if (i == -1)
                    return null;

                return _NestChain[i].Type == nestType ? _NestChain[i] : null;
            }

            public void InitDecompile()
            {
                _NestChain.Clear();

                _Nester = new NestManager { Decompiler = this };
                CurrentTokenIndex = -1;
                CodePosition = 0;

                FieldToken.LastField = null;

                // Reset these, in case of a loop in the Decompile function that did not finish due exception errors!
                _IsWithinClassContext = false;
                _CanAddSemicolon = false;
                _MustCommentStatement = false;
                _PostIncrementTabs = 0;
                _PostDecrementTabs = 0;
                _PreIncrementTabs = 0;
                _PreDecrementTabs = 0;
                PreComment = string.Empty;
                PostComment = string.Empty;

                _TempLabels = new List<(ULabelEntry, int)>();
                if (_Labels != null)
                    for (var i = 0; i < _Labels.Count; ++i)
                    {
                        // No duplicates, caused by having multiple goto's with the same destination
                        int index = _TempLabels.FindIndex(p => p.entry.Position == _Labels[i].Position);
                        if (index == -1)
                        {
                            _TempLabels.Add((_Labels[i], 1));
                        }
                        else
                        {
                            var data = _TempLabels[index];
                            data.refs++;
                            _TempLabels[index] = data;
                        }
                    }
            }

            public void JumpTo(ushort codeOffset)
            {
                int index = DeserializedTokens.FindIndex(t => t.Position == codeOffset);
                if (index == -1)
                    return;

                CurrentTokenIndex = index;
            }

            public Token TokenAt(ushort codeOffset)
            {
                return DeserializedTokens.Find(t => t.Position == codeOffset);
            }

            /// <summary>
            /// True if we are currently decompiling within a ClassContext token.
            ///
            /// HACK: For static calls -> class'ClassA'.static.FuncA();
            /// </summary>
            private bool _IsWithinClassContext;

            private bool _CanAddSemicolon;
            private bool _MustCommentStatement;

            private byte _PostIncrementTabs;
            private byte _PostDecrementTabs;
            private byte _PreIncrementTabs;
            private byte _PreDecrementTabs;

            public string PreComment;
            public string PostComment;

            public void MarkSemicolon()
            {
                _CanAddSemicolon = true;
            }

            public string Decompile()
            {
                // Make sure that everything is deserialized!
                if (!_WasDeserialized) Deserialize();

                var output = new StringBuilder();
                // Original indention, so that we can restore it later, necessary if decompilation fails to reduce nesting indention.
                string initTabs = UDecompilingState.Tabs;

#if DEBUG_TOKENPOSITIONS
                UDecompilingState.AddTabs(3);
#endif
                try
                {
                    //Initialize==========
                    InitDecompile();
                    var spewOutput = false;
                    var tokenEndIndex = 0;
                    Token lastStatementToken = null;
#if !DEBUG_HIDDENTOKENS
                    if (_Container is UFunction func
                        && func.HasOptionalParamData()
                        && DeserializedTokens.Count > 0)
                    {
                        CurrentTokenIndex = 0;
                        foreach (var parm in func.Params)
                        {
                            if (!parm.HasPropertyFlag(PropertyFlagsLO.OptionalParm))
                                continue;

                            // Skip NothingToken (No default value) and DefaultParameterToken (up to EndParmValueToken)
                            switch (CurrentToken)
                            {
                                case NothingToken _:
                                    ++CurrentTokenIndex; // NothingToken
                                    break;

                                case DefaultParameterToken _:
                                {
                                    do
                                    {
                                        ++CurrentTokenIndex;
                                    } while (!(CurrentToken is EndParmValueToken));

                                    ++CurrentTokenIndex; // EndParmValueToken
                                    break;
                                }

                                default:
                                    // Can be true e.g a function with optionals but no default values
                                    //Debug.Fail($"Unexpected token for optional parameter {parm.GetOuterGroup()}");
                                    break;
                            }
                        }
                    }
#endif
                    while (CurrentTokenIndex + 1 < DeserializedTokens.Count)
                    {
                        //Decompile chain==========
                        {
                            string tokenOutput;
                            var newToken = NextToken;
                            int tokenBeginIndex = CurrentTokenIndex;

                            // To ensure we print generated labels within a nesting block.
                            string labelsOutput = DecompileLabelForToken(CurrentToken, spewOutput);
                            if (labelsOutput != string.Empty) output.AppendLine(labelsOutput);

                            try
                            {
                                // FIX: Formatting issue on debug-compiled packages
                                if (newToken is DebugInfoToken)
                                {
                                    string nestsOutput = DecompileNests();
                                    if (nestsOutput != string.Empty)
                                    {
                                        output.Append(nestsOutput);
                                        spewOutput = true;
                                    }

#if !DEBUG_HIDDENTOKENS
                                    continue;
#endif
                                }
                            }
                            catch (Exception e)
                            {
                                output.Append($"// ({e.GetType().Name})");
                            }

                            try
                            {
                                tokenOutput = newToken.Decompile();
                                if (CurrentTokenIndex + 1 < DeserializedTokens.Count &&
                                    PeekToken is EndOfScriptToken)
                                {
                                    var firstToken = newToken is DebugInfoToken ? lastStatementToken : newToken;
                                    if (firstToken is ReturnToken)
                                    {
                                        var lastToken = newToken is DebugInfoToken ? PreviousToken : CurrentToken;
                                        if (lastToken is NothingToken || lastToken is ReturnNothingToken)
                                            _MustCommentStatement = true;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                output.AppendLine("\r\n"
                                                  + UDecompilingState.Tabs + "/* Statement decompilation error: "
                                                  + e.Message);

                                UDecompilingState.AddTab();
                                string tokensOutput = FormatAndDecompileTokens(tokenBeginIndex, tokenEndIndex);
                                output.Append(UDecompilingState.Tabs);
                                output.Append(tokensOutput);
                                UDecompilingState.RemoveTab();

                                output.AppendLine("\r\n"
                                                  + UDecompilingState.Tabs
                                                  + "*/");

                                tokenOutput = "/*@Error*/";
                            }
                            finally
                            {
                                tokenEndIndex = CurrentTokenIndex;
                            }

                            // HACK: for multiple cases for one block of code, etc!
                            if (_PreDecrementTabs > 0)
                            {
                                UDecompilingState.RemoveTabs(_PreDecrementTabs);
                                _PreDecrementTabs = 0;
                            }

                            if (_PreIncrementTabs > 0)
                            {
                                UDecompilingState.AddTabs(_PreIncrementTabs);
                                _PreIncrementTabs = 0;
                            }

                            if (_MustCommentStatement && UnrealConfig.SuppressComments)
                                continue;

                            if (!UnrealConfig.SuppressComments)
                            {
                                if (PreComment != string.Empty)
                                {
                                    tokenOutput = PreComment + (string.IsNullOrEmpty(tokenOutput)
                                        ? tokenOutput
                                        : "\r\n" + UDecompilingState.Tabs + tokenOutput);

                                    PreComment = string.Empty;
                                }

                                if (PostComment != string.Empty)
                                {
                                    tokenOutput += PostComment;
                                    PostComment = string.Empty;
                                }
                            }

                            //Preprocess output==========
                            {
#if DEBUG_HIDDENTOKENS
                                if (tokenOutput.Length == 0) tokenOutput = ";";
#endif
#if DEBUG_TOKENPOSITIONS
#endif
                                // Previous did spew and this one spews? then a new line is required!
                                if (tokenOutput != string.Empty)
                                {
                                    // Spew before?
                                    if (spewOutput)
                                        output.Append("\r\n");
                                    else spewOutput = true;
                                }

#if DEBUG_TOKENPOSITIONS
                                string orgTabs = UDecompilingState.Tabs;
                                int spaces = Math.Max(3 * UnrealConfig.Indention.Length, 4);
                                UDecompilingState.RemoveSpaces(spaces);

                                var tokens = DeserializedTokens
                                    .GetRange(tokenBeginIndex, tokenEndIndex - tokenBeginIndex + 1)
                                    .Select(FormatTokenInfo);
                                string tokensInfo = string.Join(", ", tokens);

                                output.Append(UDecompilingState.Tabs);
                                output.AppendLine($"  [{tokensInfo}] ");
                                output.Append(UDecompilingState.Tabs);
                                output.Append(
                                    $"  (+{newToken.Position:X3}  {CurrentToken.Position + CurrentToken.Size:X3}) ");
#endif
                                if (spewOutput)
                                {
                                    if (_MustCommentStatement)
                                    {
                                        output.Append(UDecompilingState.Tabs);
                                        output.Append($"//{tokenOutput}");
                                        _MustCommentStatement = false;
                                    }
                                    else
                                    {
                                        output.Append(UDecompilingState.Tabs);
                                        output.Append(tokenOutput);
                                    }

                                    // One of the decompiled tokens wanted to be ended.
                                    if (_CanAddSemicolon)
                                    {
                                        output.Append(";");
                                        _CanAddSemicolon = false;
                                    }
                                }
#if DEBUG_TOKENPOSITIONS
                                UDecompilingState.Tabs = orgTabs;
#endif
                            }
                            lastStatementToken = newToken;
                        }

                        //Postprocess output==========
                        if (_PostDecrementTabs > 0)
                        {
                            UDecompilingState.RemoveTabs(_PostDecrementTabs);
                            _PostDecrementTabs = 0;
                        }

                        if (_PostIncrementTabs > 0)
                        {
                            UDecompilingState.AddTabs(_PostIncrementTabs);
                            _PostIncrementTabs = 0;
                        }

                        try
                        {
                            string nestsOutput = DecompileNests();
                            if (nestsOutput != string.Empty)
                            {
                                output.Append(nestsOutput);
                                spewOutput = true;
                            }
                        }
                        catch (Exception e)
                        {
                            output.Append("\r\n" + UDecompilingState.Tabs + "// Failed to format nests!:"
                                          + e + "\r\n"
                                          + UDecompilingState.Tabs + "// " + _Nester.Nests.Count + " & "
                                          + _Nester.Nests[_Nester.Nests.Count - 1]);
                            spewOutput = true;
                        }
                    }

                    try
                    {
                        // Decompile remaining nests
                        output.Append(DecompileNests(true));
                    }
                    catch (Exception e)
                    {
                        output.Append("\r\n" + UDecompilingState.Tabs
                                             + "// Failed to format remaining nests!:" + e + "\r\n"
                                             + UDecompilingState.Tabs + "// " + _Nester.Nests.Count + " & "
                                             + _Nester.Nests[_Nester.Nests.Count - 1]);
                    }
                }
                catch (Exception e)
                {
                    output.Append(UDecompilingState.Tabs);
                    output.AppendLine("// Cannot recover from this decompilation error.");
                    output.Append(UDecompilingState.Tabs);
                    output.AppendLine($"// Error: {FormatTabs(e.Message)}");
                    output.Append(UDecompilingState.Tabs);
                    output.AppendLine($"// Token Index: {CurrentTokenIndex} / {DeserializedTokens.Count}");
                }
                finally
                {
                    UDecompilingState.Tabs = initTabs;
                }

                return output.ToString();
            }

            private readonly List<NestManager.Nest> _NestChain = new List<NestManager.Nest>();

            private static string FormatTabs(string nonTabbedText)
            {
                return nonTabbedText.Replace("\n", "\n" + UDecompilingState.Tabs);
            }

            public static string FormatTokenInfo(Token token)
            {
                Debug.Assert(token != null);
                return $"{token.GetType().Name} (0x{token.RepresentToken:X2})";
            }

            private string FormatAndDecompileTokens(int beginIndex, int endIndex)
            {
                var output = string.Empty;
                for (int i = beginIndex; i < endIndex && i < DeserializedTokens.Count; ++i)
                {
                    var t = DeserializedTokens[i];
                    try
                    {
                        output += "\r\n" + UDecompilingState.Tabs +
                                  $"{FormatTokenInfo(t)} << {t.Decompile()}";
                    }
                    catch (Exception e)
                    {
                        output += "\r\n" + UDecompilingState.Tabs +
                                  $"{FormatTokenInfo(t)} << {e.GetType().FullName}";
                        try
                        {
                            output += "\r\n" + UDecompilingState.Tabs + "(";
                            UDecompilingState.AddTab();
                            string inlinedTokens = FormatAndDecompileTokens(i + 1, CurrentTokenIndex);
                            UDecompilingState.RemoveTab();
                            output += inlinedTokens
                                      + "\r\n" + UDecompilingState.Tabs
                                      + ")";
                        }
                        finally
                        {
                            i += CurrentTokenIndex - beginIndex;
                        }
                    }
                }

                return output;
            }

            private string DecompileLabelForToken(Token token, bool appendNewline)
            {
                var output = new StringBuilder();
                int labelIndex = _TempLabels.FindIndex((l) => l.entry.Position == token.Position);
                if (labelIndex == -1) return string.Empty;

                var labelEntry = _TempLabels[labelIndex].entry;
                bool isStateLabel = !labelEntry.Name.StartsWith("J0x", StringComparison.Ordinal);
                string statementOutput = isStateLabel
                    ? $"{labelEntry.Name}:\r\n"
                    : $"{UDecompilingState.Tabs}{labelEntry.Name}:";
                if (appendNewline) output.Append("\r\n");

                output.Append(statementOutput);

                _TempLabels.RemoveAt(labelIndex);
                return output.ToString();
            }

            private string DecompileNests(bool outputAllRemainingNests = false)
            {
                var output = string.Empty;

                // Give { priority hence separated loops
                for (var i = 0; i < _Nester.Nests.Count; ++i)
                {
                    if (!(_Nester.Nests[i] is NestManager.NestBegin))
                        continue;

                    if (_Nester.Nests[i].IsPastOffset((int)CurrentToken.Position) || outputAllRemainingNests)
                    {
                        output += _Nester.Nests[i].Decompile();
                        UDecompilingState.AddTab();

                        _NestChain.Add(_Nester.Nests[i]);
                        _Nester.Nests.RemoveAt(i--);
                    }
                }

                for (int i = _Nester.Nests.Count - 1; i >= 0; i--)
                    if (_Nester.Nests[i] is NestManager.NestEnd nestEnd
                        && (outputAllRemainingNests ||
                            nestEnd.IsPastOffset((int)CurrentToken.Position + CurrentToken.Size)))
                    {
                        var topOfStack = _NestChain[_NestChain.Count - 1];
                        if (topOfStack.Type == NestManager.Nest.NestType.Default &&
                            nestEnd.Type != NestManager.Nest.NestType.Default)
                        {
                            // Automatically close default when one of its outer nest closes
                            output += $"\r\n{UDecompilingState.Tabs}break;";
                            UDecompilingState.RemoveTab();
                            _NestChain.RemoveAt(_NestChain.Count - 1);

                            // We closed off the last case, it's safe to close of the switch as well
                            if (nestEnd.Type != NestManager.Nest.NestType.Switch)
                            {
                                var switchScope = _NestChain[_NestChain.Count - 1];
                                if (switchScope.Type == NestManager.Nest.NestType.Switch)
                                {
                                    output += $"\r\n{UDecompilingState.Tabs}}}";
                                    UDecompilingState.RemoveTab();
                                    _NestChain.RemoveAt(_NestChain.Count - 1);
                                }
                                else
                                {
                                    output += $"/* Tried to find Switch scope, found {switchScope.Type} instead */";
                                }
                            }
                        }

                        UDecompilingState.RemoveTab();
                        output += nestEnd.Decompile();

                        topOfStack = _NestChain[_NestChain.Count - 1];
                        if (topOfStack.Type != nestEnd.Type)
                            output += $"/* !MISMATCHING REMOVE, tried {nestEnd.Type} got {topOfStack}! */";
                        _NestChain.RemoveAt(_NestChain.Count - 1);

                        _Nester.Nests.RemoveAt(i);
                        if (nestEnd.HasElseNest != null)
                        {
                            output += $"\r\n{UDecompilingState.Tabs}else{UnrealConfig.PrintBeginBracket()}";
                            UDecompilingState.AddTab();
                            var begin = new NestManager.NestBegin
                            {
                                Type = NestManager.Nest.NestType.Else,
                                Creator = nestEnd.HasElseNest,
                                Position = nestEnd.Position
                            };
                            var end = new NestManager.NestEnd
                            {
                                Type = NestManager.Nest.NestType.Else,
                                Creator = nestEnd.HasElseNest,
                                Position = nestEnd.HasElseNest.CodeOffset
                            };
                            _Nester.Nests.Add(end);
                            _NestChain.Add(begin);
                        }
                    }

                return output;
            }

            #endregion

            #region Disassemble

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
            public string Disassemble()
            {
                return string.Empty;
            }

            #endregion

#endif
        }
    }
}