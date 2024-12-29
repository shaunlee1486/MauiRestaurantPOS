using CommunityToolkit.Mvvm.ComponentModel;

namespace RestaurantPOS.Models
{
    public partial class CartModel : ObservableObject
    {
        public int ItemId { get; set; }

        [ObservableProperty]
        private string _name;

        [ObservableProperty]
        private string _icon;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(Amount))]
        private decimal _price;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(Amount))]
        private int _quantity;
        public decimal Amount => Price * Quantity;
    }
}
