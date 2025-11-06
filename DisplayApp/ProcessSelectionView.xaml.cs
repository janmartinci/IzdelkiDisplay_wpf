using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using System.Xml.Linq;


namespace DisplayApp
{
    /// <summary>
    /// Interaction logic for ProcessSelectionView.xaml
    /// </summary>
    public partial class ProcessSelectionView : Page
    {
        public ProcessSelectionView()
        {
            InitializeComponent();
        }

        private void Button_Click_Steps(object sender, RoutedEventArgs e)
        {
            int i = Properties.Settings.Default.DisplayID;

            while (File.Exists($"display{i}.json"))
            {
                i++;
            }

            Properties.Settings.Default.DisplayID = i + 1;
            Properties.Settings.Default.Save();
            NavigationService.GetNavigationService(this).Navigate(new AddDisplayStep1($"display{i}", SkupniPodatki.Izdelek, SkupniPodatki.PasiceIzFolder));
        }

        private void Button_Click_Custom(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funkcionalnost še ni na voljo.");
        }
    }
}
