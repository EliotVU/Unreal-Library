using System;
using System.Diagnostics;
using UELib.Decoding;
using UELib.Services;

namespace UELib.Branch.UE2.AA2
{
    // Only initialized for packages with LicenseeVersion >= 33
    public class PackageSerializerAA2 : PackageSerializerBase
    {
        private const int MaxNameLengthUE2 = 64;

        public override void Serialize(IUnrealStream stream, UNameTableItem item)
        {
            if (stream.Decoder is CryptoDecoderAA2)
                throw new NotSupportedException("Can't serialize encrypted name entries");
            
            item.Serialize(stream);
        }

        // Note: Names are not encrypted in AAA/AAO 2.6 (LicenseeVersion 32)
        public override void Deserialize(IUnrealStream stream, UNameTableItem item)
        {
            if (!(stream.Decoder is CryptoDecoderAA2))
            {
                // Fallback to the default implementation
                item.Deserialize(stream);
                return;
            }

            // Thanks to @gildor2, decryption code transpiled from https://github.com/gildor2/UEViewer, 
            int length = stream.ReadIndex();
            Debug.Assert(length < 0);
            int size = -length;

            const byte n = 5;
            byte shift = n;
            var buffer = new char[size];
            for (var i = 0; i < size; i++)
            {
                ushort c = stream.ReadUInt16();
                ushort c2 = CryptoCore.RotateRight(c, shift);
                Debug.Assert(c2 < byte.MaxValue);
                buffer[i] = (char)(byte)c2;
                shift = (byte)((c - n) & 0x0F);
            }

            var name = new string(buffer, 0, buffer.Length - 1);
            Debug.Assert(name.Length <= MaxNameLengthUE2, "Maximum name length exceeded! Possible corrupt or unsupported package.");
            // Part of name ?
            int number = stream.ReadIndex();
            //Debug.Assert(number == 0, "Unknown value");
            
            item.Name = name;
            item.Flags = stream.ReadUInt32();
        }

        public override void Serialize(IUnrealStream stream, UImportTableItem item)
        {
            stream.Write(item.ClassPackageName);
            stream.Write(item.ClassName);

            LibServices.LogService.SilentException(
                new NotSupportedException("Missing a byte at " + stream.Position));
            stream.Skip(1);

            //stream.Write((byte)0);
            stream.Write(item.ObjectName);
            stream.Write(item.OuterIndex);
        }

        public override void Deserialize(IUnrealStream stream, UImportTableItem item)
        {
            item.ClassPackageName = stream.ReadNameReference();
            item.ClassName = stream.ReadNameReference();
            byte unkByte = stream.ReadByte();
            Debug.WriteLine(unkByte, "unkByte");
            item.ObjectName = stream.ReadNameReference();
            item.OuterIndex = stream.ReadInt32();
        }

        public override void Serialize(IUnrealStream stream, UExportTableItem item)
        {
            stream.WriteIndex(item.SuperIndex);

            LibServices.LogService.SilentException(
                new NotSupportedException("Missing an integer at " + stream.Position));
            stream.Skip(4);

            stream.WriteIndex(item.ClassIndex);
            stream.Write(item.OuterIndex);
            stream.Write(~(uint)item.ObjectFlags);
            stream.Write(item.ObjectName);
            stream.WriteIndex(item.SerialSize );
            if (item.SerialSize > 0)
            {
                stream.WriteIndex(item.SerialOffset);
            }
        }

        public override void Deserialize(IUnrealStream stream, UExportTableItem item)
        {
            item.SuperIndex = stream.ReadIndex();
            int unkInt = stream.ReadInt32();
            Debug.WriteLine(unkInt, "unkInt");
            item.ClassIndex = stream.ReadIndex();
            item.OuterIndex = stream.ReadInt32();
            item.ObjectFlags = ~stream.ReadUInt32();
            item.ObjectName = stream.ReadNameReference();
            item.SerialSize = stream.ReadIndex();
            if (item.SerialSize > 0)
            {
                item.SerialOffset = stream.ReadIndex();
            }
        }
    }
}