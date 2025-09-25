using CommunityToolkit.Maui;
using CommunityToolkit.Maui.Storage;
using fbapp;
using fbapp.Pages;
using fbapp.Pages.Popup;
using fbapp.Services;
using fblib;
using Microsoft.Extensions.Logging;
using Mopups.Hosting;
using Syncfusion.Maui.Core.Hosting;
using System.Diagnostics;
using Plugin.Maui.Audio;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .UseMauiCommunityToolkitCamera()
            .ConfigureMopups()
            .ConfigureSyncfusionCore()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("fa-solid-900.ttf", "FontAwesomeSolid");
            });

        builder.Logging.AddDebug();

        // Register only the simple services that have no complex dependencies.
        // We will create the FaceRecognition service manually in App.xaml.cs.
        builder.Services.AddSingleton<LicenseService>();
        builder.Services.AddSingleton<FaceDataService>(s => new FaceDataService(FileSystem.AppDataDirectory));
        builder.Services.AddSingleton<FaceRecDataService>(s => new FaceRecDataService(FileSystem.AppDataDirectory));

        // Register the file services for dependency injection
        builder.Services.AddSingleton<IFileSaver>(FileSaver.Default);
        builder.Services.AddSingleton<IFilePicker>(FilePicker.Default);
        builder.Services.AddSingleton(AudioManager.Current);


        // Register all your pages and simple services
        //builder.Services.AddSingleton<LocalFaceRecognizeService>();
        builder.Services.AddSingleton<MainPage>();
        builder.Services.AddTransient<LicenseActivationPage>();
        builder.Services.AddTransient<FaceBiometric>();
        builder.Services.AddTransient<Register>();
        builder.Services.AddTransient<Setup>();
        builder.Services.AddTransient<Upload>();

        return builder.Build();
    }

    public static string GetDeviceID()
    {
        string deviceId = string.Empty;
#if ANDROID
        deviceId = Android.Provider.Settings.Secure.GetString(Android.App.Application.Context.ContentResolver, Android.Provider.Settings.Secure.AndroidId);
#elif WINDOWS
        var easyId = Windows.System.Profile.SystemIdentification.GetSystemIdForPublisher();
        deviceId = easyId.Id.ToString();
#elif IOS
        deviceId = UIKit.UIDevice.CurrentDevice.IdentifierForVendor.AsString();
#endif
        if (string.IsNullOrEmpty(deviceId))
        {
            deviceId = Guid.NewGuid().ToString();
            Debug.WriteLine("Warning: Using a non-persistent GUID as the device ID.");
        }
        return deviceId;
    }
}