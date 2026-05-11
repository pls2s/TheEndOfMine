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
            inventory = loadedState?.Survivor.Inventory ?? loadedInventory ?? new Inventory();
        }

        var catalog = await _assetResolver.LoadCatalogAsync();
        var sourceSlots = inventory.Slots.Count == 0
            ? new List<Item?>(new Item?[Inventory.Columns * 4])
            : inventory.Slots;

        _slots.Clear();
        for (var i = 0; i < sourceSlots.Count; i++)
        {
            var item = sourceSlots[i];
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
        await Navigation.PopAsync();
    }

    private void OnSlotSelected(object sender, SelectionChangedEventArgs e)
    {
        var slot = e.CurrentSelection.FirstOrDefault() as InventorySlotView;
        if (slot?.Item == null)
        {
            ClearDetails();
            return;
        }

        var item = slot.Item;
        ItemNameLabel.Text = string.IsNullOrWhiteSpace(item.NameTh) ? item.NameEn : item.NameTh;
        ItemMetaLabel.Text = $"{item.Category} • {item.Rarity} • {item.WeightKg:0.0} kg";
        ItemDescriptionLabel.Text = string.IsNullOrWhiteSpace(item.DescriptionTh)
            ? "ไม่มีคำอธิบายไอเทม"
            : item.DescriptionTh;
    }

    private void ClearDetails()
    {
        ItemNameLabel.Text = "เลือกไอเทมเพื่อดูรายละเอียด";
        ItemMetaLabel.Text = string.Empty;
        ItemDescriptionLabel.Text = string.Empty;
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
