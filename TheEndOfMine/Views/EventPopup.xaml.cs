namespace TheEndOfMine.Views;

using TheEndOfMine.Models;

public partial class EventPopup : ContentPage
{
    private readonly GameEvent? _event;
    private readonly Action<EventChoice>? _onChoiceSelected;
    private bool _choiceApplied;

    public EventPopup()
    {
        InitializeComponent();
    }

    public EventPopup(GameEvent gameEvent, string chapterLabel, Action<EventChoice> onChoiceSelected)
    {
        InitializeComponent();

        _event = gameEvent;
        _onChoiceSelected = onChoiceSelected;

        ChapterLabel.Text = chapterLabel;
        TitleLabel.Text = gameEvent.Title;
        DescriptionLabel.Text = gameEvent.Description;
        EventImage.Source = string.IsNullOrWhiteSpace(gameEvent.ImagePath)
            ? "story/chapter/chapter_ruined_city_sunset.png"
            : gameEvent.ImagePath;

        ConfigureChoiceButton(ChoiceOneButton, gameEvent.Choices.ElementAtOrDefault(0));
        ConfigureChoiceButton(ChoiceTwoButton, gameEvent.Choices.ElementAtOrDefault(1));
    }

    private static void ConfigureChoiceButton(Button button, EventChoice? choice)
    {
        if (choice == null)
        {
            button.IsVisible = false;
            return;
        }

        button.Text = choice.Text;
    }

    private void OnChoiceOneClicked(object sender, EventArgs e)
    {
        ApplyChoice(0);
    }

    private void OnChoiceTwoClicked(object sender, EventArgs e)
    {
        ApplyChoice(1);
    }

    private void ApplyChoice(int index)
    {
        if (_choiceApplied || _event == null)
            return;

        var choice = _event.Choices.ElementAtOrDefault(index);
        if (choice == null)
            return;

        _choiceApplied = true;
        ChoicePanel.IsVisible = false;
        ResultPanel.IsVisible = true;
        ResultLabel.Text = choice.ResultText;

        if (choice.ItemReward != null)
        {
            RewardLabel.Text = $"ได้รับ: {choice.ItemReward.NameTh}";
            RewardLabel.IsVisible = true;
        }

        _onChoiceSelected?.Invoke(choice);
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}
