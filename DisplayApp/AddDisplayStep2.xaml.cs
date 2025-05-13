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
                            FontSize = 15,
                            Foreground = Brushes.White,
                            Margin = new Thickness(5),
                            Background = Brushes.White,
                            Height = 50
                        };

                        StackPanel listPasiceStackPanel = new StackPanel
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            Orientation = Orientation.Horizontal,

                        };

                        var image = await LoadSlikeAsync(s);

                        Image Slikice = new Image
                        {
                            Source = image,
                            Width = 200,
                            Margin = new Thickness(5)
                        };

                        listPasiceStackPanel.Children.Add(Slikice);

                        TextBlock PasiceText = new TextBlock
                        {
                            Text = $"{System.IO.Path.GetFileName(s)}",
                            VerticalAlignment = VerticalAlignment.Center,
                        };

                        listPasiceStackPanel.Children.Add(PasiceText);

                        listBoxItemSlika.Content = listPasiceStackPanel;

                        Slike.Items.Add(listBoxItemSlika);
                    }
                    catch(Exception imgEx)
                    {
                        MessageBox.Show($"Error nalaganje slik: {s}\n{imgEx.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error async problem: {ex.Message}");
            }

        }

        private Task<BitmapImage> LoadSlikeAsync(string imagePath)
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

        private void IzbiraSlike_Click(object sender, RoutedEventArgs e)
        {
            List<string> imageUrls = new List<string>();

            foreach (var url in Slike.SelectedItems)
            {
                if (url is ListBoxItem listBoxItem && listBoxItem.Content is StackPanel stack)
                {
                    if (stack.Children[0] is Image imageControl)
                    {
                        if (imageControl.Source is BitmapImage bitmapImage)
                        {
                            imageUrls.Add(bitmapImage.UriSource.ToString());
                        }
                    }
                    else
                    {
                        MessageBox.Show("Image error");
                    }
                }
            }
            if (imageUrls.Count > 0)
            {
                var znamkeList = new List<ZnamkeClass> {

                    new ZnamkeClass
                    {
                        VrstaZnamke = SelectedZnamka,
                        Slike = imageUrls

                    }
                };
                var nastavitveJson = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(znamkeList, nastavitveJson);
                File.WriteAllText($"{FileNameData}.json", jsonString);

                Properties.Settings.Default.vodicStep = 3;
                Properties.Settings.Default.Save();

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
