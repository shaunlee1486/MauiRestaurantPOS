using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Data;
using RestaurantPOS.Models;
using System.Collections.ObjectModel;
using MenuItem = RestaurantPOS.Data.MenuItem;

namespace RestaurantPOS.ViewModels
{
    public partial class HomeViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;
        private readonly OrdersViewModel _ordersViewModel;
        private bool _isInitialized;

        [ObservableProperty]
        private MenuItem[] _menuItems;


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

        public decimal TaxAmount => (Subtotal * TaxPercentage)/100;

        public decimal Total => Subtotal + TaxAmount; 

        public HomeViewModel(DatabaseService databaseService, OrdersViewModel ordersViewModel)
        {
            _databaseService = databaseService;
            _ordersViewModel = ordersViewModel;
            CartItems.CollectionChanged += CartItems_CollectionChanged;
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

            MenuItems = await _databaseService.GetMenuItemByCategoryAsync(SelectedCategory.Id);

            IsLoading = false;
        }

        [RelayCommand]
        private async Task SelectCategoryAsync(int categoryId) 
        {
            if(SelectedCategory.Id == categoryId) return;

            IsLoading = true;

            var existingSelectedCategory = Categories.First(c => c.IsSelected);

            existingSelectedCategory.IsSelected = false;

            var newlySelectedCategory = Categories.First(c => c.Id == categoryId);
            newlySelectedCategory.IsSelected = true;

            SelectedCategory = newlySelectedCategory;
            await Task.Delay(500);
            MenuItems = await _databaseService.GetMenuItemByCategoryAsync(SelectedCategory.Id);

            IsLoading = false;
        }

        [RelayCommand]
        private void AddToCart(MenuItem menuItem)
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
    }
}