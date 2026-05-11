using System.Text.Json.Serialization;

namespace TheEndOfMine.Models;

public class GeneratedGameContent
{
    [JsonPropertyName("storyTitle")]
    public string StoryTitle { get; set; } = string.Empty;

    [JsonPropertyName("chapter_alias")]
    public string? ChapterAlias { get; set; }

    [JsonPropertyName("chapter_image_path")]
    public string ChapterImagePath { get; set; } = string.Empty;

    [JsonPropertyName("events")]
    public List<GameEvent> Events { get; set; } = new();

    [JsonPropertyName("startingItems")]
    public List<Item> StartingItems { get; set; } = new();

    [JsonIgnore]
    public bool UsedRemoteLlm { get; set; }
}
