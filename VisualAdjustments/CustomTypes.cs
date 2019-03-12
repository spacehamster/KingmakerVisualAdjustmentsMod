using Kingmaker.Blueprints;
using Kingmaker.ResourceLinks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualAdjustments
{
    static class RefExtensions
    {
        public static BlueprintRef ToRef(this BlueprintScriptableObject blueprint)
        {
            return new BlueprintRef(blueprint);
        }
        public static ResourceRef ToRef(this EquipmentEntityLink link)
        {
            return new ResourceRef(link.AssetId);
        }
    }
    [JsonConverter(typeof(RefConverter))]
    public class BlueprintRef
    {
        public string assetId;
        public BlueprintRef(BlueprintScriptableObject blueprint)
        {
            this.assetId = blueprint.AssetGuid;
        }
        public BlueprintRef(string assetId)
        {
            this.assetId = assetId;
        }
        public static implicit operator string(BlueprintRef value)
        {
            return value == null ? null : value.assetId;
        }
        public static implicit operator BlueprintRef(string value)
        {
            return string.IsNullOrEmpty(value) ? null : new BlueprintRef(value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj.GetType() != this.GetType()) return false;
            var asset = obj as BlueprintRef;
            return asset.assetId == assetId;
        }
        public override int GetHashCode()
        {
            return 2108129126 + EqualityComparer<string>.Default.GetHashCode(assetId);
        }
    }
    [JsonConverter(typeof(RefConverter))]
    public class ResourceRef
    {
        public string assetId;
        public ResourceRef(string assetId)
        {
            this.assetId = assetId;
        }
        public static implicit operator string(ResourceRef value)
        {
            return value == null ? null : value.assetId;
        }
        public static implicit operator ResourceRef(string value)
        {
            return string.IsNullOrEmpty(value) ? null : new ResourceRef(value);
        }
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj.GetType() != this.GetType()) return false;
            var asset = obj as ResourceRef;
            return asset.assetId == assetId;
        }
        public override int GetHashCode()
        {
            return 2108129126 + EqualityComparer<string>.Default.GetHashCode(assetId);
        }
    }
    public class RefConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(BlueprintRef) || objectType == typeof(ResourceRef);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return null;
            var text = reader.Value as string;
            if(objectType == typeof(BlueprintRef))
            {
                BlueprintRef result = text;
                return result;
            } else
            {
                ResourceRef result = text;
                return result;
            }
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if(value == null)
            {
                writer.WriteNull();
            } else if(value.GetType() == typeof(BlueprintRef))
            {
                string text = (BlueprintRef)value;
                writer.WriteValue(text);
            } else
            {
                string text = (ResourceRef)value;
                writer.WriteValue(text);
            }
        }
    }


}
