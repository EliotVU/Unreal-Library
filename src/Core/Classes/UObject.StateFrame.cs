using System;
using UELib.Branch;
using UELib.Services;

namespace UELib.Core;

public partial class UObject
{
    /// <summary>
    ///     Implements FStateFrame.
    /// </summary>
    public class UStateFrame : IUnrealSerializableClass
    {
        public uint LatentAction;
        public UStruct? Node;
        public int Offset;
        public ulong ProbeMask;
        public UState StateNode;

        public UArray<PushedState> StateStack;

        public void Deserialize(IUnrealStream stream)
        {
            // version >= 51
            Node = stream.ReadObject<UStruct?>();
            // version >= 51
            StateNode = stream.ReadObject<UState>();
            ProbeMask = stream.Version < (uint)PackageObjectLegacyVersion.ProbeMaskReducedAndIgnoreMaskRemoved
                ? stream.ReadUInt64()
                : stream.ReadUInt32();
            // version >= 55
            if (stream.Version >= (uint)PackageObjectLegacyVersion.StateFrameLatentActionReduced
#if SWRepublicCommando
                || (stream.Build == UnrealPackage.GameBuild.BuildName.SWRepublicCommando &&
                    stream.Version >= 156)
#endif
               )
            {
                LatentAction = stream.ReadUInt16();
            }
            else
            {
                LatentAction = stream.ReadUInt32();
            }
#if DNF
            if (stream.Build == UnrealPackage.GameBuild.BuildName.DNF &&
                stream.LicenseeVersion >= 25)
            {
                uint dnfUInt32 = stream.ReadUInt32();
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedStateStackToUStateFrame)
            {
                stream.ReadArray(out StateStack);
            }

            if (Node != null) Offset = stream.ReadIndex();
        }

        public void Serialize(IUnrealStream stream)
        {
            // version >= 51
            stream.Write(Node);
            // version >= 51
            stream.Write(StateNode);
            stream.Write(stream.Version < (uint)PackageObjectLegacyVersion.ProbeMaskReducedAndIgnoreMaskRemoved
                             ? ProbeMask
                             : (uint)ProbeMask
            );
            // version >= 55
            if (stream.Version >= (uint)PackageObjectLegacyVersion.StateFrameLatentActionReduced
#if SWRepublicCommando
                || (stream.Build == UnrealPackage.GameBuild.BuildName.SWRepublicCommando &&
                    stream.Version >= 156)
#endif
               )
            {
                stream.Write((ushort)LatentAction);
            }
            else
            {
                stream.Write(LatentAction);
            }
#if DNF
            if (stream.Build == UnrealPackage.GameBuild.BuildName.DNF &&
                stream.LicenseeVersion >= 25)
            {
                LibServices.LogService.SilentException(new NotSupportedException("Unknown data"));
                stream.Write((uint)0);
            }
#endif
            if (stream.Version >= (uint)PackageObjectLegacyVersion.AddedStateStackToUStateFrame)
            {
                stream.Write(StateStack);
            }

            if (Node != null) stream.Write(Offset);
        }

        public struct PushedState : IUnrealSerializableClass
        {
            public UState State;
            public UStruct Node;
            public int Offset;

            public void Deserialize(IUnrealStream stream)
            {
                State = stream.ReadObject<UState>();
                Node = stream.ReadObject<UStruct>();
                Offset = stream.ReadIndex();
            }

            public void Serialize(IUnrealStream stream)
            {
                stream.Write(State);
                stream.Write(Node);
                stream.Write(Offset);
            }
        }
    }
}
