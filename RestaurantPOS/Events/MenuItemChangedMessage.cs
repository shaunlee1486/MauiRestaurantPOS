using CommunityToolkit.Mvvm.Messaging.Messages;
using RestaurantPOS.Models;

namespace RestaurantPOS.Events
{
    public class MenuItemChangedMessage : ValueChangedMessage<MenuItemModel>
    {
        public MenuItemChangedMessage(MenuItemModel value) : base(value)
        {
        }

        public static MenuItemChangedMessage From(MenuItemModel value) => new(value);
    }
}
