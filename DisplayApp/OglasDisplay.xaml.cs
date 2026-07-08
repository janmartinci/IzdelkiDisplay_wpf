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

        // Image cache instance
        private readonly ImageCache _imageCache = new ImageCache();

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
            // Guard the timer callback so exceptions don't crash the app
            try
            {
                if (_isAnimating) return;

                _isAnimating = true;

                CurrentIndex++;
                if (CurrentIndex >= ArtikliList.Count)
                    CurrentIndex = 0;

                await ShowArtikelAnimatedAsync(ArtikliList[CurrentIndex]);
            }
            catch (Exception ex)
            {
                // Log and continue; do not let exception propagate to dispatcher
                ErrorLogger.LogException(ex, "Timer_Tick");
            }
            finally
            {
                _isAnimating = false;
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



        private class ImageCache
        {
            private readonly ConcurrentDictionary<string, BitmapImage> _cache = new();
            private static readonly HttpClient _http = new HttpClient();

            public async Task<BitmapImage> GetAsync(string uri)
            {
                if (string.IsNullOrWhiteSpace(uri))
                    return null;

                try
                {
                        if (_cache.TryGetValue(uri, out var cached) && cached != null)
                        return cached;

                    var downloaded = await DownloadAndCacheAsync(uri);
                    if (downloaded != null)
                        _cache[uri] = downloaded;

                    return downloaded;
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException(ex, "ImageCache.GetAsync");
                    return null;
                }
            }

            private async Task<BitmapImage> DownloadAndCacheAsync(string uri)
            {
                try
                {
                    // relative (non-absolute) -> treat as file path
                    if (Uri.TryCreate(uri, UriKind.RelativeOrAbsolute, out var parsed) && !parsed.IsAbsoluteUri)
                    {
                        string localPath = Path.GetFullPath(uri);
                        if (File.Exists(localPath))
                        {
                            var bmp = LoadBitmapFromFile(localPath);
                            if (bmp != null) _cache[uri] = bmp;
                            return bmp;
                        }
                    }

                    // file:// URI
                    if (Uri.TryCreate(uri, UriKind.Absolute, out parsed) && parsed.IsFile)
                    {
                        var localPath = parsed.LocalPath;
                        if (File.Exists(localPath))
                        {
                            var bmp = LoadBitmapFromFile(localPath);
                            if (bmp != null) _cache[uri] = bmp;
                            return bmp;
                        }
                    }

                    // HTTP/HTTPS
                    if (Uri.TryCreate(uri, UriKind.Absolute, out parsed) &&
                        (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps))
                    {
                        using var resp = await _http.GetAsync(parsed);
                        if (!resp.IsSuccessStatusCode)
                        {
                            ErrorLogger.LogException(new WebException($"HTTP {(int)resp.StatusCode} {resp.ReasonPhrase} for {uri}"), "ImageCache.HttpGet");
                            return null;
                        }

                        var bytes = await resp.Content.ReadAsByteArrayAsync();
                        using var ms = new MemoryStream(bytes);
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = ms;
                        bmp.EndInit();
                        bmp.Freeze();

                        _cache[uri] = bmp;
                        return bmp;
                    }

                    // fallback: try as file path
                    if (File.Exists(uri))
                    {
                        var bmp = LoadBitmapFromFile(uri);
                        if (bmp != null) _cache[uri] = bmp;
                        return bmp;
                    }

                    return null;
                }
                catch (HttpRequestException hre)
                {
                    ErrorLogger.LogException(hre, "ImageCache.DownloadAndCacheAsync");
                    return null;
                }
                catch (WebException we)
                {
                    ErrorLogger.LogException(we, "ImageCache.DownloadAndCacheAsync");
                    return null;
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException(ex, "ImageCache.DownloadAndCacheAsync");
                    return null;
                }
            }

            private BitmapImage LoadBitmapFromFile(string path)
            {
                try
                {
                    using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.StreamSource = fs;
                    bmp.EndInit();
                    bmp.Freeze();
                    return bmp;
                }
                catch (Exception ex)
                {
                    ErrorLogger.LogException(ex, "ImageCache.LoadBitmapFromFile");
                    return null;
                }
            }
        }



        public async Task ShowArtikelAsync(IzdelekModel artikel)
        {
            if (artikel == null) return;

            try
            {
                // Background work
                var qrTask = Task.Run(() => GenerateQrCode(artikel.QRKoda));

                var mainImgTask = _imageCache.GetAsync(artikel.SlikaVelika);
                var dodatna1Task = _imageCache.GetAsync(artikel.DodatnaSlika1);
                var dodatna2Task = _imageCache.GetAsync(artikel.DodatnaSlika2);

                await Task.WhenAll(qrTask, mainImgTask, dodatna1Task, dodatna2Task);

                var qr = qrTask.Result;
                QRKoda.Source = qr;
                QRKodaa.Source = qr;
                QRKodaaa.Source = qr;

                // UI updates (same as before)
                ImeArtikla.Text = artikel.IzdelekIme;
                OpisArtikla.Text = artikel.KratekOpis;
                CenaArtikla.Text = $"{artikel.Cena} € z DDV";
                IDShifra.Text = $"Šifra: {artikel.IzdelekID}";
                IDShifraa.Text = $"Šifra: {artikel.IzdelekID}";
                PasicaNovo.Visibility = artikel.PasicaNovo ? Visibility.Visible : Visibility.Collapsed;

                // Apply images safely (use null checks and placeholders)
                var mainBmp = await mainImgTask;
                SlikaGlavna.Source = mainBmp ?? null; // set to null or a placeholder image

                var bmpD1 = await dodatna1Task;
                var bmpD2 = await dodatna2Task;

                bool hasD1 = bmpD1 != null && !string.IsNullOrWhiteSpace(artikel.DodatnaSlika1);
                bool hasD2 = bmpD2 != null && !string.IsNullOrWhiteSpace(artikel.DodatnaSlika2);

                if (hasD1 || hasD2)
                {
                    if (hasD1)
                    {
                        SlikaSekundarnaEna.Source = bmpD1;
                        SlikaSekundarnaEnaa.Source = bmpD1;
                        EnaSlikaGrid.Visibility = Visibility.Collapsed;
                        DveSlikeGrid.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        EnaSlikaGrid.Visibility = Visibility.Visible;
                        DveSlikeGrid.Visibility = Visibility.Collapsed;
                    }

                    if (hasD2)
                    {
                        SlikaSekundarnaDva.Source = bmpD2;
                        SlikaSekundarnaEnaa.Source = bmpD2;
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
            catch (Exception ex)
            {
                // log any unexpected error and continue — do not rethrow
                ErrorLogger.LogException(ex, "ShowArtikelAsync");
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
