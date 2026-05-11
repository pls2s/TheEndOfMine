using System.Text.Json.Serialization;

namespace TheEndOfMine.Models;

public class GeneratedGameContent
{
    [JsonPropertyName("storyTitle")]
    public string StoryTitle { get; set; } = string.Empty;

    [JsonPropertyName("events")]
    public List<GameEvent> Events { get; set; } = new();

    [JsonPropertyName("startingItems")]
    public List<Item> StartingItems { get; set; } = new();

    [JsonIgnore]
    public bool UsedRemoteLlm { get; set; }
}
