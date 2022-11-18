namespace UELib.Core
{
    /// <summary>
    /// Proceeded by a string expression.
    /// </summary>
    public class EatStringToken : UStruct.UByteCodeDecompiler.Token
    {
        public override void Deserialize(IUnrealStream stream)
        {
            DeserializeNext();
        }

        public override string Decompile()
        {
            return DecompileNext();
        }
    }
}
