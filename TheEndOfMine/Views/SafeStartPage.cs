namespace TheEndOfMine.Views;

using TheEndOfMine.Data;
using TheEndOfMine.Services;

public sealed class SafeStartPage : ContentPage
{
    private readonly Label _statusLabel;

    public SafeStartPage(Exception? startupException = null)
    {
        BackgroundColor = Colors.Black;
        NavigationPage.SetHasNavigationBar(this, false);

        _statusLabel = new Label
        {
            Text = startupException == null
                ? "พร้อมเปิดเกมในโหมดปลอดภัย"
                : "พบปัญหาตอนเปิดแอป กดเริ่มใหม่เพื่อเข้าเกม",
            TextColor = Color.FromArgb("#CFC7B4"),
            FontSize = 14,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var titleLabel = new Label
        {
            Text = "TheEndOfMine",
            TextColor = Color.FromArgb("#D8B45F"),
            FontSize = 30,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        };

        var startButton = CreateButton("เริ่มเกม", StartGame);
        var resetButton = CreateButton("ล้างข้อมูลแล้วเริ่มใหม่", ResetAndStartGame);

        Content = new Grid
        {
            Padding = 28,
            Children =
            {
                new VerticalStackLayout
                {
                    Spacing = 18,
                    VerticalOptions = LayoutOptions.Center,
                    Children =
                    {
                        titleLabel,
                        _statusLabel,
                        startButton,
                        resetButton
                    }
                }
            }
        };
    }

    private static Button CreateButton(string text, EventHandler handler)
    {
        var button = new Button
        {
            Text = text,
            BackgroundColor = Color.FromArgb("#D8B45F"),
            TextColor = Colors.Black,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            Padding = new Thickness(16, 12)
        };

        button.Clicked += handler;
        return button;
    }

    private void StartGame(object? sender, EventArgs e)
    {
        TryOpenIntro(clearSaves: false);
    }

    private void ResetAndStartGame(object? sender, EventArgs e)
    {
        TryOpenIntro(clearSaves: true);
    }

    private void TryOpenIntro(bool clearSaves)
    {
        try
        {
            if (clearSaves)
            {
                new GameDatabase().DeleteAllSaves();
                new SaveService().DeleteAllSaves();
                Preferences.Remove("main_tutorial_seen");
            }

            var intro = new IntroPage();
            NavigationPage.SetHasNavigationBar(intro, false);

            var window = Application.Current?.Windows.FirstOrDefault();
            if (window != null)
                window.Page = new NavigationPage(intro);
        }
        catch (Exception ex)
        {
            App.LogStartupException("SafeStartPage.TryOpenIntro", ex);
            _statusLabel.Text = "ยังเปิดไม่ได้ ต้องดู logcat เพื่อหาจุดที่ล้ม";
        }
    }
}
