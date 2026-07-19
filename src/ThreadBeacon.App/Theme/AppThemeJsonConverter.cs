using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThreadBeacon.App.Theme;

public sealed class AppThemeJsonConverter : JsonConverter<AppTheme>
{
    public override AppTheme Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options) => reader.TokenType is JsonTokenType.String
            ? AppThemeResolver.Parse(reader.GetString())
            : AppTheme.System;

    public override void Write(
        Utf8JsonWriter writer,
        AppTheme value,
        JsonSerializerOptions options) =>
        writer.WriteStringValue(AppThemeResolver.ToStorageValue(value));
}
