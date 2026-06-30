#if DECOMPILE
namespace UELib.Core
{
    public partial class UConst
    {
        /// <summary>
        /// Decompiles this object into a text format of:
        ///
        /// const NAME = VALUE;
        /// </summary>
        /// <returns></returns>
        public override string Decompile()
        {
            if (!DeserializationState.HasFlag(ObjectState.Deserialized))
            {
                try { Load(); } catch { }
            }
            if (Value == null) return $"const {Name} = none;";
            return $"const {Name} = {Value.Trim()};";
        }
    }
}
#endif