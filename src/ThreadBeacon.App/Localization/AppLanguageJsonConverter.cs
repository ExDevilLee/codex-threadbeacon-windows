using System.Text.Json;
using System.Text.Json.Serialization;

namespace ThreadBeacon.App.Localization;

public sealed class AppLanguageJsonConverter : JsonConverter<AppLanguage>
{
    public override AppLanguage Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        return reader.TokenType is JsonTokenType.String
            ? AppLanguageResolver.Parse(reader.GetString())
            : AppLanguage.System;
    }

    public override void Write(
        Utf8JsonWriter writer,
        AppLanguage value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(AppLanguageResolver.ToStorageValue(value));
    }
}
