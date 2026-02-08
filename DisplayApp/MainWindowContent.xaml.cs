using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Security.AccessControl;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.Windows.Threading;
using System.Xml.Linq;

namespace DisplayApp
{
    public partial class MainWindowContent : Page
    {

        private DispatcherTimer vodicUtripanjeButton;
        private StackPanel BorderVodicAnimacija = new StackPanel();

        private bool fadingIn = false;

        private List<XElement> izdelek;
        private IzdelkiDisplay izdelkiDisplay;
        public MainWindowContent(List<XElement> FromMainWindowXmlload)
        {
            InitializeComponent();
            izdelek = FromMainWindowXmlload;
            SkupniPodatki.Izdelek = FromMainWindowXmlload;
            FileCheckPasice();
            VodicStatus();
            BGNadzornaPloscaColor();
            //Preveri ce pride do spremebe pri Properties settings
            Properties.Settings.Default.PropertyChanged += PropertySettingsChanged;


        }

        private void BGNadzornaPloscaColor()
        {
            BGNadzornaPloscaScrollViewer.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.NadzornaPloscaColor));
            BGNadzornaPloscaNav.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.NadzornaPloscaColor));
        }

        private async void NaloziAsyncFunkcije()
        {
            await LoadFolderPasice();
        }

        private async Task LoadFolderPasice()
        {
            var pot = Properties.Settings.Default.FolderPath;

            if (Directory.Exists(pot))
            {
                string[] slike = await Task.Run(() =>
                    Directory.GetFiles(pot)
                        .Where(file =>
                            file.ToLower().EndsWith(".jpg") ||
                            file.ToLower().EndsWith(".jpeg") ||
                            file.ToLower().EndsWith(".png") ||
                            file.ToLower().EndsWith(".bmp") ||
                            file.ToLower().EndsWith(".gif") ||
                            file.ToLower().EndsWith(".tiff"))
                        .OrderByDescending(file => File.GetCreationTime(file))
                        .ToArray()
                );

                SkupniPodatki.PasiceIzFolder = slike.ToList();

            }
        }

        private void FileCheckPasice()
        {
            if (Directory.Exists(Properties.Settings.Default.FolderPath)){
                PasiceFolderSelector.Visibility = Visibility.Collapsed;
                NaloziAsyncFunkcije();
                AllDisplays.Children.Clear();
                JsonDataLoad();

                //Spodnja koda preveri če ima datoteka slike
                //string[] tipImage = new[] { ".jpg", ".png" };
                //var imageFiles = Directory.EnumerateFiles(path).Where(file => tipImage.Contains(System.IO.Path.GetExtension(file).ToLower())).ToList();

                //if (imageFiles.Any())
                //{
                //    PasiceFolderSelector.Visibility = Visibility.Collapsed;
                //    NaloziAsyncFunkcije();
                //    AllDisplays.Children.Clear();
                //    JsonDataLoad();
                //}
                //else
                //{
                //    MessageBox.Show("Izbrana datoteka za pasice ne vsebuje nobenih slik. Dodaj slike ali pa izberi novo datoteko");

                //    if (PasiceFolderSelector.Visibility != Visibility.Visible)
                //    {
                //        PasiceFolderSelector.Visibility = Visibility.Visible;
                //    }
                //}
            }
            else
            {
                if (!Properties.Settings.Default.prvicRun) {
                    MessageBox.Show($"Datoteka {Properties.Settings.Default.FolderPath} ne obstaja več ali pa je prišlo do spremebe imena. Ponovno izberi datoteko za pasice!");
                }

                if (PasiceFolderSelector.Visibility != Visibility.Visible)
                {
                    PasiceFolderSelector.Visibility = Visibility.Visible;
                }
            }
        }
        private void FolderPopUp(object sender, RoutedEventArgs e)
        {
            var folderDialog = new OpenFolderDialog();
            if (folderDialog.ShowDialog() == true)
            {
                string folderName = folderDialog.FolderName;
                Properties.Settings.Default.FolderPath = folderName;
                Properties.Settings.Default.Save();
                FileCheckPasice();
            }
        }
        private void JsonDataLoad()
        {
            var DatotekaPath = Directory.GetCurrentDirectory();
            var oblikaFile = "*.json";

            var files = new DirectoryInfo(DatotekaPath)
                .GetFiles(oblikaFile)
                .Where(f => Regex.IsMatch(f.Name, @"^oglas\d+\.json$") || Regex.IsMatch(f.Name, @"^display\d+\.json$"))
                .OrderBy(f => int.Parse(Regex.Match(f.Name, @"\d+").Value))
                .ToArray();

            int Dispalypozicija = 1;
            int OglasPozicija = 1;
            foreach (var file in files)
            {
                if (Regex.IsMatch(file.Name, @"^display\d+\.json$"))
                {

                    if (File.Exists($"{file.Name}"))
                    {
                        var jsonString = File.ReadAllText($"{file.Name}");
                        List<ZnamkeClass> deserializedZnamkeList = JsonSerializer.Deserialize<List<ZnamkeClass>>(jsonString);
                        foreach (var znamka in deserializedZnamkeList)
                        {
                            int StSlik = 0;
                            List<string> PasiceFromFile = new List<string>();
                            foreach (var slika in znamka.Slike)
                            {
                                Uri uri = new Uri(slika);
                                if (File.Exists(uri.LocalPath))
                                {
                                    PasiceFromFile.Add(slika);
                                    StSlik++;
                                }

                            }
                            //StackPanel
                            StackPanel stackPanelDisplay = new StackPanel();
                            stackPanelDisplay.Width = 200;
                            stackPanelDisplay.Margin = new Thickness(10);
                            BorderVodicAnimacija = stackPanelDisplay;



                            //Title Display
                            Card cardTile = new Card()
                            {

                                Background = Brushes.White,

                            };

                            Grid GridCardTitle = new Grid { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.HeaderKontrolnoOknoColor)) };

                            TextBlock textBlockTitle = new TextBlock
                            {
                                VerticalAlignment = VerticalAlignment.Center,
                                FontSize = 15,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Padding = new Thickness(5),
                                FontWeight = FontWeights.SemiBold,
                                Text = $"Ekran {Dispalypozicija}",
                                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.TextKontrolnoOknoColor))

                            };

                            GridCardTitle.Children.Add(textBlockTitle);

                            Button buttonRemoveDisplay = new Button
                            {
                                VerticalAlignment = VerticalAlignment.Top,
                                HorizontalAlignment = HorizontalAlignment.Right,
                                Style = (Style)FindResource("CustomRemoveDisplayStyle"),
                                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.HeaderKontrolnoOknoColor))
                            };

                            PackIcon RemoveDispaly = new PackIcon()
                            {
                                Kind = PackIconKind.Times,
                                VerticalAlignment = VerticalAlignment.Top,
                                HorizontalAlignment = HorizontalAlignment.Right,
                                FontWeight = FontWeights.SemiBold,
                                Height = 15,
                                Width = 15,
                                Margin = new Thickness(2),
                                Foreground = Brushes.Red

                            };
                            buttonRemoveDisplay.Content = RemoveDispaly;
                            GridCardTitle.Children.Add(buttonRemoveDisplay);
                            cardTile.Content = GridCardTitle;

                            //Informacije Display
                            Card InfoCard = new Card
                            {
                                Background = Brushes.White,
                                Margin = new Thickness(0, 5, 0, 5),
                            };

                            StackPanel InfoStackPanel = new StackPanel { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.BodyKontrolnoOKnoColor)) };
                            InfoCard.Content = InfoStackPanel;

                            WrapPanel InfoWrapPanel = new WrapPanel();
                            InfoWrapPanel.VerticalAlignment = VerticalAlignment.Center;
                            InfoStackPanel.Children.Add(InfoWrapPanel);

                            TextBlock ZnamkaTextBlock = new TextBlock
                            {
                                FontSize = 15,
                                VerticalAlignment = VerticalAlignment.Center,
                                Padding = new Thickness(5),
                                Text = znamka.VrstaZnamke,
                                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.TextKontrolnoOknoColor))
                            };
                            InfoWrapPanel.Children.Add(ZnamkaTextBlock);


                            WrapPanel InfoWrapPanel2 = new WrapPanel();
                            InfoWrapPanel2.VerticalAlignment = VerticalAlignment.Center;
                            InfoStackPanel.Children.Add(InfoWrapPanel2);

                            PackIcon InfoImg = new PackIcon()
                            {
                                Kind = PackIconKind.Image,
                                VerticalAlignment = VerticalAlignment.Center,
                                Height = 25,
                                Width = 25,
                                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.TextKontrolnoOknoColor))

                            };
                            InfoWrapPanel2.Children.Add(InfoImg);

                            TextBlock StSlikTextBlock = new TextBlock
                            {
                                FontSize = 15,
                                VerticalAlignment = VerticalAlignment.Center,
                                Padding = new Thickness(5),
                                Text = $"{StSlik}",
                                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.TextKontrolnoOknoColor))
                            };
                            InfoWrapPanel2.Children.Add(StSlikTextBlock);

                            //Za button

                            var cardButton = new Card
                            {
                                BorderBrush = null,
                                Background = new SolidColorBrush(Color.FromRgb(11, 218, 194)),
                                HorizontalAlignment = HorizontalAlignment.Center,
                            };


                            var wrapPanel = new WrapPanel();

                            var buttonOpen = new Button
                            {
                                Width = 100,
                                Style = (Style)FindResource("CusttomButtonAdd"),
                                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.ButtonKortrolnoOknoOpenColor)),
                            };

                            var gridOpenContent = new Grid();

                            var borderOpenButton = new Border
                            {
                                BorderThickness = new Thickness(2),
                                Width = 100,
                                Height = 30,
                                CornerRadius = new CornerRadius(5, 5, 5, 5),
                                BorderBrush = Brushes.Yellow,
                                Opacity = 0,

                            };

                            var openPackIcon = new PackIcon
                            {
                                Kind = PackIconKind.OpenInApp,
                                VerticalAlignment = VerticalAlignment.Center,
                                Width = 25,
                                Height = 25,
                                VerticalContentAlignment = VerticalAlignment.Center,
                                HorizontalAlignment = HorizontalAlignment.Center,
                                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.TextKontrolnoOknoColor))
                            };

                            gridOpenContent.Children.Add(borderOpenButton);
                            gridOpenContent.Children.Add(openPackIcon);

                            buttonOpen.Content = gridOpenContent;

                            var buttonNastavitve = new Button
                            {
                                Width = 100,
                                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.ButtonKontrolnoOknoNastavitveColor)),
                                Style = (Style)FindResource("CusttomButtonNastaviteve"),
                            };

                            var gridNastavitve = new Grid();

                            var borderNastavitveButton = new Border
                            {
                                BorderThickness = new Thickness(2),
                                Width = 100,
                                Height = 30,
                                CornerRadius = new CornerRadius(5, 5, 5, 5),
                                BorderBrush = Brushes.Yellow,
                                Opacity = 0,

                            };

                            var nastavitvePackIcon = new PackIcon
                            {
                                Kind = PackIconKind.Settings,
                                VerticalAlignment = VerticalAlignment.Center,
                                Width = 25,
                                Height = 25,
                                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.TextKontrolnoOknoColor)),
                                VerticalContentAlignment = VerticalAlignment.Center,
                                HorizontalAlignment = HorizontalAlignment.Center,

                            };

                            gridNastavitve.Children.Add(borderNastavitveButton);
                            gridNastavitve.Children.Add(nastavitvePackIcon);

                            buttonNastavitve.Content = gridNastavitve;


                            wrapPanel.Children.Add(buttonOpen);
                            wrapPanel.Children.Add(buttonNastavitve);

                            cardButton.Content = wrapPanel;

                            //Dodajanje elementov
                            stackPanelDisplay.Children.Add(cardTile);
                            stackPanelDisplay.Children.Add(InfoCard);
                            stackPanelDisplay.Children.Add(cardButton);
                            AllDisplays.Children.Add(stackPanelDisplay);

                            var znamkeToDisplay = znamka.VrstaZnamke;
                            string DisplayXName = $"Ekran {Dispalypozicija}";
                            buttonOpen.Click += (sender, e) => PromocijskoOknoOpen(sender, e, file.Name, znamkeToDisplay, PasiceFromFile, izdelek, DisplayXName);
                            buttonRemoveDisplay.Click += (sender, e) => DeleteDisplay(sender, e, file.Name);
                            buttonNastavitve.Click += (sender, e) => NastavitvePageOpen(sender, e, file.Name, znamka.VrstaZnamke, PasiceFromFile, izdelek, DisplayXName);
                        }
                        Dispalypozicija++;
                    }
                }
                else
                {
                    var jsonString = File.ReadAllText($"{file.Name}");
                    List<IzdelekModel> deserializedZnamkeList = JsonSerializer.Deserialize<List<IzdelekModel>>(jsonString);
                    int steviloIzdelkov = deserializedZnamkeList.Count();
                    if (File.Exists($"{file.Name}"))
                    {


                        //StackPanel
                        StackPanel stackPanelDisplay = new StackPanel();
                        stackPanelDisplay.Width = 200;
                        stackPanelDisplay.Margin = new Thickness(10);
                        BorderVodicAnimacija = stackPanelDisplay;



                        //Title Display
                        Card cardTile = new Card()
                        {

                            Background = Brushes.White,

                        };

                        Grid GridCardTitle = new Grid { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.HeaderKontrolnoOknoColor)) };

                        TextBlock textBlockTitle = new TextBlock
                        {
                            VerticalAlignment = VerticalAlignment.Center,
                            FontSize = 15,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Padding = new Thickness(5),
                            FontWeight = FontWeights.SemiBold,
                            Text = $"Oglas {OglasPozicija}",
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.TextKontrolnoOknoColor))

                        };

                        GridCardTitle.Children.Add(textBlockTitle);

                        Button buttonRemoveDisplay = new Button
                        {
                            VerticalAlignment = VerticalAlignment.Top,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Style = (Style)FindResource("CustomRemoveDisplayStyle"),
                            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.HeaderKontrolnoOknoColor))
                        };

                        PackIcon RemoveDispaly = new PackIcon()
                        {
                            Kind = PackIconKind.Times,
                            VerticalAlignment = VerticalAlignment.Top,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            FontWeight = FontWeights.SemiBold,
                            Height = 15,
                            Width = 15,
                            Margin = new Thickness(2),
                            Foreground = Brushes.Red

                        };
                        buttonRemoveDisplay.Content = RemoveDispaly;
                        GridCardTitle.Children.Add(buttonRemoveDisplay);
                        cardTile.Content = GridCardTitle;

                        //Informacije Display
                        Card InfoCard = new Card
                        {
                            Background = Brushes.White,
                            Margin = new Thickness(0, 5, 0, 5),
                        };

                        StackPanel InfoStackPanel = new StackPanel { Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.BodyKontrolnoOKnoColor)) };
                        InfoCard.Content = InfoStackPanel;

                        WrapPanel InfoWrapPanel = new WrapPanel();
                        InfoWrapPanel.VerticalAlignment = VerticalAlignment.Center;
                        InfoStackPanel.Children.Add(InfoWrapPanel);

                        TextBlock ZnamkaTextBlock = new TextBlock
                        {
                            FontSize = 15,
                            VerticalAlignment = VerticalAlignment.Center,
                            Padding = new Thickness(5),
                            Text = $"Število artiklov {steviloIzdelkov.ToString()}",
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.TextKontrolnoOknoColor))
                        };
                        InfoWrapPanel.Children.Add(ZnamkaTextBlock);


                        WrapPanel InfoWrapPanel2 = new WrapPanel();
                        InfoWrapPanel2.VerticalAlignment = VerticalAlignment.Center;
                        InfoStackPanel.Children.Add(InfoWrapPanel2);

                        //PackIcon InfoImg = new PackIcon()
                        //{
                        //    Kind = PackIconKind.Tv,
                        //    VerticalAlignment = VerticalAlignment.Center,
                        //    Height = 25,
                        //    Width = 25,
                        //    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.TextKontrolnoOknoColor))s

                        //};
                        //InfoWrapPanel2.Children.Add(InfoImg);

                        //TextBlock StSlikTextBlock = new TextBlock
                        //{
                        //    FontSize = 15,
                        //    VerticalAlignment = VerticalAlignment.Center,
                        //    Padding = new Thickness(5),
                        //    Text = ,
                        //    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.TextKontrolnoOknoColor))
                        //};
                        //InfoWrapPanel2.Children.Add(StSlikTextBlock);

                        //Za button

                        var cardButton = new Card
                        {
                            BorderBrush = null,
                            Background = new SolidColorBrush(Color.FromRgb(11, 218, 194)),
                            HorizontalAlignment = HorizontalAlignment.Center,
                        };


                        var wrapPanel = new WrapPanel();

                        var buttonOpen = new Button
                        {
                            Width = 100,
                            Style = (Style)FindResource("CusttomButtonAdd"),
                            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.ButtonKortrolnoOknoOpenColor)),
                        };

                        var gridOpenContent = new Grid();

                        var borderOpenButton = new Border
                        {
                            BorderThickness = new Thickness(2),
                            Width = 100,
                            Height = 30,
                            CornerRadius = new CornerRadius(5, 5, 5, 5),
                            BorderBrush = Brushes.Yellow,
                            Opacity = 0,

                        };

                        var openPackIcon = new PackIcon
                        {
                            Kind = PackIconKind.OpenInApp,
                            VerticalAlignment = VerticalAlignment.Center,
                            Width = 25,
                            Height = 25,
                            VerticalContentAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.TextKontrolnoOknoColor))
                        };

                        gridOpenContent.Children.Add(borderOpenButton);
                        gridOpenContent.Children.Add(openPackIcon);

                        buttonOpen.Content = gridOpenContent;

                        var buttonNastavitve = new Button
                        {
                            Width = 100,
                            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.ButtonKontrolnoOknoNastavitveColor)),
                            Style = (Style)FindResource("CusttomButtonNastaviteve"),
                        };

                        var gridNastavitve = new Grid();

                        var borderNastavitveButton = new Border
                        {
                            BorderThickness = new Thickness(2),
                            Width = 100,
                            Height = 30,
                            CornerRadius = new CornerRadius(5, 5, 5, 5),
                            BorderBrush = Brushes.Yellow,
                            Opacity = 0,

                        };

                        var nastavitvePackIcon = new PackIcon
                        {
                            Kind = PackIconKind.Settings,
                            VerticalAlignment = VerticalAlignment.Center,
                            Width = 25,
                            Height = 25,
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.TextKontrolnoOknoColor)),
                            VerticalContentAlignment = VerticalAlignment.Center,
                            HorizontalAlignment = HorizontalAlignment.Center,

                        };

                        gridNastavitve.Children.Add(borderNastavitveButton);
                        gridNastavitve.Children.Add(nastavitvePackIcon);

                        buttonNastavitve.Content = gridNastavitve;


                        wrapPanel.Children.Add(buttonOpen);
                        wrapPanel.Children.Add(buttonNastavitve);

                        cardButton.Content = wrapPanel;

                        //Dodajanje elementov
                        stackPanelDisplay.Children.Add(cardTile);
                        stackPanelDisplay.Children.Add(InfoCard);
                        stackPanelDisplay.Children.Add(cardButton);
                        AllDisplays.Children.Add(stackPanelDisplay);

                        buttonRemoveDisplay.Click += (sender, e) => DeleteDisplay(sender, e, file.Name);
                        buttonOpen.Click += (sender, e) => OglasOpen(sender, e, file.Name);
                    }
                    OglasPozicija++;
                }
            }
        }

        private void OglasOpen(object sender, RoutedEventArgs e, string filename)
        {
            new OglasDisplay(filename).Show();
        }

        private void PromocijskoOknoOpen(object sender, RoutedEventArgs e, string fileName, string znamkePassWindow, List<string> pasiceFromFile, List<XElement> XmlLoadData, string DisplayXName)
        {
            if(pasiceFromFile.Count() > 0)
            {
                var path = Properties.Settings.Default.FolderPath;
                if (Directory.Exists(path))
                {
                    if(znamkePassWindow != "Novi izdelki")
                    {
                        izdelkiDisplay = new IzdelkiDisplay(fileName, znamkePassWindow, pasiceFromFile, XmlLoadData, DisplayXName);izdelkiDisplay.Show();
                    }
                    else
                    {
                        izdelkiDisplay = new IzdelkiDisplay(fileName, znamkePassWindow, pasiceFromFile, SkupniPodatki.IzdelekNovo, DisplayXName);
                        izdelkiDisplay.Show();
                    }
                }
                else
                {
                    FileCheckPasice();
                }
            }

        }

        private void NastavitvePageOpen(object sender, RoutedEventArgs e, string fileName, string VrstaZnamke, List<string> Pasice, List<XElement> XmlLoadData, string DisplayXName)
        {

            var path = Properties.Settings.Default.FolderPath;
            if (Directory.Exists(path))
            {
                NastavitvePage nastavitvePage = new NastavitvePage(fileName, VrstaZnamke, Pasice, XmlLoadData, DisplayXName, SkupniPodatki.PasiceIzFolder);
                NavigationService.GetNavigationService(this).Navigate(nastavitvePage);
            }
            else
            {
                FileCheckPasice();
            }
        }

        private void DeleteDisplay(object sender, RoutedEventArgs e, string fileName)
        {
            if (File.Exists(fileName))
            {
                File.Delete(fileName);
                AllDisplays.Children.Clear();
                JsonDataLoad();
            }
        }

        private void Dodaj(object sender, RoutedEventArgs e)
        {

            NavigationService.GetNavigationService(this).Navigate(new ProcessSelectionView());

            //do
            //{
            //    i++;
            //} while (File.Exists($"display{i}.json"));
            //NavigationService.GetNavigationService(this).Navigate( new AddDisplayStep1($"display{i}", izdelek, pasiceFromFolder));

        }

        private void Button_Close(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }

        private void FolderPopUpExit(object sender, RoutedEventArgs e)
        {
            Window.GetWindow(this)?.Close();
        }

        //Vodic stvari

        private void VodicTimer()
        {
            vodicUtripanjeButton = new DispatcherTimer();
            vodicUtripanjeButton.Interval = TimeSpan.FromSeconds(2);
            vodicUtripanjeButton.Tick += TimerVodicTick;
        }

        private void TimerVodicTick(object sender, EventArgs e)
        {
            int sekundeTimer = 0;
            if(Properties.Settings.Default.vodicStep == 2)
            {
                sekundeTimer = 1;
            }
            else
            {
                sekundeTimer = 2;
            }
            
            if (fadingIn)
            {
                DoubleAnimation animacijaIn = new DoubleAnimation()
                {

                    Duration = new Duration(TimeSpan.FromSeconds(sekundeTimer)),
                    From = 0,
                    To = 1

                };
                //Gumb za dodajanje promocijska okna fadein
                if (Properties.Settings.Default.vodicStep == 2)
                {
                    BorderVodicButonAdd.BeginAnimation(UIElement.OpacityProperty, animacijaIn);
                }
                
                //Utripajpče novo nadzorno okno fadein
                if (Properties.Settings.Default.vodicStep == 3)
                {
                    BorderVodicAnimacija.BeginAnimation(UIElement.OpacityProperty, animacijaIn);
                }
                fadingIn = false;
                vodicUtripanjeButton.Interval = TimeSpan.FromSeconds(sekundeTimer);

            }
            else
            {
                DoubleAnimation animacijaOut = new DoubleAnimation()
                {

                    Duration = new Duration(TimeSpan.FromSeconds(sekundeTimer)),
                    From = 1,
                    To = 0

                };

                //Gumb za dodajanje promocijska okna fadeout
                if (Properties.Settings.Default.vodicStep == 2)
                {
                    BorderVodicButonAdd.BeginAnimation(UIElement.OpacityProperty, animacijaOut);
                }
                //Utripajpče novo nadzorno okno fadeout
                if (Properties.Settings.Default.vodicStep == 3)
                {
                    BorderVodicAnimacija.BeginAnimation(UIElement.OpacityProperty, animacijaOut);
                }
                vodicUtripanjeButton.Interval = TimeSpan.FromSeconds(sekundeTimer);
                fadingIn = true;
            }
        }
        private void VodicStatus()
        {

            if (Properties.Settings.Default.prvicRun || Properties.Settings.Default.vodicRestart)
            {
                Vodic.Visibility = Visibility.Visible;
                VodicTimer();
                VodicShow();
            }
            else
            {
                Vodic.Visibility = Visibility.Collapsed;
            }
        }
        private void VodicShow()
        {
            VodicStackPanel.Children.Clear();

            if(Properties.Settings.Default.vodicStep == 2 || Properties.Settings.Default.vodicStep == 3)
            {
                if(!vodicUtripanjeButton.IsEnabled)
                {
                    vodicUtripanjeButton.Start();
                }
            }
            //prvi step
            if (Properties.Settings.Default.vodicStep == 1)
            {
                VodicButtonNext.Visibility = Visibility.Visible;
                Vodic.VerticalAlignment = VerticalAlignment.Center;
                Vodic.HorizontalAlignment = HorizontalAlignment.Center;
                Vodic.Margin = new Thickness(0, 0, 0, 0);

                Grid VodicTextAbout = new Grid
                {
                    Name = "VodicTextAbout"
                };

                StackPanel stackPanel = new StackPanel();

                TextBlock VodicText = new TextBlock
                {
                    Name = "VodicText",
                    Foreground = new SolidColorBrush(Colors.White),
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Text = "Živjo! Sem vodič in pomagal ti bom pri ustvarjanju tvojega prvega promocijskega okna."
                };

                stackPanel.Children.Add(VodicText);

                stackPanel.Children.Add(new TextBlock
                {
                    Margin = new Thickness(0, 10, 0, 0),
                    Foreground = new SolidColorBrush(Colors.Aqua),
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Text = "Namen aplikacije:"
                });

                stackPanel.Children.Add(new TextBlock
                {
                    Margin = new Thickness(5, 5, 0, 0),
                    Foreground = new SolidColorBrush(Colors.White),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Text = "Ta aplikacija je zasnovana za lažje in bolj pregledno upravljanje več vrst promocijskih oken v enem okolju."
                });

                stackPanel.Children.Add(new TextBlock
                {
                    Margin = new Thickness(0, 10, 0, 0),
                    Foreground = new SolidColorBrush(Colors.Aqua),
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Text = "Kaj je promocijsko okno?"
                });

                stackPanel.Children.Add(new TextBlock
                {
                    Margin = new Thickness(5, 5, 0, 0),
                    Foreground = new SolidColorBrush(Colors.White),
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Text = "Promocijsko okno je vrsta prikazovalnika, ki prikazuje pasice in naključne artikle izbrane znamke."
                });

                VodicTextAbout.Children.Add(stackPanel);
                VodicStackPanel.Children.Add(VodicTextAbout);

            }
            //drugi step
            if (Properties.Settings.Default.vodicStep == 2)
            {
                Vodic.VerticalAlignment = VerticalAlignment.Bottom;
                vodic.IsEnabled = false;

                VodicButtonNext.Visibility = Visibility.Collapsed;
                VodicButtonCancel.Visibility = Visibility.Collapsed;
                //VodicTextAbout.Visibility = Visibility.Collapsed;
                //VodicText.Text = "Klikni na gumb za dodajanje";
                //VodicText.Margin = new Thickness(0, 0, 0, 10);

                TextBlock VodicText = new TextBlock
                {
                    Name = "VodicText",
                    Foreground = Brushes.Aqua,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Text = "Klikni na gumb za dodajanje.",
                    Margin = new Thickness(0, 10, 0, 0),
                };

                var wrapPanelAdd = new WrapPanel();

                var textBlockAdd = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Text = "Dodajanje novega promocjiskega okna"
                };

                var packIconAdd = new PackIcon
                {
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("white")),
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 25,
                    Height = 25,
                    Margin = new Thickness(0,0,5,0),
                    Kind = PackIconKind.AddBox
                };
                wrapPanelAdd.Children.Add(packIconAdd);
                wrapPanelAdd.Children.Add(textBlockAdd);

                var wrapPanelVodic = new WrapPanel();

                var textBlockVodic = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Text = "Vklopi vodiča"
                };

                var packIconVodic = new PackIcon
                {
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("aqua")),
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 25,
                    Height = 25,
                    Margin = new Thickness(0, 0, 5, 0),
                    Kind = PackIconKind.QuestionMarkBox,
                    
                };
                wrapPanelVodic.Children.Add(packIconVodic);
                wrapPanelVodic.Children.Add(textBlockVodic);

                var wrapPanelSettings = new WrapPanel();

                var textBlockSettings = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Text = "Nastavitve"
                };

                var packIconSettings = new PackIcon
                {
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("white")),
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 25,
                    Height = 25,
                    Margin = new Thickness(0, 0, 5, 0),
                    Kind = PackIconKind.SettingsApplications
                };
                wrapPanelSettings.Children.Add(packIconSettings);
                wrapPanelSettings.Children.Add(textBlockSettings);

                var wrapPanelExit = new WrapPanel();

                var textBlockExit = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Text = "Izklop aplikacije"
                };

                var packIconExit = new PackIcon
                {
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("white")),
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 25,
                    Height = 25,
                    Margin = new Thickness(0, 0, 5, 0),
                    Kind = PackIconKind.ExitToApp
                };
                wrapPanelExit.Children.Add(packIconExit);
                wrapPanelExit.Children.Add(textBlockExit);

                VodicStackPanel.Children.Add(wrapPanelAdd);
                VodicStackPanel.Children.Add(wrapPanelVodic);
                VodicStackPanel.Children.Add(wrapPanelSettings);
                VodicStackPanel.Children.Add(wrapPanelExit);
                VodicStackPanel.Children.Add(VodicText);
                Vodic.VerticalAlignment = VerticalAlignment.Bottom;

            }
            else
            {
                vodic.IsEnabled = true;
            }
            //Tretji step
            if (Properties.Settings.Default.vodicStep == 3)
            {
                vodicClickIcon.Kind = PackIconKind.Check;
                VodicButtonCancel.Visibility = Visibility.Collapsed;
                TextBlock VodicText = new TextBlock
                {
                    Name = "VodicText",
                    Foreground = new SolidColorBrush(Colors.White),
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Text = "Bravo! Promocijsko okno je bilo uspešno ustvarjeno.",
                    Margin = new Thickness(0, 0, 0, 10)
                };

                TextBlock vodicTextBlock2 = new TextBlock
                {
                    Text = "Ustvarilo se je tudi nadzorno okno. Vsako promocijsko okno ima svoje nadzorno okno.",
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 5)

                };

                TextBlock vodicTextBlock3 = new TextBlock
                {
                    Text = "Preko tega nadzornega okna lahko vidiš, katero znamko bo promocijsko okno prikazovalo, koliko pasic vsebuje, ter vklopiš promocijsko okno ali spreminjaš njegove nastavitve.",
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = Brushes.White,
                    Margin = new Thickness(0, 0, 0, 5)

                };

                TextBlock vodicTextBlock4 = new TextBlock
                {
                    Text = "Namig - Na gumb za odpiranje promocijskega okna lahko klikneš večkrat, če želiš isto promocijsko okno prikazati na različnih ekranih.",
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Yellow,
                    Margin = new Thickness(0, 10, 0, 0)

                };

                //TextBlock vodicTextBlock5 = new TextBlock
                //{
                //    Text = "Odpri promocijsko okno ali nastavitve",
                //    FontSize = 14,
                //    TextWrapping = TextWrapping.Wrap,
                //    FontWeight = FontWeights.Bold,
                //    Foreground = Brushes.Aqua,
                //    Margin = new Thickness(0, 10, 0, 0)

                //};

                var wrapPanelOpen = new WrapPanel();
                wrapPanelOpen.Margin = new Thickness(0, 10, 0, 0);

                var textBlockOpen = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Text = "Odpre promocjisko okno"
                };

                var packIconOpen = new PackIcon
                {
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("white")),
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 25,
                    Height = 25,
                    Margin = new Thickness(0, 0, 5, 0),
                    Kind = PackIconKind.OpenInApp
                };
                wrapPanelOpen.Children.Add(packIconOpen);
                wrapPanelOpen.Children.Add(textBlockOpen);

                
                var wrapPanelSettings = new WrapPanel();

                var textBlockSettings = new TextBlock
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    TextWrapping = TextWrapping.Wrap,
                    Text = "Nastavitve"
                };

                var packIconSettings = new PackIcon
                {
                    Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("white")),
                    VerticalAlignment = VerticalAlignment.Center,
                    Width = 25,
                    Height = 25,
                    Margin = new Thickness(0, 0, 5, 0),
                    Kind = PackIconKind.Settings
                };
                wrapPanelSettings.Children.Add(packIconSettings);
                wrapPanelSettings.Children.Add(textBlockSettings);

                VodicStackPanel.Children.Add(VodicText);
                VodicStackPanel.Children.Add(vodicTextBlock2);
                VodicStackPanel.Children.Add(vodicTextBlock3);
                VodicStackPanel.Children.Add(wrapPanelOpen);
                VodicStackPanel.Children.Add(wrapPanelSettings);
                VodicStackPanel.Children.Add(vodicTextBlock4);

                Vodic.VerticalAlignment = VerticalAlignment.Center;
                Vodic.HorizontalAlignment = HorizontalAlignment.Center;
            }

            //Četrti step
            if (Properties.Settings.Default.vodicStep == 4)
            {
                Vodic.Visibility = Visibility.Collapsed;
                VodicButtonNext.Visibility = Visibility.Collapsed;
                //VodicTextAbout.Visibility = Visibility.Collapsed;
                //VodicText.Text = "Super! Napredujeva proti cilju.";

                //TextBlock VodicText = new TextBlock
                //{
                //    Name = "VodicText",
                //    Foreground = new SolidColorBrush(Colors.White),
                //    FontWeight = FontWeights.Bold,
                //    FontSize = 14,
                //    TextWrapping = TextWrapping.Wrap,
                //    Text = "Super! Napredujeva proti cilju.",
                //    Margin = new Thickness(0, 0, 0, 10)
                //};

                //TextBlock vodicTextBlock2 = new TextBlock
                //{
                //    Text = "Sedaj je potrebno samo še odpreti promocijsko okno.",
                //    FontSize = 14,
                //    TextWrapping = TextWrapping.Wrap,
                //    FontWeight = FontWeights.SemiBold,
                //    Foreground = Brushes.White,

                //};

                //TextBlock vodicTextBlock3 = new TextBlock
                //{
                //    Text = "Namig - Na gumb za odpiranje promocijskega okna lahko klikneš večkrat, če želiš isto promocijsko okno prikazati na različnih ekranih.",
                //    FontSize = 14,
                //    TextWrapping = TextWrapping.Wrap,
                //    FontWeight = FontWeights.Bold,
                //    Foreground = Brushes.Yellow,
                //    Margin = new Thickness(0, 10, 0, 0)

                //};

                //VodicStackPanel.Children.Add(VodicText);
                //VodicStackPanel.Children.Add(vodicTextBlock2);
                //VodicStackPanel.Children.Add(vodicTextBlock3);
                //Vodic.VerticalAlignment = VerticalAlignment.Center;
                //Vodic.HorizontalAlignment = HorizontalAlignment.Left;
                //Vodic.Margin = new Thickness(10, 0, 0, 45);
            }
        }

        private void VodicStep1_Click(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.vodicStep == 1)
            {
                Properties.Settings.Default.vodicStep = 2;
                VodicShow();
                Properties.Settings.Default.Save();
            }
            if(Properties.Settings.Default.vodicStep == 3)
            {
                Vodic.Visibility = Visibility.Collapsed;
                vodicUtripanjeButton.Stop();
                DoubleAnimation animacijaIn = new DoubleAnimation()
                {

                    Duration = new Duration(TimeSpan.FromSeconds(0)),
                    From = 0,
                    To = 1

                };
                BorderVodicAnimacija.BeginAnimation(UIElement.OpacityProperty, animacijaIn);
                Properties.Settings.Default.prvicRun = false;
                Properties.Settings.Default.vodicRestart = false;
                Properties.Settings.Default.Save();
                string markerPot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),"DisplayApp", "status.marker");
                if (!File.Exists(markerPot))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(markerPot));
                    File.WriteAllText(markerPot, "namesceno");
                }
            }
        }

        private void VodicOpen(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.vodicStep = 1;
            Properties.Settings.Default.vodicRestart = true;
            Properties.Settings.Default.vodicNastavitveProOkno = true;
            Properties.Settings.Default.vodicProOkno = true;
            Properties.Settings.Default.Save();
            vodicClickIcon.Kind = PackIconKind.ForwardOutline;
            VodicButtonCancel.Visibility = Visibility.Visible;
            VodicStatus();
        }

        private void VodicCancelClick(object sender, RoutedEventArgs e)
        {
            var msg = MessageBox.Show("Ali ste prepričani, da želite izklopiti vodiča?", "?", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (msg == MessageBoxResult.Yes) {
                Properties.Settings.Default.vodicStep = 5;
                Properties.Settings.Default.prvicRun = false;
                Properties.Settings.Default.vodicNastavitveProOkno = false;
                Properties.Settings.Default.vodicRestart=false;
                Properties.Settings.Default.Save();
                Vodic.Visibility = Visibility.Collapsed;

                string markerPot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "DisplayApp", "status.marker");
                if (!File.Exists(markerPot))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(markerPot));
                    File.WriteAllText(markerPot, "namesceno");
                }
            }
        }

        private void appNastavitveOpen(object sender, RoutedEventArgs e)
        {
            NavigationService.GetNavigationService(this).Navigate(new MainWindowSettings());
        }

        private void PropertySettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "FolderPath")
            {
                FileCheckPasice();
            }
            if (e.PropertyName == "HeaderKontrolnoOknoColor" || e.PropertyName == "BodyKontrolnoOKnoColor" || e.PropertyName == "TextKontrolnoOknoColor" || e.PropertyName == "ButtonKortrolnoOknoOpenColor" || e.PropertyName == "ButtonKontrolnoOknoNastavitveColor" || e.PropertyName == "NadzornaPloscaColor")
            {
                AllDisplays.Children.Clear();
                JsonDataLoad();
                BGNadzornaPloscaColor();
            }
        }

        private void refreshData(object sender, RoutedEventArgs e)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.RefreshData();
        }

        private void VideoOpen(object sender, RoutedEventArgs e)
        {

        }
    }

}
