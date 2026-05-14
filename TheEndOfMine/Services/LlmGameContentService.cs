using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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
    private const int MaxLlmAttempts = 3;
    private const string SystemPrompt = """
    คุณคือระบบสร้างเนื้อเรื่องของเกม The End of Mine เกมเอาตัวรอดหลังหายนะที่แสดงผลเป็นภาษาไทย

    หน้าที่ของคุณคือสร้างหนึ่งรอบการเล่นใหม่ที่เล่นได้จริง ไม่ใช่เขียนเรื่องสั้นให้อ่านอย่างเดียว

    กฎสำคัญ:
    - ตอบกลับเป็น JSON object ที่ถูกต้องเพียงก้อนเดียวเท่านั้น
    - ห้ามครอบ JSON ด้วย markdown
    - ห้ามใส่คำอธิบาย คอมเมนต์ หรือ key นอก schema ที่กำหนด
    - ข้อความทุกอย่างที่ผู้เล่นเห็นต้องเป็นภาษาไทยธรรมชาติ
    - ภาษาไทยต้องเหมือนคนไทยเขียนเอง ห้ามใช้สำนวนแปลตรงจากอังกฤษหรือประโยคแข็ง ๆ
    - ตรวจคำผิดก่อนตอบ เช่น ใช้ "คนหนึ่ง" ไม่ใช่ "คนนึ่ง", ใช้ "ขอความช่วยเหลือ" ไม่ใช่ "ขอช่วยเหลือ"
    - โทนเรื่องต้องกดดัน สมจริง เน้นการเอาตัวรอด และมีทางเลือกที่ลำบากทางศีลธรรม
    - หลีกเลี่ยงแฟนตาซี เวทมนตร์ มุกตลก ซูเปอร์ฮีโร่ และฉากแอ็กชันทหารเกินจริง
    - ทุก event ต้องเป็นสถานการณ์ที่ผู้เล่นตัดสินใจได้ทันทีในเกม
    - ทุก choice ต้องมี tradeoff ที่มีความหมาย
    - ผลกระทบต่อทรัพยากรต้องสมดุล ห้ามทำให้ตัวเลือกหนึ่งดีกว่าอีกตัวเลือกแบบชัดเจนเสมอ
    - hpEffect ที่เป็นบวกใช้ได้เฉพาะตัวเลือกที่ผู้เล่นกินอาหาร ดื่มน้ำ หรือรักษาแผล/ใช้ยาในเหตุการณ์นั้นจริง ๆ เท่านั้น
    - ไอเทมต้องเป็นของใช้เอาตัวรอดที่เข้ากับบริบทของเหตุการณ์
    - id ของไอเทมที่สร้างต้องเป็น lowercase snake_case และขึ้นต้นด้วย gen_

    game engine จะ deserialize คำตอบของคุณทันที ถ้า JSON ผิดหรือ field ไม่ครบ คำตอบจะถูกปฏิเสธ
    """;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private static readonly string[] FallbackRewardNames =
    [
        "น้ำกรองขวดเล็ก",
        "น้ำดื่มขวดเล็ก",
        "กระติกน้ำเก่า",
        "น้ำสะอาดครึ่งขวด",
        "ผ้าพันแผลสะอาด",
        "อาหารกระป๋องบุบ",
        "อาหารแห้งห่อเล็ก",
        "มีดพับขึ้นสนิม",
        "ไฟฉายมือหมุน",
        "ยาแก้ปวดเหลือครึ่งแผง",
        "เชือกขาดครึ่ง"
    ];

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(120)
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
        {
            var missingKeyMessage = settings.Provider == ProviderOpenAi
                ? "ไม่พบ OPENAI_API_KEY หรือ LLM_API_KEY"
                : "ไม่พบ TYPHOON_API_KEY หรือ LLM_API_KEY";
            return await CreateFallbackContentAsync(survivor, chapterNumber, eventsPerChapter, assetCatalog, missingKeyMessage, state, cancellationToken)
                .ConfigureAwait(false);
        }

        try
        {
            var prompt = BuildPrompt(survivor, chapterNumber, maxChapters, eventsPerChapter, state, assetCatalog);
            var requestJson = JsonSerializer.Serialize(CreateLlmRequest(settings, prompt), JsonOptions);
            var responseJson = await SendLlmRequestWithRetryAsync(settings, requestJson, cancellationToken).ConfigureAwait(false);
            var contentJson = ExtractMessageContent(responseJson);
            var generated = JsonSerializer.Deserialize<GeneratedGameContent>(NormalizeJson(contentJson), JsonOptions);

            if (generated == null)
                throw new InvalidOperationException("LLM response was empty.");

            NormalizeContent(generated, survivor, eventsPerChapter, assetCatalog, state);
            await _imageGenerationService.GenerateChapterImagesAsync(
                state ?? CreateImageState(survivor, chapterNumber, maxChapters, eventsPerChapter),
                generated,
                cancellationToken).ConfigureAwait(false);

            generated.UsedRemoteLlm = true;
            return generated;
        }
        catch (Exception ex)
        {
            return await CreateFallbackContentAsync(
                survivor,
                chapterNumber,
                eventsPerChapter,
                assetCatalog,
                $"เรียก {settings.Provider} ไม่สำเร็จ: {ex.Message}",
                state,
                cancellationToken).ConfigureAwait(false);
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

    private static async Task<string> SendLlmRequestWithRetryAsync(
        LlmRuntimeSettings settings,
        string requestJson,
        CancellationToken cancellationToken)
    {
        Exception? lastException = null;

        for (var attempt = 1; attempt <= MaxLlmAttempts; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Post, settings.Endpoint);
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
                request.Headers.ConnectionClose = true;
                request.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                using var response = await HttpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                    .ConfigureAwait(false);
                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"HTTP {(int)response.StatusCode}: {TrimForError(responseJson)}");

                return responseJson;
            }
            catch (Exception ex) when (IsTransientLlmException(ex) && attempt < MaxLlmAttempts)
            {
                lastException = ex;
                await Task.Delay(TimeSpan.FromMilliseconds(650 * attempt), cancellationToken).ConfigureAwait(false);
            }
        }

        throw lastException ?? new HttpRequestException("LLM request failed.");
    }

    private static bool IsTransientLlmException(Exception ex)
    {
        var message = ex.Message;
        return ex is HttpRequestException or TaskCanceledException or IOException ||
               message.Contains("Socket closed", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("Connection reset", StringComparison.OrdinalIgnoreCase) ||
               message.Contains("timed out", StringComparison.OrdinalIgnoreCase);
    }

    private static string TrimForError(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "empty response";

        return value.Length <= 500 ? value : value[..500];
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
        var storyMemory = BuildStoryMemoryForPrompt(state);
        var inventoryRules = BuildInventoryUseRules(state);
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
        ความทรงจำเนื้อเรื่องล่าสุด:
        {{storyMemory}}
        ไอเทมที่มีและวิธีใช้ที่สมเหตุผล:
        {{inventoryRules}}

        {{assetAliasRules}}

        ข้อกำหนด:
        - ใช้ภาษาไทยสำหรับ storyTitle, title, description, choice text, resultText, ชื่อไอเทม และคำอธิบายไอเทม
        - ภาษาไทยต้องลื่นและสมจริง หลีกเลี่ยงวลีแปลก เช่น "เสียงร้องขอช่วยเหลือ", "เปิดประตูด้วยเท้า", "มีดสนิม", "คนนึ่ง"
        - ถ้าจะบอกว่ามีคนขอให้ช่วย ให้ใช้ "เสียงร้องขอความช่วยเหลือ" หรือ "เสียงคนร้องให้ช่วย"
        - ถ้าจะเปิดประตูหรือฝาทางลง ให้ใช้ "ค่อย ๆ ผลักประตู", "ง้างฝา", "แง้มประตู" ตามบริบท ห้ามเขียนว่าเปิดด้วยเท้าเว้นแต่ตั้งใจเตะประตูจริง ๆ
        - ใช้คำว่า "ร่าง" หรือ "ศพ" ให้เหมาะกับสถานการณ์ ห้ามเขียนประโยคสะดุด เช่น "ศพผู้หญิงคนนึ่ง"
        - storyTitle ให้เป็นชื่อ chapter นี้
        - chapter_alias ต้องเลือกจากรายการ chapter_alias ด้านบนให้เข้ากับบรรยากาศ chapter
        - สร้าง event ให้ครบ {{eventsPerChapter}} เหตุการณ์
        - ทุก event ต้องมี story_alias ที่เลือกจากรายการ event.story_alias ด้านบน และต้องสัมพันธ์กับสถานการณ์ใน event
        - แต่ละ event ต้องมี choice 2 ตัวเลือกพอดี
        - แต่ละ choice ต้องมี id, text, hpEffect, hungerEffect, thirstEffect, fatigueEffect, resultText
        - ค่าผลกระทบตัวเลขต้องสมดุล และอยู่ระหว่าง -30 ถึง 30
        - hpEffect > 0 ใช้ได้เฉพาะ choice ที่กินอาหาร ดื่มน้ำ หรือรักษา/ใช้ยาในทันทีเท่านั้น ห้ามเพิ่ม HP จากการพัก หลบ ซ่อน หรือแค่เจอไอเทม
        - เกมมีระบบ Noise และ Infection แม้ schema ไม่มี field ตรง ๆ: ตัวเลือกที่ยิงปืน ทุบ งัด พัง กระจกแตก หรือตะโกนจะเพิ่ม Noise, ตัวเลือกที่โดนกัด แผลสกปรก เลือด สนิม ศพ น้ำเน่า หรือซากเน่าจะเพิ่ม Infection
        - ถ้าผู้เล่น Noise สูง ให้เขียน event ที่ศัตรู/อันตรายถูกดึงดูดจากเสียงอย่างสมเหตุผล
        - ถ้าผู้เล่น Infection สูง ให้เขียนอาการเช่นหนาวสั่น ไข้ แผลบวม มือสั่น หรือการตัดสินใจที่ยากขึ้น
        - ตัวเลือกที่ใช้ยา ทำแผล พันแผล หรือปฐมพยาบาลต้องลดความเสี่ยงติดเชื้อใน resultText และห้ามรักษาหายทันทีแบบเวทมนตร์
        - ใส่ itemRewards รวมทั้ง chapter อย่างน้อย 10 ชิ้น โดย itemRewards เป็น array ของ item object ครบถ้วน และ 1 choice ให้ได้ 1-3 ชิ้นได้ตามบริบท
        - ทุก itemRewards, itemReward และ startingItems ต้องมี story_alias ที่เลือกจากรายการ item.story_alias ด้านบน
        - story_alias ของไอเทมต้องตรงกับชื่อและคำอธิบาย เช่น ชุดปฐมพยาบาลต้องใช้ first_aid_kit, ผ้าพันแผลต้องใช้ bandage, ขวดน้ำต้องใช้ water_bottle, อาหารกระป๋องต้องใช้ canned_food, ไฟฉายต้องใช้ flashlight
        - ถ้า choice ให้ itemRewards/itemReward ต้องเขียน resultText ให้ระบุชื่อไอเทมเหล่านั้นตรง ๆ ห้ามเล่าอย่างหนึ่งแต่ให้ของอีกอย่าง
        - choice อื่นที่ไม่ได้ให้ไอเทมห้ามใส่ key itemRewards หรือ itemReward
        - ทุก event ต้องมี imagePrompt เป็นภาษาอังกฤษ สำหรับสร้างภาพประกอบแบบ realistic gritty survival game art, no text, no UI, no logo
        - ทุก item ใน startingItems, itemRewards และ itemReward ต้องมี image_prompt เป็นภาษาอังกฤษ สำหรับสร้างภาพไอเทมเดี่ยวบนพื้นหลังเรียบ, no text, no logo
        {{startingItemsRule}}
        {{endingRule}}
        - chapter นี้ต้องมีเป้าหมายหลักหนึ่งอย่างที่ชัดเจน และทุก event ต้องผลักผู้เล่นเข้าใกล้หรือไกลจากเป้าหมายนั้น
        - เหตุการณ์ทั้งหมดต้องเรียงเป็นเหตุและผลต่อเนื่องกัน ไม่ใช่ฉากสุ่มคนละเรื่อง
        - event ที่ 2 เป็นต้นไปต้องอ้างอิงผลจาก event ก่อนหน้า เช่น คนเดิม ร่องรอยเดิม สถานที่ต่อเนื่อง ไอเทม/เบาะแสที่เพิ่งได้ หรือผลเสียจากตัวเลือกก่อนหน้า
        - ถ้ามี NPC ภัยคุกคาม หรือปริศนาสำคัญ ให้กลับมาอย่างน้อย 2 ครั้งใน chapter เพื่อให้รู้สึกเป็นเส้นเรื่องเดียวกัน
        - resultText ของ choice ต้องทิ้งผลหรือเบาะแสที่ event ถัดไปใช้ต่อได้
        - ห้ามรีเซ็ตสถานการณ์เหมือนไม่เคยเกิด event ก่อนหน้า และห้ามเริ่มทุก event ด้วยฉากใหม่ที่ไม่เกี่ยวกับเรื่องเดิม
        - ถ้า choice ระบุว่า "ใช้ไอเทม" ต้องใช้ไอเทมที่ผู้เล่นมีจริง หรือไอเทมที่อยู่ในฉากนั้นอย่างชัดเจน และต้องใช้ตรงหน้าที่ของไอเทม
        - ถ้าผู้เล่นมีไอเทมที่เหมาะกับสถานการณ์ ให้สร้างอย่างน้อย 1 choice ใน chapter ที่ใช้ไอเทมนั้นอย่างสมเหตุผล เช่น ไฟฉายในที่มืด, เชือกกับการปีน/ข้าม, แผนที่กับการเลือกเส้นทาง, ยากับแผล, มีดกับการตัด/ป้องกันตัว
        - choice ที่ใช้ไอเทมควรมีผลเสียต่ำกว่าทางเลือกมือเปล่า แต่ยังต้องมี tradeoff เช่น เสีย durability, เสียงดังขึ้น, ใช้เวลา หรือเสี่ยงเสียไอเทม
        - ห้ามใช้ไอเทมผิดบริบท เช่น กระเป๋าเป้มีประโยชน์คือเพิ่มช่องเก็บของ/จัดสัมภาระเท่านั้น ห้ามใช้เปิดทาง งัดประตู ตัดเหล็ก รักษาแผล กิน ดื่ม หรือโจมตี
        - ถ้าจะให้ backpack เป็น itemReward ต้องให้เป็นไอเทม container ที่ช่วยเพิ่มช่องเก็บของจริง และ resultText ต้องสื่อว่าเก็บของได้มากขึ้น ไม่ใช่ใช้เป็นเครื่องมือ
        - เครื่องมือเปิดทางต้องเป็นของที่เหมาะจริง เช่น ชะแลง มีด คีม ไขควง เชือก ไฟฉาย หรืออุปกรณ์ซ่อม โดยต้องอธิบายว่าใช้ทำอะไร
        - ถ้าไม่มีไอเทมที่เหมาะ ให้เขียนเป็นการกระทำทั่วไป เช่น "ค่อย ๆ ลงไปตรวจ" หรือ "ถอยมาตั้งหลัก" แทนการฝืนใช้ไอเทม
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
                  "itemRewards": [
                    {
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
                  ]
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
               $"Noise {state.Noise:0}, Infection {state.Infection:0}, " +
               $"วันที่ {state.DayCount}, เวลา {state.Hour:D2}:{state.Minute:D2}, " +
               $"chapter ที่จบแล้ว: {string.Join(", ", state.CompletedChapterTitles)}, " +
               $"ไอเทม: {string.Join(", ", items)}, " +
               $"เส้นเรื่อง: {(string.IsNullOrWhiteSpace(state.StoryArcSummary) ? "ยังไม่มี" : state.StoryArcSummary)}";
    }

    private static string BuildStoryMemoryForPrompt(GameState? state)
    {
        if (state == null)
            return "ยังไม่มีเหตุการณ์ก่อนหน้า";

        state.StoryMemory ??= new List<StoryMemoryEntry>();

        if (state.StoryMemory.Count == 0 && string.IsNullOrWhiteSpace(state.StoryArcSummary))
            return "ยังไม่มีเหตุการณ์ก่อนหน้า";

        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(state.StoryArcSummary))
            lines.Add($"สรุปเส้นเรื่อง: {state.StoryArcSummary}");

        lines.AddRange(state.StoryMemory
            .TakeLast(8)
            .Select(memory => $"- บท {memory.Chapter} วันที่ {memory.Day} เวลา {memory.Time}: {memory.Summary}"));

        return string.Join("\n", lines);
    }

    private static string BuildInventoryUseRules(GameState? state)
    {
        if (state == null)
            return "เริ่มเกมใหม่: ถ้าจะใช้ไอเทมใน choice ต้องเป็น startingItems ที่สร้างให้ผู้เล่นหรือของที่อยู่ในฉากอย่างชัดเจน";

        var items = state.Survivor.Inventory.GetItems().Take(12).ToList();
        if (items.Count == 0)
            return "ไม่มีไอเทมในกระเป๋า: ห้ามเขียน choice ที่อ้างว่าใช้ไอเทมส่วนตัว";

        var lines = items.Select(item =>
        {
            var name = string.IsNullOrWhiteSpace(item.NameTh) ? item.Id : item.NameTh;
            return $"- {name}: {DescribeReasonableItemUse(item)}";
        });

        return string.Join("\n", lines);
    }

    private static string DescribeReasonableItemUse(Item item)
    {
        var effects = item.Effects;
        var category = item.Category.ToLowerInvariant();
        var text = $"{item.Id} {item.NameTh} {item.NameEn} {item.Subcategory}".ToLowerInvariant();

        if (effects?.IsContainer == true || text.Contains("backpack", StringComparison.Ordinal) || item.NameTh.Contains("กระเป๋า", StringComparison.Ordinal))
            return "เพิ่มช่องเก็บของและใช้จัดสัมภาระเท่านั้น ห้ามใช้เปิดทาง งัด ต่อสู้ กิน ดื่ม หรือรักษา";
        if (category == "food" || effects?.HungerRestore > 0)
            return "กินเพื่อเพิ่มอาหารเท่านั้น";
        if (category == "water" || effects?.ThirstRestore > 0)
            return "ดื่มเพื่อเพิ่มน้ำเท่านั้น";
        if (category == "medicine" || effects?.HpRestore > 0 || item.IsMedicalItem)
            return "ใช้รักษาแผลหรือบรรเทาอาการบาดเจ็บ";
        if (category == "weapon" || effects?.DmgMin > 0 || effects?.DmgMax > 0)
            return "ใช้ป้องกันตัว ตัดเชือก หรือข่มขู่ในระยะที่สมเหตุผล";
        if (effects?.IsLightSource == true)
            return "ใช้ส่องทางในที่มืดหรือส่งสัญญาณระยะใกล้";
        if (effects?.IsRope == true)
            return "ใช้ปีน ผูก ดึง หรือข้ามช่องว่าง";
        if (effects?.IsRepairMaterial == true || effects?.RepairAmount > 0)
            return "ใช้ซ่อมอุปกรณ์หรือเสริมของที่เสีย";
        if (effects?.IsGeneralTool == true || category == "tool")
            return "ใช้แก้ปัญหาเชิงเครื่องมือ เช่น ไข งัด ตัด ซ่อม หรือเปิดฝาที่เหมาะกับชนิดของเครื่องมือ";
        if (effects?.IsNavigation == true)
            return "ใช้ดูทิศ วางแผนเส้นทาง หรือทำเครื่องหมาย";
        if (effects?.IsCommunication == true || effects?.IsSignal == true)
            return "ใช้สื่อสาร รับสัญญาณ หรือส่งสัญญาณ";

        return "ใช้ได้เฉพาะตามคำอธิบายของไอเทม ห้ามฝืนใช้แทนเครื่องมือเฉพาะทาง";
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
        StoryAssetCatalog assetCatalog,
        GameState? state = null)
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
            ThaiNarrativeTextNormalizer.Normalize(gameEvent);
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
                NormalizeChoiceItemContext(choice);
                EventChoiceInventoryGuard.Normalize(choice, GetAvailableChoiceItems(survivor, content, state));
                choice.Text = ThaiNarrativeTextNormalizer.Normalize(choice.Text);
                choice.ResultText = ThaiNarrativeTextNormalizer.Normalize(choice.ResultText);

                if (choice.ItemReward != null && choice.ItemRewards.Count > 2)
                    choice.ItemRewards = choice.ItemRewards.Take(2).ToList();
                else if (choice.ItemRewards.Count > 3)
                    choice.ItemRewards = choice.ItemRewards.Take(3).ToList();

                var rewardIndex = 0;
                foreach (var item in choice.GetItemRewards().ToList())
                {
                    NormalizeItem(item, $"reward_{i + 1}_{c + 1}_{rewardIndex + 1}", assetCatalog);
                    rewardIndex++;
                    rewardCount++;
                }

                if (choice.ItemsAdd.Count > 0 && GetRewardCount(choice) < 3)
                {
                    foreach (var itemReference in choice.ItemsAdd.Take(3 - GetRewardCount(choice)))
                    {
                        var item = CreateFallbackItemFromStoryTreeReference(itemReference, $"story_tree_{i + 1}_{c + 1}_{rewardIndex + 1}");
                        AddReward(choice, item);
                        NormalizeItem(item, $"story_tree_{i + 1}_{c + 1}_{rewardIndex + 1}", assetCatalog);
                        ItemRewardConsistencyService.Normalize(item);
                        rewardIndex++;
                        rewardCount++;
                    }
                }

                ItemRewardConsistencyService.Normalize(choice);

                if (choice.HpEffect > 0 && !AllowsPositiveHpRecovery(choice))
                    choice.HpEffect = 0;
            }
        }

        content.StartingItems = content.StartingItems.Take(3).ToList();
        for (var i = 0; i < content.StartingItems.Count; i++)
        {
            NormalizeItem(content.StartingItems[i], $"start_{i + 1}", assetCatalog);
            ItemRewardConsistencyService.Normalize(content.StartingItems[i]);
        }

        EnsureMinimumItemRewards(content, assetCatalog, rewardCount, Math.Min(14, Math.Max(10, eventsPerChapter * 2)));
        EnsureBalancedResourceRewards(content, assetCatalog, eventsPerChapter);
    }

    private static float ClampEffect(float value) => Math.Clamp(value, -30f, 30f);

    private static int GetRewardCount(EventChoice choice) => choice.GetItemRewards().Count();

    private static void AddReward(EventChoice choice, Item item)
    {
        if (choice.ItemReward == null)
            choice.ItemReward = item;
        else
            choice.ItemRewards.Add(item);
    }

    private void EnsureMinimumItemRewards(
        GeneratedGameContent content,
        StoryAssetCatalog assetCatalog,
        int currentRewardCount,
        int targetRewardCount)
    {
        if (currentRewardCount >= targetRewardCount)
            return;

        var rewardCount = currentRewardCount;
        foreach (var gameEvent in content.Events)
        {
            foreach (var choice in gameEvent.Choices)
            {
                if (rewardCount >= targetRewardCount)
                    return;

                if (GetRewardCount(choice) >= 3)
                    continue;

                var fallbackName = FallbackRewardNames[Random.Shared.Next(FallbackRewardNames.Length)];
                var item = CreateFallbackItem(fallbackName, $"bonus_{rewardCount + 1}_{gameEvent.Id}_{choice.Id}");
                AddReward(choice, item);
                NormalizeItem(item, $"bonus_{rewardCount + 1}_{gameEvent.Id}_{choice.Id}", assetCatalog);
                ItemRewardConsistencyService.Normalize(choice);
                rewardCount++;
            }
        }
    }

    private void EnsureBalancedResourceRewards(
        GeneratedGameContent content,
        StoryAssetCatalog assetCatalog,
        int eventsPerChapter)
    {
        var targetFood = Math.Max(3, eventsPerChapter);
        var targetWater = Math.Max(5, eventsPerChapter + 1);

        EnsureResourceReward(content, assetCatalog, "Food", targetFood, "อาหารกระป๋องบุบ");
        EnsureResourceReward(content, assetCatalog, "Water", targetWater, "น้ำกรองขวดเล็ก");
    }

    private void EnsureResourceReward(
        GeneratedGameContent content,
        StoryAssetCatalog assetCatalog,
        string category,
        int targetCount,
        string fallbackName)
    {
        var currentCount = content.Events
            .SelectMany(gameEvent => gameEvent.Choices)
            .SelectMany(choice => choice.GetItemRewards())
            .Count(item => item.Category.Equals(category, StringComparison.OrdinalIgnoreCase));

        if (currentCount >= targetCount)
            return;

        foreach (var gameEvent in content.Events)
        {
            foreach (var choice in gameEvent.Choices)
            {
                if (currentCount >= targetCount)
                    return;

                if (GetRewardCount(choice) >= 3)
                    continue;

                var item = CreateFallbackItem(fallbackName, $"resource_{category.ToLowerInvariant()}_{currentCount + 1}_{gameEvent.Id}_{choice.Id}");
                AddReward(choice, item);
                NormalizeItem(item, $"resource_{category.ToLowerInvariant()}_{currentCount + 1}_{gameEvent.Id}_{choice.Id}", assetCatalog);
                ItemRewardConsistencyService.Normalize(choice);
                currentCount++;
            }
        }
    }

    private static void NormalizeChoiceItemContext(EventChoice choice)
    {
        if (!MentionsBackpack(choice.Text) && !MentionsBackpack(choice.ResultText))
            return;

        if (!LooksLikeInvalidBackpackUse(choice.Text) && !LooksLikeInvalidBackpackUse(choice.ResultText))
            return;

        choice.Text = ReplaceInvalidBackpackUse(choice.Text);
        choice.ResultText = ReplaceInvalidBackpackUse(choice.ResultText);
    }

    private static bool MentionsBackpack(string text)
    {
        return text.Contains("กระเป๋าเป้", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("กระเป๋า", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("backpack", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeInvalidBackpackUse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return text.Contains("เปิดทาง", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("เปิดประตู", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("งัด", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("ตัด", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("ฟัน", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("พัง", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("ขุด", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("โจมตี", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("รักษา", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("กิน", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("ดื่ม", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReplaceInvalidBackpackUse(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return text;

        var normalized = text
            .Replace("ใช้กระเป๋าเป้เปิดทาง", "ค่อย ๆ ตรวจทางลงไปดู", StringComparison.OrdinalIgnoreCase)
            .Replace("ใช้กระเป๋าเปิดทาง", "ค่อย ๆ ตรวจทางลงไปดู", StringComparison.OrdinalIgnoreCase)
            .Replace("ใช้ backpack เปิดทาง", "ค่อย ๆ ตรวจทางลงไปดู", StringComparison.OrdinalIgnoreCase)
            .Replace("ใช้กระเป๋าเป้งัด", "ใช้มือคลำหาจุดเปิดอย่างระวัง", StringComparison.OrdinalIgnoreCase)
            .Replace("ใช้กระเป๋างัด", "ใช้มือคลำหาจุดเปิดอย่างระวัง", StringComparison.OrdinalIgnoreCase);

        if (LooksLikeInvalidBackpackUse(normalized))
        {
            normalized = normalized
                .Replace("กระเป๋าเป้", "ความระมัดระวัง", StringComparison.OrdinalIgnoreCase)
                .Replace("กระเป๋า", "ความระมัดระวัง", StringComparison.OrdinalIgnoreCase)
                .Replace("backpack", "careful movement", StringComparison.OrdinalIgnoreCase);
        }

        return normalized;
    }

    private static bool AllowsPositiveHpRecovery(EventChoice choice)
    {
        if (choice.HungerEffect > 0 || choice.ThirstEffect > 0)
            return true;

        var text = $"{choice.Text} {choice.ResultText}".ToLowerInvariant();
        return text.Contains("heal", StringComparison.Ordinal) ||
               text.Contains("treat", StringComparison.Ordinal) ||
               text.Contains("med", StringComparison.Ordinal) ||
               text.Contains("medicine", StringComparison.Ordinal) ||
               text.Contains("bandage", StringComparison.Ordinal) ||
               text.Contains("first aid", StringComparison.Ordinal) ||
               text.Contains("eat", StringComparison.Ordinal) ||
               text.Contains("drink", StringComparison.Ordinal) ||
               text.Contains("consume", StringComparison.Ordinal) ||
               text.Contains("รักษา", StringComparison.Ordinal) ||
               text.Contains("ทำแผล", StringComparison.Ordinal) ||
               text.Contains("พันแผล", StringComparison.Ordinal) ||
               text.Contains("ปฐมพยาบาล", StringComparison.Ordinal) ||
               text.Contains("ยา", StringComparison.Ordinal) ||
               text.Contains("ผ้าพันแผล", StringComparison.Ordinal) ||
               text.Contains("กิน", StringComparison.Ordinal) ||
               text.Contains("ดื่ม", StringComparison.Ordinal);
    }

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
        var isDurableGeneratedItem = item.Category.Equals("Tool", StringComparison.OrdinalIgnoreCase) ||
                                     item.Category.Equals("Weapon", StringComparison.OrdinalIgnoreCase);
        item.DurabilityMax ??= item.Durability ?? (isDurableGeneratedItem ? 3 : 1);
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
        ItemRewardConsistencyService.Normalize(item);
    }

    private async Task<GeneratedGameContent> CreateFallbackContentAsync(
        Survivor survivor,
        int chapterNumber,
        int eventsPerChapter,
        StoryAssetCatalog assetCatalog,
        string fallbackReason = "",
        GameState? state = null,
        CancellationToken cancellationToken = default)
    {
        var storyTreeContent = await TryCreateStoryTreeFallbackContentAsync(
            survivor,
            chapterNumber,
            eventsPerChapter,
            assetCatalog,
            fallbackReason,
            state,
            cancellationToken).ConfigureAwait(false);

        if (storyTreeContent != null)
            return storyTreeContent;

        var eventsJsonContent = await TryCreateEventsJsonFallbackContentAsync(
            survivor,
            chapterNumber,
            eventsPerChapter,
            assetCatalog,
            fallbackReason,
            state,
            cancellationToken).ConfigureAwait(false);

        return eventsJsonContent ?? CreateRandomFallbackContent(survivor, chapterNumber, eventsPerChapter, assetCatalog, fallbackReason, state);
    }

    private async Task<GeneratedGameContent?> TryCreateStoryTreeFallbackContentAsync(
        Survivor survivor,
        int chapterNumber,
        int eventsPerChapter,
        StoryAssetCatalog assetCatalog,
        string fallbackReason,
        GameState? state,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync("story_tree.json").ConfigureAwait(false);
            var root = await JsonNode.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var eventsObject = root?["events"]?.AsObject();
            if (eventsObject == null || eventsObject.Count == 0)
                return null;

            var allEvents = eventsObject
                .Select(kvp => CreateGameEventFromStoryTreeNode(kvp.Value, survivor.Name))
                .Where(gameEvent => gameEvent is { Choices.Count: > 0 })
                .Cast<GameEvent>()
                .ToList();

            if (allEvents.Count == 0)
                return null;

            var startIndex = Math.Max(0, (chapterNumber - 1) * eventsPerChapter);
            var events = allEvents
                .Skip(startIndex)
                .Take(eventsPerChapter)
                .ToList();

            if (events.Count < eventsPerChapter)
                events.AddRange(allEvents.Take(eventsPerChapter - events.Count));

            var content = new GeneratedGameContent
            {
                StoryTitle = $"บทที่ {chapterNumber}: เส้นทางจาก Story Tree",
                UsedRemoteLlm = false,
                Events = events,
                StartingItems = chapterNumber == 1 ? CreateStoryTreeStartingItems() : new List<Item>(),
                FallbackReason = $"{fallbackReason}\nใช้ story_tree.json แทน Typhoon API"
            };

            NormalizeContent(content, survivor, eventsPerChapter, assetCatalog, state);
            content.UsedRemoteLlm = false;
            return content;
        }
        catch
        {
            return null;
        }
    }

    private async Task<GeneratedGameContent?> TryCreateEventsJsonFallbackContentAsync(
        Survivor survivor,
        int chapterNumber,
        int eventsPerChapter,
        StoryAssetCatalog assetCatalog,
        string fallbackReason,
        GameState? state,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync("events.json").ConfigureAwait(false);
            var events = await JsonSerializer.DeserializeAsync<List<GameEvent>>(stream, JsonOptions, cancellationToken).ConfigureAwait(false);
            if (events is not { Count: > 0 })
                return null;

            var selectedEvents = events.Take(eventsPerChapter).ToList();
            var content = new GeneratedGameContent
            {
                StoryTitle = $"บทที่ {chapterNumber}: เส้นทางจาก Events JSON",
                UsedRemoteLlm = false,
                Events = selectedEvents,
                StartingItems = chapterNumber == 1 ? CreateStoryTreeStartingItems() : new List<Item>(),
                FallbackReason = $"{fallbackReason}\nใช้ events.json แทน Typhoon API"
            };

            NormalizeContent(content, survivor, eventsPerChapter, assetCatalog, state);
            content.UsedRemoteLlm = false;
            return content;
        }
        catch
        {
            return null;
        }
    }

    private static GameEvent? CreateGameEventFromStoryTreeNode(JsonNode? node, string survivorName)
    {
        if (node is not JsonObject obj)
            return null;

        var choices = obj["choices"]?.AsArray()
            .Select(choice => CreateEventChoiceFromStoryTreeNode(choice, survivorName))
            .Where(choice => choice != null)
            .Cast<EventChoice>()
            .ToList() ?? new List<EventChoice>();

        return new GameEvent
        {
            Id = ReadString(obj, "id"),
            Title = ReadString(obj, "title"),
            Description = ReplaceSurvivorName(ReadString(obj, "description"), survivorName),
            ImageAlias = ReadString(obj, "image_hint"),
            Choices = choices
        };
    }

    private static EventChoice? CreateEventChoiceFromStoryTreeNode(JsonNode? node, string survivorName)
    {
        if (node is not JsonObject obj)
            return null;

        var effects = obj["effects"] as JsonObject;
        var itemsAdd = obj["items_add"]?.AsArray()
            .Select(item => item?.GetValue<string>())
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Cast<string>()
            .ToList() ?? new List<string>();

        return new EventChoice
        {
            Id = ReadString(obj, "id"),
            Text = ReplaceSurvivorName(ReadString(obj, "text"), survivorName),
            HpEffect = ReadFloat(effects, "hp"),
            HungerEffect = ReadFloat(effects, "hunger"),
            ThirstEffect = ReadFloat(effects, "thirst"),
            FatigueEffect = Math.Abs(ReadFloat(effects, "fatigue")),
            ResultText = ReplaceSurvivorName(ReadString(obj, "narrative"), survivorName),
            ItemsAdd = itemsAdd
        };
    }

    private static List<Item> CreateStoryTreeStartingItems()
    {
        return new List<Item>
        {
            CreateFallbackItem("น้ำกรองขวดเล็ก", "story_tree_start_water"),
            CreateFallbackItem("อาหารกระป๋องบุบ", "story_tree_start_food"),
            CreateFallbackItem("มีดทำครัว", "story_tree_start_knife")
        };
    }

    private static string ReadString(JsonObject obj, string propertyName)
    {
        return obj[propertyName]?.GetValue<string>() ?? string.Empty;
    }

    private static float ReadFloat(JsonObject? obj, string propertyName)
    {
        if (obj?[propertyName] == null)
            return 0f;

        try
        {
            return obj[propertyName]!.GetValue<float>();
        }
        catch
        {
            return 0f;
        }
    }

    private static string ReplaceSurvivorName(string value, string survivorName)
    {
        var name = string.IsNullOrWhiteSpace(survivorName) ? "คุณ" : survivorName;
        return value.Replace("[ชื่อ]", name, StringComparison.Ordinal);
    }

    private GeneratedGameContent CreateRandomFallbackContent(
        Survivor survivor,
        int chapterNumber,
        int eventsPerChapter,
        StoryAssetCatalog assetCatalog,
        string fallbackReason = "",
        GameState? state = null)
    {
        var seed = Guid.NewGuid().ToString("N")[..8];
        var arcTitle = chapterNumber switch
        {
            1 => "เสียงวิทยุจากตึกฝั่งเหนือ",
            2 => "ร่องรอยของคนที่ยังรอด",
            3 => "แบตเตอรี่ก้อนสุดท้าย",
            4 => "ทางออกก่อนเมืองปิดตาย",
            _ => "เส้นทางที่ยังไม่จบ"
        };
        var objective = chapterNumber switch
        {
            1 => "หาต้นทางของสัญญาณวิทยุที่ดังซ้ำทุกคืน",
            2 => "ตามรอยกลุ่มผู้รอดชีวิตที่ทิ้งสัญลักษณ์ไว้ตามกำแพง",
            3 => "หาอะไหล่และแบตเตอรี่เพื่อส่งสัญญาณขอความช่วยเหลือ",
            4 => "ตัดสินใจว่าจะไปจุดอพยพหรือช่วยคนที่ติดอยู่ในเมือง",
            _ => "เอาตัวรอดจากผลของการตัดสินใจที่ผ่านมา"
        };
        var recurringThreat = chapterNumber switch
        {
            1 => "เสียงเคาะสามครั้งหลังประตูเหล็ก",
            2 => "ชายผ้าพันแขนสีแดงที่ตามหลังมาเงียบๆ",
            3 => "ไฟฉุกเฉินที่ติดดับเหมือนมีคนควบคุมอยู่",
            4 => "เฮลิคอปเตอร์ที่บินวนแต่ไม่ยอมลงจอด",
            _ => "เงาที่วนกลับมาในทุกจุดพัก"
        };
        var route = chapterNumber switch
        {
            1 => new[] { "ห้องพักเก่า", "โถงบันได", "ร้านชำชั้นล่าง", "คลินิกมุมถนน", "ห้องวิทยุบนดาดฟ้า", "สะพานลอยที่พังครึ่งหนึ่ง", "ป้อมยามร้าง", "ดาดฟ้าฝั่งเหนือ" },
            2 => new[] { "ตลาดร้าง", "ซอยหลังร้านขายยา", "สถานีตำรวจ", "ลานจอดรถใต้ดิน", "บ้านที่มีสัญลักษณ์สีขาว", "โรงเรียนร้าง", "ทางลอดน้ำท่วม", "แคมป์ไฟที่เพิ่งดับ" },
            3 => new[] { "โรงงานแบตเตอรี่", "ห้องควบคุมไฟ", "โกดังเครื่องมือ", "ทางเดินใต้ดิน", "ห้องพยาบาลเก่า", "เสาส่งสัญญาณ", "ดาดฟ้าโรงงาน", "ห้องเครื่องส่งวิทยุ" },
            4 => new[] { "จุดตรวจทหารร้าง", "ถนนไป safe zone", "สะพานข้ามคลอง", "ค่ายผู้รอดชีวิต", "คลังเสบียง", "ทางเข้าเขตกักกัน", "ลานจอดเฮลิคอปเตอร์", "ประตูเมืองด้านเหนือ" },
            _ => new[] { "ถนนเปลี่ยว", "อาคารร้าง", "ซอยมืด", "ดาดฟ้า", "อุโมงค์", "ลานจอดรถ", "คลินิกเก่า", "ทางออกเมือง" }
        };
        var itemNames = new[] { "น้ำกรองขวดเล็ก", "ผ้าพันแผลสะอาด", "มีดพับขึ้นสนิม", "อาหารกระป๋องบุบ", "ไฟฉายมือหมุน", "ยาแก้ปวดเหลือครึ่งแผง" };
        var previousThread = state?.StoryMemory?.LastOrDefault()?.Summary;
        var openingThread = string.IsNullOrWhiteSpace(previousThread)
            ? $"เป้าหมายตอนนี้คือ{objective}"
            : $"จากเหตุการณ์ก่อนหน้า {previousThread} ทำให้เป้าหมายตอนนี้คือ{objective}";

        var events = new List<GameEvent>();
        for (var i = 0; i < eventsPerChapter; i++)
        {
            var place = route[i % route.Length];
            var isFinalBeat = i == eventsPerChapter - 1;
            var thread = i == 0
                ? openingThread
                : $"เบาะแสจากจุดก่อนพา {survivor.Name} มาถึง{place} และ{recurringThreat}ยังตามมาเหมือนเดิม";
            var nextLead = isFinalBeat
                ? "ทางเลือกนี้จะกำหนดว่าบทต่อไปเริ่มจากความหวังหรือหนี้ที่ต้องชดใช้"
                : $"ร่องรอยใหม่ชี้ไปยัง{route[(i + 1) % route.Length]}";

            events.Add(new GameEvent
            {
                Id = $"fallback_ch{chapterNumber}_{seed}_{i + 1:D2}",
                Title = $"{arcTitle}: {place}",
                Description = $"{thread}. ที่นี่มีร่องรอยของคนรอดชีวิตปะปนกับกับดักหยาบๆ ทุกอย่างบอกว่าใครบางคนอยากให้คุณเดินต่อ แต่ไม่อยากให้ไปถึงง่ายๆ",
                Choices = new List<EventChoice>
                {
                    new()
                    {
                        Id = "c1",
                        Text = "ตามร่องรอยต่อแม้ต้องเสี่ยง",
                        HpEffect = -Random.Shared.Next(0, 16),
                        HungerEffect = Random.Shared.Next(0, 18),
                        ThirstEffect = -Random.Shared.Next(0, 10),
                        FatigueEffect = Random.Shared.Next(8, 24),
                        ResultText = $"คุณฝ่าเข้าไปจนพบเบาะแสเพิ่มเกี่ยวกับ{objective} แต่เสียงของ{recurringThreat}ดังใกล้ขึ้น. {nextLead}",
                        ItemReward = i < Math.Min(6, eventsPerChapter)
                            ? CreateFallbackItem(itemNames[Random.Shared.Next(itemNames.Length)], $"reward_{seed}_{i}")
                            : null
                    },
                    new()
                    {
                        Id = "c2",
                        Text = "อ้อมทางเพื่อรักษาชีวิตไว้ก่อน",
                        HpEffect = 0,
                        HungerEffect = -Random.Shared.Next(0, 12),
                        ThirstEffect = -Random.Shared.Next(0, 12),
                        FatigueEffect = Random.Shared.Next(-10, 8),
                        ResultText = $"คุณรอดจากความเสี่ยงตรงหน้า แต่เสียเวลาจนร่องรอยบางส่วนหายไป. ยังเหลือสัญญาณหนึ่งที่ชี้ไปยัง{route[(i + 1) % route.Length]}"
                    }
                }
            });
        }

        var content = new GeneratedGameContent
        {
            StoryTitle = $"บทที่ {chapterNumber}: {arcTitle}",
            UsedRemoteLlm = false,
            Events = events,
            StartingItems = chapterNumber == 1 ? new List<Item>
            {
                CreateFallbackItem("น้ำกรองขวดเล็ก", $"start_{seed}_water"),
                CreateFallbackItem("อาหารกระป๋องบุบ", $"start_{seed}_food"),
                CreateFallbackItem("ผ้าพันแผลสะอาด", $"start_{seed}_bandage")
            } : new List<Item>()
        };

        NormalizeContent(content, survivor, eventsPerChapter, assetCatalog, state);
        content.UsedRemoteLlm = false;
        content.FallbackReason = fallbackReason;
        return content;
    }

    private static IReadOnlyCollection<Item> GetAvailableChoiceItems(
        Survivor survivor,
        GeneratedGameContent content,
        GameState? state)
    {
        var items = new List<Item>();
        items.AddRange(state?.Survivor.Inventory.GetItems() ?? survivor.Inventory.GetItems());
        items.AddRange(content.StartingItems);
        return items;
    }

    private static Item CreateFallbackItem(string nameTh, string id)
    {
        var category = nameTh.Contains("น้ำ", StringComparison.Ordinal) ? "Water" :
            nameTh.Contains("อาหาร", StringComparison.Ordinal) ? "Food" :
            nameTh.Contains("แผล", StringComparison.Ordinal) || nameTh.Contains("ยา", StringComparison.Ordinal) ? "Medicine" :
            nameTh.Contains("มีด", StringComparison.Ordinal) ? "Weapon" : "Tool";

        var durability = category is "Tool" or "Weapon" ? 3 : 1;

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
            DurabilityMax = durability,
            Durability = durability,
            Effects = new ItemEffects
            {
                HpRestore = category == "Medicine" ? 20 : 0,
                HungerRestore = category == "Food" ? 34 : 0,
                ThirstRestore = category == "Water" ? 38 : 0
            },
            DescriptionTh = "ไอเทมเริ่มต้นจากเรื่องราวที่สุ่มขึ้นสำหรับรอบนี้",
            StoryAlias = $"gen_{id}"
        };
    }

    private static Item CreateFallbackItemFromStoryTreeReference(string reference, string fallbackId)
    {
        var lowered = reference.ToLowerInvariant();
        var nameTh =
            lowered.Contains("drink", StringComparison.Ordinal) || lowered.Contains("water", StringComparison.Ordinal) ? "น้ำดื่ม" :
            lowered.Contains("food", StringComparison.Ordinal) ? "อาหารกระป๋อง" :
            lowered.Contains("wpn", StringComparison.Ordinal) || lowered.Contains("knife", StringComparison.Ordinal) ? "มีดพับ" :
            lowered.Contains("tool", StringComparison.Ordinal) || lowered.Contains("flashlight", StringComparison.Ordinal) ? "ไฟฉาย" :
            lowered.Contains("med", StringComparison.Ordinal) || lowered.Contains("bandage", StringComparison.Ordinal) ? "ผ้าพันแผล" :
            "ของใช้จากเหตุการณ์";

        var item = CreateFallbackItem(nameTh, fallbackId);
        item.Id = string.IsNullOrWhiteSpace(reference) ? item.Id : reference;
        item.StoryAlias = nameTh switch
        {
            "น้ำดื่ม" => "water_bottle",
            "อาหารกระป๋อง" => "canned_food",
            "มีดพับ" => "knife",
            "ไฟฉาย" => "flashlight",
            "ผ้าพันแผล" => "bandage",
            _ => item.StoryAlias
        };

        return item;
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
