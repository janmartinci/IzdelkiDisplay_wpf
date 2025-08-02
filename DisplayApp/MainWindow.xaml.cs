using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;
using MaterialDesignThemes.Wpf;
using System.IO;
using System.Windows.Input;
using System.Text.Json;
using System.ComponentModel;
using System.DirectoryServices;

namespace DisplayApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<XElement> izdelek;
        public MainWindow()
        {
            InitializeComponent();
            XmlLoad();
            prviInstall();
        }

        public static void prviInstall()
        {
            string markerPot = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "DisplayApp", "status.marker");

            if (File.Exists(markerPot))
            {
                Properties.Settings.Default.prvicRun = false;
            }
            else
            {
                if (!Properties.Settings.Default.prvicRun)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(markerPot));
                    File.WriteAllText(markerPot, "namesceno");
                }
            }
            Properties.Settings.Default.Save();

        }

        private void XmlLoad()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += (sender, e) =>
            {
                for (int i = 0; i <= 88; i++)
                {
                    System.Threading.Thread.Sleep(20);
                    worker.ReportProgress(i);
                }
                string xmlDatotekaUrl = "https://integration.techtrade.si/IzvozIzdelkovXML?X=711957605";
                try
                {
                    //xml load
                    XDocument xml = XDocument.Load(xmlDatotekaUrl);
                    izdelek = xml.Descendants("izdelek").ToList();

                    for (int i = 88; i <= 100; i++)
                    {
                        System.Threading.Thread.Sleep(20);
                        worker.ReportProgress(i);
                    }
                }
                catch (Exception ex)
                {

                    MessageBox.Show($"Xml ni bil uspešno naložen:{ex.Message}");

                };
            };
            worker.ProgressChanged += (sender, e) =>
            {
                LoadingProgressBar.Value = e.ProgressPercentage;
                PercentageText.Text = $"{e.ProgressPercentage}%";
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {

                XmlLoadProgressBar.Visibility = Visibility.Hidden;
                Pages.Visibility = Visibility.Visible;
                Pages.Navigate(new MainWindowContent(izdelek));

            };

            worker.RunWorkerAsync();


        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double scaleX = e.NewSize.Width / 1000.0;
            double scaleY = e.NewSize.Height / 600.0;
            double scale = Math.Min(scaleX, scaleY);
            MainScaleTransform.ScaleX = scale;
            MainScaleTransform.ScaleY = scale;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.Width = SystemParameters.WorkArea.Width * 0.8;
            this.Height = SystemParameters.WorkArea.Height * 0.8;
        }
    }
}