using RestaurantPOS.ViewModels;

namespace RestaurantPOS.Pages;

public partial class OrdersPage : ContentPage
{
    private readonly OrdersViewModel _ordersViewModel;

    public OrdersPage(OrdersViewModel ordersViewModel)
	{
		InitializeComponent();
        _ordersViewModel = ordersViewModel;

        _ = InitializeViewModelAsync();
    }

    private async Task InitializeViewModelAsync()
    {
        await _ordersViewModel.InitializeAsync();
    }
}