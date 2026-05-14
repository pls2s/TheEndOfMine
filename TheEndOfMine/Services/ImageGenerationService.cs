using System.Text;
using System.Text.Json;
using System.Security.Cryptography;
using Microsoft.Maui.Storage;
using TheEndOfMine.Models;

namespace TheEndOfMine.Services;

public class ImageGenerationService
{
    private const string EnvFileName = "llm.env";
    private const string DefaultProvider = "gemini";
    private const string DefaultGeminiModel = "gemini-2.5-flash-image";
    private const string GeminiEndpointTemplate = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private static readonly HttpClient HttpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(90)
    };

    public async Task GenerateChapterImagesAsync(GameState state, GeneratedGameContent content, CancellationToken cancellationToken = default)
    {
        var settings = await LoadSettingsAsync(cancellationToken).ConfigureAwait(false);
        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.ApiKey))
            return;

        var imageJobs = new List<Func<Task>>();

        foreach (var gameEvent in content.Events)
        {
            if (string.IsNullOrWhiteSpace(gameEvent.ImagePath) && !string.IsNullOrWhiteSpace(gameEvent.ImagePrompt))
            {
                imageJobs.Add(async () =>
                {
                    gameEvent.ImagePath = await GenerateImageAsync(
                        settings,
                        gameEvent.ImagePrompt,
                        $"ch{state.CurrentChapter}_event_{gameEvent.Id}",
                        cancellationToken).ConfigureAwait(false);
                });
            }

            foreach (var item in gameEvent.Choices.SelectMany(choice => choice.GetItemRewards()))
                AddItemImageJob(settings, imageJobs, item, state.CurrentChapter, cancellationToken);
        }

        foreach (var item in content.StartingItems)
            AddItemImageJob(settings, imageJobs, item, state.CurrentChapter, cancellationToken);

        foreach (var batch in imageJobs.Chunk(3))
            await Task.WhenAll(batch.Select(job => job())).ConfigureAwait(false);
    }

    private static void AddItemImageJob(
        ImageRuntimeSettings settings,
        List<Func<Task>> imageJobs,
        Item item,
        int chapter,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(item.ImagePath) || string.IsNullOrWhiteSpace(item.ImagePrompt))
            return;

        imageJobs.Add(async () =>
        {
            item.ImagePath = await GenerateImageAsync(
                settings,
                item.ImagePrompt,
                $"ch{chapter}_item_{item.Id}",
                cancellationToken).ConfigureAwait(false);
        });
    }

    private static async Task<string> GenerateImageAsync(
        ImageRuntimeSettings settings,
        string prompt,
        string fileNameSeed,
        CancellationToken cancellationToken)
    {
        if (!string.Equals(settings.Provider, DefaultProvider, StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        var imagesDir = Path.Combine(FileSystem.AppDataDirectory, "generated-images");
        Directory.CreateDirectory(imagesDir);

        var cachePath = Path.Combine(imagesDir, $"{SanitizeFileName(fileNameSeed)}_{PromptHash(prompt)}.png");
        if (File.Exists(cachePath))
            return cachePath;

        try
        {
            var endpoint = string.Format(GeminiEndpointTemplate, Uri.EscapeDataString(settings.Model));
            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = prompt }
                        }
                    }
                },
                generationConfig = new
                {
                    responseModalities = new[] { "TEXT", "IMAGE" },
                    imageConfig = new
                    {
                        aspectRatio = "1:1",
                        imageSize = "1K"
                    }
                }
            };

            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Add("x-goog-api-key", settings.ApiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody, JsonOptions), Encoding.UTF8, "application/json");

            using var response = await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            var imageBytes = ExtractGeminiImageBytes(responseJson);
            if (imageBytes.Length == 0)
                return string.Empty;

            await File.WriteAllBytesAsync(cachePath, imageBytes, cancellationToken).ConfigureAwait(false);
            return cachePath;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static byte[] ExtractGeminiImageBytes(string responseJson)
    {
        using var doc = JsonDocument.Parse(responseJson);
        if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
            return Array.Empty<byte>();

        var parts = candidates[0].GetProperty("content").GetProperty("parts");
        foreach (var part in parts.EnumerateArray())
        {
            if (!part.TryGetProperty("inlineData", out var inlineData))
                continue;

            if (inlineData.TryGetProperty("data", out var data))
                return Convert.FromBase64String(data.GetString() ?? string.Empty);
        }

        return Array.Empty<byte>();
    }

    private static async Task<ImageRuntimeSettings> LoadSettingsAsync(CancellationToken cancellationToken)
    {
        var env = await LoadEnvFileSettingsAsync(cancellationToken).ConfigureAwait(false);
        var provider = GetSetting(new[] { "IMAGE_PROVIDER" }, env) ?? DefaultProvider;
        var enabled = !string.Equals(provider, "none", StringComparison.OrdinalIgnoreCase);

        return new ImageRuntimeSettings
        {
            Enabled = enabled,
            Provider = provider,
            ApiKey = GetSetting(new[] { "GEMINI_API_KEY", "IMAGE_API_KEY" }, env),
            Model = GetSetting(new[] { "GEMINI_IMAGE_MODEL", "IMAGE_MODEL" }, env) ?? DefaultGeminiModel
        };
    }

    private static async Task<Dictionary<string, string>> LoadEnvFileSettingsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync(EnvFileName).ConfigureAwait(false);
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
            return ParseEnv(content);
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private static string? GetSetting(IReadOnlyList<string> keys, IReadOnlyDictionary<string, string> env)
    {
        foreach (var key in keys)
        {
            if (env.TryGetValue(key, out var fromEnv) && !string.IsNullOrWhiteSpace(fromEnv))
                return fromEnv;

            var fromEnvironment = Environment.GetEnvironmentVariable(key);
            if (!string.IsNullOrWhiteSpace(fromEnvironment))
                return fromEnvironment;
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
                value = value[1..^1];

            values[key] = value;
        }

        return values;
    }

    private static string SanitizeFileName(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(sanitized) ? Guid.NewGuid().ToString("N") : sanitized;
    }

    private static string PromptHash(string prompt)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(prompt));
        return Convert.ToHexString(bytes).ToLowerInvariant()[..16];
    }

    private sealed class ImageRuntimeSettings
    {
        public bool Enabled { get; set; }
        public string Provider { get; set; } = DefaultProvider;
        public string? ApiKey { get; set; }
        public string Model { get; set; } = DefaultGeminiModel;
    }
}
