using CleanOrgaCleaner.Models;
using CleanOrgaCleaner.Services;
using System.Collections.ObjectModel;

namespace CleanOrgaCleaner.Views;

public partial class ChatListPage : ContentPage
{
    private readonly ApiService _apiService;
    private ObservableCollection<CleanerInfo> _cleaners;

    public ChatListPage()
    {
        InitializeComponent();
        _apiService = ApiService.Instance;
        _cleaners = new ObservableCollection<CleanerInfo>();
        CleanersCollection.ItemsSource = _cleaners;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await LoadCleanersAsync();
    }

    private async Task LoadCleanersAsync()
    {
        try
        {
            var cleaners = await _apiService.GetAllCleanersAsync();
            _cleaners.Clear();
            foreach (var c in cleaners)
            {
                _cleaners.Add(c);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadCleaners error: {ex.Message}");
        }
    }

    private async void OnAdminChatTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync($"//ChatPage?partner=admin");
    }

    private async void OnCleanerChatTapped(object sender, EventArgs e)
    {
        if (sender is Button btn && btn.CommandParameter is CleanerInfo cleaner)
        {
            await Shell.Current.GoToAsync($"//ChatPage?partner={cleaner.Id}");
        }
    }

    private async void OnMenuTodayClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TodayPage");
    }

    private void OnMenuChatsClicked(object sender, EventArgs e)
    {
        // Already here
    }

    private async void OnMenuSettingsClicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//SettingsPage");
    }
}
