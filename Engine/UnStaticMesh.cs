using System;
using UELib.Core;


namespace UELib.Engine
{
    [UnrealRegisterClass]
    public class UStaticMesh : UPrimative, IUnrealViewable
    {
        public UArray<SMeshSurface> Surfices { get; private set; }
        public Box another_bb;
        public UArray<SMeshVertex> Verteces { get; private set; }
        public UArray<UColor> vertex_colors_1 { get; private set; }
        public UArray<UColor> vertex_colors_2 { get; private set; }
        public UArray<SMeshCoords> texture_coords { get; private set; }
        public ushort[] vertex_indicies_1;
        public ushort[] vertex_indicies_2;

        public UStaticMesh()
        {
            ShouldDeserializeOnDemand = true;
        }
        protected override void Deserialize()
        {

            base.Deserialize();

            Surfices = new UArray<SMeshSurface>();
            Surfices.Deserialize(_Buffer, delegate(SMeshSurface mm) { mm.Owner = this; });
            another_bb = new Box(_Buffer);
            Verteces = new UArray<SMeshVertex>();
            Verteces.Deserialize(_Buffer, delegate(SMeshVertex mm) { mm.Owner = this; });
            _Buffer.ReadInt32();
            vertex_colors_1 = new UArray<UColor>();
            vertex_colors_1.Deserialize(_Buffer, delegate(UColor mm) { mm.Owner = this; });
            _Buffer.ReadInt32();
            vertex_colors_2 = new UArray<UColor>();
            vertex_colors_2.Deserialize(_Buffer, delegate(UColor mm) { mm.Owner = this; });
            _Buffer.ReadInt32();
            texture_coords = new UArray<SMeshCoords>();
            texture_coords.Deserialize(_Buffer, delegate(SMeshCoords mm) { mm.Owner = this; });
            int size = _Buffer.ReadIndex();
            vertex_indicies_1 = new ushort[size];
            for (int i = 0; i < size; i++)
            {
                vertex_indicies_1[i] = _Buffer.ReadUInt16();
            }
            _Buffer.ReadInt32();
            size = _Buffer.ReadIndex();
            vertex_indicies_2 = new ushort[size];
            for (int i = 0; i < size; i++)
            {
                vertex_indicies_2[i] = _Buffer.ReadUInt16();
            }
            _Buffer.ReadInt32();
            foreach (UDefaultProperty property in Properties)
            {
                property._Buffer.Position = property._ValueOffset;
                switch (property.Name)
                {
                    case "Materials":
                       var str =  property.Decompile();
                        Console.WriteLine(str);
                        break;
                    default:
                        break;
                }
                //property.Deserialize();
                Console.WriteLine();
            }

        }
    }
    public class SMeshCoord : IUnrealSerializableClass
    {
        public float u, v;
        public SMeshCoords Owner;
        public void Serialize(IUnrealStream stream)
        {
            throw new NotImplementedException();

        }
        public void Deserialize(IUnrealStream stream)
        {
            u = stream.ReadFloat();
            v = stream.ReadFloat();
        }
    }
    public class SMeshVertex : IUnrealSerializableClass
    {
        public UVector location;
        public UVector normal;
        public UStaticMesh Owner;
        public void Serialize(IUnrealStream stream)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(IUnrealStream stream)
        {
            location = new UVector(stream);
            normal = new UVector(stream);
        }
    }
    public class SMeshCoords : IUnrealSerializableClass
    {
        public UArray<SMeshCoord> elements { get; private set; }
        public UStaticMesh Owner;
        public void Serialize(IUnrealStream stream)
        {
            throw new NotImplementedException();
        }
        public void Deserialize(IUnrealStream stream)
        {
            elements = new UArray<SMeshCoord>();
            elements.Deserialize(stream, delegate(SMeshCoord mm) { mm.Owner = this; });
            stream.ReadInt32();
            stream.ReadInt32();
        }
    }
    public class UColor : IUnrealSerializableClass
    {
        public byte R;
        public byte G;
        public byte B;
        public byte A;
        public UStaticMesh Owner;
        public void Serialize(IUnrealStream stream)
        {
            throw new NotImplementedException();
        }
        public void Deserialize(IUnrealStream stream)
        {
            B = stream.ReadByte();
            G = stream.ReadByte();
            R = stream.ReadByte();
            A = stream.ReadByte();
        }
    }
    public class UVector
    {
        public float X;
        public float Y;
        public float Z;
        public UVector(UDefaultProperty property)
        {
            property._Buffer.Position = property._ValueOffset;
            X = property._Buffer.ReadFloat();
            Y = property._Buffer.ReadFloat();
            Z = property._Buffer.ReadFloat();
        }
        public UVector(IUnrealStream stream)
        {
            X = stream.ReadFloat();
            Y = stream.ReadFloat();
            Z = stream.ReadFloat();
        }

    }
    public class SMeshSurface : IUnrealSerializableClass
    {
        private int unknown;
        public short index_offset;
        private short unknown01;
        public short vertex_max;
        public short triangle_count;
        public short triangle_max;
        public UStaticMesh Owner;
        public void Serialize(IUnrealStream stream)
        {
            throw new NotImplementedException();
        }

        public void Deserialize(IUnrealStream stream)
        {
            unknown = stream.ReadInt32();
            index_offset = stream.ReadInt16();
            unknown01 = stream.ReadInt16();
            vertex_max = stream.ReadInt16();
            triangle_count = stream.ReadInt16();
            triangle_max = stream.ReadInt16();

        }
    }
}
