using MahApps.Metro.Actions;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace DisplayApp
{
    /// <summary>
    /// Interaction logic for MainWindowSettings.xaml
    /// </summary>
    public partial class MainWindowSettings : Page, INotifyPropertyChanged
    {

        private string NadzornaPloscaColor;
        private string HeaderKontrolnoOknoColor;
        private string BodyKontrolnoOknoColor;
        private string KontrolnoOknoTextColor;
        private string ButtonKortrolnoOknoOpenColor;
        private string ButtonKontrolnoOknoNastavitveColor;
        public MainWindowSettings()
        {
            InitializeComponent();
            FolderPathText.Text = Properties.Settings.Default.FolderPath;
            DataContext = this;

            //Naloži barvo za nadzorno ploščo
            colorPickerNadzornaPlosca.SelectedColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.NadzornaPloscaColor);

            //Naloži barvo za headerKontrolnoOkno
            colorPickerHeader.SelectedColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.HeaderKontrolnoOknoColor);
            HeaderKontrolnoOkno.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.HeaderKontrolnoOknoColor));

            //Naloži barvo za BodyKontrolnoOkno
            colorPickerBody.SelectedColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.BodyKontrolnoOKnoColor);
            BodyKontrolnoOKno.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.BodyKontrolnoOKnoColor));

            //Naloži barvo za OpenKontrolnoOknoButton
            colorPickerButtonOpen.SelectedColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.ButtonKortrolnoOknoOpenColor);
            ButtonKortrolnoOknoOpen.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.ButtonKortrolnoOknoOpenColor));
            //Naloži barvo za SettingsKontrolnoOknoButton
            colorPickerButtonSettings.SelectedColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.ButtonKontrolnoOknoNastavitveColor);
            ButtonKontrolnoOknoSettings.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.ButtonKontrolnoOknoNastavitveColor));
            //Naloži barvo za TextKotrolnoOkno
            colorPickerText.SelectedColor = (Color)ColorConverter.ConvertFromString(Properties.Settings.Default.TextKontrolnoOknoColor);
            TextColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Properties.Settings.Default.TextKontrolnoOknoColor));


        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private Brush _textColor = new SolidColorBrush(Colors.Black);
        public Brush TextColor
        {
            get => _textColor;
            set
            {
                _textColor = value;
                OnPropertyChanged(nameof(TextColor));
            }
        }

        //Za nastavitve pasice

        private void CloseMainWindowSettings(object sender, RoutedEventArgs e)
        {
            NavigationService.GetNavigationService(this)?.GoBack();
        }

        private void PasiceLokacijeSettingsClick(object sender, RoutedEventArgs e)
        {
            var folderDialog = new OpenFolderDialog();
            if (folderDialog.ShowDialog() == true)
            {
                string folderName = folderDialog.FolderName;
                Properties.Settings.Default.FolderPath = folderName;
                Properties.Settings.Default.Save();
                FolderPathText.Text = folderName;
            }
        }

        private void ColorPicker_NadzornaPlosca(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            var selectedColor = e.NewValue;
            NadzornaPloscaColor = selectedColor.ToString();
        }

        private void ColorPicker_BodyKontrolnoOKno(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            var selectedColor = e.NewValue;
            BodyKontrolnoOKno.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(selectedColor.ToString()));
            BodyKontrolnoOknoColor = selectedColor.ToString();
        }

        private void ColorPicker_HeaderKontrolnoOkno(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            var selectedColor = e.NewValue;
            HeaderKontrolnoOkno.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(selectedColor.ToString()));
            HeaderKontrolnoOknoColor = selectedColor.ToString();

        }

        private void ColorPicker_ButtonKortrolnoOknoOpen(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            var selectedColor = e.NewValue;
            ButtonKortrolnoOknoOpen.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(selectedColor.ToString()));
            ButtonKortrolnoOknoOpenColor = selectedColor.ToString();        
        }

        private void ColorPicker_ButtonKontrolnoOknoNastavitve(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            var selectedColor = e.NewValue;
            ButtonKontrolnoOknoSettings.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(selectedColor.ToString()));
            ButtonKontrolnoOknoNastavitveColor = selectedColor.ToString();       
        }

        private void ColorPicker_Text(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (e.NewValue.HasValue)
            {
                TextColor = new SolidColorBrush(e.NewValue.Value);
                KontrolnoOknoTextColor = e.NewValue.Value.ToString();
            }
        }

        private void ShraniNastaviteMain(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.HeaderKontrolnoOknoColor = HeaderKontrolnoOknoColor;
            Properties.Settings.Default.BodyKontrolnoOKnoColor = BodyKontrolnoOknoColor;
            Properties.Settings.Default.ButtonKontrolnoOknoNastavitveColor = ButtonKontrolnoOknoNastavitveColor;
            Properties.Settings.Default.ButtonKortrolnoOknoOpenColor = ButtonKortrolnoOknoOpenColor;
            Properties.Settings.Default.TextKontrolnoOknoColor = KontrolnoOknoTextColor;
            Properties.Settings.Default.NadzornaPloscaColor = NadzornaPloscaColor;
            Properties.Settings.Default.Save();
            AutoCloseMessageBox.Show("Shranjeno", 1000); // Closes after 3 seconds
        }

        //Msg box show
        public static class AutoCloseMessageBox
        {
            [DllImport("user32.dll", SetLastError = true)]
            private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

            private const uint WM_CLOSE = 0x0010;

            public static void Show(string text, int timeoutMilliseconds)
            {
                string caption = " "; // must match exactly

                Thread closer = new Thread(() =>
                {
                    Thread.Sleep(timeoutMilliseconds); // wait for timeout

                    IntPtr hWnd = FindWindow(null, caption);
                    if (hWnd != IntPtr.Zero)
                    {
                        PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
                    }
                });

                closer.IsBackground = true;
                closer.Start();

                MessageBox.Show(text, caption); // will be auto-closed
            }
        }
    }
}
