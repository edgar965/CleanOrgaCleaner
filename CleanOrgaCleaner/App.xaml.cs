namespace CleanOrgaCleaner;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        // Use AppShell for navigation
        return new Window(new AppShell());
    }
}
