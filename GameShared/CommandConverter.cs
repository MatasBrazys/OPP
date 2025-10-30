using GameShared.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace GameShared
{
    /// <summary>
    /// Reads JSOn and looks for Type, decides which class to create
    /// Deserializes into correct  type -> MoveCommand, HarvestCommand
    /// </summary>
    public class CommandConverter : JsonConverter<ICommand>
    {
        public override ICommand Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (!root.TryGetProperty("Type", out var typeProperty))
                throw new JsonException("Command missing Type property");

            var type = typeProperty.GetString();

            return type switch
            {
                "move" => JsonSerializer.Deserialize<MoveCommand>(root.GetRawText(), options),
                "harvest" => JsonSerializer.Deserialize<HarvestCommand>(root.GetRawText(), options),
                _ => throw new JsonException($"Unknown command type: {type}")
            };
        }

        public override void Write(Utf8JsonWriter writer, ICommand value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value, value.GetType(), options);
        }
    }
}
