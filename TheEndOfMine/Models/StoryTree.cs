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

// ---- Story Event Models (เพิ่มใหม่) ----

public class StoryEvent
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("day_trigger")] public int? DayTrigger { get; set; }
    [JsonPropertyName("time_trigger")] public string? TimeTrigger { get; set; }
    [JsonPropertyName("probability")] public float? Probability { get; set; }
    [JsonPropertyName("image_hint")] public string? ImageHint { get; set; }
    [JsonPropertyName("description")] public string? Description { get; set; }
    [JsonPropertyName("is_skippable")] public bool? IsSkippable { get; set; }
    [JsonPropertyName("is_narrative_only")] public bool? IsNarrativeOnly { get; set; }
    [JsonPropertyName("auto_proceed")] public bool? AutoProceed { get; set; }
    [JsonPropertyName("auto_proceed_to_event")] public string? AutoProceedToEvent { get; set; }
    [JsonPropertyName("narrative")] public string? Narrative { get; set; }
    [JsonPropertyName("next_event")] public string? NextEvent { get; set; }
    [JsonPropertyName("condition")] public Dictionary<string, JsonElement>? Condition { get; set; }
    [JsonPropertyName("condition_branch")] public List<StoryConditionBranch>? ConditionBranch { get; set; }
    [JsonPropertyName("choices")] public List<StoryEventChoice>? Choices { get; set; }
    [JsonPropertyName("requires_flag")] public string? RequiresFlag { get; set; }
    [JsonPropertyName("requires_flags")] public List<string>? RequiresFlags { get; set; }

    // Ending Fields
    [JsonPropertyName("is_ending")] public bool? IsEnding { get; set; }
    [JsonPropertyName("ending_id")] public string? EndingId { get; set; }
    [JsonPropertyName("ending_type")] public string? EndingType { get; set; }
    [JsonPropertyName("ending_narration")] public string? EndingNarration { get; set; }
    [JsonPropertyName("unlock")] public string? Unlock { get; set; }
}

public class StoryConditionBranch
{
    [JsonPropertyName("condition")] public Dictionary<string, JsonElement>? Condition { get; set; }
    [JsonPropertyName("next_event")] public string? NextEvent { get; set; }
    [JsonPropertyName("narrative")] public string? Narrative { get; set; }
}

public class StoryEventChoice
{
    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;
    [JsonPropertyName("text")] public string Text { get; set; } = string.Empty;
    [JsonPropertyName("requires")] public Dictionary<string, JsonElement>? Requires { get; set; }
    [JsonPropertyName("effects")] public Dictionary<string, JsonElement>? Effects { get; set; }
    [JsonPropertyName("effects_success")] public Dictionary<string, JsonElement>? EffectsSuccess { get; set; }
    [JsonPropertyName("effects_fail")] public Dictionary<string, JsonElement>? EffectsFail { get; set; }
    [JsonPropertyName("success_chance_base")] public float? SuccessChanceBase { get; set; }
    [JsonPropertyName("success_chance")] public float? SuccessChance { get; set; }
    [JsonPropertyName("success_modifier_skill")] public Dictionary<string, JsonElement>? SuccessModifierSkill { get; set; }
    [JsonPropertyName("narrative")] public string? Narrative { get; set; }
    [JsonPropertyName("narrative_success")] public string? NarrativeSuccess { get; set; }
    [JsonPropertyName("narrative_fail")] public string? NarrativeFail { get; set; }
    [JsonPropertyName("next_event")] public string? NextEvent { get; set; }
    [JsonPropertyName("next_event_success")] public string? NextEventSuccess { get; set; }
    [JsonPropertyName("next_event_fail")] public string? NextEventFail { get; set; }
    [JsonPropertyName("flags_set")] public List<string>? FlagsSet { get; set; }
    [JsonPropertyName("flags_set_success")] public List<string>? FlagsSetSuccess { get; set; }
    [JsonPropertyName("items_add")] public List<string>? ItemsAdd { get; set; }
    [JsonPropertyName("items_remove")] public List<string>? ItemsRemove { get; set; }
    [JsonPropertyName("items_remove_one")] public List<string>? ItemsRemoveOne { get; set; }
    [JsonPropertyName("items_remove_one_any")] public List<string>? ItemsRemoveOneAny { get; set; }
    [JsonPropertyName("items_add_random")] public Dictionary<string, JsonElement>? ItemsAddRandom { get; set; }
    [JsonPropertyName("action")] public string? Action { get; set; }
}

public class StoryItem
{
    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("dmg")] public List<float>? Dmg { get; set; }
    [JsonPropertyName("hunger_restore")] public float? HungerRestore { get; set; }
    [JsonPropertyName("thirst_restore")] public float? ThirstRestore { get; set; }
    [JsonPropertyName("hp_restore")] public float? HpRestore { get; set; }
    [JsonPropertyName("fatigue_restore")] public float? FatigueRestore { get; set; }
    [JsonPropertyName("bite_risk_reduce")] public float? BiteRiskReduce { get; set; }
    [JsonPropertyName("signal_power")] public float? SignalPower { get; set; }
    [JsonPropertyName("requires")] public string? Requires { get; set; }
    [JsonPropertyName("use")] public string? Use { get; set; }
    [JsonPropertyName("one_use")] public bool? OneUse { get; set; }
    [JsonPropertyName("rarity")] public string Rarity { get; set; } = string.Empty;
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