using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UELib.Core;
using UELib.UnrealScript;

namespace UELib.Branch.UE3.GIGANTIC.Core.Classes
{
    /// <summary>
    /// A JsonObject, serialized as a string / value instead of the typical index to UObject.
    /// </summary>
    public class UMoJsonObject : UObject, IUnrealSerializableClass
    {
        public enum MoJsonValueTypes : byte
        {
            MO_JSON_NULL,
            MO_JSON_INT,
            MO_JSON_STRING,
            MO_JSON_FLOAT,
            MO_JSON_BOOL,
            MO_JSON_ARRAY,
            MO_JSON_MAP,
            MO_JSON_MAX
        };

        public string JsonID;

        private byte _ValueType;
        public MoJsonValueTypes ValueType => (MoJsonValueTypes)_ValueType;

        public class JsonValueRef<T>(T data)
            where T : unmanaged
        {
            public T Data = data;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct JsonValue
        {
            [FieldOffset(0)] public JsonValueRef<int> Int;
            [FieldOffset(0)] public string String;
            [FieldOffset(0)] public JsonValueRef<float> Float;
            [FieldOffset(0)] public JsonValueRef<bool> Bool;
            [FieldOffset(0)] public UArray<UMoJsonObject> Array;
            [FieldOffset(0)] public UMap<string, UMoJsonObject> Map;
        }

        public JsonValue Value;

        public void Deserialize(IUnrealStream stream)
        {
            stream.Read(out JsonID);
            //stream.Record(nameof(JsonID), JsonID);

            stream.Read(out _ValueType);
            //stream.Record(nameof(ValueType), ValueType);

            switch (ValueType)
            {
                case MoJsonValueTypes.MO_JSON_NULL:
                    break;

                case MoJsonValueTypes.MO_JSON_INT:
                    stream.Read(out int intValue);
                    Value.Int = new JsonValueRef<int>(intValue);
                    //stream.Record(nameof(IntValue), IntValue);
                    break;

                case MoJsonValueTypes.MO_JSON_STRING:
                    stream.Read(out Value.String);
                    //stream.Record(nameof(StringValue), StringValue);
                    break;

                case MoJsonValueTypes.MO_JSON_FLOAT:
                    stream.Read(out float floatValue);
                    Value.Float = new JsonValueRef<float>(floatValue);
                    //stream.Record(nameof(FloatValue), FloatValue);
                    break;

                case MoJsonValueTypes.MO_JSON_BOOL:
                    stream.Read(out byte boolValue);
                    Value.Bool = new JsonValueRef<bool>(boolValue > 0);
                    //stream.Record(nameof(BoolValue), BoolValue);
                    break;

                case MoJsonValueTypes.MO_JSON_ARRAY:
                    {
                        int c = stream.ReadInt32();
                        //stream.Record(nameof(c), c);

                        Value.Array = new UArray<UMoJsonObject>(c);
                        for (int i = 0; i < c; ++i)
                        {
                            var jsonObject = new UMoJsonObject();
                            jsonObject.Deserialize(stream);

                            Value.Array.Add(jsonObject);
                        }

                        stream.Record(nameof(Value.Array), Value.Array);

                        break;
                    }

                case MoJsonValueTypes.MO_JSON_MAP:
                    {
                        int c = stream.ReadInt32();
                        //stream.Record(nameof(c), c);

                        Value.Map = new UMap<string, UMoJsonObject>(c);
                        for (int i = 0; i < c; ++i)
                        {
                            stream.Read(out string key);
                            //stream.Record(nameof(key), key);

                            var jsonObject = new UMoJsonObject();
                            jsonObject.Deserialize(stream);

                            Value.Map.Add(key, jsonObject);
                        }

                        stream.Record(nameof(Value.Map), Value.Map);

                        break;
                    }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Serialize(IUnrealStream stream)
        {
            throw new NotImplementedException();
        }

        public override string Decompile()
        {
            var str = new StringBuilder();

            switch (ValueType)
            {
                case MoJsonValueTypes.MO_JSON_NULL:
                    str.Append("null");
                    break;

                case MoJsonValueTypes.MO_JSON_INT:
                    str.Append(PropertyDisplay.FormatLiteral(Value.Int.Data));
                    break;

                case MoJsonValueTypes.MO_JSON_STRING:
                    str.Append(PropertyDisplay.FormatLiteral(Value.String));
                    break;

                case MoJsonValueTypes.MO_JSON_FLOAT:
                    str.Append(PropertyDisplay.FormatLiteral(Value.Float.Data));
                    break;

                case MoJsonValueTypes.MO_JSON_BOOL:
                    str.Append(PropertyDisplay.FormatLiteral(Value.Bool.Data));
                    break;

                case MoJsonValueTypes.MO_JSON_ARRAY:
                    str.Append("[");

                    if (Value.Array.Count > 0)
                    {
                        str.Append(Environment.NewLine);
                        UDecompilingState.AddTab();

                        for (int i = 0; i < Value.Array.Count; i++)
                        {
                            var jsonObject = Value.Array[i];
                            str.Append(UDecompilingState.Tabs);
                            string value = jsonObject.Decompile();
                            str.Append(value);

                            if (i + 1 == Value.Array.Count)
                            {
                                continue;
                            }

                            str.Append(",");
                            str.Append(Environment.NewLine);
                        }

                        UDecompilingState.RemoveTab();
                        str.Append(Environment.NewLine);
                        str.Append(UDecompilingState.Tabs);
                    }

                    str.Append("]");

                    break;

                case MoJsonValueTypes.MO_JSON_MAP:
                    str.Append("{");

                    if (Value.Map.Count > 0)
                    {
                        str.Append(Environment.NewLine);
                        UDecompilingState.AddTab();

                        foreach (var pair in Value.Map)
                        {
                            str.Append(UDecompilingState.Tabs);
                            str.Append(PropertyDisplay.FormatLiteral(pair.Key));
                            str.Append(":");
                            str.Append(" ");

                            string value = pair.Value.Decompile();
                            str.Append(value);

                            if (pair.Value == Value.Map.Last().Value)
                            {
                                continue;
                            }

                            str.Append(",");
                            str.Append(Environment.NewLine);
                        }

                        UDecompilingState.RemoveTab();
                        str.Append(Environment.NewLine);
                        str.Append(UDecompilingState.Tabs);
                    }

                    str.Append("}");

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }


            return str.ToString();
        }

        public override string ToString()
        {
            return $"Id={JsonID},Type={ValueType}";
        }
    }
}
