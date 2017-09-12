using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234238

namespace IndependentProject
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>


    public sealed partial class AdvancedOptionsPage : Page
    {

        private Type previousPage = typeof(MainPage);
        SharedOptions sO;

        public AdvancedOptionsPage()
        {
            this.InitializeComponent();

        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            sO = (SharedOptions)e.Parameter;
            //densityToggle.IsOn = sO.DensityOption;
            heightToggle.IsOn = sO.HeightOption;

            PageStackEntry prevPage = Frame.BackStack.Last();
            previousPage = prevPage?.SourcePageType;



        }


        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(previousPage, sO);
        }

        /* obsolete density code
        private void densityToggle_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggle = (ToggleSwitch)sender;
            sO.DensityOption = toggle.IsOn;
            if (heightToggle.IsOn)
            {
                heightToggle.IsOn = false;
                sO.HeightOption = false;
            }
        }
        */

        private void heightToggle_Toggled(object sender, RoutedEventArgs e)
        {
            ToggleSwitch toggle = (ToggleSwitch)sender;
            sO.HeightOption = toggle.IsOn;
            /* obsolete density code
            if (densityToggle.IsOn)
            {
                densityToggle.IsOn = false;
                sO.DensityOption = false;

            }
            */
        }
    }
}
