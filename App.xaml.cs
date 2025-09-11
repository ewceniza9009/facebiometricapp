using fbapp.Pages;
using fbapp.Pages.Popup;    
using fbapp.Services;
using fblib;
using Microsoft.Extensions.DependencyInjection;
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
            MainPage = _services.GetRequiredService<MainPage>();
        }

        protected override void OnStart()
        {
            // This method can be left empty or just call the base method.
            base.OnStart();
        }
    }
}