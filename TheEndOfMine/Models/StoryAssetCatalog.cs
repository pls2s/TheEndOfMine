using System.Text.Json.Serialization;

namespace TheEndOfMine.Models;

public class StoryAssetCatalog
{
    [JsonPropertyName("chapter_aliases")]
    public List<string> ChapterAliases { get; set; } = new();

    [JsonPropertyName("gender_event_aliases")]
    public List<string> GenderEventAliases { get; set; } = new();

    [JsonPropertyName("common_event_aliases")]
    public List<string> CommonEventAliases { get; set; } = new();

    [JsonPropertyName("item_aliases")]
    public List<string> ItemAliases { get; set; } = new();

    [JsonPropertyName("ending_aliases")]
    public List<string> EndingAliases { get; set; } = new();

    [JsonIgnore]
    public IReadOnlyList<string> EventAliases =>
        GenderEventAliases.Concat(CommonEventAliases)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(alias => alias)
            .ToList();
}
