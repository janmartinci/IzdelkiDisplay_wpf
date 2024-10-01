using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text.Json;
using System.Windows.Input;
using System.Xml.Linq;

namespace DisplayApp
{
    /// <summary>
    /// Interaction logic for AddDisplayStep2.xaml
    /// </summary>
    public partial class AddDisplayStep2 : Page
    {
        private string FileNameData = string.Empty;
        private string SelectedZnamka = string.Empty;
        private List<XElement> izdelek;
        public AddDisplayStep2(string data, string FileName, List<XElement> XmlData, List<string> Pasice)
        {
            InitializeComponent();
            PasiceIzFolder(Pasice);
            FileNameData = FileName;
            SelectedZnamka = data;
            izdelek = XmlData;

            if (Properties.Settings.Default.vodicStep >= 3)
            {
                Vodic.Visibility = Visibility.Collapsed;
            }
        }

        private async void PasiceIzFolder(List<string> slike) {
            try
            {
                foreach (string s in slike)
                {
                    try {
                        var listBoxItemSlika = new ListBoxItem
                        {
                            FontSize = 20,
                            Foreground = Brushes.White,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(5),
                            Background = Brushes.White,
                        };

                        var image = await LoadImageAsync(s);

                        Image Slikice = new Image
                        {
                            Source = image,
                            Width = 900,
                        };

                        listBoxItemSlika.Content = Slikice;

                        Slikice.MouseLeftButtonDown += (sender, e) => ImageOpacity(sender, e, Slikice);

                        Slike.Items.Add(listBoxItemSlika);
                    }
                    catch(Exception imgEx)
                    {
                        MessageBox.Show($"Error loading image: {s}\n{imgEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error async problem: {ex.Message}");
            }

        }

        private Task<BitmapImage> LoadImageAsync(string imagePath)
        {
            return Task.Run(() =>
            {
                var bitmapImage = new BitmapImage();
                try
                {
                    bitmapImage.BeginInit();
                    bitmapImage.UriSource = new Uri(imagePath);
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error loading image from path: {imagePath}\n{ex.Message}");
                }
                return bitmapImage;
            });
        }

        private void ImageOpacity(object sender, MouseButtonEventArgs e, Image slikice)
        {
            if (slikice.Opacity == 0.5)
            {
                slikice.Opacity = 1;
            }
            else
            {
                slikice.Opacity = 0.5;
            }
        }

        private void IzbiraSlike_Click(object sender, RoutedEventArgs e)
        {
            List<string> imageUrls = new List<string>();

            foreach (var url in Slike.SelectedItems) {

                ListBoxItem listBoxItem = url as ListBoxItem;
                if (listBoxItem != null)
                {
                    Image imageControl = listBoxItem.Content as Image;
                    
                    if (imageControl.Source is BitmapImage bitmapImage && imageControl != null)
                    {
                        imageUrls.Add(bitmapImage.UriSource.ToString());
                    }
                    else {

                        MessageBox.Show("Image error");
                    }
                }
            }
            if(imageUrls.Count > 0)
            {
                List<SlikeClass> slikeClasses = new List<SlikeClass>();
                foreach (var getImg in imageUrls)
                {
                    slikeClasses.Add(new SlikeClass() { UrlSlike = getImg });
                }
                var znamkeList = new List<ZnamkeClass> {

                    new ZnamkeClass
                    {
                        VrstaZnamke = SelectedZnamka,
                        Slike = slikeClasses

                    }
                };
                var nastavitveJson = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(znamkeList, nastavitveJson);
                File.WriteAllText($"{FileNameData}.json", jsonString);

                if (Properties.Settings.Default.prvicRun)
                {
                    Properties.Settings.Default.vodicStep = 3;
                    Properties.Settings.Default.VodicNarejenWindow = $"{FileNameData}.json";
                    Properties.Settings.Default.Save();
                }

                NavigationService.Navigate(new MainWindowContent(izdelek));
            }

        }

        private void Zapri_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MainWindowContent(izdelek));
        }

        private void VodicStep_Ok(object sender, RoutedEventArgs e)
        {
            Vodic.Visibility = Visibility.Collapsed;
        }
    }
}
