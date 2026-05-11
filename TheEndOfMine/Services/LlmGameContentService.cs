using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;
using TheEndOfMine.Models;

namespace TheEndOfMine.Services;

public class LlmGameContentService
{
    private const string DefaultEndpoint = "https://api.openai.com/v1/chat/completions";
    private const string DefaultModel = "gpt-4o-mini";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(45)
    };

    public async Task<GeneratedGameContent> GenerateNewGameAsync(Survivor survivor, CancellationToken cancellationToken = default)
    {
        var settings = await LoadSettingsAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            return CreateFallbackContent(survivor);

        try
        {
            var endpoint = settings.Endpoint ?? DefaultEndpoint;
            var model = settings.Model ?? DefaultModel;
            var requestJson = JsonSerializer.Serialize(CreateChatRequest(model, survivor), JsonOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var contentJson = ExtractMessageContent(responseJson);
            var generated = JsonSerializer.Deserialize<GeneratedGameContent>(NormalizeJson(contentJson), JsonOptions);

            if (generated == null)
                throw new InvalidOperationException("LLM response was empty.");

            NormalizeContent(generated, survivor.Name);
            generated.UsedRemoteLlm = true;
            return generated;
        }
        catch
        {
            return CreateFallbackContent(survivor);
        }
    }

    private static object CreateChatRequest(string model, Survivor survivor)
    {
        return new
        {
            model,
            temperature = 0.95,
            response_format = new { type = "json_object" },
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = "You generate balanced Thai JSON content for a post-apocalyptic survival game. Return JSON only."
                },
                new
                {
                    role = "user",
                    content = BuildPrompt(survivor)
                }
            }
        };
    }

    private static string BuildPrompt(Survivor survivor)
    {
        return $$"""
        Generate a new game story for The End of Mine.
        Survivor name: {{survivor.Name}}
        Survivor gender: {{survivor.Gender}}

        Requirements:
        - Thai language for storyTitle, title, description, choice text, resultText, item names, and item descriptions.
        - Create exactly 8 events.
        - Each event must have exactly 2 choices.
        - Each choice must include id, text, hpEffect, hungerEffect, thirstEffect, fatigueEffect, resultText.
        - Numeric effects must be balanced between -30 and 30.
        - Add itemReward to 3-5 choices. Rewards must be full item objects.
        - Create 3 startingItems.
        - Keep the tone tense, survival-focused, and varied.

        Return JSON with this exact shape:
        {
          "storyTitle": "string",
          "events": [
            {
              "id": "evt_01",
              "title": "string",
              "description": "string",
              "choices": [
                {
                  "id": "c1",
                  "text": "string",
                  "hpEffect": 0,
                  "hungerEffect": 0,
                  "thirstEffect": 0,
                  "fatigueEffect": 0,
                  "resultText": "string",
                  "itemReward": {
                    "id": "gen_item_id",
                    "name_th": "string",
                    "name_en": "string",
                    "category": "Food|Water|Medicine|Weapon|Tool|Misc",
                    "subcategory": "string",
                    "rarity": "common|uncommon|rare",
                    "weight_kg": 1,
                    "trade_value": 1,
                    "stackable": false,
                    "max_stack": 1,
                    "found_in": ["generated_story"],
                    "durability_max": 1,
                    "effects": {
                      "hp_restore": 0,
                      "hunger_restore": 0,
                      "thirst_restore": 0,
                      "fatigue_restore": 0
                    },
                    "description_th": "string",
                    "story_alias": "gen_alias"
                  }
                }
              ]
            }
          ],
          "startingItems": []
        }
        """;
    }

    private static string ExtractMessageContent(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var first = choices[0];
            if (first.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                return content.GetString() ?? string.Empty;
            }
        }

        if (root.TryGetProperty("output_text", out var outputText))
            return outputText.GetString() ?? string.Empty;

        return responseJson;
    }

    private static string NormalizeJson(string text)
    {
        var trimmed = text.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNewLine = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewLine >= 0 && lastFence > firstNewLine)
                trimmed = trimmed[(firstNewLine + 1)..lastFence].Trim();
        }

        var start = trimmed.IndexOf('{');
        var end = trimmed.LastIndexOf('}');
        if (start >= 0 && end > start)
            return trimmed[start..(end + 1)];

        return trimmed;
    }

    private static void NormalizeContent(GeneratedGameContent content, string survivorName)
    {
        if (string.IsNullOrWhiteSpace(content.StoryTitle))
            content.StoryTitle = $"เส้นทางของ {survivorName}";

        content.Events = content.Events
            .Where(e => !string.IsNullOrWhiteSpace(e.Title) && e.Choices.Count >= 2)
            .Take(8)
            .ToList();

        if (content.Events.Count < 8)
            throw new InvalidOperationException("LLM returned too few playable events.");

        for (var i = 0; i < content.Events.Count; i++)
        {
            var gameEvent = content.Events[i];
            gameEvent.Id = string.IsNullOrWhiteSpace(gameEvent.Id) ? $"evt_{i + 1:D2}" : gameEvent.Id;
            gameEvent.Description = string.IsNullOrWhiteSpace(gameEvent.Description)
                ? "สถานการณ์ตรงหน้าบีบให้คุณต้องตัดสินใจทันที"
                : gameEvent.Description;
            gameEvent.Choices = gameEvent.Choices.Take(2).ToList();

            for (var c = 0; c < gameEvent.Choices.Count; c++)
            {
                var choice = gameEvent.Choices[c];
                choice.Id = string.IsNullOrWhiteSpace(choice.Id) ? $"c{c + 1}" : choice.Id;
                choice.HpEffect = ClampEffect(choice.HpEffect);
                choice.HungerEffect = ClampEffect(choice.HungerEffect);
                choice.ThirstEffect = ClampEffect(choice.ThirstEffect);
                choice.FatigueEffect = ClampEffect(choice.FatigueEffect);
                choice.ResultText = string.IsNullOrWhiteSpace(choice.ResultText)
                    ? "คุณรับผลจากการตัดสินใจนั้นและเดินหน้าต่อ"
                    : choice.ResultText;

                if (choice.ItemReward != null)
                    NormalizeItem(choice.ItemReward, $"reward_{i + 1}_{c + 1}");
            }
        }

        content.StartingItems = content.StartingItems.Take(3).ToList();
        for (var i = 0; i < content.StartingItems.Count; i++)
            NormalizeItem(content.StartingItems[i], $"start_{i + 1}");
    }

    private static float ClampEffect(float value) => Math.Clamp(value, -30f, 30f);

    private static void NormalizeItem(Item item, string fallbackId)
    {
        item.Id = string.IsNullOrWhiteSpace(item.Id) ? $"gen_{fallbackId}" : item.Id;
        item.NameTh = string.IsNullOrWhiteSpace(item.NameTh) ? "ของใช้ไม่ทราบที่มา" : item.NameTh;
        item.NameEn = string.IsNullOrWhiteSpace(item.NameEn) ? item.Id : item.NameEn;
        item.Category = string.IsNullOrWhiteSpace(item.Category) ? "Misc" : item.Category;
        item.Subcategory = string.IsNullOrWhiteSpace(item.Subcategory) ? "generated" : item.Subcategory;
        item.Rarity = string.IsNullOrWhiteSpace(item.Rarity) ? "common" : item.Rarity;
        item.WeightKg = item.WeightKg <= 0 ? 0.5f : item.WeightKg;
        item.TradeValue = item.TradeValue <= 0 ? 1 : item.TradeValue;
        item.MaxStack = item.MaxStack <= 0 ? 1 : item.MaxStack;
        item.FoundIn = item.FoundIn.Count == 0 ? new List<string> { "generated_story" } : item.FoundIn;
        item.DurabilityMax ??= item.Durability ?? 1;
        item.Durability = item.DurabilityMax;
        item.DescriptionTh = string.IsNullOrWhiteSpace(item.DescriptionTh)
            ? "ไอเทมจากเรื่องราวที่ถูกสร้างใหม่"
            : item.DescriptionTh;
        item.StoryAlias ??= item.Id;
    }

    private static GeneratedGameContent CreateFallbackContent(Survivor survivor)
    {
        var seed = Guid.NewGuid().ToString("N")[..8];
        var places = new[] { "โรงพยาบาลร้าง", "สถานีรถไฟใต้ดิน", "ตลาดกลางคืนที่ถูกทิ้ง", "ดาดฟ้าอพาร์ตเมนต์", "อุโมงค์ระบายน้ำ" };
        var threats = new[] { "กลุ่มคนถืออาวุธ", "ฝูงผู้ติดเชื้อ", "ไฟไหม้ที่ลามช้าๆ", "พายุฝุ่นพิษ", "เสียงวิทยุปริศนา" };
        var itemNames = new[] { "น้ำกรองขวดเล็ก", "ผ้าพันแผลสะอาด", "มีดพับขึ้นสนิม", "อาหารกระป๋องบุบ", "ไฟฉายมือหมุน", "ยาแก้ปวดเหลือครึ่งแผง" };

        var events = new List<GameEvent>();
        for (var i = 0; i < 8; i++)
        {
            var place = places[Random.Shared.Next(places.Length)];
            var threat = threats[Random.Shared.Next(threats.Length)];
            events.Add(new GameEvent
            {
                Id = $"fallback_{seed}_{i + 1:D2}",
                Title = $"{place}: ทางเลือกที่ {i + 1}",
                Description = $"{survivor.Name} พบ{place}ระหว่างออกสำรวจ แต่มี{threat}ขวางทางอยู่ ทุกวินาทีทำให้เสบียงลดลง",
                Choices = new List<EventChoice>
                {
                    new()
                    {
                        Id = "c1",
                        Text = "เสี่ยงเข้าไปค้นหา",
                        HpEffect = -Random.Shared.Next(0, 16),
                        HungerEffect = Random.Shared.Next(0, 18),
                        ThirstEffect = -Random.Shared.Next(0, 10),
                        FatigueEffect = Random.Shared.Next(8, 24),
                        ResultText = "คุณได้บางอย่างติดมือมา แต่ร่างกายเริ่มรับภาระหนักขึ้น",
                        ItemReward = i % 2 == 0 ? CreateFallbackItem(itemNames[Random.Shared.Next(itemNames.Length)], $"reward_{seed}_{i}") : null
                    },
                    new()
                    {
                        Id = "c2",
                        Text = "เลี่ยงเส้นทางและประหยัดแรง",
                        HpEffect = Random.Shared.Next(0, 8),
                        HungerEffect = -Random.Shared.Next(0, 12),
                        ThirstEffect = -Random.Shared.Next(0, 12),
                        FatigueEffect = Random.Shared.Next(-10, 8),
                        ResultText = "คุณเลือกความปลอดภัยไว้ก่อน แม้จะเสียโอกาสเก็บเสบียง"
                    }
                }
            });
        }

        return new GeneratedGameContent
        {
            StoryTitle = $"คืนเอาตัวรอด {seed}",
            UsedRemoteLlm = false,
            Events = events,
            StartingItems = new List<Item>
            {
                CreateFallbackItem("น้ำกรองขวดเล็ก", $"start_{seed}_water"),
                CreateFallbackItem("อาหารกระป๋องบุบ", $"start_{seed}_food"),
                CreateFallbackItem("ผ้าพันแผลสะอาด", $"start_{seed}_bandage")
            }
        };
    }

    private static Item CreateFallbackItem(string nameTh, string id)
    {
        var category = nameTh.Contains("น้ำ", StringComparison.Ordinal) ? "Water" :
            nameTh.Contains("อาหาร", StringComparison.Ordinal) ? "Food" :
            nameTh.Contains("แผล", StringComparison.Ordinal) || nameTh.Contains("ยา", StringComparison.Ordinal) ? "Medicine" :
            nameTh.Contains("มีด", StringComparison.Ordinal) ? "Weapon" : "Tool";

        return new Item
        {
            Id = $"gen_{id}",
            NameTh = nameTh,
            NameEn = id,
            Category = category,
            Subcategory = "generated",
            Rarity = "common",
            WeightKg = 0.5f,
            TradeValue = 1,
            Stackable = false,
            MaxStack = 1,
            FoundIn = new List<string> { "generated_story" },
            DurabilityMax = 1,
            Durability = 1,
            Effects = new ItemEffects
            {
                HpRestore = category == "Medicine" ? 15 : 0,
                HungerRestore = category == "Food" ? 20 : 0,
                ThirstRestore = category == "Water" ? 20 : 0
            },
            DescriptionTh = "ไอเทมเริ่มต้นจากเรื่องราวที่สุ่มขึ้นสำหรับรอบนี้",
            StoryAlias = $"gen_{id}"
        };
    }

    private static async Task<LlmRuntimeSettings> LoadSettingsAsync(CancellationToken cancellationToken)
    {
        var packagedSettings = await LoadPackagedSettingsAsync(cancellationToken).ConfigureAwait(false);

        return new LlmRuntimeSettings
        {
            ApiKey = GetSetting(new[] { "OPENAI_API_KEY", "LLM_API_KEY" }, packagedSettings),
            Endpoint = GetSetting(new[] { "LLM_ENDPOINT" }, packagedSettings),
            Model = GetSetting(new[] { "OPENAI_MODEL", "LLM_MODEL" }, packagedSettings)
        };
    }

    private static async Task<Dictionary<string, string>> LoadPackagedSettingsAsync(CancellationToken cancellationToken)
    {
        foreach (var fileName in new[] { "llm_config.local.json", "llm_config.json" })
        {
            try
            {
                await using var stream = await FileSystem.OpenAppPackageFileAsync(fileName).ConfigureAwait(false);
                var settings = await JsonSerializer.DeserializeAsync<Dictionary<string, string>>(
                    stream,
                    JsonOptions,
                    cancellationToken).ConfigureAwait(false);

                if (settings != null)
                    return settings;
            }
            catch
            {
                // Optional local config file is allowed to be missing.
            }
        }

        return new Dictionary<string, string>();
    }

    private static string? GetSetting(IReadOnlyList<string> keys, IReadOnlyDictionary<string, string> packagedSettings)
    {
        foreach (var key in keys)
        {
            var fromPreferences = Preferences.Default.Get(key, string.Empty);
            if (!string.IsNullOrWhiteSpace(fromPreferences))
                return fromPreferences;

            var fromEnvironment = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
                return fromEnvironment;

            if (packagedSettings.TryGetValue(key, out var fromConfig) && !string.IsNullOrWhiteSpace(fromConfig))
                return fromConfig;
        }

        return null;
    }

    private sealed class LlmRuntimeSettings
    {
        public string? ApiKey { get; set; }
        public string? Endpoint { get; set; }
        public string? Model { get; set; }
    }
}
