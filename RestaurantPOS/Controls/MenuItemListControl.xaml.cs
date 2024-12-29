using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Models;
namespace RestaurantPOS.Controls;

public partial class MenuItemListControl : ContentView
{

    public MenuItemModel[] Items
    {
        get => (MenuItemModel[])GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public static readonly BindableProperty ItemsProperty =
        BindableProperty.Create("Items", typeof(MenuItemModel[]), typeof(MenuItemListControl), Array.Empty<MenuItemModel>());



    public string ActionIcon
    {
        get { return (string)GetValue(ActionIconProperty); }
        set { SetValue(ActionIconProperty, value); }
    }

    public static readonly BindableProperty ActionIconProperty =
        BindableProperty.Create("ActionIcon", typeof(string), typeof(MenuItemListControl), "shopping_bag_regular_24.png");



    public MenuItemListControl()
	{
		InitializeComponent();
	}

    public event Action<MenuItemModel> OnSelectItem;

    [RelayCommand]
    private void SelectItem(MenuItemModel item) => OnSelectItem?.Invoke(item);
}