using System.Diagnostics;
using UELib.Branch;
using UELib.Core;
using UELib.ObjectModel.Annotations;

namespace UELib.Engine
{
    /// <summary>
    ///     Implements UPalette/Engine.Palette
    /// </summary>
    [UnrealRegisterClass]
    public class UPalette : UObject, IUnrealViewable
    {
        #region Serialized Members

        /// <summary>
        /// No alpha was serialized for packages of version 65 or less.
        /// </summary>
        [StreamRecord]
        public UArray<UColor> Colors { get; set; } = [];

#if UNDYING
        [StreamRecord, Build(UnrealPackage.GameBuild.BuildName.Undying)]
        public bool HasAlphaChannel { get; set; }
#endif

        #endregion

        public UPalette()
        {
            ShouldDeserializeOnDemand = true;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            // This could be a lot faster with a fixed array, but it's not a significant class of interest.
            int count = stream.ReadLength();
            Debug.Assert(count == 256);

            Colors = stream.ReadArray<UColor>(count);
            stream.Record(nameof(Colors), Colors);
#if UNDYING
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Undying &&
                stream.Version >= 75)
            {
                HasAlphaChannel = stream.ReadBool(); // v28
                stream.Record(nameof(HasAlphaChannel), HasAlphaChannel);
            }
#endif
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            Debug.Assert(Colors.Count == 256);
            stream.WriteArray(Colors);
#if UNDYING
            if (stream.Build == UnrealPackage.GameBuild.BuildName.Undying &&
                stream.Version >= 75)
            {
                stream.Write(HasAlphaChannel); // v28
            }
#endif
        }
    }
}
