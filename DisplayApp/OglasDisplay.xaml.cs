using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.DirectoryServices;
using System.Drawing;
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
using QRCoder;
using SQLitePCL;
using static System.Net.Mime.MediaTypeNames;
using Path = System.IO.Path;

namespace DisplayApp
{
    /// <summary>
    /// Interaction logic for OglasDisplay.xaml
    /// </summary>
    public partial class OglasDisplay : Window
    {
        private bool FullScreen = false;
        
        int CurrentIndex = 0;
        DispatcherTimer timer = new DispatcherTimer();
        List<IzdelekModel> ArtikliList = new List<IzdelekModel>();
        public OglasDisplay(string fileName)
        {
            InitializeComponent();
            if (File.Exists(fileName))
            {
                var jsonString = File.ReadAllText($"{fileName}");
                List<IzdelekModel> deserializedArtikliList = JsonSerializer.Deserialize<List<IzdelekModel>>(jsonString);
                if (deserializedArtikliList != null)
                {
                    ArtikliList = deserializedArtikliList;
                    if(deserializedArtikliList.Count > 1)
                    {
                        StartRotation();
                        _ = ShowArtikelAsync(ArtikliList[CurrentIndex]);
                    }
                    else
                    {
                        _ = ShowArtikelAsync(ArtikliList[0]);
                    }
                }
            }
        }


        public BitmapImage GenerateQrCode(string link)
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(link, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);

            Bitmap qrBitmap = qrCode.GetGraphic(
                pixelsPerModule: 20,
                darkColor: System.Drawing.Color.Black,
                lightColor: System.Drawing.Color.Transparent,
                drawQuietZones: true
            );

            using (var memory = new MemoryStream())
            {
                qrBitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Png);
                memory.Position = 0;


                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        async void StartRotation()
        {
            InitAnimations();
            await ShowArtikelAsync(ArtikliList[CurrentIndex]);

            timer.Interval = TimeSpan.FromSeconds(30);
            timer.Tick += Timer_Tick;
            timer.Start();

            await ShowArtikelAnimatedAsync(ArtikliList[CurrentIndex]);
        }

        private bool _isAnimating = false;
        private async void Timer_Tick(object sender, EventArgs e)
        {
            if (_isAnimating) return;

            _isAnimating = true;

            CurrentIndex++;
            if (CurrentIndex >= ArtikliList.Count)
                CurrentIndex = 0;

            await ShowArtikelAnimatedAsync(ArtikliList[CurrentIndex]);

            _isAnimating = false;
        }



        private Storyboard FadeOut;
        private Storyboard FadeIn;

        private void InitAnimations()
        {
            FadeOut = new Storyboard();
            FadeIn = new Storyboard();

            var fadeOutAnim = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = TimeSpan.FromSeconds(2)
            };

            var fadeInAnim = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromSeconds(2)
            };

            Storyboard.SetTarget(fadeOutAnim, MainGrid);
            Storyboard.SetTargetProperty(fadeOutAnim, new PropertyPath("Opacity"));

            Storyboard.SetTarget(fadeInAnim, MainGrid);
            Storyboard.SetTargetProperty(fadeInAnim, new PropertyPath("Opacity"));

            FadeOut.Children.Add(fadeOutAnim);
            FadeIn.Children.Add(fadeInAnim);
        }


        private async Task ShowArtikelAnimatedAsync(IzdelekModel artikel)
        {
            var tcs = new TaskCompletionSource<bool>();

            void handler(object s, EventArgs e)
            {
                FadeOut.Completed -= handler;
                tcs.SetResult(true);
            }

            FadeOut.Completed += handler;
            FadeOut.Begin();

            await tcs.Task;              // wait fade out
            await ShowArtikelAsync(artikel);

            FadeIn.Begin();
        }



        public static class ImageCache
        {
            private static readonly ConcurrentDictionary<string, BitmapImage> _cache
                = new ConcurrentDictionary<string, BitmapImage>();

            public static BitmapImage Get(string uri)
            {
                if (string.IsNullOrWhiteSpace(uri))
                    return null;

                return _cache.GetOrAdd(uri, key =>
                {
                    using (var client = new WebClient())
                    {
                        byte[] data = client.DownloadData(key);

                        using (var ms = new MemoryStream(data))
                        {
                            var bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.StreamSource = ms;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad; // fully decode
                            bitmap.EndInit();
                            bitmap.Freeze(); // now it works

                            return bitmap;
                        }
                    }
                });
            }
        }



        async Task ShowArtikelAsync(IzdelekModel artikel)
        {
            // Background work
            var qrTask = Task.Run(() => GenerateQrCode(artikel.QRKoda));
            await qrTask;
            var qr = qrTask.Result;

            QRKoda.Source = qr;
            QRKodaa.Source = qr;
            QRKodaaa.Source = qr;

            var mainImg = Task.Run(() => ImageCache.Get(artikel.SlikaVelika));
            var dodatna1 = Task.Run(() => ImageCache.Get(artikel.DodatnaSlika1));
            var dodatna2 = Task.Run(() => ImageCache.Get(artikel.DodatnaSlika2));

            await Task.WhenAll(qrTask, mainImg, dodatna1, dodatna2);

            // UI updates (same as before)
            ImeArtikla.Text = artikel.IzdelekIme;
            OpisArtikla.Text = artikel.KratekOpis;
            CenaArtikla.Text = $"{artikel.Cena} € z DDV";
            IDShifra.Text = $"Šifra: {artikel.IzdelekID}";
            IDShifraa.Text = $"Šifra: {artikel.IzdelekID}";
            PasicaNovo.Visibility = artikel.PasicaNovo ? Visibility.Visible : Visibility.Collapsed;

            SlikaGlavna.Source = mainImg.Result;

            if (artikel.DodatnaSlika1 != null || artikel.DodatnaSlika2 != null)
            {
                if (artikel.DodatnaSlika1 != null)
                {
                    SlikaSekundarnaEna.Source = dodatna1.Result;
                    SlikaSekundarnaEnaa.Source = dodatna1.Result;
                    EnaSlikaGrid.Visibility = Visibility.Collapsed;
                    DveSlikeGrid.Visibility = Visibility.Visible;
                }
                else
                {
                    EnaSlikaGrid.Visibility = Visibility.Visible;
                    DveSlikeGrid.Visibility = Visibility.Collapsed;
                }

                if (artikel.DodatnaSlika2 != null)
                {
                    SlikaSekundarnaDva.Source = dodatna2.Result;
                    SlikaSekundarnaEnaa.Source = dodatna2.Result;
                    EnaSlikaGrid.Visibility = Visibility.Collapsed;
                    DveSlikeGrid.Visibility = Visibility.Visible;
                }
                else
                {
                    EnaSlikaGrid.Visibility = Visibility.Visible;
                    DveSlikeGrid.Visibility = Visibility.Collapsed;
                }

                SlikaMainGrid.Visibility = Visibility.Collapsed;
            }
            else
            {
                EnaSlikaGrid.Visibility = Visibility.Collapsed;
                DveSlikeGrid.Visibility = Visibility.Collapsed;
                SlikaMainGrid.Visibility = Visibility.Visible;
                ZanimanjeText.Visibility = Visibility.Collapsed;
            }


            string[] ZnamkeKiImajoLogo =
            {
                "Arzopa.png",
                "Baseus.png",
                "Delock.png",
                "Digitus.png",
                "EZVIZ.png",
                "FNIRSI.png",
                "GMKtec.png",
                "HiLook.png",
                "Leviton.png",
                "Logitech.png",
                "Mikrotik.png",
                "SBOX.png",
                "Tenda.png",
                "Triton.png",
                "UBIQUITI.png"
            };

            if (ZnamkeKiImajoLogo.Contains($"{artikel.BlagovnaZnamka}.png"))
            {
                ZnamkeSlike.Source = new BitmapImage(new Uri($"/Logo/{artikel.BlagovnaZnamka}.png", UriKind.Relative));
                ZnamkeSlikee.Source = new BitmapImage(new Uri($"/Logo/{artikel.BlagovnaZnamka}.png", UriKind.Relative));
                CardZnamkeSlikee.Visibility = Visibility.Visible;
                ZnamkeSlikee.Visibility = Visibility.Visible;
                ZnamkeSlike.Visibility = Visibility.Visible;
            }
            else
            {
                CardZnamkeSlikee.Visibility = Visibility.Collapsed;
                ZnamkeSlikee.Visibility = Visibility.Collapsed;
                ZnamkeSlike.Visibility = Visibility.Hidden;
            }
            

        }


        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.F11)
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

            if (e.Key == System.Windows.Input.Key.Escape)
            {
                this.Close();

            }
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

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double scaleX = e.NewSize.Width / 1000.0;
            double scaleY = e.NewSize.Height / 600.0;
            double scale = Math.Min(scaleX, scaleY);
            MainScaleTransform.ScaleX = scale;
            MainScaleTransform.ScaleY = scale;
        }
    }
}
