using System.Text.Json;
using System.Text.Json.Serialization;

namespace TheEndOfMine.Models;

public enum ItemType { Food, Water, Medicine, Weapon, Tool, Misc }

// คลาสหลักสำหรับ Deserialize จาก items.json โดยตรง
public class ItemDatabase
{
    [JsonPropertyName("_meta")]
    public JsonElement Meta { get; set; }

    [JsonPropertyName("items")]
    public List<Item> Items { get; set; } = new();
}

public class Item
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name_th")]
    public string NameTh { get; set; } = string.Empty;

    [JsonPropertyName("name_en")]
    public string NameEn { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("subcategory")]
    public string Subcategory { get; set; } = string.Empty;

    [JsonPropertyName("rarity")]
    public string Rarity { get; set; } = string.Empty;

    [JsonPropertyName("weight_kg")]
    public float WeightKg { get; set; }

    [JsonPropertyName("trade_value")]
    public int TradeValue { get; set; }

    [JsonPropertyName("stackable")]
    public bool Stackable { get; set; }

    [JsonPropertyName("max_stack")]
    public int MaxStack { get; set; }

    [JsonPropertyName("found_in")]
    public List<string> FoundIn { get; set; } = new();

    [JsonPropertyName("durability_max")]
    public int? Durability { get; set; }
    public int? DurabilityMax { get; set; }

    [JsonPropertyName("effects")]
    public ItemEffects? Effects { get; set; }

    [JsonPropertyName("description_th")]
    public string DescriptionTh { get; set; } = string.Empty;

    [JsonPropertyName("story_alias")]
    public string? StoryAlias { get; set; }

    [JsonPropertyName("ammo_per_unit")]
    public int? AmmoPerUnit { get; set; }

    // ปรับ Logic ให้ดึงจาก Effects
    public bool IsUsable => Effects != null && (
        Effects.HpRestore.GetValueOrDefault() != 0 ||
        Effects.HungerRestore.GetValueOrDefault() != 0 ||
        Effects.ThirstRestore.GetValueOrDefault() != 0 ||
        Effects.FatigueRestore.GetValueOrDefault() != 0);
}

// รวมทุก Effect จากทุกประเภทไอเทมไว้ในคลาสเดียว (รองรับ Null ได้)
public class ItemEffects
{
    [JsonPropertyName("dmg_min")] public float? DmgMin { get; set; }
    [JsonPropertyName("dmg_max")] public float? DmgMax { get; set; }
    [JsonPropertyName("range")] public string? Range { get; set; }
    [JsonPropertyName("speed")] public string? Speed { get; set; }

    [JsonPropertyName("hunger_restore")] public float? HungerRestore { get; set; }
    [JsonPropertyName("thirst_restore")] public float? ThirstRestore { get; set; }
    [JsonPropertyName("requires_heat_source")] public bool? RequiresHeatSource { get; set; }

    [JsonPropertyName("hp_restore")] public float? HpRestore { get; set; }
    [JsonPropertyName("fatigue_restore")] public float? FatigueRestore { get; set; }
    [JsonPropertyName("bite_infection_reduce")] public float? BiteInfectionReduce { get; set; }

    [JsonPropertyName("visibility_bonus")] public float? VisibilityBonus { get; set; }
    [JsonPropertyName("battery_drain_per_hour")] public float? BatteryDrainPerHour { get; set; }
    [JsonPropertyName("is_light_source")] public bool? IsLightSource { get; set; }

    [JsonPropertyName("signal_range")] public float? SignalRange { get; set; }
    [JsonPropertyName("is_signal")] public bool? IsSignal { get; set; }

    [JsonPropertyName("communication_range")] public float? CommunicationRange { get; set; }
    [JsonPropertyName("is_communication")] public bool? IsCommunication { get; set; }

    [JsonPropertyName("charge_amount")] public float? ChargeAmount { get; set; }
    [JsonPropertyName("is_power_source")] public bool? IsPowerSource { get; set; }

    [JsonPropertyName("repair_amount")] public float? RepairAmount { get; set; }
    [JsonPropertyName("is_repair_material")] public bool? IsRepairMaterial { get; set; }

    [JsonPropertyName("utility_bonus")] public float? UtilityBonus { get; set; }
    [JsonPropertyName("is_general_tool")] public bool? IsGeneralTool { get; set; }

    [JsonPropertyName("craft_value")] public float? CraftValue { get; set; }
    [JsonPropertyName("is_craft_material")] public bool? IsCraftMaterial { get; set; }

    [JsonPropertyName("rope_length_m")] public float? RopeLengthM { get; set; }
    [JsonPropertyName("is_rope")] public bool? IsRope { get; set; }

    [JsonPropertyName("explore_bonus")] public float? ExploreBonus { get; set; }
    [JsonPropertyName("is_navigation")] public bool? IsNavigation { get; set; }

    [JsonPropertyName("detection_bonus")] public float? DetectionBonus { get; set; }
    [JsonPropertyName("is_observation")] public bool? IsObservation { get; set; }

    [JsonPropertyName("carry_capacity_bonus")] public float? CarryCapacityBonus { get; set; }
    [JsonPropertyName("is_container")] public bool? IsContainer { get; set; }

    [JsonPropertyName("is_heat_source")] public bool? IsHeatSource { get; set; }
    [JsonPropertyName("fire_start_chance")] public float? FireStartChance { get; set; }
    [JsonPropertyName("uses_max")] public int? UsesMax { get; set; }

    [JsonPropertyName("fuel_amount")] public float? FuelAmount { get; set; }
    [JsonPropertyName("is_fuel")] public bool? IsFuel { get; set; }

    [JsonPropertyName("survival_bonus")] public float? SurvivalBonus { get; set; }
    [JsonPropertyName("is_survival_tool")] public bool? IsSurvivalTool { get; set; }

    [JsonPropertyName("fatigue_restore_bonus")] public float? FatigueRestoreBonus { get; set; }
    [JsonPropertyName("is_rest_item")] public bool? IsRestItem { get; set; }

    [JsonPropertyName("shelter_quality")] public float? ShelterQuality { get; set; }
    [JsonPropertyName("is_shelter")] public bool? IsShelter { get; set; }

    [JsonPropertyName("trap_damage")] public float? TrapDamage { get; set; }
    [JsonPropertyName("detection_difficulty")] public float? DetectionDifficulty { get; set; }
    [JsonPropertyName("is_trap")] public bool? IsTrap { get; set; }

    [JsonPropertyName("is_misc")] public bool? IsMisc { get; set; }

    [JsonPropertyName("defense_bonus")] public float? DefenseBonus { get; set; }
    [JsonPropertyName("agility_bonus")] public float? AgilityBonus { get; set; }
    [JsonPropertyName("fatigue_reduce")] public float? FatigueReduce { get; set; }

    [JsonPropertyName("exp_gain")] public float? ExpGain { get; set; }
    [JsonPropertyName("skill_target")] public string? SkillTarget { get; set; }
    [JsonPropertyName("one_time_use")] public bool? OneTimeUse { get; set; }
}