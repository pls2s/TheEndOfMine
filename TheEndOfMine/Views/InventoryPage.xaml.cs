namespace TheEndOfMine.Views;

using System.Collections.ObjectModel;
using TheEndOfMine.Data;
using TheEndOfMine.Models;
using TheEndOfMine.Services;

public partial class InventoryPage : ContentPage
{
    private readonly ObservableCollection<InventorySlotView> _slots = new();
    private readonly SaveService _saveService = new();
    private readonly StoryAssetResolver _assetResolver = new();
    private GameState? _state;
    private Inventory _inventory = new();
    private InventorySlotView? _selectedSlot;

    public InventoryPage()
    {
        InitializeComponent();
        InventoryGrid.ItemsSource = _slots;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadInventoryAsync();
    }

    private async Task LoadInventoryAsync()
    {
        var state = await _saveService.LoadCheckpointAsync();
        Inventory? inventory = state?.Survivor.Inventory;

        if (inventory == null || inventory.Slots.Count == 0)
        {
            var db = new GameDatabase();
            var (_, loadedState, loadedInventory) = await db.LoadAsync();
            state = loadedState ?? state;
            inventory = loadedState?.Survivor.Inventory ?? loadedInventory ?? new Inventory();
        }

        _state = state;
        _inventory = inventory;
        _inventory.ApplyContainerCapacityBonuses();
        if (_state != null)
            _state.Survivor.Inventory = _inventory;

        var catalog = await _assetResolver.LoadCatalogAsync();
        var sourceSlots = _inventory.Slots.Count == 0
            ? new List<Item?>(new Item?[Inventory.Columns * 4])
            : _inventory.Slots;

        _slots.Clear();
        for (var i = 0; i < sourceSlots.Count; i++)
        {
            var item = sourceSlots[i];
            if (item != null)
                ItemRewardConsistencyService.Normalize(item);
            if (item != null && string.IsNullOrWhiteSpace(item.ImagePath) && !string.IsNullOrWhiteSpace(item.StoryAlias))
                item.ImagePath = _assetResolver.ResolveItemPath(item.StoryAlias, catalog);

            _slots.Add(new InventorySlotView(i, item));
        }

        var itemCount = _slots.Count(slot => slot.HasItem);
        var totalWeight = _slots.Where(slot => slot.Item != null).Sum(slot => slot.Item!.WeightKg);
        CapacityLabel.Text = $"{itemCount}/{_slots.Count} ช่อง";
        WeightLabel.Text = $"{totalWeight:0.0} kg";
        ClearDetails();
    }

    private async void OnBackClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();
        await Navigation.PopAsync();
    }

    private void OnSlotSelected(object sender, SelectionChangedEventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();

        var slot = e.CurrentSelection.FirstOrDefault() as InventorySlotView;
        if (slot?.Item == null)
        {
            _selectedSlot = null;
            ClearDetails();
            return;
        }

        _selectedSlot = slot;
        var item = slot.Item;
        ItemNameLabel.Text = string.IsNullOrWhiteSpace(item.NameTh) ? item.NameEn : item.NameTh;
        ItemMetaLabel.Text = $"{item.Category} • {item.Rarity} • {item.WeightKg:0.0} kg{FormatDurability(item)}{FormatEffects(item)}";
        ItemDescriptionLabel.Text = string.IsNullOrWhiteSpace(item.DescriptionTh)
            ? "ไม่มีคำอธิบายไอเทม"
            : item.DescriptionTh;
        UseItemButton.IsVisible = item.IsUsable;
        UseItemButton.IsEnabled = item.IsUsable;
    }

    private void ClearDetails()
    {
        _selectedSlot = null;
        ItemNameLabel.Text = "เลือกไอเทมเพื่อดูรายละเอียด";
        ItemMetaLabel.Text = string.Empty;
        ItemDescriptionLabel.Text = string.Empty;
        UseItemButton.IsVisible = false;
        UseItemButton.IsEnabled = false;
    }

    private async void OnUseItemClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();

        if (_state == null || _selectedSlot?.Item == null)
            return;

        var item = _selectedSlot.Item;
        if (!item.IsUsable)
            return;

        ApplyItemEffects(_state, item);
        ConsumeOrWearItem(item);
        _state.Survivor.Inventory = _inventory;

        _saveService.SaveCheckpoint(_state);
        var db = new GameDatabase();
        await db.SaveAsync(_state.Survivor, _state, _inventory);

        await LoadInventoryAsync();
        await ShowUseItemOverlayAsync(BuildUseMessage(item));
    }

    private async Task ShowUseItemOverlayAsync(string message)
    {
        UseItemMessageLabel.Text = message;
        UseItemOverlay.InputTransparent = false;
        UseItemOverlay.Opacity = 0;
        UseItemOverlay.IsVisible = true;
        await UseItemOverlay.FadeTo(1, 160, Easing.CubicOut);
    }

    private async void OnUseItemOverlayOkClicked(object sender, EventArgs e)
    {
        AudioFeedbackService.PlayButtonTap();
        await HideUseItemOverlayAsync();
    }

    private async Task HideUseItemOverlayAsync()
    {
        if (!UseItemOverlay.IsVisible)
            return;

        await UseItemOverlay.FadeTo(0, 120, Easing.CubicIn);
        UseItemOverlay.IsVisible = false;
        UseItemOverlay.InputTransparent = true;
    }

    private static void ApplyItemEffects(GameState state, Item item)
    {
        var effects = item.Effects;
        if (effects == null) return;

        var survivor = state.Survivor;
        survivor.HP = Math.Clamp(survivor.HP + GetHpRestore(item), 0f, 100f);
        survivor.Hunger = Math.Clamp(survivor.Hunger + effects.HungerRestore.GetValueOrDefault(), 0f, 100f);
        survivor.Thirst = Math.Clamp(survivor.Thirst + effects.ThirstRestore.GetValueOrDefault(), 0f, 100f);
        survivor.Fatigue = Math.Clamp(survivor.Fatigue - effects.FatigueRestore.GetValueOrDefault(), 0f, 100f);
        state.Infection = Math.Clamp(state.Infection - GetInfectionReduction(item), 0f, 100f);
    }

    private void ConsumeOrWearItem(Item item)
    {
        if (item.Effects?.OneTimeUse != false)
        {
            _inventory.RemoveItem(item);
            return;
        }

        if (item.Durability == null)
            return;

        item.Durability = Math.Max(0, item.Durability.Value - 1);
        if (item.Durability <= 0)
            _inventory.RemoveItem(item);
    }

    private static string FormatEffects(Item item)
    {
        var effects = item.Effects;
        if (effects == null) return string.Empty;

        var parts = new List<string>();
        var hpRestore = GetHpRestore(item);
        if (hpRestore != 0)
            parts.Add($"HP +{hpRestore:0}");
        if (effects.HungerRestore.GetValueOrDefault() != 0)
            parts.Add($"อาหาร +{effects.HungerRestore.GetValueOrDefault():0}");
        if (effects.ThirstRestore.GetValueOrDefault() != 0)
            parts.Add($"น้ำ +{effects.ThirstRestore.GetValueOrDefault():0}");
        if (effects.FatigueRestore.GetValueOrDefault() != 0)
            parts.Add($"เหนื่อย -{effects.FatigueRestore.GetValueOrDefault():0}");
        if (effects.BiteInfectionReduce.GetValueOrDefault() != 0)
            parts.Add($"ติดเชื้อ -{effects.BiteInfectionReduce.GetValueOrDefault():0}");
        else if (item.IsMedicalItem)
            parts.Add("ติดเชื้อ -8");
        if (effects.CarryCapacityBonus.GetValueOrDefault() > 0)
            parts.Add($"ช่องเก็บของ +{effects.CarryCapacityBonus.GetValueOrDefault():0}");

        return parts.Count == 0 ? string.Empty : $" • {string.Join(", ", parts)}";
    }

    private static string FormatDurability(Item item)
    {
        return item.DurabilityMax > 1 && item.Durability != null
            ? $" • ใช้ได้ {item.Durability}/{item.DurabilityMax}"
            : string.Empty;
    }

    private static string BuildUseMessage(Item item)
    {
        var name = string.IsNullOrWhiteSpace(item.NameTh) ? item.NameEn : item.NameTh;
        var effects = FormatEffects(item);
        return string.IsNullOrWhiteSpace(effects)
            ? $"ใช้ {name} แล้ว"
            : $"ใช้ {name} แล้ว\n{effects.TrimStart(' ', '•')}";
    }

    private static float GetHpRestore(Item item)
    {
        var explicitRestore = item.Effects?.HpRestore.GetValueOrDefault() ?? 0f;
        if (explicitRestore != 0)
            return explicitRestore;

        return item.IsMedicalItem ? 8f : 0f;
    }

    private static float GetInfectionReduction(Item item)
    {
        var explicitReduce = item.Effects?.BiteInfectionReduce.GetValueOrDefault() ?? 0f;
        if (explicitReduce != 0)
            return explicitReduce;

        return item.IsMedicalItem ? 8f : 0f;
    }

    public sealed class InventorySlotView
    {
        public InventorySlotView(int index, Item? item)
        {
            Index = index;
            Item = item;
        }

        public int Index { get; }
        public Item? Item { get; }
        public bool HasItem => Item != null;
        public bool IsEmpty => Item == null;
        public string EmptyText => "+";
        public string ImageSource => Item?.ImagePath ?? string.Empty;
        public string ShortName
        {
            get
            {
                if (Item == null)
                    return string.Empty;

                var name = string.IsNullOrWhiteSpace(Item.NameTh) ? Item.NameEn : Item.NameTh;
                return string.IsNullOrWhiteSpace(name) ? Item.Id : name;
            }
        }
    }
}
