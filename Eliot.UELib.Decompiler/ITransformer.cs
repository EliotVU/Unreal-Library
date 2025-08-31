namespace UELib.Decompiler;

public interface ITransformer<in TIn, out TOut>
{
    TOut Transform(TIn subject);
}
