namespace TheEndOfMine.Views;

using TheEndOfMine.Models;
using TheEndOfMine.Services;

public partial class EventPopup : ContentPage
{
    private readonly GameEvent? _event;
    private readonly Inventory? _inventory;
    private readonly Action<EventChoice>? _onChoiceSelected;
    private bool _choiceApplied;

    public EventPopup()
    {
        InitializeComponent();
    }

    public EventPopup(GameEvent gameEvent, string chapterLabel, Action<EventChoice> onChoiceSelected)
        : this(gameEvent, chapterLabel, null, onChoiceSelected)
    {
    }

    public EventPopup(GameEvent gameEvent, string chapterLabel, Inventory? inventory, Action<EventChoice> onChoiceSelected)
    {
        InitializeComponent();

        _event = gameEvent;
        _inventory = inventory;
        _onChoiceSelected = onChoiceSelected;
        ThaiNarrativeTextNormalizer.Normalize(gameEvent);
        EventChoiceInventoryGuard.Normalize(gameEvent, inventory);
        ItemRewardConsistencyService.Normalize(gameEvent);
        ThaiNarrativeTextNormalizer.Normalize(gameEvent);

        ChapterLabel.Text = chapterLabel;
        TitleLabel.Text = gameEvent.Title;
        DescriptionLabel.Text = gameEvent.Description;
        EventImage.Source = string.IsNullOrWhiteSpace(gameEvent.ImagePath)
            ? "story/chapter/chapter_ruined_city_sunset.png"
            : gameEvent.ImagePath;

        ConfigureChoiceButton(ChoiceOneButton, gameEvent.Choices.ElementAtOrDefault(0));
        ConfigureChoiceButton(ChoiceTwoButton, gameEvent.Choices.ElementAtOrDefault(1));
    }

    private void ConfigureChoiceButton(Button button, EventChoice? choice)
    {
        if (choice == null)
        {
            button.IsVisible = false;
            return;
        }

        var lines = new List<string> { choice.Text };
        var itemUse = GetChoiceItemUseDisplay(choice);
        if (itemUse != null)
            lines.Add($"{itemUse.Label}: {itemUse.ItemName}");

        if (!string.IsNullOrWhiteSpace(choice.InventoryEffectNote))
            lines.Add(choice.InventoryEffectNote);

        button.Text = string.Join(Environment.NewLine, lines);
        ApplyChoiceButtonStyle(button, itemUse != null);
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
        var rewards = choice.GetItemRewards().ToList();
        PlayChoiceFeedback(choice, rewards);

        ChoicePanel.IsVisible = false;
        ResultPanel.IsVisible = true;
        ResultLabel.Text = string.IsNullOrWhiteSpace(choice.InventoryEffectNote)
            ? choice.ResultText
            : $"{choice.ResultText}\n\n{choice.InventoryEffectNote}";

        if (rewards.Count > 0)
        {
            RewardLabel.Text = $"ได้รับ: {string.Join(", ", rewards.Select(item => item.NameTh))}";
            RewardLabel.IsVisible = true;
        }

        _onChoiceSelected?.Invoke(choice);
    }

    private void PlayChoiceFeedback(EventChoice choice, IReadOnlyCollection<Item> rewards)
    {
        var playedItemUse = AudioFeedbackService.PlayChoiceItemUse(choice, _inventory);
        if (!playedItemUse)
            AudioFeedbackService.PlayStoryChoice();

        if (rewards.Count > 0)
            AudioFeedbackService.PlayItemReward(rewards, playedItemUse ? 420 : 260);
    }

    private async void OnContinueClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();
        await Navigation.PopModalAsync();
    }

    protected override bool OnBackButtonPressed()
    {
        return !_choiceApplied;
    }

    private ChoiceItemUseDisplay? GetChoiceItemUseDisplay(EventChoice choice)
    {
        var consumed = GetItemDisplayName(choice.ConsumedItemId);
        if (!string.IsNullOrWhiteSpace(consumed))
            return new ChoiceItemUseDisplay("ใช้แล้วหมด", consumed);

        var used = GetItemDisplayName(choice.UsedItemId);
        if (!string.IsNullOrWhiteSpace(used))
            return new ChoiceItemUseDisplay("ใช้อุปกรณ์", used);

        var required = GetItemDisplayName(choice.RequiredItemId);
        return string.IsNullOrWhiteSpace(required)
            ? null
            : new ChoiceItemUseDisplay("ต้องมี", required);
    }

    private string GetItemDisplayName(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
            return string.Empty;

        var token = itemId.Trim();
        var item = FindInventoryItem(token);
        if (item == null)
            return token;

        return string.IsNullOrWhiteSpace(item.NameTh) ? item.NameEn : item.NameTh;
    }

    private Item? FindInventoryItem(string itemId)
    {
        if (_inventory == null)
            return null;

        var items = _inventory.GetItems().ToList();
        return items.FirstOrDefault(item =>
                   MatchesItemToken(itemId, item.Id) ||
                   MatchesItemToken(itemId, item.StoryAlias) ||
                   MatchesItemToken(itemId, item.NameTh) ||
                   MatchesItemToken(itemId, item.NameEn))
               ?? items.FirstOrDefault(item =>
                   ContainsItemToken(itemId, item.Id) ||
                   ContainsItemToken(itemId, item.StoryAlias) ||
                   ContainsItemToken(itemId, item.NameTh) ||
                   ContainsItemToken(itemId, item.NameEn));
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

    private static void ApplyChoiceButtonStyle(Button button, bool usesItem)
    {
        button.BackgroundColor = Color.FromArgb(usesItem ? "#553B13" : "#DD161616");
        button.BorderColor = Color.FromArgb(usesItem ? "#D8B45F" : "#776C58");
        button.TextColor = Color.FromArgb(usesItem ? "#FFF3D1" : "#FFFFFF");
        button.FontAttributes = usesItem ? FontAttributes.Bold : FontAttributes.None;
    }

    private sealed record ChoiceItemUseDisplay(string Label, string ItemName);
}
