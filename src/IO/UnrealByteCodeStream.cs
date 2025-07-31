using System.IO;
using UELib.Core;

namespace UELib.IO;

internal interface IUnrealByteCodeStream : IUnrealStream
{
    public T ReadToken<T>() where T : UStruct.UByteCodeDecompiler.Token;
    public void WriteToken<T>(T token) where T : UStruct.UByteCodeDecompiler.Token;
}

// Ideally we could displace AlignSize calls with overriden read methods; but, for that work we would also have to proxy the BinaryReader...
public sealed class UnrealByteCodeStream(IUnrealStream baseStream, UByteCodeScript script) : UnrealProxyStream(baseStream), IUnrealByteCodeStream
{
    public T ReadToken<T>() where T : UStruct.UByteCodeDecompiler.Token
    {
        return (T)script.DeserializeNextToken(this);
    }

    public void WriteToken<T>(T token) where T : UStruct.UByteCodeDecompiler.Token
    {
        token.Serialize(this);
    }

    public T? ReadObject<T>() where T : UObject
    {
        //script.AlignObjectSize();
        return base.ReadObject<T>();
    }

    public UName ReadName()
    {
        //script.AlignNameSize();
        return base.ReadName();
    }

    public void Skip(int bytes)
    {
        //script.AlignSize(bytes);
        base.Skip(bytes);
    }

    public long Seek(long offset, SeekOrigin origin)
    {
        return base.Seek(offset, origin);
    }

    public int Read(byte[] buffer, int index, int count)
    {
        //script.AlignSize(count);
        return base.Read(buffer, index, count);
    }
}