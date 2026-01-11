using CleanOrgaCleaner.Services;
using Microsoft.Maui.Controls.Shapes;

namespace CleanOrgaCleaner.Views.Components;

/// <summary>
/// Base page class with shared header and menu functionality.
/// All pages should inherit from this class for consistent navigation.
/// </summary>
public partial class BasePage : ContentPage
{
    protected Grid? MenuOverlayGrid { get; set; }
    protected Button? MenuButton { get; set; }
    protected string CurrentPageName { get; set; } = "Menu";

    public BasePage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Creates the standard app header with logo, menu button, and logout.
    /// Call this from derived pages to add the header.
    /// </summary>
    protected Grid CreateAppHeader(string pageName)
    {
        CurrentPageName = pageName;
        
        var headerGrid = new Grid
        {
            Padding = new Thickness(15, 45, 15, 15),
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Auto)
            }
        };
        
        headerGrid.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#2196F3"), Offset = 0 },
                new GradientStop { Color = Color.FromArgb("#1976D2"), Offset = 1 }
            }
        };

        // Logo and app name
        var logoStack = new HorizontalStackLayout { Spacing = 6, VerticalOptions = LayoutOptions.Center };
        var logo = new Image { Source = "logo.jpg", WidthRequest = 28, HeightRequest = 28 };
        logo.Clip = new EllipseGeometry { Center = new Point(14, 14), RadiusX = 14, RadiusY = 14 };
        logoStack.Add(logo);
        logoStack.Add(new Label { Text = "CleanOrga", FontSize = 16, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, VerticalOptions = LayoutOptions.Center });
        Grid.SetColumn(logoStack, 0);
        headerGrid.Add(logoStack);

        // Menu button
        MenuButton = new Button
        {
            Text = pageName + " ▼",
            BackgroundColor = Color.FromArgb("#ffffff33"),
            TextColor = Colors.White,
            FontSize = 13,
            FontAttributes = FontAttributes.Bold,
            CornerRadius = 8,
            Padding = new Thickness(10, 6),
            HorizontalOptions = LayoutOptions.Center
        };
        MenuButton.Clicked += OnMenuButtonClicked;
        Grid.SetColumn(MenuButton, 1);
        headerGrid.Add(MenuButton);

        // Logout button
        var logoutButton = new Button
        {
            Text = "Logout",
            BackgroundColor = Color.FromArgb("#ffffff33"),
            TextColor = Colors.White,
            FontSize = 13,
            CornerRadius = 8,
            Padding = new Thickness(10, 6)
        };
        logoutButton.Clicked += OnLogoutClicked;
        Grid.SetColumn(logoutButton, 2);
        headerGrid.Add(logoutButton);

        return headerGrid;
    }

    /// <summary>
    /// Creates the menu overlay that should be added to the main grid.
    /// </summary>
    protected Grid CreateMenuOverlay()
    {
        MenuOverlayGrid = new Grid { IsVisible = false, ZIndex = 1000 };
        
        // Transparent background to close menu on tap
        var background = new BoxView { Color = Colors.Transparent };
        var tapGesture = new TapGestureRecognizer();
        tapGesture.Tapped += OnOverlayTapped;
        background.GestureRecognizers.Add(tapGesture);
        MenuOverlayGrid.Add(background);

        // Menu dropdown
        var menuBorder = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Stroke = Colors.Transparent,
            Padding = 0,
            Margin = new Thickness(60, 95, 60, 0),
            VerticalOptions = LayoutOptions.Start,
            Shadow = new Shadow { Brush = Brush.Gray, Offset = new Point(2, 2), Radius = 5, Opacity = 0.3f }
        };
        menuBorder.Background = new LinearGradientBrush
        {
            StartPoint = new Point(0, 0),
            EndPoint = new Point(1, 1),
            GradientStops = new GradientStopCollection
            {
                new GradientStop { Color = Color.FromArgb("#2196F3"), Offset = 0 },
                new GradientStop { Color = Color.FromArgb("#1976D2"), Offset = 1 }
            }
        };

        var menuStack = new VerticalStackLayout { Spacing = 0 };
        
        AddMenuItem(menuStack, "🏠 Heute", OnMenuTodayClicked);
        AddMenuDivider(menuStack);
        AddMenuItem(menuStack, "💬 Chat", OnMenuChatClicked);
        AddMenuDivider(menuStack);
        AddMenuItem(menuStack, "📋 Neue Aufgabe", OnMenuMyTasksClicked);
        AddMenuDivider(menuStack);
        AddMenuItem(menuStack, "⚙️ Einstellungen", OnMenuSettingsClicked);

        menuBorder.Content = menuStack;
        MenuOverlayGrid.Add(menuBorder);

        return MenuOverlayGrid;
    }

    private void AddMenuItem(VerticalStackLayout stack, string text, EventHandler handler)
    {
        var btn = new Button
        {
            Text = text,
            BackgroundColor = Color.FromArgb("#2196F3"),
            TextColor = Colors.White,
            FontSize = 16,
            Padding = new Thickness(20, 15),
            HorizontalOptions = LayoutOptions.Fill
        };
        btn.Clicked += handler;
        stack.Add(btn);
    }

    private void AddMenuDivider(VerticalStackLayout stack)
    {
        stack.Add(new BoxView { HeightRequest = 1, Color = Color.FromArgb("#ffffff33") });
    }

    protected virtual void OnMenuButtonClicked(object? sender, EventArgs e)
    {
        if (MenuOverlayGrid != null)
            MenuOverlayGrid.IsVisible = !MenuOverlayGrid.IsVisible;
    }

    protected virtual void OnOverlayTapped(object? sender, EventArgs e)
    {
        if (MenuOverlayGrid != null)
            MenuOverlayGrid.IsVisible = false;
    }

    protected virtual async void OnLogoutClicked(object? sender, EventArgs e)
    {
        ApiService.Instance.Logout();
        await Shell.Current.GoToAsync("//LoginPage");
    }

    protected virtual async void OnMenuTodayClicked(object? sender, EventArgs e)
    {
        if (MenuOverlayGrid != null) MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//TodayPage");
    }

    protected virtual async void OnMenuChatClicked(object? sender, EventArgs e)
    {
        if (MenuOverlayGrid != null) MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//MainTabs/ChatListPage");
    }

    protected virtual async void OnMenuMyTasksClicked(object? sender, EventArgs e)
    {
        if (MenuOverlayGrid != null) MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//MyTasksPage");
    }

    protected virtual async void OnMenuSettingsClicked(object? sender, EventArgs e)
    {
        if (MenuOverlayGrid != null) MenuOverlayGrid.IsVisible = false;
        await Shell.Current.GoToAsync("//SettingsPage");
    }
}
