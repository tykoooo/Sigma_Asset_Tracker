﻿using Sigma3.Views;
using System;
using System.Collections.Generic;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Sigma3
{
    public partial class App : Application
    {

        public App()
        {
            InitializeComponent();
            
            MainPage = new NavigationPage(new Login());

        }

        protected override void OnStart()
        {
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
