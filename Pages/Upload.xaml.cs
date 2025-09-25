using CommunityToolkit.Mvvm.ComponentModel;
using fbapp.LocalData;
using fbapp.Pages.Popup;
using Syncfusion.Maui.Inputs;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http.Json;
using static SQLite.SQLite3;

namespace fbapp.Pages;

public partial class Upload : ContentPage
{
    private readonly DbContext _db;
    private readonly UploadViewModel _vm;
    private List<DTRLog>? logs;
    private HttpClient _httpClient;
    private string hrisApiUrl = "NA";
    private bool isPageEnabled = false;
   // private Timer _timer;

    public Upload()
    {
        InitializeComponent();

        _db = new DbContext(Global.dbPath);
        _vm = new UploadViewModel();

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
            {
                return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
            }
        };
        _httpClient = new HttpClient(handler);

        EmployeeEntry.SelectionChanged += _vm.OnEmployeeSelectionChanged;
        Search.Clicked += _vm.OnSearch;
        _vm.SearchLog = SearchLog;

        BindingContext = _vm;

        //_timer = new Timer(async (e) =>
        //{
        //    try
        //    {
        //        await UploadLogs();
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Timer error: {ex.Message}");
        //    }
        //}, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        EnablePage(true);

        try
        {
            var setting = await _db.GetSettingFirstAsync();
            hrisApiUrl = setting?.HRISApiURL ?? string.Empty;

            _vm.StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            _vm.EndDate = DateTime.Now;

            logs = await _db.GetDTRLogsQueryAsync(_vm.StartDate, _vm.EndDate);
            _vm._allSuggestions = logs.GroupBy(x => x.Name).Select(y => y.Key).ToList();

            EmployeeEntry.ItemsSource = _vm._allSuggestions;
        }
        catch (Exception ex)
        {
            await DisplayAlert("Error", "Failed to load page data: " + ex.Message, "OK");
        }
    }

    async void SearchLog()
    {
        try
        {
            logs = await _db.GetDTRLogsQueryAsync(_vm.StartDate, _vm.EndDate, _vm.Name);
            Logs.ItemsSource = logs;
        }
        catch (Exception ex)
        {
            DisplayMessage("Error", "Failed to search logs: " + ex.Message);
        }
    }

    public void EnablePage(bool enable)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            StartDateFilter.IsEnabled = enable;
            EndDateFilter.IsEnabled = enable;
            EmployeeEntry.IsEnabled = enable;
            Search.IsEnabled = enable;
            //UploadLogButton.IsEnabled = enable; uncomment this to bring back the upload button
            Logs.IsEnabled = enable;
            isPageEnabled = enable;
        });
    }

    public async Task UploadLogs() 
    {
        var uploadLogs = logs?.Where(x => x.Name == "OFFLINE").ToList();

        if (uploadLogs == null || !uploadLogs.Any())
        {
            //DisplayMessage("Info", "No OFFLINE logs found to upload.");

            //Dispatcher.Dispatch(() =>
            //{
            //    SearchLog();
            //});

            return;
        }

        var toUploadLogs = uploadLogs.Select(log => new LogBiometricDataRequestDto
        {
            BiometricIdNumber = log.BioId,
            LogDateTime = new DateTimeOffset(log.Log).ToString("o"),
            LogType = log.LogType,
        }).ToList();

        try
        {
            var responseLogPost = await _httpClient.PostAsJsonAsync($"{hrisApiUrl}/MobileRepPayroll/InsertOfflineLogs", toUploadLogs);

            if (responseLogPost.IsSuccessStatusCode)
            {
                DisplayMessage("Success", "All offline logs have been uploaded.");
                //await _db.DeleteDTROfflineLogsByBioIdAsync();

                Dispatcher.Dispatch(() =>
                {
                    SearchLog();
                });

            }
            else
            {
                var error = await responseLogPost.Content.ReadAsStringAsync();
                DisplayMessage("Upload Failed", $"The server returned an error: {error}");
            }
        }
        catch (Exception ex)
        {
            DisplayMessage("Error", $"An error occurred during upload: {ex.Message}");
        }
    }

    private async void UploadLogButton_Clicked(object sender, EventArgs e)
    {
        var uploadLogs = logs?.Where(x => x.Name == "OFFLINE").ToList();

        if (uploadLogs == null || !uploadLogs.Any())
        {
            DisplayMessage("Info", "No OFFLINE logs found to upload.");
            return;
        }

        var toUploadLogs = uploadLogs.Select(log => new LogBiometricDataRequestDto
        {
            BiometricIdNumber = log.BioId,
            LogDateTime = new DateTimeOffset(log.Log).ToString("o"),
            LogType = log.LogType,
        }).ToList();

        try
        {
            var responseLogPost = await _httpClient.PostAsJsonAsync($"{hrisApiUrl}/MobileRepPayroll/InsertOfflineLogs", toUploadLogs);

            if (responseLogPost.IsSuccessStatusCode)
            {
                DisplayMessage("Success", "All offline logs have been uploaded.");
                //await _db.DeleteDTROfflineLogsByBioIdAsync();
                SearchLog();
            }
            else
            {
                var error = await responseLogPost.Content.ReadAsStringAsync();
                DisplayMessage("Upload Failed", $"The server returned an error: {error}");
            }
        }
        catch (Exception ex)
        {
            DisplayMessage("Error", $"An error occurred during upload: {ex.Message}");
        }
    }

    void DisplayMessage(string title, string message)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await DisplayAlert(title, message, "OK");
        });
    }

    private async void Unlock_Clicked(object sender, EventArgs e)
    {
        if (isPageEnabled)
        {
            EnablePage(false);
            return;
        }

        var password = await MaskedInputPromptHelper.DisplayMaskedPromptAsync("Authenticate", "Please input admin password");

        if (password == null)
        {
            return;   
        }

        try
        {
            var setting = await _db.GetSettingFirstAsync();
            if (password == setting.CurrentPassword)
            {
                EnablePage(true);
            }
            else
            {
                DisplayMessage("Admin", "Invalid password!");
            }
        }
        catch (Exception ex)
        {
            DisplayMessage("Error", "Could not verify password: " + ex.Message);
        }
    }
}

public class LogBiometricDataRequestDto
{
    public string? BiometricIdNumber { get; set; }
    public string? LogDateTime { get; set; }
    public string? LogType { get; set; }
}

partial class UploadViewModel : ObservableObject
{
    [ObservableProperty]
    private DateTime startDate;

    [ObservableProperty]
    private DateTime endDate;

    [ObservableProperty]
    private string? name;

    public List<string> _allSuggestions = new List<string>();

    public Action? SearchLog;

    internal void OnSearch(object? sender, EventArgs e)
    {
        SearchLog?.Invoke();
    }

    internal void OnEmployeeSelectionChanged(object? sender, Syncfusion.Maui.Inputs.SelectionChangedEventArgs e)
    {
        Name = (sender as SfAutocomplete)?.SelectedValue?.ToString() ?? string.Empty;
    }
}