using System;
using UELib.Core;

namespace UELib.Engine
{
    public class UPrimative : UObject
    {
        public Box bounding_box;
        public Sphere bounding_sphere;
        protected override void Deserialize()
        {
            base.Deserialize();
            bounding_box = new Box(_Buffer);
            bounding_sphere = new Sphere(_Buffer);
        }
    }
    public class Box
    {
        public UVector min;
        public UVector max;
        public byte is_valid;
        public Box(UObjectStream stream)
        {
            min = new UVector(stream);
            max = new UVector(stream);
            is_valid = stream.ReadByte();
        }
    }
    public class Sphere
    {
        public UVector location;
        public float radius;
        public Sphere(UObjectStream stream)
        {
            location = new UVector(stream);
            radius = stream.ReadFloat();
        }
    }
}
