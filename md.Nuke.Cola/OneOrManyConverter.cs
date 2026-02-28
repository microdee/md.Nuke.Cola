using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nuke.Cola;

public class OneOrManyConverter<T> : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
    {
        if (value is List<T?> values)
        {
            writer.WriteStartArray();
            foreach (var item in values)
            {
                writer.WriteValue(item);
            }
            writer.WriteEndArray();
        }
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
    {
        var token = JToken.Load(reader);

        return token.Type switch
        {
            JTokenType.Array => token.ToObject<List<T?>>(),
            JTokenType.Null => null,
            _ => [token.ToObject<T>()]
        };
    }

    public override bool CanConvert(Type objectType) => objectType == typeof(List<T?>);
}
