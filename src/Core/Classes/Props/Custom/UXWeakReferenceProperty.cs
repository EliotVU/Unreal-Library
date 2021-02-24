#if BIOSHOCK
    namespace UELib.JsonDecompiler.Core
    {
        /// <summary>
        /// WeakReference Property
        /// </summary>
        [UnrealRegisterClass]
        public class UXWeakReferenceProperty : UObjectProperty
        {
            /// <inheritdoc/>
            public override string GetFriendlyType()
            {
                return base.GetFriendlyType() + "&";
            }
        }
    }
#endif