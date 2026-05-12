namespace TheEndOfMine.Services;

public static class AudioFeedbackService
{
    private const string ButtonTapPath = "audio/ui/sound_select_1.wav";
    private const string StoryChoicePath = "audio/ui/sound_select_2.wav";

    public static void PlayButtonTap()
    {
        Play(ButtonTapPath);
    }

    public static void PlayStoryChoice()
    {
        Play(StoryChoicePath);
    }

    private static void Play(string assetPath)
    {
#if ANDROID
        _ = Task.Run(() => PlayAndroid(assetPath));
#endif
    }

#if ANDROID
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
