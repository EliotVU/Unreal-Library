using System;
using System.Linq;
using System.Text;
using UELib.Core;
using UELib.UnrealScript;

namespace UELib.Branch.UE3.GIGANTIC.Core.Classes
{
    public class UJsonNodeRoot : UObject
    {
        public UMap<string, UMoJsonObject> JsonObjects;

        public UJsonNodeRoot()
        {
            ShouldDeserializeOnDemand = true;
        }

        protected override void Deserialize()
        {
            base.Deserialize();

            int c = _Buffer.ReadInt32();
            _Buffer.Record(nameof(c), c);

            JsonObjects = new UMap<string, UMoJsonObject>(c);
            for (int i = 0; i < c; ++i)
            {
                _Buffer.Read(out string key);
                _Buffer.Record(nameof(key), key);

                // In the engine 'ReadObject<UMoJsonObject>' is overriden here to serialize as a string / value
                var jsonObject = new UMoJsonObject();
                jsonObject.Deserialize(_Buffer);
                _Buffer.Record(nameof(jsonObject), jsonObject);

                JsonObjects.Add(key, jsonObject);
            }

            //_Buffer.Record(nameof(JsonObjects), JsonObjects);
        }

        public override string Decompile()
        {
            if (ShouldDeserializeOnDemand && JsonObjects == null)
            {
                BeginDeserializing();
            }

            if (JsonObjects == null)
            {
                return base.Decompile();
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
