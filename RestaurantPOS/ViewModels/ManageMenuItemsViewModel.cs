using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RestaurantPOS.Data;
using RestaurantPOS.Events;
using RestaurantPOS.Models;

namespace RestaurantPOS.ViewModels
{
    public partial class ManageMenuItemsViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private bool _isInitialized;
        [ObservableProperty]
        private MenuItemModel[] _menuItems;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private MenuItemModel _menuItem = new();

        [ObservableProperty]
        private MenuCategoryModel[] _categories = [];

        [ObservableProperty]
        private MenuCategoryModel? _selectedCategory = null;

        public ManageMenuItemsViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async ValueTask InitializeAsync()
        {
            if (_isInitialized) return;

            _isInitialized = true;
            IsLoading = true;
            Categories = (await _databaseService.GetMenuCategoriesAsync())
                        .Select(MenuCategoryModel.FromEntity)
                        .ToArray();
            Categories[0].IsSelected = true;
            SelectedCategory = Categories[0];

            await GetListMenuItemsByCategoryId(SelectedCategory.Id);

            SetEmptyCategoriesToItem();

            IsLoading = false;
        }

        private async Task GetListMenuItemsByCategoryId(int categoryId)
        {
            MenuItems = (await _databaseService.GetMenuItemByCategoryAsync(categoryId))
                .Select(c => new MenuItemModel
                {
                    Id = c.Id,
                    Name = c.Name,
                    Price = c.Price,
                    Description = c.Description,
                    Icon = c.Icon,
                }).ToArray();
        }

        [RelayCommand]
        private async Task SelectCategoryAsync(int categoryId)
        {
            if (SelectedCategory is null || SelectedCategory.Id == categoryId)
            {
                return;
            };

            IsLoading = true;

            var existingSelectedCategory = Categories.First(c => c.IsSelected);

            existingSelectedCategory.IsSelected = false;

            var newlySelectedCategory = Categories.First(c => c.Id == categoryId);
            newlySelectedCategory.IsSelected = true;

            SelectedCategory = newlySelectedCategory;
            await Task.Delay(500);
            await GetListMenuItemsByCategoryId(SelectedCategory.Id);

            IsLoading = false;
        }

        [RelayCommand]
        private async Task EditMenuItemAsync(MenuItemModel menuItem)
        {
            var itemCategories = await _databaseService.GetCategoriesOfMenuItem(menuItem.Id);

            foreach (var category in Categories)
            {
                var categoryOfItem = new MenuCategoryModel
                {
                    Id = category.Id,
                    Icon = category.Icon,
                    Name = category.Name,
                    IsSelected = itemCategories.Any(c => c.Id == category.Id)
                };

                menuItem.MenuCategories.Add(categoryOfItem);
            }

            MenuItem = menuItem;
        }

        private void SetEmptyCategoriesToItem()
        {
            MenuItem.MenuCategories.Clear();
            foreach (var category in Categories)
            {
                var categoryOfItem = new MenuCategoryModel
                {
                    Id = category.Id,
                    Icon = category.Icon,
                    Name = category.Name,
                    IsSelected = false
                };

                MenuItem.MenuCategories.Add(categoryOfItem);
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            MenuItem = new();

            SetEmptyCategoriesToItem();
        }

        [RelayCommand]
        private async Task SaveMenuItem(MenuItemModel menuItemModel)
        {
            IsLoading = true;

            // Save item to database
            var errorMessage = await _databaseService.SaveMenuItemAsync(menuItemModel);
            
            if (errorMessage != null) 
            {
                await Shell.Current.DisplayAlert("Error", errorMessage, "Ok");
            }
            else
            {
                await Toast.Make("Menu item saved successfully").Show();
                HandleMenuItemChanged(menuItemModel);
                WeakReferenceMessenger.Default.Send(MenuItemChangedMessage.From(menuItemModel));
                Cancel();
            }
            IsLoading = false;
        }

        private void HandleMenuItemChanged(MenuItemModel model) 
        {
            var menuItem = MenuItems.FirstOrDefault(c => c.Id == model.Id);

            if (menuItem != null) 
            {
                // this menu item is on the screen the right now

                // check if this item still has a mapping to selected category
                if (!model.MenuCategories.Any(c => c.IsSelected && c.Id == SelectedCategory.Id))
                {
                    // this item no longer belongs to the selected category
                    // remove this item from the current ui menu items list
                    MenuItems = [.. MenuItems.Where(c => c.Id != model.Id)];
                    return;
                }

                // update the details
                menuItem.Price = model.Price;
                menuItem.Description = model.Description;
                menuItem.Icon = model.Icon;
                menuItem.Name = model.Name;

                //MenuItems = [.. MenuItems];
            }
            else if (model.MenuCategories.Any(c => c.IsSelected && c.Id == SelectedCategory.Id))
            {
                // this item was not on the ui
                // we updated the item by adding this currently selected category
                // so add this menu item to the current ui menu item list

                MenuItems = [.. MenuItems, model];
            }
        }
    }
}
