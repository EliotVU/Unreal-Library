using UELib.Core;

namespace UELib;

/// <summary>
/// A provider for `native(000)` <see cref="UFunction"/>s.
/// </summary>
public sealed class UnrealNativeFunctionResolver
{
    private readonly Dictionary<UStruct, Dictionary<ushort, UFunction>?> _FunctionCache = new(64);
    private readonly
#if NET5_0_OR_GREATER
        Lock
#else
        object
#endif
    _FunctionLock = new();

    public bool TryResolveNativeFunction(ushort nativeToken, UState state, out UFunction function)
    {
        foreach (var superStruct in ((UState[])[state]).Concat(state.EnumerateSuper()))
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
