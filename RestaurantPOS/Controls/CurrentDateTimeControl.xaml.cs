namespace RestaurantPOS.Controls;

public partial class CurrentDateTimeControl : ContentView, IDisposable
{
	private readonly PeriodicTimer _timer;

	public CurrentDateTimeControl()
	{
		InitializeComponent();

		dayTimeLabel.Text = DateTime.Now.ToString("dddd, hh:mm:ss tt");
		dateLabel.Text = DateTime.Now.ToString("MMM dd, yyyy");

		_timer = new PeriodicTimer(TimeSpan.FromSeconds(1));

		_ = UpdateTimerAsync();
    }

    private async Task UpdateTimerAsync()
	{
		while (await _timer.WaitForNextTickAsync())
		{
            dayTimeLabel.Text = DateTime.Now.ToString("dddd, hh:mm:ss tt");
            dateLabel.Text = DateTime.Now.ToString("MMM dd, yyyy");
        }
	}

    public void Dispose()
    {
        _timer.Dispose();
    }
}
