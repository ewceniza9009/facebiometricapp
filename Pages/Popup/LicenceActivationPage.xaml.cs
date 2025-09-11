using fbapp.Services;

namespace fbapp.Pages.Popup
{
    public partial class LicenseActivationPage : ContentPage
    {
        private readonly LicenseService _licenseService;

        public LicenseActivationPage(string deviceId, LicenseService licenseService)
        {
            InitializeComponent();
            _licenseService = licenseService;
            DeviceIdEntry.Text = deviceId;
        }

        private async void CopyButton_Clicked(object sender, EventArgs e)
        {
            await Clipboard.SetTextAsync(DeviceIdEntry.Text);
            await DisplayAlert("Copied", "Device ID has been copied to the clipboard.", "OK");
        }

        private async void ActivateButton_Clicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(LicenseKeyEditor.Text))
            {
                await DisplayAlert("Error", "Please enter a license key.", "OK");
                return;
            }

            await _licenseService.SetLicenseKeyAsync(LicenseKeyEditor.Text);

            await DisplayAlert("Activation Complete", "The new license key has been saved. The application will now restart to apply the changes.", "OK");

            Application.Current.MainPage = new AppShell();
        }
    }
}