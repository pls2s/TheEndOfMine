using System.Text.Json.Serialization;

namespace TheEndOfMine.Models;

public class EventChoice
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    // ผลกระทบต่อ stats
    [JsonPropertyName("hpEffect")]
    public float HpEffect { get; set; }

    [JsonPropertyName("hungerEffect")]
    public float HungerEffect { get; set; }

    [JsonPropertyName("thirstEffect")]
    public float ThirstEffect { get; set; }

    [JsonPropertyName("fatigueEffect")]
    public float FatigueEffect { get; set; }

    [JsonPropertyName("itemReward")]
    public Item? ItemReward { get; set; }

    [JsonPropertyName("itemRewards")]
    public List<Item> ItemRewards { get; set; } = new();

    [JsonPropertyName("items_add")]
    public List<string> ItemsAdd { get; set; } = new();

    [JsonPropertyName("requiredItemId")]
    public string RequiredItemId { get; set; } = string.Empty;

    [JsonPropertyName("consumedItemId")]
    public string ConsumedItemId { get; set; } = string.Empty;

    [JsonPropertyName("usedItemId")]
    public string UsedItemId { get; set; } = string.Empty;

    [JsonIgnore]
    public string InventoryEffectNote { get; set; } = string.Empty;

    [JsonPropertyName("resultText")]
    public string ResultText { get; set; } = string.Empty;

    public IEnumerable<Item> GetItemRewards()
    {
        if (ItemReward != null)
            yield return ItemReward;

        foreach (var item in ItemRewards)
            yield return item;
    }
}

public class GameEvent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("story_alias")]
    public string? StoryAlias { get; set; }

    [JsonPropertyName("image_alias")]
    public string? ImageAlias { get; set; }

    [JsonPropertyName("imagePrompt")]
    public string ImagePrompt { get; set; } = string.Empty;

    [JsonPropertyName("imagePath")]
    public string ImagePath { get; set; } = string.Empty;

    [JsonPropertyName("choices")]
    public List<EventChoice> Choices { get; set; } = new();
}
