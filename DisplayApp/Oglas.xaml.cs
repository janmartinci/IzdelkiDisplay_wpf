using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml;
using System.Xml.Linq;
using HtmlAgilityPack;
using MaterialDesignThemes.Wpf;
using SQLitePCL;

namespace DisplayApp
{
    /// <summary>
    /// Interaction logic for Oglas.xaml
    /// </summary>
    /// 
    public partial class Oglas : Page
    {

        private List<IzdelekModel> _allIzdelki = new();
        public List<IzdelekModel> FilteredIzdelki { get; set; } = new();

        public List<IzdelekModel> izbraniIzdelki { get; set; } = new();

        public Oglas()
        {
            InitializeComponent();
        }

        // Ta metoda se sproži, ko se stran naloži. Na začetku pokliče metodo ArtikliLoadAsync, ki naloži seznam izdelkov glede na izbrano znamko.
        private async Task ArtikliLoadAsync(string znamka)
        {
            try
            {
                var izdelki = await Task.Run(() => SkupniPodatki.Izdelek).ConfigureAwait(false);
                if (izdelki == null)
                    return;

                _allIzdelki = izdelki
                    .Select(izdelek => new IzdelekModel
                    {
                        IzdelekID = izdelek.Element("izdelekID")?.Value,
                        IzdelekIme = izdelek.Element("izdelekIme")?.Value,
                        BlagovnaZnamka = izdelek.Element("blagovnaZnamka")?.Value,
                        KratekOpis = izdelek.Element("kratekOpis")?.Value,
                        Cena = izdelek.Element("PPC")?.Value,
                        SlikaVelika = izdelek.Element("slikaVelika")?.Value,
                        DodatnaSlika1 = izdelek.Element("dodatneSlike")?.Element("dodatnaSlika1")?.Value,
                        DodatnaSlika2 = izdelek.Element("dodatneSlike")?.Element("dodatnaSlika2")?.Value
                    })
                    .Where(v => !string.IsNullOrEmpty(v.IzdelekID) && !string.IsNullOrEmpty(v.IzdelekIme) && v.BlagovnaZnamka == znamka)
                    .ToList();

                await Dispatcher.InvokeAsync(() =>
                {
                   FilteredIzdelki = new List<IzdelekModel>(_allIzdelki);
                   ArtikliListBox.ItemsSource = FilteredIzdelki.Select(i => $"{i.IzdelekID}, {i.IzdelekIme}").ToList();
                   ArtikliListBox.Foreground = Brushes.White;

                }).Task.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Napaka pri nalaganju artiklov: {ex.Message}");
                }).Task.ConfigureAwait(false);
            }
        }

        // Ta metoda se sproži, ko uporabnik klikne na gumb, ki je povezan z njo. Preveri trenutno vidnost elementa Nastavitve_DodatneOglas in jo preklopi med Visible in Collapsed.
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (Nastavitve_DodatneOglas.Visibility == Visibility.Collapsed)
            {
                Nastavitve_DodatneOglas.Visibility = Visibility.Visible;
            }
            else { 
                Nastavitve_DodatneOglas.Visibility = Visibility.Collapsed;
            }
        }

        private void qr_koda_TextChanged(object sender, TextChangedEventArgs e)
        {
            var placeholder = qr_koda.Template.FindName("Placeholder", qr_koda) as TextBlock;
            string searchText = qr_koda.Text.Trim().ToLower();

            if (placeholder != null)
            {
                placeholder.Text = null;
            }
            else
            {
                placeholder.Text = "Vnesite URL za QR kodo";
            }
        }

        private void Video_url_TextChanged(object sender, TextChangedEventArgs e)
        {
            var placeholder = Video_url.Template.FindName("Placeholder", Video_url) as TextBlock;
            string searchText = Video_url.Text.Trim().ToLower();

            if (placeholder != null)
            {
                placeholder.Text = null;
            }
            else
            {
                placeholder.Text = "Vnesite URL videoposnetka";
            }
        }

        // Ta metoda se sproži, ko se besedilo v iskalnem polju spremeni. Filtrira seznam izdelkov glede na vneseno besedilo in posodobi prikazani seznam.
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var placeholder = SearchBox.Template.FindName("Placeholder", SearchBox) as TextBlock;
            string searchText = SearchBox.Text.Trim().ToLower();

            if (placeholder != null)
            {
                placeholder.Text = null;
            }

            FilteredIzdelki.Clear();

            var results = _allIzdelki.Where(i =>
                (!string.IsNullOrEmpty(i.IzdelekID) && i.IzdelekID.ToLower().Contains(searchText)) ||
                (!string.IsNullOrEmpty(i.IzdelekIme) && i.IzdelekIme.ToLower().Contains(searchText))
            ).ToList();

            foreach (var item in results)
            {
                FilteredIzdelki.Add(item);
            }
            ArtikliListBox.ItemsSource = FilteredIzdelki.Select(i => $"{i.IzdelekID}, {i.IzdelekIme}").ToList();
        }

        // Ta metoda se sproži, ko uporabnik spremeni izbiro v ComboBoxu znamkaComboBox. Na podlagi izbrane znamke ponovno naloži seznam izdelkov, ki ustrezajo tej znamki.

        private void znamkaComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItem = (ComboBoxItem)znamkaComboBox.SelectedItem;
            string value = selectedItem.Content.ToString();

            _ = ArtikliLoadAsync(value);



        }

        private void ArtikliListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            shifra_artikla.Text = FilteredIzdelki.ElementAtOrDefault(ArtikliListBox.SelectedIndex)?.IzdelekID ?? string.Empty;
            izbrani_artikel.Text = FilteredIzdelki.ElementAtOrDefault(ArtikliListBox.SelectedIndex)?.IzdelekIme ?? string.Empty;
            KratekOpisTextBox.Document.Blocks.Clear();
            KratekOpisTextBox.Document.Blocks.Add(new Paragraph(new Run(FilteredIzdelki.ElementAtOrDefault(ArtikliListBox.SelectedIndex)?.KratekOpis ?? string.Empty)));


        }

        private void DodajIzdelek_Gumb_Click(object sender, RoutedEventArgs e)
        {
            // Preveri, ali je izdelek že v seznamu izbranih izdelkov. Če ni, ga doda; če je, prikaže sporočilo.
            if (!izbraniIzdelki.Any(i => i.IzdelekID == shifra_artikla.Text))
            {
                
                if(qr_koda.Text == "")
                {
                    MessageBox.Show("Vnesite URL za QR kodo");
                    return;
                }

                if (Dodaj_video.IsChecked == true)
                {
                    if (Video_url.Text == "")
                    {
                        MessageBox.Show("Vnesite URL videoposnetka");
                        return;
                    }
                }
                //Preveri če je artikel izbran če ni opozori z messageboxom da izbere artikel.
                if(ArtikliListBox.SelectedItems.Count <= 0)
                {
                    MessageBox.Show("Izberite artikel iz seznama");
                    return;
                }

                var cenaDDVDecimalText = string.Empty;

                var cena = FilteredIzdelki.ElementAtOrDefault(ArtikliListBox.SelectedIndex)?.Cena;
                double cenaDDV = Convert.ToDouble(cena) + (0.22 * Convert.ToDouble(cena));
                cenaDDVDecimalText = cenaDDV.ToString("N2", new CultureInfo("sl-SI"));

                if (Dodaj_video.IsChecked == true)
                {

                    izbraniIzdelki.Add(new IzdelekModel
                    {
                        IzdelekID = shifra_artikla.Text,
                        IzdelekIme = izbrani_artikel.Text,
                        KratekOpis = new TextRange(KratekOpisTextBox.Document.ContentStart, KratekOpisTextBox.Document.ContentEnd).Text.Trim(),
                        QRKoda = qr_koda.Text,
                        Cena = cenaDDVDecimalText,
                        SlikaVelika = FilteredIzdelki.ElementAtOrDefault(ArtikliListBox.SelectedIndex)?.SlikaVelika,
                        Video = Video_url.Text

                    });
                }
                else
                {
                    izbraniIzdelki.Add(new IzdelekModel
                    {
                        IzdelekID = shifra_artikla.Text,
                        IzdelekIme = izbrani_artikel.Text,
                        BlagovnaZnamka = FilteredIzdelki.ElementAtOrDefault(ArtikliListBox.SelectedIndex)?.BlagovnaZnamka,
                        KratekOpis = new TextRange(KratekOpisTextBox.Document.ContentStart, KratekOpisTextBox.Document.ContentEnd).Text.Trim(),
                        QRKoda = qr_koda.Text,
                        Cena = cenaDDVDecimalText,
                        SlikaVelika = FilteredIzdelki.ElementAtOrDefault(ArtikliListBox.SelectedIndex)?.SlikaVelika,
                        DodatnaSlika1 = FilteredIzdelki.ElementAtOrDefault(ArtikliListBox.SelectedIndex)?.DodatnaSlika1,
                        DodatnaSlika2 = FilteredIzdelki.ElementAtOrDefault(ArtikliListBox.SelectedIndex)?.DodatnaSlika2,


                    });
                }

                qr_koda.Text = "";
            }
            else
            {
                MessageBox.Show("Artikel je že izbran");
            }


                IzbraniArtikliListBox.ItemsSource = null;
            IzbraniArtikliListBox.ItemsSource = izbraniIzdelki.Select(i => $"{i.IzdelekID}, {i.IzdelekIme}").ToList();
        }

        private void Shrani_Gumb_Click(object sender, RoutedEventArgs e)
        {
            int i = Properties.Settings.Default.OglasID;
            while (File.Exists($"oglas{i}.json")){
                i++;
            }
            Properties.Settings.Default.OglasID = i + 1;
            Properties.Settings.Default.Save();
            SaveJson($"oglas{i}");
            MessageBox.Show("Podatki so bili shranjeni");
            IzbraniArtikliListBox.ClearValue(ItemsControl.ItemsSourceProperty);
            NavigationService.Navigate(new MainWindowContent(SkupniPodatki.Izdelek));

        }

        private async void SaveJson(string FileNameData)
        {
            try
            {
                var nastavitveJson = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(izbraniIzdelki, nastavitveJson);
                File.WriteAllText($"{FileNameData}.json", jsonString);

            }
            catch (Exception ex)
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    MessageBox.Show($"Napaka pri shranjevanju: {ex.Message}");
                }).Task.ConfigureAwait(false);
            }
        }

        private void znamkaComboBox_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //open combo box on click
            if (!znamkaComboBox.IsDropDownOpen)
            {
                znamkaComboBox.IsDropDownOpen = true;
            }
            else
            {
               znamkaComboBox.IsDropDownOpen = false;
            }
        }
    }
}
