using Plugin.Maui.Audio;
using CommunityToolkit.Maui.Views;
using fbapp.LocalData;
using fbapp.Pages.Popup;
using fbapp.Services;
using fblib;
using Microsoft.Maui.Controls;
using Mopups.Services;
using SkiaSharp;
using System.Net.Http.Json;
using System.Xml.Linq;
using System.Diagnostics.CodeAnalysis;

namespace fbapp.Pages;

public partial class FaceBiometric : ContentPage
{
    private readonly DbContext _db;
    private readonly UploadViewModel _vm;
    private HttpClient _hrisApiClient;
    private string messageTitle = string.Empty;
    private string logType = string.Empty;
    private readonly LoadingPopup _loadingPopup;
    private int cameraRotation = 0;
    private bool isOffline = true;
    private readonly LocalFaceRecognizeService _faceRecognizeService = ServiceHelper.LocalFaceRecognizeService;
    private string hrisApiUrl = "NA";
    private List<DTRLog>? logs;
    private HttpClient _httpClient;
    private Timer _timer;
    private readonly IAudioManager _audioManager;
    private const int eight_hours_in_minutes = 60 * 8;

    public FaceBiometric()
    {
        InitializeComponent();

        _db = new DbContext(Global.dbPath);
        _vm = new UploadViewModel();
        _audioManager = AudioManager.Current;
        _loadingPopup = new LoadingPopup();

        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, sslPolicyErrors) =>
            {
                return sslPolicyErrors == System.Net.Security.SslPolicyErrors.None;
            }
        };
        _hrisApiClient = new HttpClient(handler);
        _httpClient = new HttpClient(handler);
        _vm.StartDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        _vm.EndDate = DateTime.Now;    

        _timer = new Timer(async (e) =>
        {
            try
            {
                await UploadLogs();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Timer error: {ex.Message}");
            }
        }, null, TimeSpan.Zero, TimeSpan.FromMinutes(5));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        cameraView.StopCameraPreview();

        try
        {
            logs = await _db.GetDTRLogsQueryAsync(_vm.StartDate, _vm.EndDate);
            _vm._allSuggestions = logs.GroupBy(x => x.Name).Select(y => y.Key).ToList();
            var setting = await _db.GetSettingFirstAsync();
            if (setting != null)
            {
                hrisApiUrl = setting.HRISApiURL;
                cameraRotation = setting.ImageRotate;
                var cameras = await cameraView.GetAvailableCameras(CancellationToken.None);
                var camera = cameras.FirstOrDefault(x => x.Name.ToLower().Contains(setting.Camera.ToLower()));
                if (camera != null)
                {
                    cameraView.SelectedCamera = camera;
                }
            }
        }
        catch (Exception ex)
        {
            DisplayMessage("Error", $"Could not load settings: {ex.Message}");
        }

        await Task.Delay(250);
        await cameraView.StartCameraPreview(CancellationToken.None);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        cameraView.StopCameraPreview();
    }

    void DisplayMessage(string title, string message)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await DisplayAlert(title, message, "OK");
        });
    }

    async Task<bool> Confirm(string title, string message)
    {
        return await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert(title, message, "Confirm", "Cancel"));
    }

    private Task<string> PromptMessageAsync(string title, string message, string placeholder)
    {
        return MainThread.InvokeOnMainThreadAsync(() => Application.Current?.MainPage?.DisplayPromptAsync(title, message, placeholder: placeholder));
    }
    
    private async Task UploadImageAsync(Stream photoStream, string title, string logType)
    {
        try
        {
            await MopupService.Instance.PushAsync(_loadingPopup);

            await using var memoryStream = new MemoryStream();
            await photoStream.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();

            var processedImageBytes = await ProcessImageAsync(imageBytes);

            await using var finalImageStream = new MemoryStream(processedImageBytes);

            string result = await _faceRecognizeService.Recognize(finalImageStream);

            if (result == "Spoof detected")
            {
                DisplayMessage("Warning", "Attempted a spoof attack, User tries to bypass biometric security.");
                return;
            }

            var resultSplit = result.Split(',');
            if (resultSplit.Length > 1)
            {
                var biometricId = resultSplit[0];
                var name = resultSplit[1];
                var logNow = DateTime.Now;

                var dtrLog = await _db.GetDTRLogByBioIdAsync(biometricId, logType);

                var setting = await _db.GetSettingFirstAsync();
                var intervalRestriction = setting.DoublePunchInterval;

                var logTypeString = logType switch
                {
                    "I" => "Time In",
                    "0" => "Break-Out",
                    "1" => "Break-In",
                    "O" => "Time Out",
                    _ => string.Empty
                };

                if (dtrLog is not null)
                {
                    var interval = (logNow - dtrLog.Log).TotalMinutes;

                    if (logType == "I" && interval <= eight_hours_in_minutes) 
                    {
                        DisplayMessage("Info", $"You have already swiped '{logTypeString}' in this shift on {dtrLog.Log.ToLongTimeString()}.");
                        return;
                    }

                    if (interval <= intervalRestriction)
                    {
                        DisplayMessage("Info", $"You have already swiped '{logTypeString}' in this shift on {dtrLog.Log.ToLongTimeString()}.");
                        return;
                    }
                }

                if (logType is not "I")
                {         
                    var bkInLog = await _db.GetDTRLogByBioIdAsync(biometricId, "1");
                    var bkOutLog = await _db.GetDTRLogByBioIdAsync(biometricId, "0");
                    var timeIn = await _db.GetDTRLogByBioIdAsync(biometricId, "I");

                    if (logType == "0") 
                    {
                        if (!setting.BypassRestriction)
                        {
                            if (timeIn is null)
                            {
                                DisplayMessage("Info", "Please swipe 'Time In' first.");
                                return;
                            }
                        }
                    }

                    if (timeIn is null)
                    {
                        //DisplayMessage("Info", "Please swipe 'Time In' first.");
                        //return;
                    }

                  
                    if (logType == "1" && bkOutLog is null && timeIn is not null)
                    {
                        DisplayMessage("Info", "BreakOut Required");
                        return;
                    }

                    if (bkInLog is not null)
                    {
                        var interval = (logNow - bkInLog.Log).TotalMinutes;

                        if (interval <= intervalRestriction)
                        {
                            DisplayMessage("Info", $"Cannot swipe '{logTypeString}' until {bkInLog.Log.AddMinutes(intervalRestriction).ToLongTimeString()}.");
                            return;
                        }
                    }                    
                }
                
                bool confirm = await Confirm($":{title}:", $"Biometric Id: {biometricId}\nName: {name}\nLog Time: {logNow:T}");

                if (!confirm) return;

                try
                {
                    if (!isOffline)
                    {
                        var request = new { BiometricIdNumber = biometricId, LogDateTime = new DateTimeOffset(logNow).ToString("o"), LogType = logType };
                        var responseLogPost = await _hrisApiClient.PostAsJsonAsync($"{hrisApiUrl}/MobileRepPayroll/LogBiometricData", request);

                        if (!responseLogPost.IsSuccessStatusCode)
                        {
                            DisplayMessage("Error", "Cannot connect to the HRIS API. Please check settings.");
                            return;
                        }
                    }
                }
                catch (Exception ex)
                {
                    DisplayMessage("Error", $"An error occurred while logging: {ex.Message}");
                    return;
                }

                if (!isOffline)
                {
                    await _db.SaveDTRLogAsync(new DTRLog { BioId = biometricId, Name = name, LogType = logType, Log = logNow });
                }
                else
                {
                    await _db.SaveDTRLogAsync(new DTRLog { BioId = biometricId, Name = name + " OFFLINE", LogType = logType, Log = logNow });
                    // Play sound after saving
                    var player = _audioManager.CreatePlayer(
                        await FileSystem.OpenAppPackageFileAsync("yeaBuddy.mp3"));
                    player.Play();
                }

                //if (logType is "O")
                //{
                //    await _db.DeleteDTRLogsByBioIdAsync(biometricId);
                //}
            }
            else
            {
                DisplayMessage("Try Again", "Could not recognize face. Please ensure a proper pose.");
            }
        }
        catch (Exception ex)
        {
            DisplayMessage("Error", $"An error occurred: {ex.Message}");
        }
        finally
        {
            if (MopupService.Instance.PopupStack.Any())
            {
                await MopupService.Instance.PopAsync();
            }
        }
    }

    private async Task<byte[]> ProcessImageAsync(byte[] imageBytes)
    {
        using var inputData = SKData.CreateCopy(imageBytes);
        using var originalImage = SKBitmap.Decode(inputData);

        if (originalImage == null)      
        {
            throw new ArgumentNullException(nameof(originalImage), "Failed to decode image.");
        }

        int newWidth = originalImage.Width / 2;
        int newHeight = originalImage.Height / 2;

        var samplingOptions = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);
        using var resizedImage = originalImage.Resize(new SKImageInfo(newWidth, newHeight), samplingOptions);

        if (resizedImage == null)      
        {
            throw new InvalidOperationException("Failed to resize image.");
        }

        return await RotateImageAsync(resizedImage, cameraRotation);
    }

    private Task<byte[]> RotateImageAsync(SKBitmap originalImage, int rotationAngle)
    {
        if (rotationAngle == 0)
        {
            using var outputStream = new SKDynamicMemoryWStream();
            originalImage.Encode(outputStream, SKEncodedImageFormat.Jpeg, 75);
            return Task.FromResult(outputStream.DetachAsData().ToArray());
        }

        using var rotatedImage = new SKBitmap(originalImage.Height, originalImage.Width);
        using (var canvas = new SKCanvas(rotatedImage))
        {
            switch (rotationAngle)
            {
                case 90:
                    canvas.Translate(rotatedImage.Width, 0);
                    break;
                case 180:
                    canvas.Translate(rotatedImage.Width, rotatedImage.Height);
                    break;
                case 270:
                    canvas.Translate(0, rotatedImage.Height);
                    break;
                default:
                    throw new ArgumentException("Unsupported rotation angle. Supported values are 0, 90, 180, or 270.");
            }

            canvas.RotateDegrees(rotationAngle);
            canvas.DrawBitmap(originalImage, 0, 0);
        }

        using var rotatedImageStream = new SKDynamicMemoryWStream();
        rotatedImage.Encode(rotatedImageStream, SKEncodedImageFormat.Jpeg, 75);
        return Task.FromResult(rotatedImageStream.DetachAsData().ToArray());
    }

    private async void IN1_Clicked(object sender, EventArgs e)
    {
        messageTitle = "TIME IN";
        logType = "I";
        await cameraView.CaptureImage(CancellationToken.None);
    }

    private async void BKOUT_Clicked(object sender, EventArgs e)
    {
        messageTitle = "BREAK OUT";
        logType = "0";
        await cameraView.CaptureImage(CancellationToken.None);
    }

    private async void BKIN_Clicked(object sender, EventArgs e)
    {
        messageTitle = "BREAK IN";
        logType = "1";
        await cameraView.CaptureImage(CancellationToken.None);
    }

    private async void OUT_Clicked(object sender, EventArgs e)
    {
        messageTitle = "TIME OUT";
        logType = "O";
        await cameraView.CaptureImage(CancellationToken.None);
    }

    private async Task HandleOfflineLog()
    {
        var bioId = await PromptMessageAsync($"Offline {messageTitle}", "Biometric Id: ", "Input Biometric Id");
        if (string.IsNullOrWhiteSpace(bioId))
        {
            if (bioId != null) DisplayMessage("Info", "Biometric Id is required");
            return;
        }
        await _db.SaveDTRLogAsync(new DTRLog { BioId = bioId, Name = "OFFLINE", LogType = logType, Log = DateTime.Now });
        DisplayMessage("Success", $"Offline {messageTitle} recorded for {bioId}");
    }

    private async void cameraView_MediaCaptured(object sender, CommunityToolkit.Maui.Views.MediaCapturedEventArgs e)
    {
        if (e.Media is null)
        {
            return;
        }
        await UploadImageAsync(e.Media, messageTitle, logType);
    }

    private async void Offline_Clicked(object sender, EventArgs e)
    {
        if (isOffline)
        {
            isOffline = false;
            Offline.BackgroundColor = Color.FromArgb("#1ea883");
            return;
        }

        var password = await MaskedInputPromptHelper.DisplayMaskedPromptAsync("Authenticate", "Please input admin password");
        if (password == null) return;

        try
        {
            var setting = await _db.GetSettingFirstAsync();
            if (password == setting.CurrentPassword)
            {
                isOffline = true;
                Offline.BackgroundColor = Color.FromArgb("#FF0000");
            }
            else
            {
                DisplayMessage("Admin", "Invalid password!");
            }
        }
        catch (Exception ex)
        {
            DisplayMessage("Error", $"An error occurred: {ex.Message}");
        }
    }

    public async Task UploadLogs()
    {
        logs = await _db.GetDTRLogsQueryAsync(DateTime.Now.AddMonths(-1), DateTime.Now);
        var uploadLogs = logs?.Where(x => x.Name.Contains("OFFLINE")).ToList();

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
                //DisplayMessage("Success", "All offline logs have been uploaded.");
                await _db.CleanUpOfflineLogsAsync();
            }
            else
            {
                var error = await responseLogPost.Content.ReadAsStringAsync();
               //DisplayMessage("Upload Failed", $"The server returned an error: {error}");
            }
        }
        catch (Exception ex)
        {
            //DisplayMessage("Error", $"An error occurred during upload: {ex.Message}");
        }
    }
}