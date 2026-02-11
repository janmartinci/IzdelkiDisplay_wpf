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
                    StartRotation();
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

            ShowArtikelAnimatedAsync(ArtikliList[CurrentIndex]);
        }


        async void Timer_Tick(object sender, EventArgs e)
        {
            CurrentIndex++;

            if (CurrentIndex >= ArtikliList.Count)
                CurrentIndex = 0;

            if (ArtikliList.Count > 1)
                ShowArtikelAnimatedAsync(ArtikliList[CurrentIndex]);
            else
            {
                await ShowArtikelAsync(ArtikliList[CurrentIndex]);
                timer.Stop();
            }
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

        async void ShowArtikelAnimatedAsync(IzdelekModel artikel)
        {
            FadeOut.Completed += async (s, e) =>
            {
                await ShowArtikelAsync(artikel);
                FadeIn.Begin();
            };

            FadeOut.Begin();
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
            var qr1 = Task.Run(() => GenerateQrCode(artikel.QRKoda));
            var qr2 = Task.Run(() => GenerateQrCode(artikel.QRKoda));
            var qr3 = Task.Run(() => GenerateQrCode(artikel.QRKoda));

            var mainImg = Task.Run(() => ImageCache.Get(artikel.SlikaVelika));
            var dodatna1 = Task.Run(() => ImageCache.Get(artikel.DodatnaSlika1));
            var dodatna2 = Task.Run(() => ImageCache.Get(artikel.DodatnaSlika2));

            await Task.WhenAll(qr1, qr2, qr3, mainImg, dodatna1, dodatna2);

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

            ZnamkeSlike.Source = new BitmapImage(new Uri($"/Logo/{artikel.BlagovnaZnamka}.png", UriKind.Relative));

            QRKoda.Source = qr1.Result;
            QRKodaa.Source = qr2.Result;
            QRKodaaa.Source = qr3.Result;
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
