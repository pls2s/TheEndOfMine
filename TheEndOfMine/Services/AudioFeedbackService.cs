namespace TheEndOfMine.Services;

public static class AudioFeedbackService
{
    private const string ButtonTapPath = "audio/ui/sound_select_1.wav";
    private const string StoryChoicePath = "audio/ui/sound_select_2.wav";
    private const string BackgroundMusicPath = "audio/bgm/Cold_Iron_Drips.mp3";
    private const float BackgroundMusicVolume = 0.28f;

#if ANDROID
    private static readonly object BackgroundLock = new();
    private static Android.Media.MediaPlayer? _backgroundPlayer;
#endif

    public static void PlayButtonTap()
    {
        Play(ButtonTapPath);
    }

    public static void PlayStoryChoice()
    {
        Play(StoryChoicePath);
    }

    public static void StartBackgroundLoop()
    {
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
#if ANDROID
        _ = Task.Run(() => PlayAndroid(assetPath));
#endif
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
