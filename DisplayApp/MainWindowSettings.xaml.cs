using MahApps.Metro.Actions;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace DisplayApp
{
    /// <summary>
    /// Interaction logic for MainWindowSettings.xaml
    /// </summary>
    public partial class MainWindowSettings : Page
    {
        public MainWindowSettings()
        {
            InitializeComponent();
            FolderPathText.Text = Properties.Settings.Default.FolderPath;
        }

        //Za nastavitve pasice

        private void CloseMainWindowSettings(object sender, RoutedEventArgs e)
        {
            NavigationService.GetNavigationService(this)?.GoBack();
        }

        private void PasiceLokacijeSettingsClick(object sender, RoutedEventArgs e)
        {
            var folderDialog = new OpenFolderDialog();
            if (folderDialog.ShowDialog() == true)
            {
                string folderName = folderDialog.FolderName;
                Properties.Settings.Default.FolderPath = folderName;
                Properties.Settings.Default.Save();
                FolderPathText.Text = folderName;
            }
        }

        private void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            var selectedColor = e.NewValue;
        }
    }
}
