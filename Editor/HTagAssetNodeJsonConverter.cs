using System;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json;

namespace HTags.Editor
{
    public class HTagAssetNodeJsonConverter : JsonConverter<HTagAssetNode>
    {
        [Serializable]
        public struct HTagAssetNodeDto
        {
            public string NewFullName { get; set; }
            public string OldFullName { get; set; }
            public TagChange Change { get; set; }
        }

        public HTagAsset TagAsset { get; set; }

        public override void WriteJson(JsonWriter writer, HTagAssetNode value, JsonSerializer serializer)
        {
            var assetNodesList = new List<HTagAssetNodeDto>();
            foreach (var node in value)
            {
                if (node.IsRoot && string.IsNullOrEmpty(node.FullName))
                {
                    continue;
                }
                
                assetNodesList.Add(new HTagAssetNodeDto
                {
                    NewFullName = node.FullName,
                    OldFullName = node.HTag?.name,
                    Change = node.Change
                });
            }
            serializer.Serialize(writer, assetNodesList);
        }

        public override HTagAssetNode ReadJson(JsonReader reader, Type objectType, HTagAssetNode existingValue, bool hasExistingValue,
            JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }

            var assetNodesList = serializer.Deserialize<List<HTagAssetNodeDto>>(reader);
            if (assetNodesList == null)
            {
                return null;
            }

            var root = new HTagAssetNode("");
            foreach (var dto in assetNodesList)
            {
                BaseHTagSo hTag = null;
                if (!string.IsNullOrEmpty(dto.OldFullName))
                {
                    hTag = TagAsset.Tags.Find(t => t.name == dto.OldFullName);
                }
                
                root.TryAdd(new HTagAssetNode(hTag, dto.NewFullName)
                {
                    Change = dto.Change
                });
            }
            
            return root;
        }
    }
}