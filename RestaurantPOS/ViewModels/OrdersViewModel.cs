
using CommunityToolkit.Maui.Alerts;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantPOS.Data;
using RestaurantPOS.Models;
using System.Collections.ObjectModel;

namespace RestaurantPOS.ViewModels
{
    public partial class OrdersViewModel : ObservableObject
    {
        private readonly DatabaseService _databaseService;

        public ObservableCollection<Order> Orders { get; set; } = [];

        private bool _isInitialized;

        [ObservableProperty]
        private bool _isLoading;

        [ObservableProperty]
        private OrderItem[] _orderItems = [];

        public OrdersViewModel(DatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        public async Task<bool> PlaceOrderAsync(CartModel[] cartItems, bool isPaidOnline)
        {
            var orderItems = cartItems.Select(c => new OrderItem
            {
                Icon = c.Icon,
                ItemId = c.ItemId,
                Name = c.Name,
                Price = c.Price,
                Quantity = c.Quantity
            }).ToArray();

            var orderModel = new OrderModel()
            {
                OrderDate = DateTime.Now,
                PaymentMode = isPaidOnline ? "Online" : "Cash",
                TotalAmountPaid = cartItems.Sum(c => c.Amount),
                TotalItemCount = cartItems.Length,
                Items = orderItems
            };

            var result = await _databaseService.PlaceOrderAsync(orderModel);

            if (!string.IsNullOrEmpty(result))
            {
                // Order creation  failed
                await Shell.Current.DisplayAlert("Error", result, "Ok");
                return false;
            }

            // order creation was successfully
            await Toast.Make("Order placed successfully").Show();
            return true;
        }

        [RelayCommand]
        private async Task SelectOrderAsync(Order? order)
        {
            if (order == null || order.Id == 0) 
            {
                OrderItems = [];
                return;
            }

            OrderItems = await _databaseService.GetOrderItemsAsync(order.Id);
        }

        public async ValueTask InitializeAsync()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            IsLoading = true;

            var orders = await _databaseService.GetOrdersAsync();
            
            await Task.Delay(1000);

            foreach (var order in orders) 
            {
                Orders.Add(order);
            }

            IsLoading = false;
        }
    }
}
