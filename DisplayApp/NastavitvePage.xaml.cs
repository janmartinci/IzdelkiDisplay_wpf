using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Xml.Linq;
using XamlAnimatedGif;

namespace DisplayApp
{
    /// <summary>
    /// Interaction logic for NastavitvePage.xaml
    /// </summary>
    public partial class NastavitvePage : Page
    {
        public event EventHandler RefreshPasice;

        public List<XElement> izdelki;

        private List<string> ListTrenutnePasiceHolder;
        private List<string> ListTrenutnePasicePointer;
        private string FileNameData;
        private string SelectedZnamka;
        private List<string> slike;
        public NastavitvePage(string fileName, string VrstaZnamke, List<string> ListTrenutnePasice, List<XElement> XmlLoadData, string DisplayXName, List<string> Pasice)
        {
            InitializeComponent();
            izdelki = XmlLoadData;
            DisplayX.Text = $"{DisplayXName}";

            ListTrenutnePasiceHolder = new List<string>(ListTrenutnePasice);
            ListTrenutnePasicePointer = ListTrenutnePasice;

            FileNameData = fileName;
            SelectedZnamka = VrstaZnamke;
            slike = Pasice;


            TrenutnePasice(ListTrenutnePasice);

            if (!Properties.Settings.Default.vodicNatavitve)
            {
                Vodic.Visibility = Visibility.Collapsed;
                Vodic2.Visibility = Visibility.Collapsed;
                Vodic3.Visibility = Visibility.Collapsed;
                Vodic4.Visibility = Visibility.Collapsed;
            }
        }

        private async void TrenutnePasice(List<string> ListTrenutnePasice)
        {

            try
            {
                SlikePasice.Items.Clear();
                List<string> ImageUrlList = new List<string>();
                foreach (var pas in ListTrenutnePasice)
                {
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

                    var image = await LoadImageAsync($"{pas}");

                    Image Slikice = new Image
                    {
                        Source = image,
                        Width = 200,
                        Margin = new Thickness(5)
                    };

                    listPasiceStackPanel.Children.Add(Slikice);

                    TextBlock PasiceText = new TextBlock
                    {
                        Text = $"{System.IO.Path.GetFileName(new Uri(pas).LocalPath)}",
                        VerticalAlignment = VerticalAlignment.Center,
                    };

                    listPasiceStackPanel.Children.Add(PasiceText);

                    listBoxItemSlika.Content = listPasiceStackPanel;

                    SlikePasice.Items.Add(listBoxItemSlika);
                }
            }
            catch (Exception ex) {

                MessageBox.Show($"Error: {ex.Message}");
            }

        }
        
        private async void PasiceIzFolder(List<string> ListTrenutnePasice)
        {
            try
            {
                var pot = Properties.Settings.Default.FolderPath;

                DodajNovePasice.Items.Clear();
                List<string> VseSlikeFolder = new List<string>();
                List<string> TrenutneSlike = new List<string>();
                foreach(var arraySlike in slike)
                {
                    VseSlikeFolder.Add(System.IO.Path.GetFileName(arraySlike));
                }
                foreach(var zaTrenutneSlike in ListTrenutnePasice)
                {
                    TrenutneSlike.Add(System.IO.Path.GetFileName(zaTrenutneSlike));
                }
                IEnumerable<string> filterdSlike = VseSlikeFolder.Except(TrenutneSlike);

                foreach(string Filterd in filterdSlike)
                {
                    var listBoxItemSlika = new ListBoxItem
                    {
                        FontSize = 15,
                        Foreground = Brushes.White,
                        Margin = new Thickness(5),
                        Background = Brushes.White,
                        Height = 50,
                        
                    };

                    StackPanel listPasiceStackPanel = new StackPanel
                    {
                        VerticalAlignment = VerticalAlignment.Center,
                        Orientation = Orientation.Horizontal,

                    };

                    var image = await LoadImageAsync($"{pot}\\{Filterd}");

                    Image Slikice = new Image
                    {
                        Source = image,
                        Width = 200,
                        Margin = new Thickness(5)
                    };

                    listPasiceStackPanel.Children.Add(Slikice);

                    TextBlock PasiceText = new TextBlock
                    {
                        Text = $"{System.IO.Path.GetFileName(Filterd)}",
                        VerticalAlignment = VerticalAlignment.Center,



                    };

                    listPasiceStackPanel.Children.Add(PasiceText);

                    listBoxItemSlika.Content = listPasiceStackPanel;

                    if (listBoxItemSlika.IsSelected)
                    {
                        Slikice.Opacity = 0.5;
                    }

                    DodajNovePasice.Items.Add(listBoxItemSlika);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
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


        private void DodajPasice(object sender, RoutedEventArgs e)
        {
            DodajNovePasice.Visibility = Visibility.Visible;
            ShraniNovePasiceButton.Visibility = Visibility.Visible;
            NazajNaTrenutne.Visibility = Visibility.Visible;

            SlikePasice.Visibility = Visibility.Collapsed;
            DodajNovePasiceButton.Visibility = Visibility.Collapsed;
            SaveButtonNastavitve.Visibility = Visibility.Collapsed;
            IzbrišiVsePasiceButton.Visibility = Visibility.Collapsed;

            TitleNastavitve.Text = "DODAJANJE PASIC";
            PasiceIzFolder(ListTrenutnePasiceHolder);
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

        private void CloseNastavitve(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new MainWindowContent(izdelki));
        }

        private void Shrani_Nove_Pasice_Click(object sender, RoutedEventArgs e)
        {
            foreach (var url in DodajNovePasice.SelectedItems)
            {
                if (url is ListBoxItem listBoxItem && listBoxItem.Content is StackPanel stack)
                {
                    if (stack.Children[0] is Image imageControl)
                    {
                        if (imageControl.Source is BitmapImage bitmapImage)
                        {
                            ListTrenutnePasiceHolder.Add(bitmapImage.UriSource.ToString());
                            Notifikacija("PASICA JE BILA DODANA", "S");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Image error");
                    }
                }
            }

            DodajNovePasice.Visibility = Visibility.Collapsed;
            ShraniNovePasiceButton.Visibility = Visibility.Collapsed;
            NazajNaTrenutne.Visibility = Visibility.Collapsed;

            SlikePasice.Visibility = Visibility.Visible;
            DodajNovePasiceButton.Visibility = Visibility.Visible;
            SaveButtonNastavitve.Visibility = Visibility.Visible;
            IzbrišiVsePasiceButton.Visibility = Visibility.Visible;

            TitleNastavitve.Text = "TRENUTNE PASICE";

            TrenutnePasice(ListTrenutnePasiceHolder);

        }

        private void IzbrisiIzbrano(object sender, RoutedEventArgs e)
        {
            foreach (var url in SlikePasice.SelectedItems)
            {
                if (url is ListBoxItem listBoxItem && listBoxItem.Content is StackPanel stack)
                {
                    if (stack.Children[0] is Image imageControl)
                    {
                        if (imageControl.Source is BitmapImage bitmapImage)
                        {
                            ListTrenutnePasiceHolder.Remove(bitmapImage.UriSource.ToString());
                            Notifikacija("IZBRISANO", "S");
                        }
                    }
                    else
                    {
                        MessageBox.Show("Image not found");
                    }
                }
            }
            TrenutnePasice(ListTrenutnePasiceHolder);

        }

        private void IzbrisiVse(object sender, RoutedEventArgs e)
        {
            if(ListTrenutnePasiceHolder.Count != 0)
            {
                Notifikacija("VSE PASICE IZBRISANE", "S");
            }
            ListTrenutnePasiceHolder.Clear();
            TrenutnePasice(ListTrenutnePasiceHolder);
        }

        private void Notifikacija(string msg, string type)
        {
            //Types {S = Success, W = Warning}
            NotificationPopText.Text = msg;

            if (type == "S")
            {
                NotificationPopText.Foreground = Brushes.YellowGreen;
                PackIconNotification.Kind = MaterialDesignThemes.Wpf.PackIconKind.Success;
            }
            if (type == "W")
            {
                NotificationPopText.Foreground = Brushes.Red;
                PackIconNotification.Kind = MaterialDesignThemes.Wpf.PackIconKind.Warning;
            }

            NotificationPop.Visibility = Visibility.Visible;

            Task.Delay(1000).ContinueWith(_ =>
            {
                Dispatcher.Invoke(() =>
                {
                    NotificationPop.Visibility = Visibility.Collapsed;
                });
            });
        }

        private void SaveButtonNastavitve_Click(object sender, RoutedEventArgs e)
        {

            if (ListTrenutnePasiceHolder.Count != 0)
            {
                var znamkeList = new List<ZnamkeClass> {

                    new ZnamkeClass
                    {
                        VrstaZnamke = SelectedZnamka,
                        Slike = ListTrenutnePasiceHolder

                    }
                };
                var nastavitveJson = new JsonSerializerOptions { WriteIndented = true };
                string jsonString = JsonSerializer.Serialize(znamkeList, nastavitveJson);
                File.WriteAllText($"{FileNameData}", jsonString);

                ListTrenutnePasicePointer.Clear();
                ListTrenutnePasicePointer.AddRange(ListTrenutnePasiceHolder);
                RefreshPasice?.Invoke(this, EventArgs.Empty);

                NavigationService.Navigate(new MainWindowContent(izdelki));
            }
            else
            {
                Notifikacija("Dodaj vsaj eno pasico", "W");
            }
        }

        private void NazajNaTrenutne_Click(object sender, RoutedEventArgs e)
        {
            DodajNovePasice.Visibility = Visibility.Collapsed;
            ShraniNovePasiceButton.Visibility = Visibility.Collapsed;
            NazajNaTrenutne.Visibility = Visibility.Collapsed;

            SlikePasice.Visibility = Visibility.Visible;
            DodajNovePasiceButton.Visibility = Visibility.Visible;
            SaveButtonNastavitve.Visibility = Visibility.Visible;
            IzbrišiVsePasiceButton.Visibility = Visibility.Visible;

            TitleNastavitve.Text = "TRENUTNE PASICE";


            TrenutnePasice(ListTrenutnePasiceHolder);
        }

        private void VodicButtonNext_Click(object sender, RoutedEventArgs e)
        {
            var gifAnimationPosIzbris = AnimationBehavior.GetAnimator(gifPosIzbris);
            if (gifAnimationPosIzbris != null)
            {
                gifAnimationPosIzbris.Play();
            }
            Vodic.Visibility = Visibility.Collapsed;
            Vodic2.Visibility = Visibility.Visible;

        }

        private void Vodic2_Birsanje_Click(object sender, RoutedEventArgs e)
        {
            Vodic2.Visibility = Visibility.Collapsed;
            Vodic3.Visibility = Visibility.Visible;
            var gifAnimationDodajanje = AnimationBehavior.GetAnimator(gifDodajanje);
            if(gifAnimationDodajanje != null )
            {
                gifAnimationDodajanje.Play();
            }

            var gifAnimationPosIzbris = AnimationBehavior.GetAnimator(gifPosIzbris);
            var gifAnimationVseIzbris = AnimationBehavior.GetAnimator(gifVseIzbris);
            if (gifAnimationPosIzbris != null && gifAnimationVseIzbris != null)
            {
                gifAnimationPosIzbris.Pause();
                gifAnimationVseIzbris.Pause();
            }
        }


        private void ScrollViewerVodic2_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var gifAnimationVseIzbris = AnimationBehavior.GetAnimator(gifVseIzbris);
            double verticalPosition = ScrollViewerVodic2.VerticalOffset;
            if (gifAnimationVseIzbris != null)
            {
                if (verticalPosition > 245)
                {
                    gifAnimationVseIzbris.Play();
                }
            }
        }

        private void Vodic3_Dodajanje_Click(object sender, RoutedEventArgs e)
        {
            Vodic3.Visibility= Visibility.Collapsed;
            Vodic4.Visibility= Visibility.Visible;
            var gifAnimationDodajanje = AnimationBehavior.GetAnimator(gifDodajanje);
            if (gifAnimationDodajanje != null)
            {
                gifAnimationDodajanje.Pause();
            }
        }

        private void Vodic4_Click(object sender, RoutedEventArgs e)
        {
            Vodic4.Visibility = Visibility.Collapsed;
            Properties.Settings.Default.vodicNatavitve = false;
            Properties.Settings.Default.Save();
        }
    }
}
