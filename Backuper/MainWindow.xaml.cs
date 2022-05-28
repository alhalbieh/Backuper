using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
namespace Backuper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public MainWindow()
        {
            InitializeComponent();
            ChromeOptions chromeOptions = new();
            //chromeOptions.AddArgument("headless");
            using var home = new ChromeDriver(chromeOptions);
            using var scrapper = new ChromeDriver(chromeOptions);

            var cwd = Directory.CreateDirectory(@"C:\TestingApp");
            Directory.SetCurrentDirectory(cwd.FullName);

            Utils.Login(home);
            Utils.Login(scrapper);

            var accountNav = home.FindElements(By.XPath("//ul[@id='user-account-nav']/li/a/span"));
            bool isLoggedin = accountNav[1].Text == "Logout";

            var downloader = new Downloader(scrapper, isLoggedin);

            var categories = home.FindElements(By.XPath("//div[@class='content-box']/ul/li"));

            foreach (var category in categories)
            {
                Console.WriteLine(category.Text);
                var courseFolders = category.FindElements(By.XPath("ul/li"));
                foreach (IWebElement courseFolder in courseFolders)
                {
                    using var writer = new StreamWriter("Completed Courses.txt", true);

                    string courseName = courseFolder.FindElement(By.XPath("span/a")).GetAttribute("innerHTML");
                    string courseUrl = courseFolder.FindElement(By.XPath("span/a")).GetAttribute("href");

                    var subFolders = courseFolder.FindElements(By.XPath("ul/li"));
                    if (subFolders.Count > 0)
                    {
                        downloader.DownloadCourseWithSubs(category.Text, courseName, subFolders, 1);
                    }
                    else
                    {
                        downloader.DownloadCourse(category.Text, courseName, courseUrl, 1);
                    }
                    writer.WriteLine(courseName);
                }
                Console.WriteLine("***********************************");
            }
        }
    }
}