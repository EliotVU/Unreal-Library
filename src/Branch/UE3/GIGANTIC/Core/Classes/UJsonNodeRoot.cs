using System;
using System.Linq;
using System.Text;
using UELib.Core;
using UELib.ObjectModel.Annotations;
using UELib.UnrealScript;

namespace UELib.Branch.UE3.GIGANTIC.Core.Classes
{
    public class UJsonNodeRoot : UObject
    {
        #region Serialized Members

        [StreamRecord]
        public UMap<string, UMoJsonObject> JsonObjects { get; set; }

        #endregion

        public UJsonNodeRoot()
        {
            ShouldDeserializeOnDemand = true;
        }

        public override void Deserialize(IUnrealStream stream)
        {
            base.Deserialize(stream);

            int c = stream.ReadInt32();
            stream.Record(nameof(c), c);

            JsonObjects = new UMap<string, UMoJsonObject>(c);
            for (int i = 0; i < c; ++i)
            {
                stream.Read(out string key);
                stream.Record(nameof(key), key);

                // In the engine 'ReadObject<UMoJsonObject>' is overriden here to serialize as a string / value
                var jsonObject = new UMoJsonObject();
                jsonObject.Deserialize(stream);
                stream.Record(nameof(jsonObject), jsonObject);

                JsonObjects.Add(key, jsonObject);
            }

            //stream.Record(nameof(JsonObjects), JsonObjects);
        }
        
        public override void Serialize(IUnrealStream stream)
        {
            base.Serialize(stream);
            
            stream.Write(JsonObjects.Count);
            foreach (var pair in JsonObjects)
            {
                stream.Write(pair.Key);
                pair.Value.Serialize(stream);
            }
        }

        public override string Decompile()
        {
            if (ShouldDeserializeOnDemand)
            {
                Load();
            }

            var str = new StringBuilder(JsonObjects.Count * 16);

            str.Append(UDecompilingState.Tabs);
            str.Append("[");
            str.Append(Environment.NewLine);
            UDecompilingState.AddTab();

            foreach (var pair in JsonObjects)
            {
                str.Append(UDecompilingState.Tabs);
                str.Append(PropertyDisplay.FormatLiteral(pair.Key));
                str.Append(":");
                str.Append(" ");

                string value = pair.Value.Decompile();

                if (pair.Value == JsonObjects.Last().Value)
                {
                    continue;
                }

                str.Append(value);
                str.Append(",");
                str.Append(Environment.NewLine);
            }

            UDecompilingState.RemoveTab();
            str.Append(Environment.NewLine);
            str.Append(UDecompilingState.Tabs);
            str.Append("]");

            return str.ToString();
        }
    }
}
