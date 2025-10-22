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
        // Private fields
        private string FileName = string.Empty;
        private List<XElement> izdelek;
        private List<XElement> Noviizdelek;
        private List<string> pasice;
        private List<string> allZnamke = new List<string>();
        public AddDisplayStep1(string nameFile, List<XElement> XmlData, List<string> Pasice)
        {
            InitializeComponent();
            pasice = Pasice;
            izdelek = XmlData;
            FileName = nameFile;
            // Znamke
            string[] znamkaListItem = { "Vse znamke" , "Novi izdelki", "Bachmann", "Baseus", "Datech", "Digitus", "HiLook", "KELine", "Leviton", "Logitech", "Mikrotik", "SBOX", "Tenda", "TP-Link", "Triton", "Teltonika", "UBIQUITI", "White Shark" };
            // Dodajanje znamk v ListBox
            foreach (var i  in znamkaListItem)
            {
                allZnamke.Add(i);

                if (i != "Vse znamke" && i != "Novi izdelki")
                {
                    ListBoxItem znamkaItem = new ListBoxItem()
                    {
                        FontSize = 20,
                        Foreground = Brushes.White,
                        Content = i
                    };

                    ZnamkaListBox.Items.Add(znamkaItem);
                }
                else
                {
                    ListBoxItem znamkaItem = new ListBoxItem()
                    {
                        FontSize = 20,
                        Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF31E4BF")),
                        Content = i
                    };

                    ZnamkaListBox.Items.Add(znamkaItem);
                }
            }

            if (Properties.Settings.Default.vodicStep >= 3)
            {
                Vodic.Visibility = Visibility.Collapsed;
            }
        }

        private void Next_Click(object sender, RoutedEventArgs e)
        {
            // Preveri, če je izbrana znamka
            var izbranaZnamka = ZnamkaListBox.SelectedValue as ListBoxItem;
            if (izbranaZnamka != null)
            {
                string razultatZnamke = izbranaZnamka.Content.ToString();
                // Navigiraj na naslednji korak z izbrano znamko
                NavigationService.GetNavigationService(this).Navigate(new AddDisplayStep2(razultatZnamke, FileName, izdelek, pasice));
                Vodic.Visibility = Visibility.Collapsed;

            }
        }

        private void Zapri_Click(object sender, RoutedEventArgs e)
        {
            // Zapri in se vrni na glavno okno
            NavigationService.Navigate(new MainWindowContent(izdelek));
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var placeholder = SearchBox.Template.FindName("Placeholder", SearchBox) as TextBlock;
            string searchText = SearchBox.Text.Trim().ToLower();

            if(placeholder != null) { 
                placeholder.Text = null;
            }

            // Počisti trenutne elemente v ListBoxu
            ZnamkaListBox.Items.Clear();

            // Filtriraj znamke glede na iskalni niz
            var filtered = allZnamke.Where(x => x.ToLower().Contains(searchText)).ToList();

            // Dodaj filtrirane znamke nazaj v ListBox
            foreach (var i in filtered)
            {
                var color = (i == "Vse znamke" || i == "Novi izdelki")? (Brush)new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF31E4BF")): Brushes.White;

                ListBoxItem znamkaItem = new ListBoxItem()
                {
                    FontSize = 20,
                    Foreground = color,
                    Content = i
                };

                ZnamkaListBox.Items.Add(znamkaItem);
            }
        }
    }
}
