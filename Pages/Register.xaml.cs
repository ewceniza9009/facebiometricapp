using fbapp.LocalData;
using fbapp.Pages.Popup;
using fbapp.Services;
using Mopups.Services;
using SkiaSharp;
using System.Net.Http.Headers;

namespace fbapp.Pages;

public partial class Register : ContentPage
{
    private readonly LocalFaceRecognizeService _faceRecognizeService = ServiceHelper.LocalFaceRecognizeService;
    private readonly LoadingPopup _loadingPopup;
    private readonly DbContext _db;
    private int _cameraRotation = 0;
    private bool _isPageEnabled = false;

    public Register()
    {
        InitializeComponent();
        _loadingPopup = new LoadingPopup();
        _db = new DbContext(Global.dbPath);    
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        EnablePage(false);
        cameraView.StopCameraPreview();

        try
        {
            var setting = await _db.GetSettingFirstAsync();
            if (setting != null)
            {
                _cameraRotation = setting.ImageRotate;
                var cameras = await cameraView.GetAvailableCameras(CancellationToken.None);
                var frontCamera = cameras.FirstOrDefault(x => x.Name.ToLower().Contains(setting.Camera.ToLower()));
                if (frontCamera != null)
                {
                    cameraView.SelectedCamera = frontCamera;
                }
            }
        }
        catch (Exception ex)
        {
            DisplayMessage("Error", $"Failed to load settings: {ex.Message}");
        }

        await Task.Delay(250);
        await cameraView.StartCameraPreview(CancellationToken.None);
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        cameraView.StopCameraPreview();
    }

    public void EnablePage(bool enable)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            cameraView.IsEnabled = enable;
            biometricIdEntry.IsEnabled = enable;
            nameEntry.IsEnabled = enable;
            CmdRegister.IsEnabled = enable;
            _isPageEnabled = enable;
        });
    }

    private async void Register_Clicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(biometricIdEntry.Text) || string.IsNullOrWhiteSpace(nameEntry.Text))
        {
            DisplayMessage("Validation Error", "Biometric ID and Name cannot be empty.");
            return;
        }
        await cameraView.CaptureImage(CancellationToken.None);
    }

    void DisplayMessage(string title, string message)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await DisplayAlert(title, message, "OK");
        });
    }

    private async void cameraView_MediaCaptured(object sender, CommunityToolkit.Maui.Views.MediaCapturedEventArgs e)
    {
        try
        {
            if (!int.TryParse(biometricIdEntry.Text, out _))
            {
                DisplayMessage("Information", "Please enter a valid Biometric ID (numbers only).");
                return;
            }

            var biometricId = biometricIdEntry.Text;
            var name = nameEntry.Text;

            await MopupService.Instance.PushAsync(_loadingPopup);

            await using var memoryStream = new MemoryStream();
            await e.Media.CopyToAsync(memoryStream);
            var originalImageBytes = memoryStream.ToArray();

            var rotatedImageBytes = await RotateImageAsync(originalImageBytes, _cameraRotation);

            await using var finalImageStream = new MemoryStream(rotatedImageBytes);

            var result = await _faceRecognizeService.Register(biometricId, name, finalImageStream);

            if (result == "Registration successful")
            {
                DisplayMessage("Success", "Registration successful!");
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    biometricIdEntry.Text = string.Empty;
                    nameEntry.Text = string.Empty;
                });
            }
            else
            {
                DisplayMessage("Error", $"Registration failed: {result}");
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

    private Task<byte[]> RotateImageAsync(byte[] imageBytes, int rotationAngle)
    {
        if (rotationAngle == 0)
        {
            return Task.FromResult(imageBytes);
        }

        using var inputStream = new SKMemoryStream(imageBytes);
        using var originalImage = SKBitmap.Decode(inputStream);

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
                    return Task.FromResult(imageBytes);
            }

            canvas.RotateDegrees(rotationAngle);
            canvas.DrawBitmap(originalImage, 0, 0);
        }

        using var rotatedImageStream = new SKDynamicMemoryWStream();
        rotatedImage.Encode(rotatedImageStream, SKEncodedImageFormat.Jpeg, 75);
        return Task.FromResult(rotatedImageStream.DetachAsData().ToArray());
    }

    private async void Unlock_Clicked(object sender, EventArgs e)
    {
        if (_isPageEnabled)
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
            DisplayMessage("Error", $"An error occurred: {ex.Message}");
        }
    }
}