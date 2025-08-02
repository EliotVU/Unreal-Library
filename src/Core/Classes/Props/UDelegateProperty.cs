using System;
using UELib.Branch;
using UELib.ObjectModel.Annotations;
using UELib.Types;

namespace UELib.Core
{
    /// <summary>
    ///     Implements UDelegateProperty/Core.DelegateProperty
    /// </summary>
    [UnrealRegisterClass]
    [BuildGenerationRange(BuildGeneration.UE2, BuildGeneration.UE4)]
    public class UDelegateProperty : UProperty
    {
        #region Serialized Members

        [StreamRecord]
        public UFunction? Function { get; set; }

        [StreamRecord]
        public UFunction? Delegate { get; set; }

        [StreamRecord]
        private UName? _DelegateSourceName;

        #endregion

        public UDelegateProperty()
        {
            Type = PropertyType.DelegateProperty;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            Function = stream.ReadObject<UFunction?>();
            stream.Record(nameof(Function), Function);

            if (stream.Version < (uint)PackageObjectLegacyVersion.AddedDelegateSourceToUDelegateProperty)
            {
                return;
            }

            if (stream.Version < (uint)PackageObjectLegacyVersion.ChangedDelegateSourceFromNameToObject)
            {
                _DelegateSourceName = stream.ReadName();
                stream.Record(nameof(_DelegateSourceName), _DelegateSourceName);

                if (_DelegateSourceName.Value.IsNone() == false)
                {
                    Delegate = Package.FindObject<UFunction>(_DelegateSourceName);
                    // Cannot find imported delegates 
                    //Debug.Assert(Delegate != null, $"Couldn't retrieve delegate source '{_DelegateSourceName}'");
                }
                else
                {
                    Delegate = null;
                }
            }
            else
            {
                Delegate = stream.ReadObject<UFunction?>();
                stream.Record(nameof(Delegate), Delegate);
            }
        }

        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);

            stream.Write(Function);

            if (stream.Version < (uint)PackageObjectLegacyVersion.AddedDelegateSourceToUDelegateProperty)
            {
                return;
            }

            if (stream.Version < (uint)PackageObjectLegacyVersion.ChangedDelegateSourceFromNameToObject)
            {
                // 'None' if Delegate is null?
                stream.Write(_DelegateSourceName ?? Delegate.Name);
            }
            else
            {
                // Upgrade the delegate source if it was set by name previously.
                if (_DelegateSourceName != null)
                {
                    if (_DelegateSourceName.Value.IsNone() == false)
                    {
                        Delegate = Package.FindObject<UFunction>(_DelegateSourceName);
                        if (Delegate == null)
                        {
                            throw new NotImplementedException(
                                $"Couldn't upgrade delegate source '{_DelegateSourceName}'");
                        }
                    }
                    else
                    {
                        Delegate = null;
                    }
                }

                stream.Write(Delegate);
            }
        }

        public override string GetFriendlyType()
        {
            return $"delegate<{GetFriendlyInnerType()}>";
        }

        public override string GetFriendlyInnerType()
        {
            return Function != null ? Function.GetFriendlyType() : _DelegateSourceName ?? "@NULL";
        }
    }
}
