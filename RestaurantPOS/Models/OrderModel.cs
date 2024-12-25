
using CommunityToolkit.Mvvm.ComponentModel;
using RestaurantPOS.Data;

namespace RestaurantPOS.Models
{
    public partial class OrderModel : ObservableObject
    {
        public long Id { get; set; }
        public DateTime OrderDate { get; set; }
        public int TotalItemCount { get; set; }
        public decimal TotalAmountPaid { get; set; }
        public string PaymentMode { get; set; } // cash or online

        public OrderItem[] Items;

        [ObservableProperty]
        private bool _isSelected;
    }
}
