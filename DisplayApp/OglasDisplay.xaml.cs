using System;
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

        void StartRotation()
        {
            timer.Interval = TimeSpan.FromSeconds(30);
            timer.Tick += Timer_Tick;
            timer.Start();

            ShowArtikel(ArtikliList[CurrentIndex]);
        }

         void Timer_Tick(object sender, EventArgs e)
        {
            CurrentIndex++;

            if(CurrentIndex >= ArtikliList.Count)
            {
                CurrentIndex = 0;
            }

            ShowArtikel(ArtikliList[CurrentIndex]);
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

                return bitmapImage;
            }
        }

        void ShowArtikel(IzdelekModel artikel)
        {
            ImeArtikla.Text = artikel.IzdelekIme;
            OpisArtikla.Text = artikel.KratekOpis;
            CenaArtikla.Text = $"{artikel.Cena} € z DDV";
            IDShifra.Text = $"Šifra: {artikel.IzdelekID}";
            SlikaGlavna.Source = new BitmapImage(new Uri(artikel.SlikaVelika));
            SlikaSekundarnaEna.Source = new BitmapImage(new Uri(artikel.DodatnaSlika1));
            if(artikel.DodatnaSlika2 != null)
            {
                SlikaSekundarnaDva.Source = new BitmapImage(new Uri(artikel.DodatnaSlika2));
            }

            ZnamkeSlike.Source = new BitmapImage(new Uri($"/Logo/{artikel.BlagovnaZnamka}.png", UriKind.Relative));
            QRKoda.Source = GenerateQrCode(artikel.QRKoda);


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
