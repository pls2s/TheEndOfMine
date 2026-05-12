using System.Text.Json;
using Microsoft.Maui.Storage;
using TheEndOfMine.Models;

namespace TheEndOfMine.Services;

public class StoryAssetResolver
{
    private const string CatalogFileName = "story_assets.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private StoryAssetCatalog? _cachedCatalog;

    public async Task<StoryAssetCatalog> LoadCatalogAsync(CancellationToken cancellationToken = default)
    {
        if (_cachedCatalog != null)
            return _cachedCatalog;

        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync(CatalogFileName).ConfigureAwait(false);
            var catalog = await JsonSerializer.DeserializeAsync<StoryAssetCatalog>(stream, JsonOptions, cancellationToken)
                .ConfigureAwait(false);

            _cachedCatalog = IsUsable(catalog) ? catalog! : CreateFallbackCatalog();
        }
        catch
        {
            _cachedCatalog = CreateFallbackCatalog();
        }

        return _cachedCatalog;
    }

    public string ResolveChapterPath(Gender gender, string? alias, StoryAssetCatalog catalog)
    {
        var normalizedAlias = NormalizeAlias(alias, catalog.ChapterAliases, "chapter");
        var genderName = GetGenderFolder(gender);
        return $"story/{genderName}/chapter/{genderName}_chapter_{normalizedAlias}.png";
    }

    public string ResolveEventPath(Gender gender, string? alias, StoryAssetCatalog catalog)
    {
        var normalizedAlias = NormalizeAlias(alias, catalog.EventAliases, "event");
        var genderName = GetGenderFolder(gender);

        return ContainsAlias(catalog.GenderEventAliases, normalizedAlias)
            ? $"story/{genderName}/event/{genderName}_event_{normalizedAlias}.png"
            : $"story/event/event_{genderName}_{normalizedAlias}.png";
    }

    public string ResolveItemPath(string? alias, StoryAssetCatalog catalog)
    {
        var normalizedAlias = NormalizeAlias(alias, catalog.ItemAliases, "item");
        return $"story/item/item_{normalizedAlias}.png";
    }

    public string ResolveEndingPath(Gender gender, string? alias, StoryAssetCatalog catalog)
    {
        var normalizedAlias = NormalizeAlias(alias, catalog.EndingAliases, "ending");
        var genderName = GetGenderFolder(gender);
        return $"story/{genderName}/ending/{genderName}_ending_{normalizedAlias}.png";
    }

    public string NormalizeAlias(string? requestedAlias, IReadOnlyList<string> allowedAliases, string fallbackSeed)
    {
        if (allowedAliases.Count == 0)
            return SanitizeAlias(requestedAlias ?? fallbackSeed);

        var sanitized = SanitizeAlias(requestedAlias ?? string.Empty);
        var exact = allowedAliases.FirstOrDefault(alias => string.Equals(alias, sanitized, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(exact))
            return exact;

        if (!string.IsNullOrWhiteSpace(sanitized))
        {
            var compact = sanitized.Replace("_", string.Empty, StringComparison.Ordinal);
            var fuzzy = allowedAliases.FirstOrDefault(alias =>
                alias.Replace("_", string.Empty, StringComparison.Ordinal).Contains(compact, StringComparison.OrdinalIgnoreCase) ||
                compact.Contains(alias.Replace("_", string.Empty, StringComparison.Ordinal), StringComparison.OrdinalIgnoreCase));

            if (!string.IsNullOrWhiteSpace(fuzzy))
                return fuzzy;
        }

        var hash = StringComparer.OrdinalIgnoreCase.GetHashCode(fallbackSeed);
        var index = (int)((uint)hash % (uint)allowedAliases.Count);
        return allowedAliases[index];
    }

    public string FormatPromptAliases(StoryAssetCatalog catalog)
    {
        return $"""
        รูปที่มีอยู่แล้วในเกม ให้ใช้ alias จากรายการนี้เท่านั้น ห้ามแต่งชื่อไฟล์เอง:
        - chapter_alias เลือกจาก: {JoinAliases(catalog.ChapterAliases)}
        - event.story_alias เลือกจาก: {JoinAliases(catalog.EventAliases)}
        - item.story_alias เลือกจาก: {JoinAliases(catalog.ItemAliases)}
        - ending_alias ถ้าจำเป็น เลือกจาก: {JoinAliases(catalog.EndingAliases)}

        ระบบเกมจะ map alias เป็น path รูปเองตามเพศตัวละคร เช่น female_event_<alias>.png หรือ male_event_<alias>.png
        """;
    }

    private static bool IsUsable(StoryAssetCatalog? catalog)
    {
        return catalog is
        {
            ChapterAliases.Count: > 0,
            ItemAliases.Count: > 0
        };
    }

    private static bool ContainsAlias(IEnumerable<string> aliases, string alias)
    {
        return aliases.Any(candidate => string.Equals(candidate, alias, StringComparison.OrdinalIgnoreCase));
    }

    private static string GetGenderFolder(Gender gender) => gender == Gender.Male ? "male" : "female";

    private static string JoinAliases(IEnumerable<string> aliases) => string.Join(", ", aliases);

    private static string SanitizeAlias(string value)
    {
        var chars = value
            .Trim()
            .ToLowerInvariant()
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : '_')
            .ToArray();

        var sanitized = new string(chars);
        while (sanitized.Contains("__", StringComparison.Ordinal))
            sanitized = sanitized.Replace("__", "_", StringComparison.Ordinal);

        return sanitized.Trim('_');
    }

    private static StoryAssetCatalog CreateFallbackCatalog()
    {
        return new StoryAssetCatalog
        {
            ChapterAliases = new List<string>
            {
                "broken_bridge", "checkpoint", "clinic_ruins", "damaged_home", "factory_ruins",
                "final_escape", "fire_spread", "government_building", "hallway_blood",
                "hospital_corridor", "loot_store", "looted_store", "market_ruins",
                "police_station", "quiet_village", "radio_signal", "rooftop_sunset",
                "ruined_city_sunset", "street_sunset", "survivor_camp", "toxic_rain",
                "underground_tunnel"
            },
            GenderEventAliases = new List<string>
            {
                "abandoned_house_search", "broken_bridge", "campfire", "empty_street_crossing",
                "factory_tools", "fire_escape", "hospital_supplies", "looted_store_entrance",
                "looted_store_inside", "market_ruins", "mirror_zombie", "quiet_village",
                "radio_signal", "rooftop_signal", "safehouse_night", "settlement_arrival",
                "toxic_rain", "underground_tunnel"
            },
            CommonEventAliases = new List<string>
            {
                "lab_vaccine", "sunset_street"
            },
            ItemAliases = new List<string>
            {
                "antiseptic", "backpack", "bandage", "battery_pack", "binoculars", "blanket",
                "canned_food", "canteen", "compass", "cookpot", "first_aid_kit", "flare",
                "flashlight", "fuel_can", "gloves", "helmet", "knife", "knife_sheath",
                "lighter", "lockpick_set", "machete", "map", "mask", "matches",
                "medicine_bottle", "painkillers", "pliers", "radio", "radio_battery",
                "rope_coil", "screwdriver", "sewing_kit", "sleeping_bag", "stove",
                "tape_roll", "torch", "water_bottle", "water_filter", "whistle", "wrench"
            },
            EndingAliases = new List<string> { "helicopter_survivors" }
        };
    }
}
