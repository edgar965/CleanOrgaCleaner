using CleanOrgaCleaner.Views;

namespace CleanOrgaCleaner;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Start with Login page
        return new Window(new NavigationPage(new LoginPage()));
    }
}
