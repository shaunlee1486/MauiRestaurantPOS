using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Models;

namespace RestaurantPOS.Controls;

public partial class SaveMenuItemFormControl : ContentView
{
    public MenuItemModel Item
    {
        get { return (MenuItemModel)GetValue(ItemProperty); }
        set { SetValue(ItemProperty, value); }
    }

    public static readonly BindableProperty ItemProperty =
        BindableProperty.Create("Item", typeof(MenuItemModel), typeof(SaveMenuItemFormControl), new MenuItemModel());

    public SaveMenuItemFormControl()
    {
        InitializeComponent();
    }

    [RelayCommand]
    private void ToggleCategorySelection(MenuCategoryModel category) => category.IsSelected = !category.IsSelected;

    public event Action? OnCancel;

    public event Action<MenuItemModel>? OnSaveItem;

    [RelayCommand]
    private void Cancel() => OnCancel?.Invoke();

    [RelayCommand]
    private async Task SaveMenuItemAsync()
    {
        // Validation
        if (string.IsNullOrEmpty(Item.Name) || string.IsNullOrEmpty(Item.Description) || string.IsNullOrEmpty(Item.Icon))
        {
            await ErrorAlertAsync("Item name, description and icon are mendatory");
            return;
        }

        if (!Item.MenuCategories.Any(c => c.IsSelected))
        {
            await ErrorAlertAsync("Please select at-least 1 category");
            return;
        }

        OnSaveItem?.Invoke(Item);

        static async Task ErrorAlertAsync(string message) => await Shell.Current.DisplayAlert("Validation Error", message, "Ok");
    }

    private async void PickImageButton_Clicked(object sender, EventArgs e)
    {
        var fileResult = await MediaPicker.PickPhotoAsync();

        if (fileResult != null)
        {
            // user selected an image from the image picker dialog

            // Upload, save the image on disc
            var imageStream = await fileResult.OpenReadAsync();

            var localPath = Path.Combine(FileSystem.AppDataDirectory, fileResult.FileName);

            using var fileStream = new FileStream(localPath, FileMode.Create, FileAccess.Write);

            await imageStream.CopyToAsync(fileStream);

            // update the image icon on the ui
            Item.Icon = localPath;
        }
    }
}