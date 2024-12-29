using RestaurantPOS.Models;
using RestaurantPOS.ViewModels;

namespace RestaurantPOS.Pages;

public partial class ManageMenuItemPage : ContentPage
{
    private readonly ManageMenuItemsViewModel _manageMenuItemsViewModel;

    public ManageMenuItemPage(ManageMenuItemsViewModel manageMenuItemsViewModel)
	{
		InitializeComponent();
        _manageMenuItemsViewModel = manageMenuItemsViewModel;
        BindingContext = manageMenuItemsViewModel;

        _ = InitializeAsync();
    }

    private async Task InitializeAsync() => await _manageMenuItemsViewModel.InitializeAsync();

    private async void CategoryListControl_OnCategorySelected(Models.MenuCategoryModel category)
    {
        await _manageMenuItemsViewModel.SelectCategoryCommand.ExecuteAsync(category.Id);
    }

    private async void MenuItemListControl_OnSelectItem(MenuItemModel menuItem)
    {
        await _manageMenuItemsViewModel.EditMenuItemCommand.ExecuteAsync(menuItem);
    }

    private void SaveMenuItemFormControl_OnCancel()
    {
        _manageMenuItemsViewModel.CancelCommand.Execute(null);
    }

    private async void SaveMenuItemFormControl_OnSaveItem(Models.MenuItemModel menuItem)
    {
        await _manageMenuItemsViewModel.SaveMenuItemCommand.ExecuteAsync(menuItem);
    }
}