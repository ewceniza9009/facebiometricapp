using CommunityToolkit.Maui.Storage;
using CommunityToolkit.Maui.Views;
using fbapp.LocalData;
using fbapp.Pages.Popup;
using fbapp.Services;
using fblib;
using Mopups.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Windows.Input;

namespace fbapp.Pages;

public partial class Setup : ContentPage
{
    private readonly DbContext _db;
    private readonly LocalFaceRecognizeService _faceRecognizeService = ServiceHelper.LocalFaceRecognizeService;
    private readonly IFileSaver _fileSaver;
    private readonly IFilePicker _filePicker;

    public ObservableCollection<FaceRecord> Employees { get; set; }
    private List<FaceRecord> _allEmployees;     
    public ICommand BiometricIdTappedCommand { get; set; }
    public ICommand DeleteCommand { get; set; }

    private bool isPageEnabled = false;

    public Setup(IFileSaver fileSaver, IFilePicker filePicker)
    {
        InitializeComponent();

        _db = new DbContext(Global.dbPath);
        _fileSaver = fileSaver;
        _filePicker = filePicker;

        _allEmployees = new List<FaceRecord>();
        Employees = new ObservableCollection<FaceRecord>();

        BiometricIdTappedCommand = new Command<string>(BiometricIdTapped);
        DeleteCommand = new Command<string>(DeleteEmployee);

        BindingContext = this;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        NoItems.IsVisible = true;
        Loading.IsVisible = false;
        EmployeeListView.IsVisible = false;
        EnablePage(false);

        try
        {
            var setting = await _db.GetSettingFirstAsync();
            if (setting != null)
            {
                hrisApiUrl.Text = setting.HRISApiURL;
                adminPassword.Text = setting.CurrentPassword;
                currentBioId.Text = setting.CurrenBioId;
                camera.Text = setting.Camera;
                cameraRotation.Text = setting.ImageRotate.ToString();
                doublePunchInterval.Text = setting.DoublePunchInterval.ToString();
                BypassCheckBox.IsChecked = setting.BypassRestriction;
            }
        }
        catch (Exception ex)
        {
            DisplayMessage("Error", $"Could not load settings: {ex.Message}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        EmployeeListView.IsVisible = false;
    }

    public void EnablePage(bool enable)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            hrisApiUrl.IsEnabled = enable;
            adminPassword.IsEnabled = enable;
            peripheralPropsInput.IsEnabled = enable;
            CmdSaveSettings.IsEnabled = enable;
            EmployeeListView.IsEnabled = enable;
            CmdImportData.IsEnabled = enable;
            CmdExportData.IsEnabled = enable;
            EmployeeSearchBar.IsEnabled = enable;
            BypassCheckBox.IsEnabled = enable;
            isPageEnabled = enable;

            if (!enable)
            {
                EmployeeSearchBar.Text = string.Empty;     
            }
        });
    }

    private void EmployeeSearchBar_TextChanged(object sender, TextChangedEventArgs e)
    {
        string searchText = e.NewTextValue?.ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(searchText))
        {
            EmployeeListView.ItemsSource = new ObservableCollection<FaceRecord>(_allEmployees);
        }
        else
        {
            var filteredEmployees = _allEmployees.Where(emp => emp.name.ToLowerInvariant().Contains(searchText)).ToList();
            EmployeeListView.ItemsSource = new ObservableCollection<FaceRecord>(filteredEmployees);
        }
    }


    private async void SaveSettings_Clicked(object sender, EventArgs e)
    {
        try
        {
            var first = await _db.GetSettingFirstAsync();
            var setting = new Setting()
            {
                Id = first.Id,
                FBApiURL = "__LOCAL__",
                HRISApiURL = hrisApiUrl.Text,
                CurrenBioId = currentBioId.Text,
                Camera = camera.Text,
                ImageRotate = int.Parse(cameraRotation.Text),
                DoublePunchInterval = int.Parse(doublePunchInterval.Text),
                CurrentPassword = adminPassword.Text,
                BypassRestriction = BypassCheckBox.IsChecked
            };
            await _db.SaveSettingAsync(setting);
            DisplayMessage("Settings", "Settings are saved. Please restart the app for some changes to take effect.");
        }
        catch (Exception ex)
        {
            DisplayMessage("Error", $"Could not save settings: {ex.Message}");
        }
    }

    private async void ExportData_Clicked(object sender, EventArgs e)
    {
        if (!await Confirm("Export Data", "This will create a backup of specific database files and folders. Continue?"))
        {
            return;
        }

        string appDataDir = FileSystem.AppDataDirectory;
        string tempZipPath = Path.Combine(Path.GetTempPath(), "fbapp_embeddings_backup.zip");

        var itemsToInclude = new[]
        {
            "Registered",
            "FaceDatabase.db",
            "FaceRecDatabase.db",
            "license.json"
        };

        try
        {
            if (File.Exists(tempZipPath))
            {
                File.Delete(tempZipPath);
            }

            using (var stream = new FileStream(tempZipPath, FileMode.Create))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                foreach (var item in itemsToInclude)
                {
                    string fullPath = Path.Combine(appDataDir, item);

                    if (File.Exists(fullPath))
                    {
                        archive.CreateEntryFromFile(fullPath, Path.GetFileName(fullPath));
                    }
                    else if (Directory.Exists(fullPath))
                    {
                        var filesInDir = Directory.GetFiles(fullPath, "*.*", SearchOption.AllDirectories);
                        foreach (var filePath in filesInDir)
                        {
                            string entryName = Path.GetRelativePath(appDataDir, filePath);
                            archive.CreateEntryFromFile(filePath, entryName);
                        }
                    }
                }
            }

            using (var finalStream = File.OpenRead(tempZipPath))
            {
                var result = await _fileSaver.SaveAsync($"fbapp_embeddings_backup_{DateTime.Now:yyyyMMdd_HHmmss}.zip", finalStream, default);

                if (result.IsSuccessful)
                {
                    await DisplayAlert("Export Successful", $"Backup has been saved to: {result.FilePath}", "OK");
                }
                else
                {
                    await DisplayAlert("Export Failed", "There was an error saving the backup file.", "OK");
                }
            }
        }
        catch (Exception ex)
        {
            DisplayMessage("Export Error", $"Failed to export data: {ex.Message}");
        }
        finally
        {
            if (File.Exists(tempZipPath))
            {
                File.Delete(tempZipPath);
            }
        }
    }

    private async void ImportData_Clicked(object sender, EventArgs e)
    {
        if (!await Confirm("Import Data", "WARNING: This will overwrite all current data. This action cannot be undone. Are you sure you want to continue?"))
        {
            return;
        }

        try
        {
            var fileResult = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Select a backup file (.zip)"
            });

            if (fileResult == null)
            {
                DisplayMessage("Cancelled", "Import operation was cancelled.");
                return;
            }

            string destinationDirectory = FileSystem.AppDataDirectory;

            var directoryInfo = new DirectoryInfo(destinationDirectory);
            foreach (var file in directoryInfo.GetFiles())
            {
                file.Delete();
            }
            foreach (var dir in directoryInfo.GetDirectories())
            {
                if (dir.Name != ".__override__")
                {
                    dir.Delete(true);
                }
            }

            using (var zipStream = await fileResult.OpenReadAsync())
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
            {
                foreach (var entry in archive.Entries)
                {
                    string fullPath = Path.Combine(destinationDirectory, entry.FullName);

                    string directory = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    if (!string.IsNullOrEmpty(entry.Name))
                    {
                        entry.ExtractToFile(fullPath, true);
                    }
                }
            }

            await DisplayAlert("Import Successful", "Data has been imported successfully. Please RESTART the application for the changes to take effect.", "OK");
        }
        catch (Exception ex)
        {
            DisplayMessage("Import Error", $"Failed to import data: {ex.GetType().Name} - {ex.Message}");
        }
    }

    private async void BiometricIdTapped(string biometricId)
    {
        try
        {
            var imagePath = _faceRecognizeService.GetBioImage(biometricId);
            if (!string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
            {
                // --- START OF FIX ---
                // Read the file into a byte array to ensure you get the latest version
                var imageBytes = File.ReadAllBytes(imagePath);
                var memoryStream = new MemoryStream(imageBytes);

                // Create the ImageSource from the stream to bypass the cache
                var imageSource = ImageSource.FromStream(() => memoryStream);
                // --- END OF FIX ---

                var popup = new ImagePopup(imageSource);
                await MopupService.Instance.PushAsync(popup);
            }
            else
            {
                DisplayMessage("Error", "Image file not found.");
            }
        }
        catch (Exception ex)
        {
            DisplayMessage("Error", $"Failed to load image: {ex.Message}");
        }
    }
    public static bool BypassRestriction { get; private set; }

    private void BypassCheckBox_CheckedChanged(object sender, CheckedChangedEventArgs e)
    {
        BypassRestriction = e.Value; // true if checked
    }

    private async void DeleteEmployee(string biometricId)
    {
        if (!await Confirm("Confirm Deletion", "Are you sure you want to delete this employee?"))
        {
            DisplayMessage("Cancelled", "Employee deletion cancelled.");
            return;
        }

        try
        {
            await _faceRecognizeService.DeleteBio(biometricId);

            var employeeToRemove = _allEmployees.FirstOrDefault(x => x.biometricId == biometricId);
            if (employeeToRemove != null)
            {
                _allEmployees.Remove(employeeToRemove);
                Employees.Remove(employeeToRemove);          
            }

            EmployeeSearchBar_TextChanged(EmployeeSearchBar, new TextChangedEventArgs(EmployeeSearchBar.Text, EmployeeSearchBar.Text));

            DisplayMessage("Success", "Deleted successfully");
        }
        catch (Exception ex)
        {
            DisplayMessage("Error", $"Failed to delete: {ex.Message}");
        }
    }

    async Task<bool> Confirm(string title, string message)
    {
        return await MainThread.InvokeOnMainThreadAsync(() => DisplayAlert(title, message, "Yes", "Cancel"));
    }

    void DisplayMessage(string title, string message)
    {
        MainThread.BeginInvokeOnMainThread(async () => await DisplayAlert(title, message, "OK"));
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
            if (password != setting.CurrentPassword)
            {
                DisplayMessage("Admin", "Invalid password!");
                return;
            }

            EnablePage(true);
            NoItems.IsVisible = false;
            Loading.IsVisible = true;
            EmployeeListView.IsVisible = false;

            var serviceRecords = _faceRecognizeService.GetAllRegistered();
            if (serviceRecords != null && serviceRecords.Any())
            {
                var pageRecords = serviceRecords.Select(r => new FaceRecord
                {
                    id = r.Id,
                    biometricId = r.BiometricId,
                    name = r.Name,
                    imagePath = r.ImagePath
                }).OrderBy(e => e.name).ToList();    

                _allEmployees = new List<FaceRecord>(pageRecords);
                Employees = new ObservableCollection<FaceRecord>(_allEmployees);

                EmployeeListView.ItemsSource = Employees;
                EmployeeListView.IsVisible = true;
            }
            else
            {
                DisplayMessage("Info", "No registered employees found.");
                NoItems.IsVisible = true;
            }
        }
        catch (Exception ex)
        {
            DisplayMessage("Error", $"An error occurred: {ex.Message}");
        }
        finally
        {
            Loading.IsVisible = false;
        }
    }
}

public class FaceRecord
{
    public int id { get; set; }
    public string biometricId { get; set; } = "NA";
    public string name { get; set; } = "NA";
    public string? imagePath { get; set; }
}