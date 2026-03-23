using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Nuke.Cola;

/// <summary>
/// A JSON converter for property which may have a single value or an array of values when de-serializing. It will
/// always serialize an array.
/// </summary>
/// <typeparam name="T">Underlying type of the property</typeparam>
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
