﻿using System;
using System.Diagnostics.CodeAnalysis;
using UELib.Engine;

namespace UELib.Core
{
    /// <summary>
    /// Implements UTexture/Engine.Texture
    /// </summary>
    [UnrealRegisterClass]
    public class UTexture : UBitmapMaterial, IUnrealViewable
    {
        public UArray<MipMap> Mips;
        public bool HasComp;

        protected override void Deserialize()
        {
            base.Deserialize();

            if (_Buffer.Version > 160)
            {
                throw new NotSupportedException("UTexture is not supported for this build");
            }

            _Buffer.ReadArray(out Mips);
            Record(nameof(Mips), Mips);

            var bHasCompProperty = Properties.Find("bHasComp");
            if (bHasCompProperty != null)
            {
                HasComp = bool.Parse(bHasCompProperty.Value);
                if (HasComp)
                {
                    throw new NotSupportedException("UTexture of this kind is not supported");
                }
            }
        }

        [SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
        public struct MipMap : IUnrealSerializableClass
        {
            public byte[] Data;
            public int USize;
            public int VSize;
            public byte UBits;
            public byte VBits;

            public void Deserialize(IUnrealStream stream)
            {
                stream.ReadLazyArray(out Data);
                USize = stream.ReadInt32();
                VSize = stream.ReadInt32();
                UBits = stream.ReadByte();
                VBits = stream.ReadByte();
            }

            public void Serialize(IUnrealStream stream)
            {
                throw new NotImplementedException();
            }
        }
    }
}