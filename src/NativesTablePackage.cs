﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UELib
{
    using Core;

    public sealed class NativeTableItem
    {
        public string Name;
        public byte OperPrecedence;
        public FunctionType Type;
        public int ByteToken;

        public NativeTableItem()
        {
        }

        public NativeTableItem(UFunction function)
        {
            if (function.IsPost())
            {
                Type = FunctionType.PostOperator;
            }
            else if (function.IsPre())
            {
                Type = FunctionType.PreOperator;
            }
            else if (function.IsOperator())
            {
                Type = FunctionType.Operator;
            }
            else
            {
                Type = FunctionType.Function;
            }

            OperPrecedence = function.OperPrecedence;
            ByteToken = function.NativeToken;
            Name = function.FriendlyName;
        }
    }

    public enum FunctionType : byte
    {
        Function = 1,
        Operator = 2,
        PreOperator = 3,
        PostOperator = 4,
        Max = PostOperator
    }

    public sealed class NativesTablePackage
    {
        private const uint Signature = 0x2C8D14F1;
        public const string Extension = ".NTL";

        public List<NativeTableItem> NativeTableList { get; set; } = [];
        public Dictionary<ushort, NativeTableItem> NativeTokenMap { get; set; }

        [Obsolete]
        public NativeTableItem FindTableItem(uint nativeIndex)
        {
            NativeTokenMap.TryGetValue((ushort)nativeIndex, out var item);
            return item;
        }

        public void Serialize(Stream stream)
        {
            using var binWriter = new BinaryWriter(stream);

            binWriter.Write(Signature);
            binWriter.Write(NativeTableList.Count);

            foreach (var item in NativeTableList)
            {
                binWriter.Write(item.Name);
                binWriter.Write(item.OperPrecedence);
                binWriter.Write((byte)item.Type);
                binWriter.Write(item.ByteToken);
            }
        }

        public void Deserialize(Stream stream)
        {
            using var binReader = new BinaryReader(stream);

            if (binReader.ReadUInt32() != Signature)
            {
                throw new FileLoadException($"File {stream} is not a NTL file!");
            }

            int count = binReader.ReadInt32();
            NativeTableList = new List<NativeTableItem>(count);
            for (var i = 0; i < count; ++i)
            {
                var item = new NativeTableItem
                {
                    Name = binReader.ReadString(),
                    OperPrecedence = binReader.ReadByte(),
                    Type = (FunctionType)binReader.ReadByte(),
                    ByteToken = binReader.ReadInt32()
                };
                // Avoid duplicates to prevent ToDictionary from throwing an exception.
                if (NativeTableList.Find(it => it.ByteToken == item.ByteToken) != null)
                {
                    continue;
                }

                NativeTableList.Add(item);
            }

            NativeTableList.Sort((nt1, nt2) => nt1.ByteToken.CompareTo(nt2.ByteToken));
            NativeTokenMap = NativeTableList.ToDictionary(item => (ushort)item.ByteToken);
        }

        [Obsolete("Use Serialize")]
        public void CreatePackage(string filePath)
        {
            using var stream = new FileStream(filePath + Extension, FileMode.Create, FileAccess.Write);
            Serialize(stream);
        }

        [Obsolete("Use Deserialize")]
        public void LoadPackage(string filePath)
        {
            using var stream = new FileStream(filePath + Extension, FileMode.Open, FileAccess.Read);
            Deserialize(stream);
        }
    }
}
