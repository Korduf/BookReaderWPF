using System;
using System.Net.Mime;
using System.Windows;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Markup;
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
using Microsoft.Win32;
using System.Configuration;
using System.Windows.Annotations;
using System.Windows.Controls.Primitives;


namespace DeepReadApp
{
    public partial class MainWindow
    {
        private INIManager _iniManager = new INIManager(@"./ApplicationSettings.ini");
        private List<string> _pathBookLibrary = new List<string>();
        private List<double> _listMarkZoom = new List<double>();
        private List<int> _listMarkPage = new List<int>();
        private TextBox _textBoxDelMark = new TextBox();
        private StackPanel _panel = new StackPanel();
        private Button _buttonDelMark = new Button();
        private Window _windowDelMark = new Window();
        private bool IsOpen { get; set; } = false;
        private string Extension { get; set; }
        private string FilePath { get; set; }
        private string FileName { get; set; }


        public MainWindow()
        {
            InitializeComponent();
        }
        
        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadBookLibrary();
            RemoveMarkWindowInitialize();
        }

        public void ExitApp(object sender, EventArgs e)
        {
            SavePositionInBook_INI();
            SaveMark();
            SaveBookLibrary();
            Application.Current.Shutdown();
        }

        private void LoadFileDialog(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == false) return;
            SavePositionInBook_INI();
            SaveMark();
            FilePath = openFileDialog.FileName;
            Extension = System.IO.Path.GetExtension(FilePath);
            FileName = System.IO.Path.GetFileName(FilePath);
            ChapterExpander.Header = FileName;
            IsOpen = true;
            AddBookLibrary();
            LoadBookFromFile();
            RestorePositionInBook_INI();
        }
        
        private void AddBookLibrary()
        {
            var find = false;
            for (var i = 0; i < _pathBookLibrary.Count; i++)
            {
                if (FilePath != _pathBookLibrary[i]) continue;
                find = true;
            }
            if (find == true) return;
            _pathBookLibrary.Add(FilePath);
            var menuItem = new MenuItem();
            menuItem.Header = System.IO.Path.GetFileName(FilePath);
            menuItem.Click += ClickBookLibrary;
            MenuLibrary.Items.Add(menuItem);
        }

        private void LoadBookLibrary()
        {
            var maxIndexItem = 0;
            while (_iniManager.GetPrivateString("Library", maxIndexItem.ToString()) != "")
            {
                maxIndexItem++;
            }
            for (var i = 0; i < maxIndexItem; i++)
            {
                var menuItem = new MenuItem();
                menuItem.Header = System.IO.Path.GetFileName(_iniManager.GetPrivateString("Library", Convert.ToString(i)));
                menuItem.Click += ClickBookLibrary;
                MenuLibrary.Items.Add(menuItem);
                _pathBookLibrary.Add(_iniManager.GetPrivateString("Library", Convert.ToString(i)));
            }
        }

        private void SaveBookLibrary()
        {
            for (var i = 0; i < _pathBookLibrary.Count; i++)
            {
                _iniManager.WritePrivateString("Library", Convert.ToString(i), _pathBookLibrary[i]);
            }
        }

        private void ClickBookLibrary(object sender, RoutedEventArgs e)
        {
            SavePositionInBook_INI();
            SaveMark();
            var senderMenuItem = (MenuItem)sender;
            for (var i = 0; i < _pathBookLibrary.Count; i++)
            {
                var temp = System.IO.Path.GetFileName(_pathBookLibrary[i]);
                if (temp != senderMenuItem.Header.ToString()) continue;
                FilePath = _pathBookLibrary[i];
            }
            Extension = System.IO.Path.GetExtension(FilePath);
            FileName = System.IO.Path.GetFileName(FilePath);
            ChapterExpander.Header = FileName;
            IsOpen = true;
            LoadBookFromFile();
            RestorePositionInBook_INI();
        }
        
        private void SavePositionInBook_INI()
        {
            if (!IsOpen) return;
            var zoom = FlowView.Zoom;
            var curpage = FlowView.MasterPageNumber;
            _iniManager.WritePrivateString("CurrentPosition", FileName + "/Page", Convert.ToString(curpage));
            _iniManager.WritePrivateString("CurrentPosition", FileName + "/Zoom", Convert.ToString(zoom));
        }

        private void RestorePositionInBook_INI()
        {
            if (!IsOpen) return;
            if (!FlowView.CanGoToPage(
                    Convert.ToInt32(_iniManager.GetPrivateString("CurrentPosition", FileName + "/Page")))) return;
            if (MessageBox.Show("Recovery progress reading?", "Attention!", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                FlowView.Zoom =
                    Convert.ToDouble(_iniManager.GetPrivateString("CurrentPosition", FileName + "/Zoom"));
                FlowView.GoToPage(
                    Convert.ToInt32(_iniManager.GetPrivateString("CurrentPosition", FileName + "/Page")));
            }
            else
            {
                FlowView.Zoom = 100;
                FlowView.FirstPage();
            }
        }

        private void LoadBookFromFile()
        {
            switch (Extension)
            {
                case ".txt" :
                {
                    FileStream fileStream;
                    MemoryStream memoryStream = new MemoryStream();
                    fileStream = new FileStream(FilePath, FileMode.Open);
                    fileStream.CopyTo(memoryStream);
                    TextRange range = new TextRange(FlowDoc.ContentStart, FlowDoc.ContentEnd);
                    range.Load(memoryStream, DataFormats.Text);
                    _panel.Children.Clear();
                    LoadMark();
                    memoryStream.Close();
                    fileStream.Close();
                    break;
                }
                case ".rtf":
                {
                    FileStream fileStream;
                    fileStream = new FileStream(FilePath, FileMode.Open);
                    TextRange range = new TextRange(FlowDoc.ContentStart, FlowDoc.ContentEnd);
                    range.Load(fileStream, DataFormats.Rtf);
                    _panel.Children.Clear();
                    LoadMark();
                    fileStream.Close();
                    break;
                }
                case "fb2":
                {
                    
                    break;
                }
            }
            FlowView.FirstPage();
        }

        private void AddMark(object sender, RoutedEventArgs e)
        {
            _listMarkZoom.Add(FlowView.Zoom);
            _listMarkPage.Add(FlowView.MasterPageNumber);
            Button button = new Button() { Content = Convert.ToString(_panel.Children.Count + " : " + FlowView.MasterPageNumber), Tag = _panel.Children.Count};
            button.Click += ClickMark;
            _panel.Children.Add(button);
            ChapterExpander.Content = _panel;
        }

        private void RemoveMarkWindowInitialize()
        {
            var label = new Label();
            var grid = new Grid();
            var row1 = new RowDefinition();
            var row2 = new RowDefinition();
            var row3 = new RowDefinition();
            _textBoxDelMark.MaxLength = 5;
            _textBoxDelMark.Background = Brushes.LightSlateGray;
            label.HorizontalContentAlignment = HorizontalAlignment.Center;
            label.Content = "Enter bookmark index to delete";
            label.FontSize = 16;
            label.VerticalContentAlignment = VerticalAlignment.Center;
            label.HorizontalContentAlignment = HorizontalAlignment.Center;
            _buttonDelMark.Height = 40;
            _buttonDelMark.Width = 110;
            _buttonDelMark.FontSize = 16;
            _buttonDelMark.Content = "Delete";
            _buttonDelMark.Click += RemoveMarkButton;
            grid.RowDefinitions.Add(row1);
            grid.RowDefinitions.Add(row2);
            grid.RowDefinitions.Add(row3);
            Grid.SetRow(label, 0);
            Grid.SetRow(_textBoxDelMark, 1);
            Grid.SetRow(_buttonDelMark, 2);
            grid.Children.Add(label);
            grid.Children.Add(_textBoxDelMark);
            grid.Children.Add(_buttonDelMark);
            _windowDelMark.Height = 200;
            _windowDelMark.Width = 300;
            _windowDelMark.Background = Brushes.SlateGray;
            _windowDelMark.ResizeMode = ResizeMode.NoResize;
            _windowDelMark.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _windowDelMark.Content = grid;
            _windowDelMark.Closing += RemoveMarkWindowClose;
        }

        private void RemoveMarkWindowClose(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            _windowDelMark.Hide();
        }

        private void RemoveMarkWindowOpen(object sender, RoutedEventArgs e)
        {
            if (IsOpen != true) return;
            _textBoxDelMark.Text = "";
            if (_windowDelMark.ShowDialog() != true) return;
        }

        private void RemoveMarkButton(object sender, RoutedEventArgs e)
        {
            _listMarkPage.RemoveAt(Convert.ToInt32(_textBoxDelMark.Text));
            _listMarkZoom.RemoveAt(Convert.ToInt32(_textBoxDelMark.Text));
            var maxIndexMark = 0;
            while (_iniManager.GetPrivateString("MarkPosition", FileName + "/Pages/" + Convert.ToString(maxIndexMark)) != "")
            {
                maxIndexMark++;
            }
            for (var indexMark = 0; indexMark < maxIndexMark; indexMark++)
            {
                _iniManager.WritePrivateString("MarkPosition", FileName + "/Zoom/" + Convert.ToString(indexMark), null);
                _iniManager.WritePrivateString("MarkPosition", FileName + "/Pages/" + Convert.ToString(indexMark), null);
            }
            SaveMark();
            /*for (var indexMark = 0; indexMark < _listMarkPage.Count; indexMark++)
            {
                _iniManager.WritePrivateString("MarkPosition", FileName + "/Zoom/" + Convert.ToString(indexMark),
                    Convert.ToString(_listMarkZoom[indexMark]));
                _iniManager.WritePrivateString("MarkPosition", FileName + "/Pages/" + Convert.ToString(indexMark),
                    Convert.ToString(_listMarkPage[indexMark]));
            }*/
            _panel.Children.Clear();
            for (var indexMark = 0; indexMark < _listMarkPage.Count; indexMark++)
            {
                Button button = new Button() { Content = Convert.ToString(indexMark) + " : " + Convert.ToString(_listMarkPage[indexMark]),
                    Tag = Convert.ToString(indexMark)};
                button.Click += ClickMark;
                _panel.Children.Add(button);
                ChapterExpander.Content = _panel;
            }
        }
        
        private void SaveMark()
        {
            if (!IsOpen) return;
            for (var indexMark = 0; indexMark < _listMarkPage.Count; indexMark++)
            {
                _iniManager.WritePrivateString("MarkPosition", FileName + "/Zoom/" + Convert.ToString(indexMark),
                    Convert.ToString(_listMarkZoom[indexMark]));
                _iniManager.WritePrivateString("MarkPosition", FileName + "/Pages/" + Convert.ToString(indexMark),
                    Convert.ToString(_listMarkPage[indexMark]));
            }
        }

        private void ClickMark(object sender, RoutedEventArgs e)
        {
            if (!IsOpen) return;
            for (var i = 0; i < _panel.Children.Count; i++)
            {
                Button button = (Button)sender;
                if (Convert.ToInt32(button.Tag) != i) continue;
                FlowView.Zoom = _listMarkZoom[i];
                FlowView.GoToPage(_listMarkPage[i]);
            }
        }

        private void LoadMark()
        {
            _listMarkPage.Clear();
            _listMarkZoom.Clear();
            var maxIndexMark = 0;
            while (_iniManager.GetPrivateString("MarkPosition", FileName + "/Pages/" + Convert.ToString(maxIndexMark)) != "")
            {
                maxIndexMark++;
            }
            int indexMark;
            for (indexMark = 0; indexMark < maxIndexMark; indexMark++)
            {
                _listMarkPage.Add(Convert.ToInt32(_iniManager.GetPrivateString("MarkPosition", FileName + "/Pages/" + Convert.ToString(indexMark))));
                _listMarkZoom.Add(Convert.ToDouble(_iniManager.GetPrivateString("MarkPosition", FileName + "/Zoom/" + Convert.ToString(indexMark))));
                Button button = new Button() { Content = Convert.ToString(indexMark) + " : " + Convert.ToString(_listMarkPage[indexMark]),
                    Tag = Convert.ToString(indexMark)};
                button.Click += ClickMark;
                _panel.Children.Add(button);
                ChapterExpander.Content = _panel;
            }
        }
    }
}