using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheEndOfMine.Models;

// ---- Location Event Map ----

public class LocationStoryEvent
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("condition")]
    public Dictionary<string, JsonElement>? Condition { get; set; }
}

public class LocationConfig
{
    [JsonPropertyName("requires_flag")]
    public string? RequiresFlag { get; set; }

    [JsonPropertyName("first_visit")]
    public string? FirstVisit { get; set; }

    [JsonPropertyName("random_events")]
    public List<string>? RandomEvents { get; set; }

    [JsonPropertyName("story_events")]
    public List<LocationStoryEvent>? StoryEvents { get; set; }
}

// ---- Skill Config ----

public class SkillConfig
{
    [JsonPropertyName("base_success")]
    public float BaseSuccess { get; set; }

    [JsonPropertyName("level_thresholds")]
    public List<int> LevelThresholds { get; set; } = new();

    [JsonPropertyName("per_level_bonus")]
    public float PerLevelBonus { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
}

// ---- Time Skip Costs ----

public class DifficultyValues
{
    [JsonPropertyName("easy")]
    public float Easy { get; set; }

    [JsonPropertyName("normal")]
    public float Normal { get; set; }

    [JsonPropertyName("hard")]
    public float Hard { get; set; }
}

public class TimeSkipCosts
{
    [JsonPropertyName("hunger_per_hour")]
    public DifficultyValues HungerPerHour { get; set; } = new();

    [JsonPropertyName("thirst_per_hour")]
    public DifficultyValues ThirstPerHour { get; set; } = new();

    [JsonPropertyName("fatigue_restore_per_hour_sleep")]
    public float FatigueRestorePerHourSleep { get; set; }

    [JsonPropertyName("fatigue_per_hour_awake")]
    public float FatiguePerHourAwake { get; set; }
}

// ---- Random Event Pool ----

public class RandomEventPool
{
    [JsonPropertyName("events")]
    public List<string> Events { get; set; } = new();
}

// ---- Root: StoryTree ----

public class StoryTree
{
    [JsonPropertyName("default_flags")]
    public Dictionary<string, JsonElement> DefaultFlags { get; set; } = new();

    [JsonPropertyName("ending_conditions")]
    public Dictionary<string, JsonElement> EndingConditions { get; set; } = new();

    [JsonPropertyName("location_event_map")]
    public Dictionary<string, LocationConfig> LocationEventMap { get; set; } = new();

    [JsonPropertyName("events")]
    public Dictionary<string, StoryEvent> Events { get; set; } = new();

    [JsonPropertyName("items_catalog")]
    public Dictionary<string, StoryItem> ItemsCatalog { get; set; } = new();

    [JsonPropertyName("skill_levels")]
    public Dictionary<string, SkillConfig> SkillLevels { get; set; } = new();

    [JsonPropertyName("random_event_pool")]
    public RandomEventPool? RandomEventPool { get; set; }

    [JsonPropertyName("time_skip_costs")]
    public TimeSkipCosts? TimeSkipCosts { get; set; }
}
