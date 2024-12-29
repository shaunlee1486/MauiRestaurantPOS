using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RestaurantPOS.Data;
using RestaurantPOS.Events;
using RestaurantPOS.Models;
using System.Collections.ObjectModel;

namespace RestaurantPOS.ViewModels
{
    public partial class HomeViewModel : ObservableObject, IRecipient<MenuItemChangedMessage>
    {
        private readonly DatabaseService _databaseService;
        private readonly OrdersViewModel _ordersViewModel;
        private readonly SettingsViewModel _settingsViewModel;
        private bool _isInitialized;

        [ObservableProperty]
        private MenuItemModel[] _menuItems;


        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private MenuCategoryModel[] _categories = [];

        [ObservableProperty]
        private MenuCategoryModel? _selectedCategory = null;
        
        public ObservableCollection<CartModel> CartItems { get; set; } = new();

        [ObservableProperty, NotifyPropertyChangedFor(nameof(TaxAmount))]
        [NotifyPropertyChangedFor(nameof(Total))]
        private decimal _subtotal;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(TaxAmount))]
        [NotifyPropertyChangedFor(nameof(Total))]
        private int _taxPercentage;

        [ObservableProperty]
        private string _name = "Guest";

        public decimal TaxAmount => (Subtotal * TaxPercentage)/100;

        public decimal Total => Subtotal + TaxAmount; 

        public HomeViewModel(DatabaseService databaseService, OrdersViewModel ordersViewModel, SettingsViewModel settingsViewModel)
        {
            _databaseService = databaseService;
            _ordersViewModel = ordersViewModel;
            _settingsViewModel = settingsViewModel;
            CartItems.CollectionChanged += CartItems_CollectionChanged;

            // option1
            //WeakReferenceMessenger.Default.Register<MenuItemChangedMessage>(this, (recipient, message) =>
            //{

            //});

            // option2 with IRecipient
            WeakReferenceMessenger.Default.Register<MenuItemChangedMessage>(this);
            WeakReferenceMessenger.Default.Register<NameChangedMessage>(this, (recipient, message) =>
            {
                Name = message.Value;
            });

            TaxPercentage = settingsViewModel.GetTaxPercentage();
        }

        private void CartItems_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            // it will be executed whenever
            // we are adding any item to the cart
            // removing item from the cart
            // or clearing the cart
            RecalculateAmounts();
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
            if(SelectedCategory == null || SelectedCategory.Id == categoryId)
            {
                return;
            }

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
        private void AddToCart(MenuItemModel menuItem)
        {
            var cartItem = CartItems.FirstOrDefault(c => c.ItemId == menuItem.Id);
            if (cartItem == null)
            {
                CartItems.Add(new CartModel
                {
                    ItemId = menuItem.Id,
                    Icon = menuItem.Icon,
                    Name = menuItem.Name,
                    Price = menuItem.Price,
                    Quantity = 1
                });
            }
            else
            {
                cartItem.Quantity++;
            }

            RecalculateAmounts();
        }

        [RelayCommand]
        private void IncreaseQuantity(CartModel cartItem)
        {
            cartItem.Quantity++;
            RecalculateAmounts();
        }

        [RelayCommand]
        private void DecreaseQuantity(CartModel cartItem)
        {
            cartItem.Quantity--;

            if (cartItem.Quantity == 0) 
            {
                CartItems.Remove(cartItem);
            }
            else
            {
                RecalculateAmounts();
            }

        }

        [RelayCommand]
        private void RemoveItemFormCart(CartModel cartItem)
        {
            CartItems.Remove(cartItem);
            //RecalculateAmounts();
        }

        [RelayCommand]
        private async Task ClearCart()
        {
            if (await Shell.Current.DisplayAlert("Clear Cart?", "Do you really want to clear the cart?", "Yes", "No")) 
            {
                CartItems.Clear();
            }
        }
        
        private void RecalculateAmounts()
        {
            Subtotal = CartItems.Sum(c => c.Amount);
        }

        [RelayCommand]
        private async Task TaxPercentageClickAsync()
        {
            var result = await Shell.Current.DisplayPromptAsync("Tax Percentage", "Enter the applicable tax percentage", placeholder: "10", initialValue: TaxPercentage.ToString());

            if (!string.IsNullOrWhiteSpace(result) && int.TryParse(result, out var taxPercentage))
            {
                if (taxPercentage > 100)
                {
                    await Shell.Current.DisplayAlert("Invalid value", "Entered tax can not be more than 100", "Ok");
                    return;
                }

                TaxPercentage = taxPercentage;

                // Save it in preferences
                _settingsViewModel.SetTaxPercentage(taxPercentage);
            }
            else
            {
                await Shell.Current.DisplayAlert("Invalid value", "Entered tax percentage is invalid", "Ok");
            }
        }

        [RelayCommand]
        private async Task PlaceOrderAsync(bool isPaidOnline)
        {
            IsLoading = true;

            var isSuccessful = await _ordersViewModel.PlaceOrderAsync([.. CartItems], isPaidOnline);

            if (isSuccessful) 
            { 
                CartItems.Clear();
            }

            IsLoading = false;
        }

        public void Receive(MenuItemChangedMessage message)
        {
            var model = message.Value;
            RefreshUpdateMenuItemList(model);

            var cartItem = CartItems.FirstOrDefault(c => c.ItemId == model.Id);
            if (cartItem != null) 
            { 
                cartItem.Price = model.Price;
                cartItem.Icon = model.Icon;
                cartItem.Name = model.Name;

                RecalculateAmounts();
            }
        }

        private void RefreshUpdateMenuItemList(MenuItemModel model)
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