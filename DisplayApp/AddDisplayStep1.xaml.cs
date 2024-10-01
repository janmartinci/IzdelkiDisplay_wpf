using MahApps.Metro.Actions;
using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.IO;
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
using System.Xml.Linq;

namespace DisplayApp
{
    /// <summary>
    /// Interaction logic for AddDisplayStep1.xaml
    /// </summary>
    public partial class AddDisplayStep1 : Page
    {
        private string FileName = string.Empty;
        private List<XElement> izdelek;
        private List<string> pasice;
        public AddDisplayStep1(string nameFile, List<XElement> XmlData, List<string> Pasice)
        {
            InitializeComponent();
            pasice = Pasice;
            izdelek = XmlData;
            FileName = nameFile;
            string[] znamkaListItem = { "Vsi", "Bachmann", "Baseus", "Datech", "Digitus", "HiLook", "KELine", "Leviton", "Mikrotik", "SBOX", "Tenda", "TP-Link", "Triton", "UBIQUITI", "White Shark" };
            foreach(var i  in znamkaListItem)
            {
                ListBoxItem znamkaItem = new ListBoxItem()
                {
                    FontSize = 20,
                    Foreground = Brushes.White,
                    Content = i
                };

                ZnamkaListBox.Items.Add(znamkaItem);
            }

            if (Properties.Settings.Default.vodicStep >= 3)
            {
                Vodic.Visibility = Visibility.Collapsed;
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            var izbranaZnamka = ZnamkaListBox.SelectedValue as ListBoxItem;
            if (izbranaZnamka != null)
            {
                string razultatZnamke = izbranaZnamka.Content.ToString();
                NavigationService.GetNavigationService(this).Navigate(new AddDisplayStep2(razultatZnamke, FileName, izdelek, pasice));
                Vodic.Visibility = Visibility.Collapsed;

            }
        }

        private void Zapri_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MainWindowContent(izdelek));
        }
    }
}
