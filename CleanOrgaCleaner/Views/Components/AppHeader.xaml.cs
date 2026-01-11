using CleanOrgaCleaner.Services;

namespace CleanOrgaCleaner.Views.Components;

public partial class AppHeader : ContentView
{
    public static readonly BindableProperty CurrentPageProperty =
        BindableProperty.Create(nameof(CurrentPage), typeof(string), typeof(AppHeader), "Heute", propertyChanged: OnCurrentPageChanged);

    public string CurrentPage
    {
        get => (string)GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, value);
    }

    private static void OnCurrentPageChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is AppHeader header && newValue is string pageName)
        {
            header.MenuButton.Text = pageName + " ▼";
        }
    }

    public event EventHandler? MenuOpened;
    public event EventHandler? MenuClosed;

    public AppHeader()
    {
        InitializeComponent();
    }

    private void OnMenuButtonClicked(object sender, EventArgs e)
    {
        MenuOpened?.Invoke(this, EventArgs.Empty);
    }

    private async void OnLogoutClicked(object sender, EventArgs e)
    {
        ApiService.Instance.Logout();
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
