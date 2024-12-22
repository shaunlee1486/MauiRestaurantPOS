using RestaurantPOS.ViewModels;

namespace RestaurantPOS.Pages
{
    public partial class MainPage : ContentPage
    {
        private readonly HomeViewModel _homeViewModel;

        public MainPage(HomeViewModel homeViewModel)
        {
            InitializeComponent();
            _homeViewModel = homeViewModel;
            BindingContext = _homeViewModel;

            _ = Initialize();
        }

        private async Task Initialize()
        {
            await _homeViewModel.InitializeAsync();
        }
    }
}