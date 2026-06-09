using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RestaurantOS.Application.DTOs.Floor;
using RestaurantOS.Application.Interfaces;
using RestaurantOS.Domain.Enums;
using RestaurantOS.WPF.Services;

namespace RestaurantOS.WPF.ViewModels;

public partial class FloorViewModel : ObservableObject
{
    private readonly IFloorService _floorService;
    private readonly ISessionService _sessionService;
    private readonly CurrentUserService _currentUser;
    private readonly ToastService _toast;
    private readonly System.Windows.Threading.DispatcherTimer _refreshTimer;

    [ObservableProperty] private ObservableCollection<TableCardDto> _tables = new();
    [ObservableProperty] private ObservableCollection<string> _sections = new();
    [ObservableProperty] private string _selectedSection = "Tümü";
    [ObservableProperty] private bool _isLoading = true;
    [ObservableProperty] private SessionDetailViewModel? _sessionDetail;
    [ObservableProperty] private bool _isSessionPanelOpen;
    [ObservableProperty] private bool _isNewSessionDialogOpen;
    [ObservableProperty] private TableCardDto? _selectedTableForNewSession;
    [ObservableProperty] private int _newSessionGuestCount = 2;

    public FloorViewModel(IFloorService floorService, ISessionService sessionService,
        CurrentUserService currentUser, ToastService toast, Func<Guid, SessionDetailViewModel> sessionDetailFactory)
    {
        _floorService = floorService;
        _sessionService = sessionService;
        _currentUser = currentUser;
        _toast = toast;
        _sessionDetailFactory = sessionDetailFactory;

        _refreshTimer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _refreshTimer.Tick += async (_, _) => await LoadTablesAsync(false);
        _refreshTimer.Start();

        _ = InitializeAsync();
    }

    private readonly Func<Guid, SessionDetailViewModel> _sessionDetailFactory;

    private async Task InitializeAsync()
    {
        var sections = await _floorService.GetSectionsAsync();
        Sections = new ObservableCollection<string>(new[] { "Tümü" }.Concat(sections));
        await LoadTablesAsync();
    }

    [RelayCommand]
    private async Task LoadTablesAsync(bool showLoading = true)
    {
        if (showLoading) IsLoading = true;
        try
        {
            var tables = await _floorService.GetTablesAsync(SelectedSection == "Tümü" ? null : SelectedSection);
            Tables = new ObservableCollection<TableCardDto>(tables);
        }
        catch (Exception ex)
        {
            _toast.Error(ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedSectionChanged(string value) => _ = LoadTablesAsync();

    [RelayCommand]
    private async Task SelectTableAsync(TableCardDto? table)
    {
        if (table == null) return;

        if (table.Status == TableDisplayStatus.Empty)
        {
            SelectedTableForNewSession = table;
            NewSessionGuestCount = 2;
            IsNewSessionDialogOpen = true;
            return;
        }

        if (table.SessionId.HasValue)
        {
            try
            {
                var detail = _sessionDetailFactory(table.SessionId.Value);
                detail.OnSessionClosed = async () =>
                {
                    IsSessionPanelOpen = false;
                    SessionDetail = null;
                    await LoadTablesAsync(false);
                };
                detail.OnSessionUpdated = async () => await LoadTablesAsync(false);
                await detail.LoadAsync();
                SessionDetail = detail;
                IsSessionPanelOpen = true;
            }
            catch (Exception ex)
            {
                _toast.Error(ex.Message);
            }
        }
    }

    [RelayCommand]
    private async Task OpenNewSessionAsync()
    {
        if (SelectedTableForNewSession == null) return;
        try
        {
            var session = await _sessionService.OpenSessionAsync(
                SelectedTableForNewSession.TableId, NewSessionGuestCount, _currentUser.UserId);
            IsNewSessionDialogOpen = false;
            _toast.Success($"Masa {SelectedTableForNewSession.TableNumber} açıldı.");
            await LoadTablesAsync(false);
            var detail = _sessionDetailFactory(session.SessionId);
            detail.OnSessionClosed = async () =>
            {
                IsSessionPanelOpen = false;
                SessionDetail = null;
                await LoadTablesAsync(false);
            };
            detail.OnSessionUpdated = async () => await LoadTablesAsync(false);
            await detail.LoadAsync();
            SessionDetail = detail;
            IsSessionPanelOpen = true;
        }
        catch (Exception ex)
        {
            _toast.Error(ex.Message);
        }
    }

    [RelayCommand]
    private void CloseSessionPanel()
    {
        IsSessionPanelOpen = false;
        SessionDetail = null;
    }

    [RelayCommand]
    private void CloseNewSessionDialog() => IsNewSessionDialogOpen = false;
}
