
using CommunityToolkit.Maui.Views;

namespace RestaurantPOS.Popups;

public partial class HelpPopup : Popup
{
	public HelpPopup()
	{
		InitializeComponent();
	}

    private async void ClosePopup_Tapped(object sender, TappedEventArgs e)
    {
		await this.CloseAsync();
    }
}