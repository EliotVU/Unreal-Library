using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UELib.Branch;
using UELib.Core.Tokens;
using UELib.IO;
using UELib.Tokens;

namespace UELib.Core;

public sealed class UByteCodeScript(UStruct source, int memorySize, int storageSize) : IUnrealSerializableClass
{
    /// <summary>
    ///     Size of FName in memory (int Index, (>= 343) int Number).
    /// </summary>
    private byte _NameMemorySize = sizeof(int);

    /// <summary>
    ///     Size of a pointer to an UObject in memory.
    ///     32bit, 64bit as of version 587 (even on 32bit platforms)
    /// </summary>
    private byte _ObjectMemorySize = sizeof(int);

    /// <summary>
    ///     The current in memory position relative to the first byte-token.
    /// </summary>
    private int _MemoryPosition;

    private long _StoragePosition;
    private TokenFactory _TokenFactory;

    public int MemorySize { get; private set; } = memorySize;
    public int StorageSize { get; private set; } = storageSize;

    public List<UStruct.UByteCodeDecompiler.Token> Tokens { get; } = [];
    public List<UStruct.UByteCodeDecompiler.Token> Statements { get; } = [];

    public UStruct Source => source;

    public UByteCodeScript(UStruct source, List<UStruct.UByteCodeDecompiler.Token> statements) : this(source, 0, 0)
    {
        Statements = statements ?? throw new ArgumentNullException(nameof(statements));
    }

    /// <summary>
    /// Deserializes the script from a stream.
    /// </summary>
    /// <param name="stream">The input stream.</param>
    public void Deserialize(IUnrealStream stream)
    {
        _MemoryPosition = 0;
        _StoragePosition = stream.Position;
        _TokenFactory = stream.Package.Branch.GetTokenFactory(stream.Package);

        CacheMemorySizes(stream);
        var scriptStream = new UnrealByteCodeStream(stream, this);

        while (_MemoryPosition < MemorySize)
        {
            var token = DeserializeNextToken(scriptStream);
            Statements.Add(token);
        }

        Services.LibServices.LogService.SilentAssert(
            _MemoryPosition == MemorySize,
            $"ByteCodeScript '{Source.Name}' has an invalid memory size. Expected: {MemorySize}, Actual: {_MemoryPosition}");
    }

    /// <summary>
    /// Serializes the script to a stream.
    /// </summary>
    /// <param name="stream">The output stream.</param>
    public void Serialize(IUnrealStream stream)
    {
        _MemoryPosition = 0;
        _StoragePosition = stream.Position;
        _TokenFactory = stream.Package.Branch.GetTokenFactory(stream.Package);

        CacheMemorySizes(stream);
        var scriptStream = new UnrealByteCodeStream(stream, this);

        // Serialize using statements, because each token will handle the serialization of the next tokens.
        foreach (var token in Statements)
        {
            SerializeToken(scriptStream, token);
        }

        MemorySize = _MemoryPosition;
        StorageSize = (int)(stream.Position - _StoragePosition);
    }

    internal void DeserializeNextDebugToken(IUnrealStream stream)
    {
        // Sometimes we may end up at the end of a script
        // -- and by coincidence pickup a DebugInfo byte-code outside the script-boundary.
        if (_MemoryPosition + sizeof(byte) + sizeof(int) >= MemorySize)
        {
            return;
        }

        long p = stream.Position;
        byte opCode = stream.ReadByte();

        // Let's avoid converting native calls to a token type ;D
        if (opCode >= _TokenFactory.ExtendedNative)
        {
            stream.Position = p;

            return;
        }

        int version = 0;

        var tokenType = _TokenFactory.GetTokenTypeFromOpCode(opCode);
        if (tokenType == typeof(UStruct.UByteCodeDecompiler.DebugInfoToken))
        {
            // Sometimes we may catch a false positive,
            // e.g. A FinalFunction within an Iterator may expect a debug token and by mere coincidence match the Iterator CodeOffset.
            // So let's verify the next 4 bytes too.
            version = stream.ReadInt32();
        }

        stream.Position = p;
        if (version == 100)
        {
            // Expecting a DebugInfo token.
            DeserializeNextToken(stream);
        }
    }

    private UStruct.UByteCodeDecompiler.Token DeserializeNextOpCodeToToken(IUnrealStream stream)
    {
        byte opCode = stream.ReadByte();
        AlignSize(sizeof(byte));

        if (opCode < _TokenFactory.ExtendedNative)
        {
            return _TokenFactory.CreateToken<UStruct.UByteCodeDecompiler.Token>(opCode);
        }

        if (opCode >= _TokenFactory.FirstNative)
        {
            return _TokenFactory.CreateNativeToken(opCode);
        }

        byte opCodeExtension = stream.ReadByte();
        AlignSize(sizeof(byte));

        var nativeIndex = (ushort)(((opCode - _TokenFactory.ExtendedNative) << 8) | opCodeExtension);
        Debug.Assert(nativeIndex < (ushort)ExprToken.MaxNative);

        return _TokenFactory.CreateNativeToken(nativeIndex);
    }

    internal T DeserializeNextToken<T>(IUnrealStream stream) where T : UStruct.UByteCodeDecompiler.Token
    {
        return (T)DeserializeNextToken(stream);
    }

    internal UStruct.UByteCodeDecompiler.Token DeserializeNextToken(IUnrealStream stream)
    {
        int tokenMemoryPosition = _MemoryPosition;
        int storagePosition = (int)(stream.Position - Source.ScriptOffset);
        var token = DeserializeNextOpCodeToToken(stream);

        Tokens.Add(token);
        token.Script = this;
        token.Position = tokenMemoryPosition;
        token.StoragePosition = storagePosition;
        token.Deserialize(stream);
        token.Size = (short)(_MemoryPosition - tokenMemoryPosition);
        token.StorageSize = (short)(stream.Position - Source.ScriptOffset - token.StoragePosition);

        return token;
    }

    internal void SerializeToken(IUnrealStream stream, UStruct.UByteCodeDecompiler.Token token)
    {
        token.Script = this;

        int tokenMemoryPosition = _MemoryPosition;
        int storagePosition = (int)(stream.Position - Source.ScriptOffset);

        if (token is not UStruct.UByteCodeDecompiler.NativeFunctionToken nativeFunctionToken)
        {
            stream.Write(token.OpCode);
            AlignSize(sizeof(byte));
        }
        else
        {
            int opCode = nativeFunctionToken.NativeToken;
            if (opCode < 256)
            {
                stream.Write((byte)opCode);
                AlignSize(sizeof(byte));
            }
            else
            {
                stream.Write((byte)(opCode / 256 + _TokenFactory.ExtendedNative));
                stream.Write((byte)(opCode % 256));
                AlignSize(sizeof(ushort));
            }
        }

        token.Position = tokenMemoryPosition;
        token.StoragePosition = storagePosition;
        token.Serialize(stream);
        token.Size = (short)(_MemoryPosition - tokenMemoryPosition);
        token.StorageSize = (short)(stream.Position - Source.ScriptOffset - token.StoragePosition);
    }

    internal T SerializeToken<T>(IUnrealStream stream, ExprToken tokenKind)
    {
        throw new NotImplementedException();
    }

    internal void SerializeDebugToken(IUnrealStream stream, DebugInfo debugKind)
    {
        // Do nothing, we won't serialize debug tokens.
        return;

        // TODO: Reverse-mapping, create the correct token for each engine branch.
        var token = _TokenFactory.CreateToken<UStruct.UByteCodeDecompiler.DebugInfoToken>(0x41);
        token.Version = 100;
        token.Line = 0; // not possible, no source code available
        token.TextPos = 0; // not possible, no source code available
        token.OpCode = debugKind;
        SerializeToken(stream, token);
    }

    private void CacheMemorySizes(IUnrealStream stream)
    {
#if BIOSHOCK
        if (stream.Build == UnrealPackage.GameBuild.BuildName.BioShock)
        {
            _NameMemorySize = sizeof(int) + sizeof(int);

            return;
        }
#endif
        if (stream.Version >= (uint)PackageObjectLegacyVersion.NumberAddedToName)
        {
            _NameMemorySize = sizeof(int) + sizeof(int);
        }
#if TERA
        // Tera's reported version is false (partial upgrade?)
        if (stream.Build == UnrealPackage.GameBuild.BuildName.Tera)
        {
            return;
        }
#endif
#if GOWUE
        if (stream.Build == UnrealPackage.GameBuild.BuildName.GoWUE)
        {
            return;
        }
#endif
        if (stream.Version >= 587)
        {
            _ObjectMemorySize = sizeof(long);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AlignSize(int size)
    {
        _MemoryPosition += size;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AlignNameSize()
    {
        _MemoryPosition += _NameMemorySize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AlignObjectSize()
    {
        _MemoryPosition += _ObjectMemorySize;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal UName ReadNameAligned(IUnrealStream stream)
    {
#if BATMAN
        // (Only for byte-codes) No int32 numeric followed after a name index for Batman4
        if (stream.Build == UnrealPackage.GameBuild.BuildName.Batman4)
        {
            return ReadNumberlessNameAligned(stream);
        }
#endif
        var name = stream.ReadName();
        AlignNameSize();

        return name;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WriteNameAligned(IUnrealStream stream, in UName value)
    {
#if BATMAN
        // (Only for byte-codes) No int32 numeric followed after a name index for Batman4
        if (stream.Build == UnrealPackage.GameBuild.BuildName.Batman4)
        {
            WriteNumberlessNameAligned(stream, value);
        }
#endif
        stream.WriteName(value);
        AlignNameSize();
    }

    /// <summary>
    /// FIXME: No name reference tracking when using this method.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal UName ReadNumberlessNameAligned(IUnrealStream stream)
    {
        Debug.Assert(stream.Build == UnrealPackage.GameBuild.BuildName.Batman4);

        int nameIndex = stream.ReadInt32();
        AlignSize(sizeof(int));

        var name = new UName(stream.Package.Names[nameIndex]);
        return name;
    }

    /// <summary>
    /// FIXME: No name reference tracking when using this method.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void WriteNumberlessNameAligned(IUnrealStream stream, in UName value)
    {
        Debug.Assert(stream.Build == UnrealPackage.GameBuild.BuildName.Batman4);

        // Yeah, obviously this is not the best way to do this.
        int nameHash = value.Index;
        int nameIndex = stream.Package.Names.FindIndex(entry => entry.IndexName!.Index == nameHash);
        stream.Write(nameIndex);
        AlignSize(sizeof(int));
    }
}
