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
            return $"const {Name} = {Value.Trim()};";
        }
    }
}
#endif