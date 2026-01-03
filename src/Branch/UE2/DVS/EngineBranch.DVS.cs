using System.Diagnostics;
using UELib.Branch.UE2.DNF.Tokens;
using UELib.Branch.UE2.DVS.Tokens;
using UELib.Core;
using UELib.Core.Tokens;
using UELib.ObjectModel.Annotations;
using UELib.Tokens;

namespace UELib.Branch.UE2.DVS
{
    public class EngineBranchDVS : DefaultEngineBranch
    {
        public EngineBranchDVS(BuildGeneration generation) : base(BuildGeneration.UE2)
        {
        }

        protected override TokenMap BuildTokenMap(UnrealPackage package)
        {
            var tokenMap = base.BuildTokenMap(package);
            tokenMap[0x05] = typeof(SwitchToken);
            tokenMap[0x12] = typeof(ClassContextToken);
            tokenMap[0x19] = typeof(ContextToken);
            tokenMap[0x1B] = typeof(VirtualFunctionToken);
            tokenMap[0x1C] = typeof(FinalFunctionToken);
            tokenMap[0x38] = typeof(GlobalFunctionToken);
            tokenMap[0x43] = typeof(DelegateFunctionToken);
            tokenMap[0x46] = typeof(DynamicArrayEmptyToken);
            tokenMap[0x47] = typeof(UStruct.UByteCodeDecompiler.DynamicArraySortToken);
            tokenMap[0x48] = typeof(UStruct.UByteCodeDecompiler.ConditionalToken);
            tokenMap[0x49] = typeof(ColorConstToken);

            // cast token, 0x5B StructToString

            return tokenMap;
        }
    }

    [ExprToken(ExprToken.Switch)]
    public sealed class SwitchToken : UStruct.UByteCodeDecompiler.SwitchToken
    {
        // DVS: Missing PropertyType
        public override void Deserialize(IUnrealStream stream)
        {
            Expression = Script.DeserializeNextToken(stream);
        }

        public override void Serialize(IUnrealStream stream)
        {
            Debug.Assert(Expression != null);
            Script.SerializeToken(stream, Expression);
        }
    }

    [ExprToken(ExprToken.FinalFunction)]
    public sealed class FinalFunctionToken : UStruct.UByteCodeDecompiler.FinalFunctionToken
    {
        public ushort SkipSize { get; private set; }

        protected override void DeserializeCall(IUnrealStream stream)
        {
            SkipSize = stream.ReadUInt16();
            Script.AlignSize(sizeof(ushort));

            base.DeserializeCall(stream);
        }

        protected override void SerializeCall(IUnrealStream stream)
        {
            long skipSizePeek = stream.Position;
            stream.Write((ushort)0);
            Script.AlignSize(sizeof(ushort));

            int memorySize = Script.MemorySize;
            base.SerializeCall(stream);

            using (stream.Peek(skipSizePeek))
            {
                SkipSize = (ushort)(Script.MemorySize - memorySize);
                stream.Write(SkipSize);
            }
        }
    }

    [ExprToken(ExprToken.VirtualFunction)]
    public sealed class VirtualFunctionToken : UStruct.UByteCodeDecompiler.VirtualFunctionToken
    {
        public ushort SkipSize { get; private set; }

        protected override void DeserializeCall(IUnrealStream stream)
        {
            SkipSize = stream.ReadUInt16();
            Script.AlignSize(sizeof(ushort));

            base.DeserializeCall(stream);
        }

        protected override void SerializeCall(IUnrealStream stream)
        {
            long skipSizePeek = stream.Position;
            stream.Write((ushort)0);
            Script.AlignSize(sizeof(ushort));

            int memorySize = Script.MemorySize;
            base.SerializeCall(stream);

            using (stream.Peek(skipSizePeek))
            {
                SkipSize = (ushort)(Script.MemorySize - memorySize);
                stream.Write(SkipSize);
            }
        }
    }

    [ExprToken(ExprToken.GlobalFunction)]
    public sealed class GlobalFunctionToken : UStruct.UByteCodeDecompiler.GlobalFunctionToken
    {
        public ushort SkipSize { get; private set; }

        protected override void DeserializeCall(IUnrealStream stream)
        {
            SkipSize = stream.ReadUInt16();
            Script.AlignSize(sizeof(ushort));

            base.DeserializeCall(stream);
        }

        protected override void SerializeCall(IUnrealStream stream)
        {
            long skipSizePeek = stream.Position;
            stream.Write((ushort)0);
            Script.AlignSize(sizeof(ushort));

            int memorySize = Script.MemorySize;
            base.SerializeCall(stream);

            using (stream.Peek(skipSizePeek))
            {
                SkipSize = (ushort)(Script.MemorySize - memorySize);
                stream.Write(SkipSize);
            }
        }
    }

    [ExprToken(ExprToken.DelegateFunction)]
    public sealed class DelegateFunctionToken : UStruct.UByteCodeDecompiler.DelegateFunctionToken
    {
        public ushort SkipSize { get; private set; }

        protected override void DeserializeCall(IUnrealStream stream)
        {
            SkipSize = stream.ReadUInt16();
            Script.AlignSize(sizeof(ushort));

            base.DeserializeCall(stream);
        }

        protected override void SerializeCall(IUnrealStream stream)
        {
            long skipSizePeek = stream.Position;
            stream.Write((ushort)0);
            Script.AlignSize(sizeof(ushort));

            int memorySize = Script.MemorySize;
            base.SerializeCall(stream);

            using (stream.Peek(skipSizePeek))
            {
                SkipSize = (ushort)(Script.MemorySize - memorySize);
                stream.Write(SkipSize);
            }
        }
    }

    [ExprToken(ExprToken.Context)]
    public sealed class ContextToken : UStruct.UByteCodeDecompiler.ContextToken
    {
        public override void Deserialize(IUnrealStream stream)
        {
            // A.?
            ContextExpression = Script.DeserializeNextToken(stream);

            SkipSize = stream.ReadUInt16();
            Script.AlignSize(sizeof(ushort));

            // ?.B
            MemberExpression = Script.DeserializeNextToken(stream);
        }

        public override void Serialize(IUnrealStream stream)
        {
            Debug.Assert(ContextExpression != null);
            Script.SerializeToken(stream, ContextExpression);

            long skipSizePeek = stream.Position;
            stream.Write((ushort)0);
            Script.AlignSize(sizeof(ushort));
            int memorySize = Script.MemorySize;

            Debug.Assert(MemberExpression != null);
            Script.SerializeToken(stream, MemberExpression);

            using (stream.Peek(skipSizePeek))
            {
                SkipSize = (ushort)(Script.MemorySize - memorySize);
                stream.Write(SkipSize);
            }
        }
    }

    [ExprToken(ExprToken.ClassContext)]
    public sealed class ClassContextToken : UStruct.UByteCodeDecompiler.ClassContextToken
    {
        public ushort SkipSize { get; private set; }

        public override void Deserialize(IUnrealStream stream)
        {
            // A.?
            ContextExpression = Script.DeserializeNextToken(stream);

            SkipSize = stream.ReadUInt16();
            Script.AlignSize(sizeof(ushort));

            // ?.B
            MemberExpression = Script.DeserializeNextToken(stream);
        }

        public override void Serialize(IUnrealStream stream)
        {
            Debug.Assert(ContextExpression != null);
            Script.SerializeToken(stream, ContextExpression);

            long skipSizePeek = stream.Position;
            stream.Write((ushort)0);
            Script.AlignSize(sizeof(ushort));
            int memorySize = Script.MemorySize;

            Debug.Assert(MemberExpression != null);
            Script.SerializeToken(stream, MemberExpression);

            using (stream.Peek(skipSizePeek))
            {
                SkipSize = (ushort)(Script.MemorySize - memorySize);
                stream.Write(SkipSize);
            }
        }
    }
}
