using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using UELib.Annotations;
using static UELib.Core.UStruct.UByteCodeDecompiler;

namespace UELib.Core.Tokens
{
    [UsedImplicitly]
    public class TokenFactory
    {
        private NativeTokenResolver NativeTokenFunctionResolver => field ??= new NativeTokenResolver();

        protected readonly TokenMap TokenMap;
        protected readonly Dictionary<ushort, NativeTableItem> NativeTokenMap;

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
        public Type GetTokenTypeFromOpCode(byte opCode)
        {
            Debug.Assert(opCode < ExtendedNative, $"Unexpected native OpCode 0x{opCode:X2}");
            Debug.Assert(TokenMap.ContainsKey(opCode), $"OpCode 0x{opCode:X2} is missing from ${nameof(TokenMap)}");
            return TokenMap[opCode];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public byte GetOpCodeFromTokenType<T>()
            where T : Token
        {
            return GetOpCodeFromTokenType(typeof(T));
        }

        // Slow, but this will do for now.
        public byte GetOpCodeFromTokenType(Type tokenType)
        {
            foreach (var tokenPair in TokenMap)
            {
                if (tokenPair.Value == tokenType)
                {
                    return tokenPair.Key;
                }
            }

            throw new InvalidOperationException($"Token type {tokenType} not found in {nameof(TokenMap)}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T CreateToken<T>(byte opCode)
            where T : Token
        {
            var tokenType = GetTokenTypeFromOpCode(opCode);
            var token = (T)Activator.CreateInstance(tokenType);
            token.OpCode = opCode;

            return token;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T CreateToken<T>()
            where T : Token
        {
            return CreateToken<T>(typeof(T));
        }

        public T CreateToken<T>(Type tokenType)
            where T : Token
        {
            var token = (T)Activator.CreateInstance(tokenType);
            token.OpCode = GetOpCodeFromTokenType(tokenType);

            return token;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeUnresolvedFunctionToken CreateNativeToken(ushort nativeToken)
        {
            return new NativeUnresolvedFunctionToken(nativeToken);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeFinalFunctionToken CreateNativeToken(UFunction nativeFunction)
        {
            Contract.Assert(nativeFunction.NativeToken != 0);

            return new NativeFinalFunctionToken(nativeFunction);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public NativeTableItem CreateNativeItem(ushort nativeToken)
        {
            if (NativeTokenMap.TryGetValue(nativeToken, out var item))
            {
                return item;
            }

            return new NativeTableItem
            {
                Type = FunctionType.Function,
                Name = UnrealName.None,
                ByteToken = nativeToken
            };
        }

        /// <inheritdoc cref="NativeTokenResolver.TryResolveNativeToken"/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryResolveNativeToken(ushort nativeToken, UState state, out UFunction function)
        {
            return NativeTokenFunctionResolver.TryResolveNativeToken(nativeToken, state, out function);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string CreateGeneratedName(string id)
        {
            return $"__{id}__";
        }

        [Obsolete]
        public static Dictionary<ushort, NativeTableItem> FromPackage(NativesTablePackage? package)
        {
            return package?.NativeTokenMap ?? new Dictionary<ushort, NativeTableItem>();
        }

        private sealed class NativeTokenResolver
        {
            private readonly Dictionary<UStruct, Dictionary<ushort, UFunction>?> _FunctionCache = new(64);
            private readonly
#if NET5_0_OR_GREATER
                Lock
#else
        object
#endif
            _FunctionLock = new();

            /// <summary>
            /// Resolve a native token to a native <see cref="UFunction"/> lazily.
            ///
            /// i.e. On the first lookup of a token, the entire set of native functions in any <see cref="UState"/> will be collected and hashed.
            /// </summary>
            /// <param name="nativeToken">The native token to lookup.</param>
            /// <param name="state">The state and its inherited states to search through.</param>
            /// <param name="function">The resolved function, if any.</param>
            /// <returns><see langword="true"/> if successful, <see langword="false"/> otherwise.</returns>
            public bool TryResolveNativeToken(ushort nativeToken, UState state, out UFunction function)
            {
                // Reverse because it most likely to be found in /Core/Object.uc
                foreach (var superStruct in ((UState[])[state]).Concat(state.EnumerateSuper()).Reverse())
                {
                    // Lock caching for multithreaded decompilation.
                    lock (_FunctionLock)
                    {
                        UFunction? cachedNativeFunction;

                        if (_FunctionCache.TryGetValue(superStruct, out var stateFunctionCache))
                        {
                            // Pass it to the superStruct, this one didn't have any native functions in the last caching event.
                            if (stateFunctionCache == null)
                            {
                                continue;
                            }

                            if (stateFunctionCache.TryGetValue(nativeToken, out cachedNativeFunction) && cachedNativeFunction.NativeToken == nativeToken)
                            {
                                function = cachedNativeFunction;
                                return true;
                            }

                            continue;
                        }

                        var nativeFunctions = superStruct
                            .EnumerateFields<UFunction>()
                            .Where(field => field.NativeToken != 0)
                            .ToDictionary(field => field.NativeToken);

                        // Use null as an indicator for an empty cache.
                        if (nativeFunctions.Count == 0)
                        {
                            _FunctionCache.Add(superStruct, null);

                            continue;
                        }

                        _FunctionCache.Add(superStruct, nativeFunctions);
                        if (_FunctionCache[superStruct].TryGetValue(nativeToken, out cachedNativeFunction))
                        {
                            function = cachedNativeFunction;
                            return true;
                        }
                    }
                }

                // Search the outer class of a UState as well.
                // Eh, state never have any native(000) declarations...
                //if (state.Outer is UClass outerClass)
                //{
                //    ResolveNativeFunction(nativeToken, outerClass);
                //}

                function = null;
                return false;
            }
        }
    }
}
