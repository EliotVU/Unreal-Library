using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UELib.Annotations;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace UELib.Core.Tokens
{
    [UsedImplicitly]
    public class TokenFactory
    {
        [NotNull] protected readonly TokenMap TokenMap;
        [NotNull] protected readonly Dictionary<ushort, NativeTableItem> NativeTokenMap;
        
        public readonly byte ExtendedNative;
        public readonly byte FirstNative;
        
        public TokenFactory(
            TokenMap tokenMap, 
            Dictionary<ushort, NativeTableItem> nativeTokenMap, 
            byte extendedNative, 
            byte firstNative)
        {
            TokenMap = tokenMap;
            Debug.Assert(TokenMap != null, $"{nameof(TokenMap)} cannot be null");

            NativeTokenMap = nativeTokenMap;
            Debug.Assert(NativeTokenMap != null, $"{nameof(NativeTokenMap)} cannot be null");

            ExtendedNative = extendedNative;
            FirstNative = firstNative;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [NotNull]
        public Type GetTokenTypeFromOpCode(byte opCode)
        {
            Debug.Assert(opCode < ExtendedNative, $"Unexpected native OpCode 0x{opCode:X2}");
            Debug.Assert(TokenMap.ContainsKey(opCode), $"OpCode 0x{opCode:X2} is missing from ${nameof(TokenMap)}");
            return TokenMap[opCode];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [NotNull]
        public T CreateToken<T>(byte opCode) 
            where T : Token
        {
            var tokenType = GetTokenTypeFromOpCode(opCode);
            var token = (T)Activator.CreateInstance(tokenType);
            token.OpCode = opCode;
            return token;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [NotNull]
        public static T CreateToken<T>(Type tokenType) 
            where T : Token
        {
            return (T)Activator.CreateInstance(tokenType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [NotNull]
        public NativeFunctionToken CreateNativeToken(ushort nativeIndex)
        {
            if (NativeTokenMap.TryGetValue(nativeIndex, out var item))
            {
                return new NativeFunctionToken
                {
                    NativeItem = item,
                };
            }
            
            return new NativeFunctionToken
            {
                NativeItem = new NativeTableItem
                {
                    Type = FunctionType.Function,
                    Name = CreateGeneratedName($"NFUN_{nativeIndex}"),
                    ByteToken = nativeIndex
                },
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string CreateGeneratedName(string id)
        {
            return $"__{id}__";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<ushort, NativeTableItem> FromPackage([CanBeNull] NativesTablePackage package)
        {
            return package?.NativeTokenMap ?? new Dictionary<ushort, NativeTableItem>();
        }
    }
}