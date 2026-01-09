using CleanOrgaCleaner.Views;

namespace CleanOrgaCleaner;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Register routes for navigation
        Routing.RegisterRoute("TaskDetailPage", typeof(TaskDetailPage));
    }
}
