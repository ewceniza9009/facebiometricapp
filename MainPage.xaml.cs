using fbapp.Pages;
using fbapp.Pages.Popup;
using fbapp.Services;
using fblib;
using Mopups.Services;

namespace fbapp
{
    public partial class MainPage : TabbedPage
    {
        private readonly IServiceProvider _services;
        private bool _isInitialized = false;

        public MainPage(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;

            this.Loaded += MainPage_Loaded;
        }

        private async void MainPage_Loaded(object sender, EventArgs e)
        {
            if (_isInitialized)
            {
                return;
            }
            _isInitialized = true;

            await InitializeApplicationAsync();
        }

        private async Task InitializeApplicationAsync()
        {
            var loadingPopup = new LoadingPopup();
            await MopupService.Instance.PushAsync(loadingPopup);

            bool isSuccess = await Task.Run(async () =>
            {
                try
                {
                    var licenseService = _services.GetRequiredService<LicenseService>();
                    var faceDataService = _services.GetRequiredService<FaceDataService>();
                    var faceRecDataService = _services.GetRequiredService<FaceRecDataService>();
                    string licenseKey = await licenseService.GetLicenseKeyAsync();

                    if (string.IsNullOrEmpty(licenseKey)) return false;

                    var licenseManager = new LicenseManager();
                    var licenseInfo = licenseManager.GetLicenseInfo(licenseKey);
                    var currentDeviceId = MauiProgram.GetDeviceID();

                    var isLicenseValid = !string.IsNullOrEmpty(licenseInfo.DeviceId) && 
                        licenseInfo.DeviceId?.Trim() == currentDeviceId?.Trim() &&
                        !licenseInfo.IsExpired;

                    if (isLicenseValid)
                    {
                        var faceRecognition = new fblib.FaceRecognition(
                            faceDataService, faceRecDataService, true, 0.7f, 0.1f, licenseKey, currentDeviceId ?? string.Empty);

                        await faceRecognition.LoadFacesFromDatabaseAsync();

                        ServiceHelper.LocalFaceRecognizeService = new LocalFaceRecognizeService(
                            faceDataService, faceRecDataService, faceRecognition);

                        return true;
                    }
                    return false;    
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"BACKGROUND INIT FAILED: {ex}");
                    return false;
                }
            });

            if (isSuccess)
            {
                SetupTabs();
            }
            else
            {
                var licenseService = _services.GetRequiredService<LicenseService>();
                var deviceId = MauiProgram.GetDeviceID();
                await Navigation.PushModalAsync(new LicenseActivationPage(deviceId, licenseService));
            }

            await MopupService.Instance.PopAsync();
        }

        private void SetupTabs()
        {
            var faceBiometricPage = new FaceBiometric
            {
                Title = "BIOMETRIC",
                IconImageSource = new FontImageSource { FontFamily = "FontAwesomeSolid", Glyph = "\uf2be" }
            };
            var registerPage = new Register
            {
                Title = "REGISTER",
                IconImageSource = new FontImageSource { FontFamily = "FontAwesomeSolid", Glyph = "\uf2c2" }
            };
            var uploadPage = new Upload
            {
                Title = "LOGS",
                IconImageSource = new FontImageSource { FontFamily = "FontAwesomeSolid", Glyph = "\uf017" }
            };
            var setupPage = _services.GetRequiredService<Setup>();
            {
                setupPage.Title = "SETUP";
                setupPage.IconImageSource = new FontImageSource { FontFamily = "FontAwesomeSolid", Glyph = "\uf013" };
            };

            MainThread.BeginInvokeOnMainThread(() =>
            {
                Children.Add(faceBiometricPage);
                Children.Add(registerPage);
                Children.Add(uploadPage);
                Children.Add(setupPage);
            });
        }
    }
}