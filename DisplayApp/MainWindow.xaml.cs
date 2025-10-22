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
using System.Text;
using System.Security.Cryptography;
using System.Net;

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

        private static string GetHashString(string input)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
                StringBuilder sb = new StringBuilder();
                foreach (byte b in hashBytes)
                {
                    sb.Append(b.ToString("x2"));
                }
                return sb.ToString();
            }
        }


        private static readonly HttpClient http = new HttpClient();

        private async void XmlLoad()
        {
            try
            {
                string xmlDatotekaUrl = "https://integration.techtrade.si/IzvozIzdelkovXML?X=711957605";
                string xmlLoadNoviIzdelki = "https://www.techtrade.si/newproducts/rss";

                XmlLoadProgressBar.Visibility = Visibility.Visible;
                Pages.Visibility = Visibility.Hidden;

                for (int i = 0; i <= 88; i++)
                {
                    await Task.Delay(30);
                    LoadingProgressBar.Value = i;
                    PercentageText.Text = $"{i}%";
                }

                // Hkrati naloži oba XML-ja

                var xmlNoviTask = http.GetStringAsync(xmlLoadNoviIzdelki);
                var xmlMainTask = http.GetStringAsync(xmlDatotekaUrl);

                await Task.WhenAll(xmlNoviTask, xmlMainTask);

                // Glavni Xml

                string xmlTextMain = xmlMainTask.Result;
                var xml = XDocument.Parse(xmlTextMain);
                izdelek = xml.Descendants("izdelek").ToList();

                // Novi Izdelki Xml

                string xmlTextNovi = xmlNoviTask.Result;

                var xmlNovo = XDocument.Parse(xmlTextNovi);
                xmlNovo.Root.Element("channel")?.Element("lastBuildDate")?.Remove();
                string xmlHashNoviIzdelki = GetHashString(xmlNovo.ToString());
                izdelekNovo = xmlNovo.Descendants("item").ToList();

                if (Properties.Settings.Default.HashPreverjanjeNoviArtikli != xmlHashNoviIzdelki)
                {
                    await NoviIzdelkiVxml(izdelekNovo);
                    Properties.Settings.Default.HashPreverjanjeNoviArtikli = xmlHashNoviIzdelki;
                    Properties.Settings.Default.Save();
                }
                else
                {
                    if (!File.Exists("NoviIzdellkiXml.xml"))
                    {
                        izdelekNovo = xmlNovo.Descendants("item").ToList();
                        await NoviIzdelkiVxml(izdelekNovo);
                    }
                    SkupniPodatki.IzdelekNovo = await NoviIzdelkiXmlAsync("NoviIzdellkiXml.xml");
                    if (izdelekNovo.Count != SkupniPodatki.IzdelekNovo.Count)
                    {
                        izdelekNovo = xmlNovo.Descendants("item").ToList();
                        await NoviIzdelkiVxml(izdelekNovo);
                    }
                }

                for (int i = 88; i <= 100; i++)
                    {
                        await Task.Delay(30);
                        LoadingProgressBar.Value = i;
                        PercentageText.Text = $"{i}%";
                    }

                XmlLoadProgressBar.Visibility = Visibility.Hidden;
                Pages.Visibility = Visibility.Visible;
                Pages.Navigate(new MainWindowContent(izdelek));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Xml ni bil uspešno naložen: {ex.Message}");
            }
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

                MessageBox.Show($"Error nalaganje: {ex.Message}");
            }
        }

        public async Task ScrapeProductAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                var response = await client.GetAsync(url);
                byte[] htmlBytes = await response.Content.ReadAsByteArrayAsync();

                string charset = response.Content.Headers.ContentType?.CharSet;

                if (string.IsNullOrEmpty(charset))
                {
                    string snippet = Encoding.ASCII.GetString(htmlBytes, 0, Math.Min(htmlBytes.Length, 1024));
                    var match = System.Text.RegularExpressions.Regex.Match(snippet, "<meta.*?charset=[\"']?(.*?)[\"'>]", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    if (match.Success)
                        charset = match.Groups[1].Value.Trim();
                }

                if (string.IsNullOrEmpty(charset))
                    charset = "utf-8";

                Encoding encoding;
                try { encoding = Encoding.GetEncoding(charset); }
                catch { encoding = Encoding.UTF8; }

                string html = encoding.GetString(htmlBytes);

                HtmlDocument doc = new HtmlDocument();
                doc.LoadHtml(html);

                string Decode(string input) => WebUtility.HtmlDecode(input);

                string titleNode = Decode(doc.DocumentNode.SelectSingleNode("//h1[@itemprop='name']")?.InnerText.Trim() ?? "Not found"); 
                string shifra = Decode (doc.DocumentNode.SelectSingleNode("//span[@itemprop='sku']")?.InnerText.Trim() ?? "Not found");
                string imageNode = Decode (doc.DocumentNode.SelectSingleNode("//img[@itemprop='image']")?.GetAttributeValue("src", "Not found"));
                string price = Decode(doc.DocumentNode.SelectSingleNode("//span[@itemprop='price']")?.InnerText.Trim() ?? "Not found");

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
                DodajElement("price", price);

                var obostojoci = xml.SelectSingleNode($"NoviIzdelek[shifra='{shifra}']");
                if (obostojoci != null)
                {
                    obostojoci["Title"].InnerText = titleNode;
                    obostojoci["picture"].InnerText = imageNode;
                    obostojoci["price"].InnerText = price;
                }
                else
                {
                    xml.AppendChild(NoviIzdelek);
                }

                using (var writer = new XmlTextWriter("NoviIzdellkiXml.xml", Encoding.UTF8))
                {
                    xmlNoviIzdellki.Save(writer);
                }
                SkupniPodatki.IzdelekNovo = await NoviIzdelkiXmlAsync("NoviIzdellkiXml.xml");
            }
        }

        public async Task<List<XElement>> NoviIzdelkiXmlAsync(string filepath)
        {

            using FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

            XDocument xdoc = await XDocument.LoadAsync(fs, LoadOptions.None, default);

            List<XElement> noviIzdelek = new List<XElement>(xdoc.Root.Elements());

            return noviIzdelek;

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