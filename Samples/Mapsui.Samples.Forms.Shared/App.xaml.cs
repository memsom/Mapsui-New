﻿
using Xamarin.Forms;

namespace Mapsui.Samples.Forms
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new Mapsui.Samples.Forms.Shared.DebugPage(); //new Mapsui.Samples.Forms.MainPageLarge();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
