using fbapp.Pages;
using fbapp.Pages.Popup;    
using fbapp.Services;
using fblib;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.Licensing;
using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Threading.Tasks;    

namespace fbapp
{
    public partial class App : Application
    {
        private readonly IServiceProvider _services;

        public App(IServiceProvider services)
        {
            InitializeComponent();
            _services = services;

            SyncfusionLicenseProvider.RegisterLicense("MzcxM0AzMjMwMkUzNDJFMzBpYnpCR0U4NjhVTjR2QWFIRkZHa2VHOGI3N1JRYlFKQ3dYbk5iTE9JTmdFPQ==");

            MainPage = _services.GetRequiredService<MainPage>();
        }

        protected override void OnStart()
        {
            // This method can be left empty or just call the base method.
            base.OnStart();
        }
    }
}