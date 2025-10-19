using System.ComponentModel;
using System.DirectoryServices;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Linq;
using MaterialDesignThemes.Wpf;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;
using SQLitePCL;
using System.Windows.Documents;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.Pkcs;
using System.Windows.Automation.Peers;
using System.Xml;

namespace DisplayApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<XElement> izdelek;
        private List<XElement> izdelekNovo;
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
                string xmlLoadNoviIzdelki = "https://www.techtrade.si/newproducts/rss";
                try
                {
                    //xml load
                    XDocument xmlNovo = XDocument.Load(xmlLoadNoviIzdelki);
                    XDocument xml = XDocument.Load(xmlDatotekaUrl);

                    izdelek = xml.Descendants("izdelek").ToList();
                    izdelekNovo = xmlNovo.Descendants("item").ToList();

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
               _ = NoviIzdelkiVxml(izdelekNovo);
                Pages.Navigate(new MainWindowContent(izdelek));

            };

            worker.RunWorkerAsync();


        }

        
        private async Task NoviIzdelkiVxml(List<XElement> izdelekNovo)
        {
            try
            {
                foreach(var NoviIzdelek in izdelekNovo)
                {
                    var WebLink = NoviIzdelek.Element("link")?.Value;
                    if (!string.IsNullOrWhiteSpace(WebLink))
                    {
                        await ScrapeProductAsync(WebLink);
                    }
                }
            }
            catch(Exception ex) {

                MessageBox.Show($"Error nalaganje slik: {ex.Message}");
            }
        }

        public async Task ScrapeProductAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                string html = await client.GetStringAsync(url);

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                string titleNode = doc.DocumentNode.SelectSingleNode("//h1[@itemprop='name']")?.InnerText.Trim() ?? "Not found"; ;
                string shifra = doc.DocumentNode.SelectSingleNode("//span[@itemprop='sku']")?.InnerText.Trim() ?? "Not found"; ;
                string imageNode = doc.DocumentNode.SelectSingleNode("//img[@itemprop='image']")?.GetAttributeValue("src", "Not found"); ;

                XmlDocument xmlNoviIzdellki = new XmlDocument();
                XmlElement xml;

                if (File.Exists("NoviIzdellkiXml.xml"))
                {
                    xmlNoviIzdellki.Load("NoviIzdellkiXml.xml");
                    xml = xmlNoviIzdellki.DocumentElement;
                }
                else
                {
                    var dec = xmlNoviIzdellki.CreateXmlDeclaration("1.0", "utf-8", null);
                    xmlNoviIzdellki.AppendChild(dec);
                    xml = xmlNoviIzdellki.CreateElement("NoviIzdelki");
                    xmlNoviIzdellki.AppendChild(xml);
                }

                XmlElement NoviIzdelek = xmlNoviIzdellki.CreateElement("NoviIzdelek");

                void DodajElement(string name, string value)
                {
                    var node = xmlNoviIzdellki.CreateElement(name);
                    node.InnerText = value;
                    NoviIzdelek.AppendChild(node);
                }

                DodajElement("Title", titleNode);
                DodajElement("shifra", shifra);
                DodajElement("picture", imageNode);

                var obostojoci = xml.SelectSingleNode($"NoviIzdelek[shifra='{shifra}']");
                if (obostojoci != null)
                {
                    obostojoci["Title"].InnerText = titleNode;
                    obostojoci["picture"].InnerText = imageNode;
                }
                else
                {
                    xml.AppendChild(NoviIzdelek);
                }

                xmlNoviIzdellki.Save("NoviIzdellkiXml.xml");
            }
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