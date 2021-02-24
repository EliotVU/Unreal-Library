#if BIOSHOCK
    namespace UELib.JsonDecompiler.Core
    {
        /// <summary>
        /// QWord Property
        /// </summary>
        [UnrealRegisterClass]
        public class UQWordProperty : UIntProperty
        {
            /// <inheritdoc/>
            public override string GetFriendlyType()
            {
                return "Qword";
            }
        }
    }
#endif