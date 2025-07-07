using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnpackerGui.Collections;

/// <summary>
/// Converts a recent item collection to or from JSON.
/// </summary>
public class JsonRecentItemCollectionConverter<T> : JsonConverter<RecentItemCollection<T>>
{
    /// <inheritdoc/>
    public override RecentItemCollection<T> Read(ref Utf8JsonReader reader,
                                                 Type typeToConvert,
                                                 JsonSerializerOptions options)
        => new(JsonSerializer.Deserialize<T[]>(ref reader, options)!);

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer,
                               RecentItemCollection<T> value,
                               JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, options);
}
