using System.Diagnostics.CodeAnalysis;

namespace UELib.Core
{
    /// <summary>
    /// Implements FStateFrame.
    /// </summary>
    [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
    public class UStateFrame : IUnrealSerializableClass
    {
        // FIXME: Version, added somewhere between 186 ... 230
        private const int VStateStack = 187;
        private const int VLatentActionReduced = 566;

        public UStruct Node;
        public UState StateNode;
        public ulong ProbeMask;
        public uint LatentAction;

        public UArray<PushedState> StateStack;
        public int Offset;

        public void Deserialize(IUnrealStream stream)
        {
            Node = stream.ReadObject<UStruct>();
            StateNode = stream.ReadObject<UState>();
            ProbeMask = stream.Version < UState.VProbeMaskReducedAndIgnoreMaskRemoved
                ? stream.ReadUInt64()
                : stream.ReadUInt32();
            LatentAction = stream.Version < VLatentActionReduced
                ? stream.ReadUInt32()
                : stream.ReadUInt16();
            if (stream.Package.Build == UnrealPackage.GameBuild.BuildName.DNF &&
                stream.LicenseeVersion >= 25)
            {
                uint dnfUInt32 = stream.ReadUInt32();
            }
            if (stream.Version >= VStateStack) stream.ReadArray(out StateStack);
            if (Node != null) Offset = stream.ReadIndex();
        }

        public void Serialize(IUnrealStream stream)
        {
            stream.Write(Node);
            stream.Write(StateNode);
            stream.Write(stream.Version < UState.VProbeMaskReducedAndIgnoreMaskRemoved
                ? ProbeMask
                : (uint)ProbeMask
            );
            stream.Write(stream.Version < VLatentActionReduced
                ? LatentAction
                : (ushort)LatentAction
            );
            if (stream.Version >= VStateStack) stream.Write(ref StateStack);
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