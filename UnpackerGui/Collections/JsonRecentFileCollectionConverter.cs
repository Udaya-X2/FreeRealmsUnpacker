using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace UnpackerGui.Collections;

/// <summary>
/// Converts a recent file collection to or from JSON.
/// </summary>
public class JsonRecentFileCollectionConverter : JsonConverter<RecentFileCollection>
{
    /// <inheritdoc/>
    public override RecentFileCollection Read(ref Utf8JsonReader reader,
                                              Type typeToConvert,
                                              JsonSerializerOptions options)
        => new(JsonSerializer.Deserialize<string[]>(ref reader, options)!);

    /// <inheritdoc/>
    public override void Write(Utf8JsonWriter writer,
                               RecentFileCollection value,
                               JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, options);
}
