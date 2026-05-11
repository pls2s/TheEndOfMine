using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Maui.Storage;
using TheEndOfMine.Models;

namespace TheEndOfMine.Services;

public class LlmGameContentService
{
    private const string ProviderOpenAi = "openai";
    private const string ProviderTyphoon = "typhoon";
    private const string DefaultProvider = ProviderTyphoon;
    private const string OpenAiEndpoint = "https://api.openai.com/v1/responses";
    private const string OpenAiModel = "gpt-5.4-mini";
    private const string TyphoonEndpoint = "https://api.opentyphoon.ai/v1/chat/completions";
    private const string TyphoonModel = "typhoon-v2.5-30b-a3b-instruct";
    private const string SystemPrompt = """
    คุณคือระบบสร้างเนื้อเรื่องของเกม The End of Mine เกมเอาตัวรอดหลังหายนะที่แสดงผลเป็นภาษาไทย

    หน้าที่ของคุณคือสร้างหนึ่งรอบการเล่นใหม่ที่เล่นได้จริง ไม่ใช่เขียนเรื่องสั้นให้อ่านอย่างเดียว

    กฎสำคัญ:
    - ตอบกลับเป็น JSON object ที่ถูกต้องเพียงก้อนเดียวเท่านั้น
    - ห้ามครอบ JSON ด้วย markdown
    - ห้ามใส่คำอธิบาย คอมเมนต์ หรือ key นอก schema ที่กำหนด
    - ข้อความทุกอย่างที่ผู้เล่นเห็นต้องเป็นภาษาไทยธรรมชาติ
    - โทนเรื่องต้องกดดัน สมจริง เน้นการเอาตัวรอด และมีทางเลือกที่ลำบากทางศีลธรรม
    - หลีกเลี่ยงแฟนตาซี เวทมนตร์ มุกตลก ซูเปอร์ฮีโร่ และฉากแอ็กชันทหารเกินจริง
    - ทุก event ต้องเป็นสถานการณ์ที่ผู้เล่นตัดสินใจได้ทันทีในเกม
    - ทุก choice ต้องมี tradeoff ที่มีความหมาย
    - ผลกระทบต่อทรัพยากรต้องสมดุล ห้ามทำให้ตัวเลือกหนึ่งดีกว่าอีกตัวเลือกแบบชัดเจนเสมอ
    - ไอเทมต้องเป็นของใช้เอาตัวรอดที่เข้ากับบริบทของเหตุการณ์
    - id ของไอเทมที่สร้างต้องเป็น lowercase snake_case และขึ้นต้นด้วย gen_

    game engine จะ deserialize คำตอบของคุณทันที ถ้า JSON ผิดหรือ field ไม่ครบ คำตอบจะถูกปฏิเสธ
    """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(45)
    };

    private readonly ImageGenerationService _imageGenerationService;
    private readonly StoryAssetResolver _storyAssetResolver;

    public LlmGameContentService(
        ImageGenerationService? imageGenerationService = null,
        StoryAssetResolver? storyAssetResolver = null)
    {
        _imageGenerationService = imageGenerationService ?? new ImageGenerationService();
        _storyAssetResolver = storyAssetResolver ?? new StoryAssetResolver();
    }

    public async Task<GeneratedGameContent> GenerateNewGameAsync(Survivor survivor, CancellationToken cancellationToken = default)
    {
        return await GenerateChapterContentAsync(survivor, chapterNumber: 1, maxChapters: 4, eventsPerChapter: 8, state: null, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<GeneratedGameContent> GenerateNextChapterAsync(GameState state, CancellationToken cancellationToken = default)
    {
        var nextChapter = Math.Clamp(state.CurrentChapter + 1, 1, state.MaxChapters);
        return await GenerateChapterContentAsync(state.Survivor, nextChapter, state.MaxChapters, state.EventsPerChapter, state, cancellationToken)
            .ConfigureAwait(false);
    }

    private async Task<GeneratedGameContent> GenerateChapterContentAsync(
        Survivor survivor,
        int chapterNumber,
        int maxChapters,
        int eventsPerChapter,
        GameState? state,
        CancellationToken cancellationToken)
    {
        var assetCatalog = await _storyAssetResolver.LoadCatalogAsync(cancellationToken).ConfigureAwait(false);
        var settings = await LoadSettingsAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            return CreateFallbackContent(survivor, chapterNumber, eventsPerChapter, assetCatalog);

        try
        {
            var prompt = BuildPrompt(survivor, chapterNumber, maxChapters, eventsPerChapter, state, assetCatalog);
            var requestJson = JsonSerializer.Serialize(CreateLlmRequest(settings, prompt), JsonOptions);

            using var request = new HttpRequestMessage(HttpMethod.Post, settings.Endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

            using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var contentJson = ExtractMessageContent(responseJson);
            var generated = JsonSerializer.Deserialize<GeneratedGameContent>(NormalizeJson(contentJson), JsonOptions);

            if (generated == null)
                throw new InvalidOperationException("LLM response was empty.");

            NormalizeContent(generated, survivor, eventsPerChapter, assetCatalog);
            await _imageGenerationService.GenerateChapterImagesAsync(
                state ?? CreateImageState(survivor, chapterNumber, maxChapters, eventsPerChapter),
                generated,
                cancellationToken).ConfigureAwait(false);

            generated.UsedRemoteLlm = true;
            return generated;
        }
        catch
        {
            return CreateFallbackContent(survivor, chapterNumber, eventsPerChapter, assetCatalog);
        }
    }

    private static GameState CreateImageState(Survivor survivor, int chapterNumber, int maxChapters, int eventsPerChapter)
    {
        return new GameState
        {
            Survivor = survivor,
            CurrentChapter = chapterNumber,
            MaxChapters = maxChapters,
            EventsPerChapter = eventsPerChapter
        };
    }

    private static object CreateLlmRequest(LlmRuntimeSettings settings, string prompt)
    {
        return settings.Provider == ProviderOpenAi
            ? CreateOpenAiResponsesRequest(settings.Model, prompt)
            : CreateChatCompletionsRequest(settings.Model, prompt);
    }

    private static object CreateOpenAiResponsesRequest(string model, string prompt)
    {
        return new
        {
            model,
            input = new[]
            {
                new
                {
                    role = "system",
                    content = SystemPrompt
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            text = new
            {
                format = new
                {
                    type = "json_object"
                }
            }
        };
    }

    private static object CreateChatCompletionsRequest(string model, string prompt)
    {
        return new
        {
            model,
            messages = new[]
            {
                new
                {
                    role = "system",
                    content = SystemPrompt
                },
                new
                {
                    role = "user",
                    content = prompt
                }
            },
            max_tokens = 8192,
            temperature = 0.7,
            top_p = 0.95,
            repetition_penalty = 1.05,
            stream = false
        };
    }

    private string BuildPrompt(
        Survivor survivor,
        int chapterNumber,
        int maxChapters,
        int eventsPerChapter,
        GameState? state,
        StoryAssetCatalog assetCatalog)
    {
        var phase = chapterNumber switch
        {
            1 => "ตื่นรอด / ตั้งหลัก",
            2 => "ออกหาเสบียง / เจอภัยจริง",
            3 => "เป้าหมายหลัก / ซ่อมวิทยุหรือหาทางหนี",
            4 => "ทางเลือกสุดท้าย / ไป safe zone หรือจบแบบอื่น",
            _ => "เอาตัวรอดต่อเนื่อง"
        };
        var currentStatus = state == null
            ? "เริ่มเกมใหม่ ยังไม่มีประวัติการเล่น"
            : BuildStateSummary(state);
        var startingItemsRule = chapterNumber == 1
            ? "- สร้าง startingItems 3 ชิ้น"
            : "- startingItems ต้องเป็น array ว่าง [] เพราะไม่ใช่ chapter แรก";
        var endingRule = chapterNumber >= maxChapters
            ? "- chapter นี้เป็นบทสุดท้าย event ที่ 8 ต้องเป็นเหตุการณ์ปลายทางที่นำไปสู่ฉากจบ"
            : "- event ที่ 8 ต้องเปิดทางไป chapter ถัดไป แต่ยังไม่จบเกม";
        var assetAliasRules = _storyAssetResolver.FormatPromptAliases(assetCatalog);

        return $$"""
        สร้างเนื้อเรื่อง chapter สำหรับเกม The End of Mine
        ชื่อตัวละคร: {{survivor.Name}}
        เพศตัวละคร: {{survivor.Gender}}
        Chapter: {{chapterNumber}}/{{maxChapters}}
        ธีมของ chapter นี้: {{phase}}
        สถานะเกมปัจจุบัน: {{currentStatus}}

        {{assetAliasRules}}

        ข้อกำหนด:
        - ใช้ภาษาไทยสำหรับ storyTitle, title, description, choice text, resultText, ชื่อไอเทม และคำอธิบายไอเทม
        - storyTitle ให้เป็นชื่อ chapter นี้
        - chapter_alias ต้องเลือกจากรายการ chapter_alias ด้านบนให้เข้ากับบรรยากาศ chapter
        - สร้าง event ให้ครบ {{eventsPerChapter}} เหตุการณ์
        - ทุก event ต้องมี story_alias ที่เลือกจากรายการ event.story_alias ด้านบน และต้องสัมพันธ์กับสถานการณ์ใน event
        - แต่ละ event ต้องมี choice 2 ตัวเลือกพอดี
        - แต่ละ choice ต้องมี id, text, hpEffect, hungerEffect, thirstEffect, fatigueEffect, resultText
        - ค่าผลกระทบตัวเลขต้องสมดุล และอยู่ระหว่าง -30 ถึง 30
        - ใส่ itemReward ให้ choice จำนวน 4 จุดพอดี โดย itemReward ต้องเป็น item object ครบถ้วน
        - ทุก itemReward และ startingItems ต้องมี story_alias ที่เลือกจากรายการ item.story_alias ด้านบน
        - choice อื่นที่ไม่ได้ให้ไอเทมห้ามใส่ key itemReward
        - ทุก event ต้องมี imagePrompt เป็นภาษาอังกฤษ สำหรับสร้างภาพประกอบแบบ realistic gritty survival game art, no text, no UI, no logo
        - ทุก item ใน startingItems และ itemReward ต้องมี image_prompt เป็นภาษาอังกฤษ สำหรับสร้างภาพไอเทมเดี่ยวบนพื้นหลังเรียบ, no text, no logo
        {{startingItemsRule}}
        {{endingRule}}
        - โทนต้องกดดัน เน้นการเอาตัวรอด และแต่ละเหตุการณ์ต้องไม่ซ้ำอารมณ์กัน

        ตอบ JSON ตามรูปแบบนี้เท่านั้น:
        {
          "storyTitle": "string",
          "chapter_alias": "alias_from_chapter_list",
          "events": [
            {
              "id": "evt_01",
              "title": "string",
              "description": "string",
              "story_alias": "alias_from_event_list",
              "imagePrompt": "English image prompt",
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
                    "image_prompt": "English item image prompt",
                    "story_alias": "alias_from_item_list"
                  }
                }
              ]
            }
          ],
          "startingItems": []
        }
        """;
    }

    private static string BuildStateSummary(GameState state)
    {
        var s = state.Survivor;
        var items = s.Inventory.GetItems()
            .Take(8)
            .Select(i => string.IsNullOrWhiteSpace(i.NameTh) ? i.Id : i.NameTh);

        return $"HP {s.HP:0}, Hunger {s.Hunger:0}, Thirst {s.Thirst:0}, Fatigue {s.Fatigue:0}, " +
               $"วันที่ {state.DayCount}, เวลา {state.Hour:D2}:{state.Minute:D2}, " +
               $"chapter ที่จบแล้ว: {string.Join(", ", state.CompletedChapterTitles)}, " +
               $"ไอเทม: {string.Join(", ", items)}";
    }

    private static string ExtractMessageContent(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        var root = doc.RootElement;

        if (root.TryGetProperty("output_text", out var responsesOutputText))
            return responsesOutputText.GetString() ?? string.Empty;

        if (root.TryGetProperty("output", out var output) && output.ValueKind == JsonValueKind.Array)
        {
            foreach (var outputItem in output.EnumerateArray())
            {
                if (!outputItem.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
                    continue;

                foreach (var contentItem in content.EnumerateArray())
                {
                    if (contentItem.TryGetProperty("text", out var text))
                        return text.GetString() ?? string.Empty;
                }
            }
        }

        if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var first = choices[0];
            if (first.TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                return content.GetString() ?? string.Empty;
            }
        }

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

    private void NormalizeContent(
        GeneratedGameContent content,
        Survivor survivor,
        int eventsPerChapter,
        StoryAssetCatalog assetCatalog)
    {
        if (string.IsNullOrWhiteSpace(content.StoryTitle))
            content.StoryTitle = $"เส้นทางของ {survivor.Name}";

        content.ChapterAlias = _storyAssetResolver.NormalizeAlias(
            content.ChapterAlias,
            assetCatalog.ChapterAliases,
            content.StoryTitle);
        content.ChapterImagePath = _storyAssetResolver.ResolveChapterPath(survivor.Gender, content.ChapterAlias, assetCatalog);

        content.Events = content.Events
            .Where(e => !string.IsNullOrWhiteSpace(e.Title) && e.Choices.Count >= 2)
            .Take(eventsPerChapter)
            .ToList();

        if (content.Events.Count < eventsPerChapter)
            throw new InvalidOperationException("LLM returned too few playable events.");

        var rewardCount = 0;
        for (var i = 0; i < content.Events.Count; i++)
        {
            var gameEvent = content.Events[i];
            gameEvent.Id = string.IsNullOrWhiteSpace(gameEvent.Id) ? $"evt_{i + 1:D2}" : gameEvent.Id;
            gameEvent.StoryAlias = _storyAssetResolver.NormalizeAlias(
                gameEvent.StoryAlias ?? gameEvent.ImageAlias,
                assetCatalog.EventAliases,
                gameEvent.Title);
            gameEvent.ImagePath = _storyAssetResolver.ResolveEventPath(survivor.Gender, gameEvent.StoryAlias, assetCatalog);
            gameEvent.Description = string.IsNullOrWhiteSpace(gameEvent.Description)
                ? "สถานการณ์ตรงหน้าบีบให้คุณต้องตัดสินใจทันที"
                : gameEvent.Description;
            gameEvent.ImagePrompt = string.IsNullOrWhiteSpace(gameEvent.ImagePrompt)
                ? $"Realistic gritty post-apocalyptic Thai survival game scene, {gameEvent.Title}, no text, no UI, no logo"
                : gameEvent.ImagePrompt;
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
                {
                    NormalizeItem(choice.ItemReward, $"reward_{i + 1}_{c + 1}", assetCatalog);
                    rewardCount++;
                    if (rewardCount > 5)
                        choice.ItemReward = null;
                }
            }
        }

        content.StartingItems = content.StartingItems.Take(3).ToList();
        for (var i = 0; i < content.StartingItems.Count; i++)
            NormalizeItem(content.StartingItems[i], $"start_{i + 1}", assetCatalog);
    }

    private static float ClampEffect(float value) => Math.Clamp(value, -30f, 30f);

    private void NormalizeItem(Item item, string fallbackId, StoryAssetCatalog assetCatalog)
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
        item.ImagePrompt = string.IsNullOrWhiteSpace(item.ImagePrompt)
            ? $"Single survival item, {item.NameEn}, realistic game inventory icon, plain dark background, no text, no logo"
            : item.ImagePrompt;
        item.StoryAlias = _storyAssetResolver.NormalizeAlias(
            item.StoryAlias,
            assetCatalog.ItemAliases,
            $"{item.NameEn} {item.NameTh} {item.Id}");
        item.ImagePath = _storyAssetResolver.ResolveItemPath(item.StoryAlias, assetCatalog);
    }

    private GeneratedGameContent CreateFallbackContent(
        Survivor survivor,
        int chapterNumber,
        int eventsPerChapter,
        StoryAssetCatalog assetCatalog)
    {
        var seed = Guid.NewGuid().ToString("N")[..8];
        var places = new[] { "โรงพยาบาลร้าง", "สถานีรถไฟใต้ดิน", "ตลาดกลางคืนที่ถูกทิ้ง", "ดาดฟ้าอพาร์ตเมนต์", "อุโมงค์ระบายน้ำ" };
        var threats = new[] { "กลุ่มคนถืออาวุธ", "ฝูงผู้ติดเชื้อ", "ไฟไหม้ที่ลามช้าๆ", "พายุฝุ่นพิษ", "เสียงวิทยุปริศนา" };
        var itemNames = new[] { "น้ำกรองขวดเล็ก", "ผ้าพันแผลสะอาด", "มีดพับขึ้นสนิม", "อาหารกระป๋องบุบ", "ไฟฉายมือหมุน", "ยาแก้ปวดเหลือครึ่งแผง" };

        var events = new List<GameEvent>();
        for (var i = 0; i < eventsPerChapter; i++)
        {
            var place = places[Random.Shared.Next(places.Length)];
            var threat = threats[Random.Shared.Next(threats.Length)];
            events.Add(new GameEvent
            {
                Id = $"fallback_ch{chapterNumber}_{seed}_{i + 1:D2}",
                Title = $"บทที่ {chapterNumber} - {place}: ทางเลือกที่ {i + 1}",
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

        var content = new GeneratedGameContent
        {
            StoryTitle = $"บทที่ {chapterNumber}: คืนเอาตัวรอด {seed}",
            UsedRemoteLlm = false,
            Events = events,
            StartingItems = chapterNumber == 1 ? new List<Item>
            {
                CreateFallbackItem("น้ำกรองขวดเล็ก", $"start_{seed}_water"),
                CreateFallbackItem("อาหารกระป๋องบุบ", $"start_{seed}_food"),
                CreateFallbackItem("ผ้าพันแผลสะอาด", $"start_{seed}_bandage")
            } : new List<Item>()
        };

        NormalizeContent(content, survivor, eventsPerChapter, assetCatalog);
        content.UsedRemoteLlm = false;
        return content;
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
        var envFileSettings = await LoadEnvFileSettingsAsync(cancellationToken).ConfigureAwait(false);
        var packagedSettings = await LoadPackagedSettingsAsync(cancellationToken).ConfigureAwait(false);
        var provider = NormalizeProvider(GetSetting(new[] { "LLM_PROVIDER" }, envFileSettings, packagedSettings));

        return new LlmRuntimeSettings
        {
            Provider = provider,
            ApiKey = provider == ProviderOpenAi
                ? GetSetting(new[] { "OPENAI_API_KEY", "LLM_API_KEY" }, envFileSettings, packagedSettings)
                : GetSetting(new[] { "TYPHOON_API_KEY", "LLM_API_KEY" }, envFileSettings, packagedSettings),
            Endpoint = provider == ProviderOpenAi
                ? GetSetting(new[] { "OPENAI_ENDPOINT", "LLM_ENDPOINT" }, envFileSettings, packagedSettings) ?? OpenAiEndpoint
                : GetSetting(new[] { "TYPHOON_ENDPOINT", "LLM_ENDPOINT" }, envFileSettings, packagedSettings) ?? TyphoonEndpoint,
            Model = provider == ProviderOpenAi
                ? GetSetting(new[] { "OPENAI_MODEL", "LLM_MODEL" }, envFileSettings, packagedSettings) ?? OpenAiModel
                : GetSetting(new[] { "TYPHOON_MODEL", "LLM_MODEL" }, envFileSettings, packagedSettings) ?? TyphoonModel
        };
    }

    private static string NormalizeProvider(string? provider)
    {
        if (string.Equals(provider, ProviderOpenAi, StringComparison.OrdinalIgnoreCase))
            return ProviderOpenAi;

        return ProviderTyphoon;
    }

    private static async Task<Dictionary<string, string>> LoadEnvFileSettingsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync("llm.env").ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            return ParseEnv(content);
        }
        catch
        {
            return new Dictionary<string, string>();
        }
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

    private static string? GetSetting(
        IReadOnlyList<string> keys,
        IReadOnlyDictionary<string, string> envFileSettings,
        IReadOnlyDictionary<string, string> packagedSettings)
    {
        foreach (var key in keys)
        {
            if (envFileSettings.TryGetValue(key, out var fromEnvFile) && !string.IsNullOrWhiteSpace(fromEnvFile))
                return fromEnvFile;

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

    private static Dictionary<string, string> ParseEnv(string content)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        using var reader = new StringReader(content);

        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('#'))
                continue;

            var equalsIndex = trimmed.IndexOf('=');
            if (equalsIndex <= 0)
                continue;

            var key = trimmed[..equalsIndex].Trim();
            var value = trimmed[(equalsIndex + 1)..].Trim();

            if ((value.StartsWith('"') && value.EndsWith('"')) ||
                (value.StartsWith('\'') && value.EndsWith('\'')))
            {
                value = value[1..^1];
            }

            values[key] = value;
        }

        return values;
    }

    private sealed class LlmRuntimeSettings
    {
        public string Provider { get; set; } = DefaultProvider;
        public string? ApiKey { get; set; }
        public string Endpoint { get; set; } = TyphoonEndpoint;
        public string Model { get; set; } = TyphoonModel;
    }
}
