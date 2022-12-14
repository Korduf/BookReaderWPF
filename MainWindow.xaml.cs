using System;
using System.Windows;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;


namespace DeepReadApp
{
    [SuppressMessage("ReSharper", "FieldCanBeMadeReadOnly.Local")]
    public partial class MainWindow
    {
        private INIManager _iniManager = new INIManager(@"./ApplicationSettings.ini");
        private List<string> _pathBookLibrary = new List<string>();
        private List<double> _listMarkZoom = new List<double>();
        private List<int> _listMarkPage = new List<int>();
        private WrapPanel _wrapPanel = new WrapPanel();
        private StackPanel _panel = new StackPanel();
        private Window _windowLibrary = new Window();
        private bool IsOpen { get; set; }
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
            LibraryWindowInitialize();
        }

        private void ExitApp(object sender, EventArgs e)
        {
            SavePositionInBook_INI();
            SaveMark();
            SaveBookLibrary();
            Application.Current.Shutdown();
        }

        private void LoadFileDialog(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == false) return;
            SavePositionInBook_INI();
            SaveMark();
            FilePath = openFileDialog.FileName;
            Extension = Path.GetExtension(FilePath);
            FileName = Path.GetFileName(FilePath);
            ChapterExpander.Header = FileName;
            IsOpen = true;
            AddBookLibrary();
            LoadBookFromFile();
            RestorePositionInBook_INI();
        }
        
        private void AddBookLibrary()
        {
            var find = false;
            foreach (var unused in _pathBookLibrary.Where(t => FilePath == t))
            {
                find = true;
            }
            if (find) return;
            _pathBookLibrary.Add(FilePath);
            var imageBrush = new ImageBrush(new BitmapImage(
                new Uri(@"redbook.jpg", UriKind.Relative)));
            var button = new Button
            {
                Height = 140, Width = 116.8, Background = imageBrush, FontSize = 7,
                VerticalContentAlignment = VerticalAlignment.Center,
                Content = Path.GetFileName(FilePath)
            };
            button.Click += ClickBookLibrary;
            _wrapPanel.Children.Add(button);
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
                var imageBrush = new ImageBrush(new BitmapImage(
                    new Uri(@"redbook.jpg", UriKind.Relative)));
                var button = new Button
                {
                    Height = 140, Width = 116.8, Background = imageBrush, FontSize = 7,
                    VerticalContentAlignment = VerticalAlignment.Center,
                    Content = Path.GetFileName(_iniManager.GetPrivateString("Library", Convert.ToString(i)))
                };
                button.Click += ClickBookLibrary;
                _wrapPanel.Children.Add(button);
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
            var find = false;
            var senderButton = (Button)sender;
            foreach (var t in from t in _pathBookLibrary let temp = Path.GetFileName(t) where temp == Convert.ToString(senderButton.Content) select t)
            {
                find = true;
                FilePath = t;
            }

            if (find == false) return;
            Extension = Path.GetExtension(FilePath);
            FileName = Path.GetFileName(FilePath);
            ChapterExpander.Header = FileName;
            IsOpen = true;
            LoadBookFromFile();
            _windowLibrary.Hide();
            RestorePositionInBook_INI();
        }
        
        private void SavePositionInBook_INI()
        {
            if (!IsOpen) return;
            var zoom = FlowView.Zoom;
            var curPage = FlowView.MasterPageNumber;
            _iniManager.WritePrivateString("CurrentPosition", FileName + "/Page", Convert.ToString(curPage));
            _iniManager.WritePrivateString("CurrentPosition", FileName + "/Zoom", Convert.ToString(zoom, CultureInfo.CurrentCulture));
        }

        private void RestorePositionInBook_INI()
        {
            if (!IsOpen) return;
            if (_iniManager.GetPrivateString("CurrentPosition", FileName + "/Page") == "") return;
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
                    var memoryStream = new MemoryStream();
                    var fileStream = new FileStream(FilePath, FileMode.Open);
                    fileStream.CopyTo(memoryStream);
                    var range = new TextRange(FlowDoc.ContentStart, FlowDoc.ContentEnd);
                    range.Load(memoryStream, DataFormats.Text);
                    _panel.Children.Clear();
                    LoadMark();
                    memoryStream.Close();
                    fileStream.Close();
                    break;
                }
                case ".rtf":
                {
                    var fileStream = new FileStream(FilePath, FileMode.Open);
                    var range = new TextRange(FlowDoc.ContentStart, FlowDoc.ContentEnd);
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
            var button = new Button() { Content = Convert.ToString(_listMarkPage.Count + " Bookmark, " + Convert.ToString(FlowView.MasterPageNumber) + " Page"), Tag = _panel.Children.Count};
            
            var contextMenu = new ContextMenu();
            var menuItem = new MenuItem();
            button.ContextMenu = contextMenu;
            menuItem.Header = "Delete";
            menuItem.Tag = Convert.ToString(_panel.Children.Count);
            menuItem.Click += RemoveMarkButton;
            contextMenu.Items.Add(menuItem);
            
            button.Click += ClickMark;
            _panel.Children.Add(button);
            ChapterExpander.Content = _panel;
        }

        private void LibraryWindowInitialize()
        {
            _windowLibrary.Height = 500;
            _windowLibrary.Width = 500;
            _windowLibrary.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _windowLibrary.Background = Brushes.SlateGray;
            _windowLibrary.ResizeMode = ResizeMode.NoResize;
            _windowLibrary.Closing += LibraryWindowClose;
        }

        private void LibraryWindowClose(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            _windowLibrary.Hide();
        }

        private void LibraryWindowOpen(object sender, RoutedEventArgs e)
        {
            var scrollViewer = new ScrollViewer
            {
                Content = _wrapPanel
            };
            _windowLibrary.Content = scrollViewer;
            _windowLibrary.ShowDialog();
        }

        private void RemoveMarkButton(object sender, RoutedEventArgs e)
        {
            var it = (MenuItem)sender;
            _listMarkPage.RemoveAt(Convert.ToInt32(it.Tag));
            _listMarkZoom.RemoveAt(Convert.ToInt32(it.Tag));
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
            _panel.Children.Clear();
            for (var indexMark = 0; indexMark < _listMarkPage.Count; indexMark++)
            {
                var button = new Button() { Content = Convert.ToString(indexMark + 1) + " Bookmark, " + Convert.ToString(_listMarkPage[indexMark]) + " Page",
                    Tag = Convert.ToString(indexMark)};
                
                var contextMenu = new ContextMenu();
                var menuItem = new MenuItem();
                button.ContextMenu = contextMenu;
                menuItem.Header = "Delete";
                menuItem.Tag = Convert.ToString(indexMark);
                menuItem.Click += RemoveMarkButton;
                contextMenu.Items.Add(menuItem);
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
                    Convert.ToString(_listMarkZoom[indexMark], CultureInfo.CurrentCulture));
                _iniManager.WritePrivateString("MarkPosition", FileName + "/Pages/" + Convert.ToString(indexMark),
                    Convert.ToString(_listMarkPage[indexMark]));
            }
        }

        private void ClickMark(object sender, RoutedEventArgs e)
        {
            if (!IsOpen) return;
            for (var i = 0; i < _panel.Children.Count; i++)
            {
                var button = (Button)sender;
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
                var button = new Button() { Content = Convert.ToString(indexMark + 1) + " Bookmark, " + Convert.ToString(_listMarkPage[indexMark]) + " Page",
                    Tag = Convert.ToString(indexMark)};
                
                var contextMenu = new ContextMenu();
                var menuItem = new MenuItem();
                button.ContextMenu = contextMenu;
                menuItem.Header = "Delete";
                menuItem.Tag = Convert.ToString(indexMark);
                menuItem.Click += RemoveMarkButton;
                contextMenu.Items.Add(menuItem);
                
                
                button.Click += ClickMark;
                _panel.Children.Add(button);
                ChapterExpander.Content = _panel;
            }
        }
    }
}
