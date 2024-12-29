using RestaurantPOS.Models;
using RestaurantPOS.ViewModels;

namespace RestaurantPOS.Pages
{
    public partial class MainPage : ContentPage
    {
        private readonly HomeViewModel _homeViewModel;
        private readonly SettingsViewModel _settingsViewModel;

        public MainPage(HomeViewModel homeViewModel, SettingsViewModel settingsViewModel)
        {
            InitializeComponent();
            _homeViewModel = homeViewModel;
            _settingsViewModel = settingsViewModel;
            BindingContext = _homeViewModel;

            _ = Initialize();
        }

        private async Task Initialize()
        {
            await _homeViewModel.InitializeAsync();
        }

        protected override async void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            await _settingsViewModel.InitializeAsync();
        }

        private async void CategoryListControl_OnCategorySelected(Models.MenuCategoryModel category)
        {
            await _homeViewModel.SelectCategoryCommand.ExecuteAsync(category.Id);
        }

        private void MenuItemListControl_OnSelectItem(MenuItemModel menuItem)
        {
            _homeViewModel.AddToCartCommand.Execute(menuItem);
        }
    }
}