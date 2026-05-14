namespace TheEndOfMine.Services;

using Microsoft.Maui.Storage;
using TheEndOfMine.Models;

public static class AudioFeedbackService
{
    private const string MutedPreferenceKey = "audio_muted";
    private const string ButtonTapPath = "audio/ui/sound_select_1.wav";
    private const string StoryChoicePath = "audio/ui/sound_select_2.wav";
    private const string BackgroundMusicPath = "audio/bgm/Cold_Iron_Drips.mp3";
    private const float BackgroundMusicVolume = 0.28f;
    private const string ItemSoundDirectory = "audio/ui/";
    private const string ItemSoundExtension = ".wav";

    private static readonly HashSet<string> ItemSoundAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        "antiseptic",
        "backpack",
        "bandage",
        "battery_pack",
        "battery_pack_alt",
        "binoculars",
        "blanket",
        "canned_food",
        "canteen",
        "compass",
        "cookpot",
        "first_aid_kit",
        "flashlight",
        "fuel_can",
        "gloves",
        "helmet",
        "knife",
        "knife_sheath",
        "lighter",
        "lockpick_set",
        "machete",
        "map",
        "mask",
        "matches",
        "medicine_bottle",
        "painkillers",
        "pliers",
        "radio",
        "radio_battery",
        "sewing_kit",
        "stove",
        "torch",
        "water_bottle",
        "water_filter",
        "whistle",
        "wrench"
    };

    private static readonly Dictionary<string, string> ItemAliasFallbacks = new(StringComparer.OrdinalIgnoreCase)
    {
        ["battery"] = "battery_pack",
        ["battery_pack_alt"] = "battery_pack",
        ["first_aid"] = "first_aid_kit",
        ["firstaid"] = "first_aid_kit",
        ["medkit"] = "first_aid_kit",
        ["medicine"] = "medicine_bottle",
        ["painkiller"] = "painkillers",
        ["food"] = "canned_food",
        ["water"] = "water_bottle",
        ["drink"] = "water_bottle",
        ["lockpick"] = "lockpick_set",
        ["rope"] = "backpack",
        ["rope_coil"] = "backpack",
        ["screwdriver"] = "wrench",
        ["sleeping_bag"] = "blanket",
        ["flare"] = "torch",
        ["tape"] = "backpack",
        ["tape_roll"] = "backpack"
    };

#if ANDROID
    private static readonly object BackgroundLock = new();
    private static Android.Media.MediaPlayer? _backgroundPlayer;
#endif

    public static bool IsMuted
    {
        get
        {
            try
            {
                return Preferences.Get(MutedPreferenceKey, false);
            }
            catch
            {
                return false;
            }
        }
    }

    public static bool ToggleMuted()
    {
        var isMuted = !IsMuted;

        try
        {
            Preferences.Set(MutedPreferenceKey, isMuted);
        }
        catch
        {
            // If preferences are unavailable, keep gameplay running and only apply in-memory effect where possible.
        }

        if (isMuted)
            PauseBackgroundLoop();
        else
            StartBackgroundLoop();

        return isMuted;
    }

    public static void PlayButtonTap()
    {
        Play(ButtonTapPath);
    }

    public static void PlayStoryChoice()
    {
        Play(StoryChoicePath);
    }

    public static bool PlayItemUse(Item? item)
    {
        if (item == null)
            return false;

        Play(GetItemSoundPath(item));
        return true;
    }

    public static bool PlayChoiceItemUse(EventChoice choice, Inventory? inventory)
    {
        var item = FindInventoryItem(inventory, choice.ConsumedItemId)
                   ?? FindInventoryItem(inventory, choice.UsedItemId)
                   ?? FindInventoryItem(inventory, choice.RequiredItemId);
        if (item != null)
            return PlayItemUse(item);

        var alias = ResolveKnownAlias(choice.ConsumedItemId)
                    ?? ResolveKnownAlias(choice.UsedItemId)
                    ?? ResolveKnownAlias(choice.RequiredItemId);
        if (alias == null)
            return false;

        Play(BuildItemSoundPath(alias));
        return true;
    }

    public static bool PlayItemReward(IReadOnlyCollection<Item> rewards, int delayMs = 260)
    {
        if (rewards.Count == 0)
            return false;

        PlayDelayed(BuildItemSoundPath("backpack"), delayMs);
        return true;
    }

    public static void StartBackgroundLoop()
    {
        if (IsMuted)
            return;

#if ANDROID
        _ = Task.Run(StartBackgroundLoopAndroid);
#endif
    }

    public static void PauseBackgroundLoop()
    {
#if ANDROID
        lock (BackgroundLock)
        {
            try
            {
                if (_backgroundPlayer?.IsPlaying == true)
                    _backgroundPlayer.Pause();
            }
            catch
            {
                StopBackgroundLoopAndroid();
            }
        }
#endif
    }

    public static void StopBackgroundLoop()
    {
#if ANDROID
        lock (BackgroundLock)
            StopBackgroundLoopAndroid();
#endif
    }

    private static void Play(string assetPath)
    {
        if (IsMuted)
            return;

#if ANDROID
        _ = Task.Run(() => PlayAndroid(assetPath));
#endif
    }

    private static void PlayDelayed(string assetPath, int delayMs)
    {
        if (IsMuted)
            return;

        _ = Task.Run(async () =>
        {
            if (delayMs > 0)
                await Task.Delay(delayMs);

            Play(assetPath);
        });
    }

    private static string GetItemSoundPath(Item item)
    {
        var alias = ResolveKnownAlias(item.StoryAlias)
                    ?? ResolveKnownAlias(item.Id)
                    ?? ResolveKnownAlias(item.NameEn)
                    ?? ResolveKnownAlias(item.NameTh)
                    ?? GetCategoryFallbackAlias(item);

        return BuildItemSoundPath(alias);
    }

    private static string BuildItemSoundPath(string alias)
    {
        return $"{ItemSoundDirectory}{alias}{ItemSoundExtension}";
    }

    private static string? ResolveKnownAlias(string? value)
    {
        var token = NormalizeItemToken(value);
        if (string.IsNullOrWhiteSpace(token))
            return null;

        if (ItemSoundAliases.Contains(token))
            return token;

        if (ItemAliasFallbacks.TryGetValue(token, out var directFallback))
            return directFallback;

        foreach (var alias in ItemSoundAliases.OrderByDescending(alias => alias.Length))
        {
            if (token.Contains(alias, StringComparison.OrdinalIgnoreCase))
                return alias;
        }

        foreach (var (key, fallback) in ItemAliasFallbacks)
        {
            if (token.Contains(key, StringComparison.OrdinalIgnoreCase))
                return fallback;
        }

        return null;
    }

    private static string NormalizeItemToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var token = value.Trim().Replace('\\', '/');
        var slashIndex = token.LastIndexOf('/');
        if (slashIndex >= 0)
            token = token[(slashIndex + 1)..];

        if (token.EndsWith(ItemSoundExtension, StringComparison.OrdinalIgnoreCase))
            token = token[..^ItemSoundExtension.Length];

        return token
            .Trim()
            .Replace('-', '_')
            .Replace(' ', '_')
            .ToLowerInvariant();
    }

    private static string GetCategoryFallbackAlias(Item item)
    {
        var category = item.Category?.Trim().ToLowerInvariant() ?? string.Empty;
        if (category.Contains("food", StringComparison.OrdinalIgnoreCase))
            return "canned_food";
        if (category.Contains("water", StringComparison.OrdinalIgnoreCase))
            return "water_bottle";
        if (category.Contains("medicine", StringComparison.OrdinalIgnoreCase) ||
            category.Contains("medical", StringComparison.OrdinalIgnoreCase))
            return "first_aid_kit";
        if (category.Contains("weapon", StringComparison.OrdinalIgnoreCase))
            return "knife";
        if (category.Contains("tool", StringComparison.OrdinalIgnoreCase))
            return "wrench";

        return "backpack";
    }

    private static Item? FindInventoryItem(Inventory? inventory, string itemId)
    {
        if (inventory == null || string.IsNullOrWhiteSpace(itemId))
            return null;

        var token = itemId.Trim();
        var items = inventory.GetItems().ToList();

        return items.FirstOrDefault(item =>
                   MatchesItemToken(token, item.Id) ||
                   MatchesItemToken(token, item.StoryAlias) ||
                   MatchesItemToken(token, item.NameTh) ||
                   MatchesItemToken(token, item.NameEn))
               ?? items.FirstOrDefault(item =>
                   ContainsItemToken(token, item.Id) ||
                   ContainsItemToken(token, item.StoryAlias) ||
                   ContainsItemToken(token, item.NameTh) ||
                   ContainsItemToken(token, item.NameEn));
    }

    private static bool MatchesItemToken(string token, string? itemValue)
    {
        return !string.IsNullOrWhiteSpace(itemValue) &&
               string.Equals(token, itemValue.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsItemToken(string token, string? itemValue)
    {
        return !string.IsNullOrWhiteSpace(itemValue) &&
               itemValue.Contains(token, StringComparison.OrdinalIgnoreCase);
    }

#if ANDROID
    private static void StartBackgroundLoopAndroid()
    {
        lock (BackgroundLock)
        {
            try
            {
                if (_backgroundPlayer != null)
                {
                    if (!_backgroundPlayer.IsPlaying)
                        _backgroundPlayer.Start();

                    return;
                }

                var assets = Microsoft.Maui.ApplicationModel.Platform.AppContext.Assets;
                if (assets == null) return;

                using var descriptor = assets.OpenFd(BackgroundMusicPath);
                _backgroundPlayer = new Android.Media.MediaPlayer();
                _backgroundPlayer.SetDataSource(descriptor.FileDescriptor, descriptor.StartOffset, descriptor.Length);
                _backgroundPlayer.Looping = true;
                _backgroundPlayer.SetVolume(BackgroundMusicVolume, BackgroundMusicVolume);
                _backgroundPlayer.Prepare();
                _backgroundPlayer.Start();
            }
            catch
            {
                StopBackgroundLoopAndroid();
            }
        }
    }

    private static void StopBackgroundLoopAndroid()
    {
        var player = _backgroundPlayer;
        _backgroundPlayer = null;

        if (player == null) return;

        try
        {
            if (player.IsPlaying)
                player.Stop();
            player.Release();
        }
        catch
        {
            // Ignore audio cleanup failures. Background audio must not break gameplay.
        }
        finally
        {
            player.Dispose();
        }
    }

    private static void PlayAndroid(string assetPath)
    {
        Android.Media.MediaPlayer? player = null;

        try
        {
            var assets = Microsoft.Maui.ApplicationModel.Platform.AppContext.Assets;
            if (assets == null) return;

            using var descriptor = assets.OpenFd(assetPath);
            player = new Android.Media.MediaPlayer();
            if (player == null) return;

            player.SetDataSource(descriptor.FileDescriptor, descriptor.StartOffset, descriptor.Length);
            player.SetVolume(0.85f, 0.85f);
            player.Prepare();
            var activePlayer = player;
            player.Completion += (_, _) => DisposePlayer(activePlayer);
            player.Start();
        }
        catch
        {
            DisposePlayer(player);
        }
    }

    private static void DisposePlayer(Android.Media.MediaPlayer? player)
    {
        if (player == null) return;

        try
        {
            player.Release();
        }
        catch
        {
            // Ignore audio cleanup failures. UI feedback must never break gameplay.
        }
        finally
        {
            player.Dispose();
        }
    }
#endif
}
