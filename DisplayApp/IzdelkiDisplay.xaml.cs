using MaterialDesignThemes.Wpf;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;

namespace DisplayApp
{
    /// <summary>
    /// Interaction logic for IzdelkiDisplay.xaml
    /// </summary>
    public partial class IzdelkiDisplay : Window
    {

        //FullScreen info
        private bool FullScreen = false;

        //Timers
        private DispatcherTimer timerIzdelki;
        private DispatcherTimer timerPasice;
        //Display Artiklov
        private bool fadingIn = false;
        private string znamkaHolder = string.Empty;
        private List<XElement> izdelek;
        private string fileName = string.Empty;

        //Za Pasice
        private int IndexOfPasice = 0;
        private List<string> pasiceFrFile;
        public IzdelkiDisplay(string fileName, string znamka, List<string> pasiceFromFile, List<XElement> XmlLoadData, string DisplayXName)
        {
            InitializeComponent();
            znamkaHolder = znamka;
            pasiceFrFile = pasiceFromFile;
            izdelek = XmlLoadData;
            this.Title = DisplayXName;
            this.fileName = fileName;
            TimerPasice();
            TimerIzdelki();

            if (!Properties.Settings.Default.vodicProOkno)
            {
                Vodic.Visibility = Visibility.Collapsed;
            }

        }

        public void RefreshDisplay()
        {
            pasiceFrFile.Clear();
            timerPasice.Stop();
            var jsonString = File.ReadAllText($"{fileName}");
            List<ZnamkeClass> deserializedZnamkeList = JsonSerializer.Deserialize<List<ZnamkeClass>>(jsonString);
            foreach (var znamka in deserializedZnamkeList)
            {
                foreach (var slika in znamka.Slike)
                {
                    Uri uri = new Uri(slika);
                    if (File.Exists(uri.LocalPath))
                    {
                        pasiceFrFile.Add(slika);
                    }

                }
            }
            TimerPasice();
            LoadData(znamkaHolder);
        }

        //Error Log
        public static class Logger
        {
            private static readonly string logFilePath = "error_log.txt";

            public static void LogError(string message, Exception ex = null)
            {
                try
                {
                    using (StreamWriter writer = new StreamWriter(logFilePath, true))
                    {
                        writer.WriteLine("----- ERROR -----");
                        writer.WriteLine($"Time: {DateTime.Now}");
                        writer.WriteLine($"Message: {message}");
                        if (ex != null)
                        {
                            writer.WriteLine($"Exception: {ex}");
                        }
                        writer.WriteLine("-----------------");
                        writer.WriteLine();
                    }
                }
                catch
                {
                    
                }
            }
        }
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double scaleX = e.NewSize.Width / 1000.0;
            double scaleY = e.NewSize.Height / 600.0;
            double scale = Math.Min(scaleX, scaleY);
            MainScaleTransform.ScaleX = scale;
            MainScaleTransform.ScaleY = scale;
        }


        //Pasice Timer
        private void TimerPasice()
        {
            timerPasice = new DispatcherTimer();
            timerPasice.Interval = TimeSpan.FromSeconds(10);
            timerPasice.Tick += TickPasice;
            timerPasice.Start();

            DisplayPasica();
        }

        private void TickPasice(object sender, EventArgs e) {
            IndexOfPasice++;
            if( IndexOfPasice >= pasiceFrFile.Count)
            {
                IndexOfPasice = 0;
            }
            if(pasiceFrFile.Count == 1) {

                timerPasice.Stop();
            }

            DisplayPasica();
        }

        private void DisplayPasica()
        {
            try {

                BitmapImage bitmapPasica = new BitmapImage();
                bitmapPasica.BeginInit();
                bitmapPasica.UriSource = new Uri(pasiceFrFile[IndexOfPasice]);
                bitmapPasica.EndInit();

                XmalPasica.Source = bitmapPasica;

            }
            catch(Exception ex)
            {
                Logger.LogError($"Error prikazovanje pasice: {ex.Message}");
            }
        }

        //Artikle Timer
        private void TimerIzdelki()
        {
            timerIzdelki = new DispatcherTimer();
            timerIzdelki.Interval = TimeSpan.FromSeconds(15);
            timerIzdelki.Tick += TickIzdelki;
            timerIzdelki.Start();

            LoadData(znamkaHolder);
        }

        private void TickIzdelki(object sender, EventArgs e)
        {
            if (fadingIn)
            {
                DoubleAnimation animacijaIn = new DoubleAnimation()
                {

                    Duration = new Duration(TimeSpan.FromSeconds(2)),
                    From = 0,
                    To = 1

                };
                mainContainer.BeginAnimation(UIElement.OpacityProperty, animacijaIn);
                fadingIn = false;
                timerIzdelki.Interval = TimeSpan.FromSeconds(15);
                LoadData(znamkaHolder);

            }
            else
            {
                DoubleAnimation animacijaOut = new DoubleAnimation()
                {

                    Duration = new Duration(TimeSpan.FromSeconds(2)),
                    From = 1,
                    To = 0

                };
                mainContainer.BeginAnimation(UIElement.OpacityProperty, animacijaOut);
                timerIzdelki.Interval = TimeSpan.FromSeconds(2);
                fadingIn = true;
            }
        }

        private int[] GetRandomIzdelkeLoad(string znamka)
        {
            //Prešteje koliko artiklov je v xml
            int steviloArtiklov = 0;
            foreach (var z in izdelek)
            {
                steviloArtiklov++;
            }
            int[] array = new int[5];

            //izbere 5 random izdelkov in shrani v array ter preveri ce je ta artikel ze v arreju
            Random random = new Random();
            if (znamka == "Novi izdelki")
            {
                for (int i = 0; i < 5; i++)
                {
                    int stRandom = random.Next(steviloArtiklov);
                    do
                    {
                        stRandom = random.Next(steviloArtiklov);
                    } while (array.Contains(stRandom));
                    array[i] = stRandom;
                }
            }
            else
            {
                if (znamka != "Vse znamke")
                {

                    for (int i = 0; i < 5; i++)
                    {
                        int stRandom = random.Next(steviloArtiklov);
                        do
                        {
                            stRandom = random.Next(steviloArtiklov);
                        } while (array.Contains(stRandom) || izdelek[stRandom].Element("blagovnaZnamka")?.Value != $"{znamka}" || izdelek[stRandom].Element("zaloga")?.Value == $"0");
                        array[i] = stRandom;
                    }
                }
                else
                {
                    for (int i = 0; i < 5; i++)
                    {
                        int stRandom = random.Next(steviloArtiklov);
                        do
                        {
                            stRandom = random.Next(steviloArtiklov);
                        } while (array.Contains(stRandom) || izdelek[stRandom].Element("zaloga")?.Value == $"0");
                        array[i] = stRandom;
                    }
                }
            }

            return array;
        }
        
        private void LoadData(string znamka)
        {
            mainContainer.Children.Clear();
            var arrayData = GetRandomIzdelkeLoad(znamka);
            //izbiše iz arreja 5 izdelkov
            // *** Iterval za display
            int GridPos = 0;
            foreach (var z in arrayData)
            {

                var InfoIzdelk = izdelek[z];
                var  id = string.Empty;
                var ime = string.Empty;
                var slika = string.Empty;
                var zaloga = string.Empty;
                var cenaDDVDecimal = string.Empty;
                var cenaDDVDecimalText = string.Empty;
                if (znamkaHolder != "Novi izdelki")
                {

                    id = InfoIzdelk.Element("izdelekID")?.Value;
                    ime = InfoIzdelk.Element("izdelekIme")?.Value;
                    slika = InfoIzdelk.Element("slikaVelika")?.Value;
                    zaloga = InfoIzdelk.Element("zaloga")?.Value;
                    var ppc = InfoIzdelk.Element("PPC")?.Value;

                    double cenaDDV = Convert.ToDouble(ppc) + (0.22 * Convert.ToDouble(ppc));
                    cenaDDVDecimal = cenaDDV.ToString("N2", new CultureInfo("sl-SI"));
                    cenaDDVDecimalText = $"{cenaDDVDecimal} € z DDV";
                }
                else 
                {
                    id = InfoIzdelk.Element("shifra")?.Value;
                    ime = InfoIzdelk.Element("Title")?.Value;
                    slika = InfoIzdelk.Element("picture")?.Value;
                    zaloga = "NOVO";
                    cenaDDVDecimal = InfoIzdelk.Element("price")?.Value;
                    cenaDDVDecimalText = $"{cenaDDVDecimal}";
                }

                //Main stackPanel
                StackPanel stackPanel = new StackPanel
                {
                    Margin = new Thickness(5),
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,

                };
                Grid.SetColumn(stackPanel, GridPos);
                GridPos++;

                //id in količina card in grid
                Grid cardIdKol = new Grid
                {
                    Background = Brushes.White,
                };

                stackPanel.Children.Add(cardIdKol);

                Grid gridIdKol = new Grid
                {
                    Margin = new Thickness(5)
                };
                cardIdKol.Children.Add(gridIdKol);

                //ID
                WrapPanel wrapPanelId = new WrapPanel();
                wrapPanelId.HorizontalAlignment = HorizontalAlignment.Left;
                wrapPanelId.VerticalAlignment = VerticalAlignment.Center;

                PackIcon packIconId = new PackIcon
                {
                    Kind = PackIconKind.Tag,
                    VerticalAlignment = VerticalAlignment.Center
                };

                wrapPanelId.Children.Add(packIconId);

                TextBlock textBlockId = new TextBlock
                {
                    Text = id,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 0, 0, 0)
                };
                wrapPanelId.Children.Add(textBlockId);

                gridIdKol.Children.Add(wrapPanelId);

                //kolicina
                WrapPanel wrapPanelKolicina = new WrapPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Right,
                    VerticalAlignment = VerticalAlignment.Center
                };

                PackIcon packIconKol = new PackIcon
                {
                    Kind = PackIconKind.PackageVariant,
                    VerticalAlignment = VerticalAlignment.Center
                };


                TextBlock textBlockKol = new TextBlock
                {
                    Text = zaloga,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(5, 0, 0, 0)
                };

                if(znamka != "Novi izdelki")
                {
                    wrapPanelKolicina.Children.Add(packIconKol);

                    if (Int32.Parse(zaloga) > 10)
                    {
                        textBlockKol.Text = "+10";
                        textBlockKol.Foreground = Brushes.Green;
                        packIconKol.Foreground = Brushes.Green;
                    }
                    else if ((Int32.Parse(zaloga) is > 10 or not 0))
                    {
                        textBlockKol.Foreground = Brushes.Orange;
                        packIconKol.Foreground = Brushes.Orange;
                    }
                    else
                    {
                        textBlockKol.Foreground = Brushes.Red;
                        packIconKol.Foreground = Brushes.Red;
                    }

                }
                else
                {
                    textBlockKol.Foreground = (SolidColorBrush)new BrushConverter().ConvertFromString("#03b0ef");
                    textBlockKol.FontWeight = FontWeights.SemiBold;
                }

                    wrapPanelKolicina.Children.Add(textBlockKol);

                gridIdKol.Children.Add(wrapPanelKolicina);

                //img
                Card cardImg = new Card
                {
                    Margin = new Thickness(0, 10, 0, 10),
                    BorderThickness = new Thickness(0),
                };

                stackPanel.Children.Add(cardImg);

                Grid gridImg = new Grid();
                cardImg.Content = gridImg;

                BitmapImage bitmapSlika = new BitmapImage();
                bitmapSlika.BeginInit();
                bitmapSlika.UriSource = new Uri(slika);
                bitmapSlika.CacheOption = BitmapCacheOption.OnLoad;
                bitmapSlika.EndInit();

                Image img = new Image
                {
                    Source = bitmapSlika
                };

                gridImg.Children.Add(img);

                //Opsi in Cena
                Grid cardCenaOps = new Grid
                {
                    Background = Brushes.White,
                };

                stackPanel.Children.Add(cardCenaOps);

                StackPanel stackPanelCenaOpis = new StackPanel();
                cardCenaOps.Children.Add(stackPanelCenaOpis);

                TextBlock textBlockOpis = new TextBlock
                {
                    TextWrapping = TextWrapping.Wrap,
                    Padding = new Thickness(5),
                    Text = ime,
                    Height = 49,

                };
                stackPanelCenaOpis.Children.Add(textBlockOpis);

                Grid cenaGrid = new Grid
                {
                    Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF57B568"))
                };
                stackPanelCenaOpis.Children.Add(cenaGrid);

                TextBlock textBlockCena = new TextBlock
                {
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Padding= new Thickness(5),
                    Text = $"{cenaDDVDecimalText}"
                };

                cenaGrid.Children.Add(textBlockCena);



                mainContainer.Children.Add(stackPanel);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if(e.Key == System.Windows.Input.Key.F11)
            {
                if (FullScreen)
                {
                    WindowState = WindowState.Normal;
                    FullScreen = false;
                }
                else{
                    WindowState = WindowState.Maximized;
                    FullScreen = true;
                }
            }

            if(e.Key == System.Windows.Input.Key.Escape)
            {
                this.Close();
                
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            timerPasice.Stop();
            timerPasice.Tick -= TickPasice;
            timerPasice = null;

            timerIzdelki.Stop();
            timerIzdelki.Tick -= TickIzdelki;
            timerIzdelki = null;

            mainContainer.Children.Clear();
            XmalPasica.Source = null;

            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void VodicZakljuci_Click(object sender, RoutedEventArgs e)
        {
            Vodic.Visibility = Visibility.Collapsed;
            Properties.Settings.Default.vodicProOkno = false;
            Properties.Settings.Default.Save();
        }

        private void Window_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (FullScreen)
            {
                WindowState = WindowState.Normal;
                FullScreen = false;
            }
            else
            {
                WindowState = WindowState.Maximized;
                FullScreen = true;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Width = SystemParameters.WorkArea.Width * 0.8;
            this.Height = SystemParameters.WorkArea.Height * 0.8;
        }
    }
}
