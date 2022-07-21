using DataDownload;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
namespace WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private string downloadPath = "Pick a folder to store";
        private bool isNotDownloading = true;
        public ObservableCollection<string> AvaliableCategories { get; set; }
        public ObservableCollection<string> DownloadCategories { get; set; }
        //public Dictionary<string, ObservableCollection<string>> AvaliableCourses { get; set; }

        public string DownloadPath
        {
            get { return downloadPath; }
            set
            {
                if (downloadPath != value)
                {
                    downloadPath = value;
                    OnPropertyChanged();
                }
            }
        }
        public bool IsNotDownloading
        {
            get { return isNotDownloading; }
            set
            {
                if (isNotDownloading != value)
                {
                    isNotDownloading = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public MainWindow()
        {
            using var _driver = Utils.CreateChromeDriver(login: false);
            AvaliableCategories = new ObservableCollection<string>(Bank.GetCategories(_driver).Select(x => x.Text).ToList());
            DownloadCategories = new ObservableCollection<string>();
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            IsNotDownloading = false;
            await Task.Run(() => Bank.Backup(DownloadCategories, DownloadPath));
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void ChangeStateButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Normal)
                WindowState = WindowState.Maximized;
            else
                WindowState = WindowState.Normal;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = (AvaliableListView.SelectedItems).Cast<string>().ToList();
            foreach (var selectedItem in selectedItems)
            {
                DownloadCategories.Add(selectedItem);
                AvaliableCategories.Remove(selectedItem);
            }

        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            List<string> selectedItems = (DownloadListView.SelectedItems).Cast<string>().ToList();
            foreach (var selectedItem in selectedItems)
            {
                AvaliableCategories.Add(selectedItem);
                DownloadCategories.Remove(selectedItem);
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {

            var dialog = new SaveFileDialog();
            dialog.Title = "Select a Directory";
            dialog.Filter = "Directory|*.this.directory";
            dialog.FileName = "select";
            if (dialog.ShowDialog() == true)
            {
                string path = dialog.FileName;
                path = path.Replace("\\select.this.directory", "").Replace(".this.directory", "");
                DownloadPath = System.IO.Directory.CreateDirectory(path).FullName;
            }
        }
    }
}
