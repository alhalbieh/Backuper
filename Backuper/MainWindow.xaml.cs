using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
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
            using var opener = new ChromeDriver(chromeOptions);

            var cwd = Directory.CreateDirectory(@"C:\TestingApp");
            Directory.SetCurrentDirectory(cwd.FullName);

            Login(home);
            Login(scrapper);
            Login(opener);

            var accountNav = home.FindElements(By.XPath("//ul[@id='user-account-nav']/li/a/span"));
            bool isLoggedin = accountNav[1].Text == "Logout";

            var downloader = new Downloader(scrapper, opener, isLoggedin);

            var mainFolders = home.FindElements(By.XPath("//div[@class='content-box']/ul/li"));

            foreach (var mainFolder in mainFolders)
            {
                Console.WriteLine(mainFolder.Text);
                var courseFolders = mainFolder.FindElements(By.XPath("ul/li"));
                foreach (IWebElement courseFolder in courseFolders)
                {
                    string courseName = courseFolder.FindElement(By.XPath("span/a")).GetAttribute("innerHTML");
                    string courseUrl = courseFolder.FindElement(By.XPath("span/a")).GetAttribute("href");

                    var subFolders = courseFolder.FindElements(By.XPath("ul/li"));
                    Console.WriteLine(courseName);
                    if (subFolders.Count > 0)
                    {
                        downloader.CustomDownload(mainFolder.Text, courseName, subFolders);
                    }
                    else
                    {
                        downloader.CustomDownload(mainFolder.Text, courseName, courseUrl);
                    }
                }
                Console.WriteLine("***********************************");
            }

        }


        public void Login(IWebDriver driver)
        {
            driver.Navigate().GoToUrl("https://bank.engzenon.com/login");
            driver.FindElement(By.Id("UserUsername")).SendKeys("AhmadHalabieh");
            Thread.Sleep(250);
            driver.FindElement(By.Id("UserPassword")).SendKeys("5fcf01ebb");
            Thread.Sleep(250);
            driver.FindElement(By.XPath("//button[@class='btn-icon submit']")).Click();
        }
    }
}

/*
ChromeOptions chromeOptions = new ChromeOptions();
            chromeOptions.AddArgument("headless");
            IWebDriver driver = new ChromeDriver(chromeOptions);
            IWebDriver scraper = new ChromeDriver(chromeOptions);
            Downloader downloader = new(scraper);

            DirectoryInfo cwd = Directory.CreateDirectory(@"C:\TestingApp");
            Directory.SetCurrentDirectory(cwd.FullName);
            driver.Navigate().GoToUrl("https://bank.engzenon.com");

            var mainFolders = driver.FindElements(By.XPath("//div[@class='content-box']/ul/li"));
            foreach (IWebElement mainFolder in mainFolders)
            {
                //Directory.CreateDirectory(mainFolder.Text);

                var courseFolders = mainFolder.FindElements(By.XPath("ul/li"));
                foreach (IWebElement courseFolder in courseFolders)
                {
                    string courseName = courseFolder.FindElement(By.XPath("span/a")).GetAttribute("innerHTML");
                    string courseUrl = courseFolder.FindElement(By.XPath("span/a")).GetAttribute("href");

                    var subFolders = courseFolder.FindElements(By.XPath("ul/li"));
                    if (subFolders.Count > 0)
                        downloader.CustomDownload(mainFolder.Text, courseName, subFolders, 10);
                    else
                        downloader.CustomDownload(mainFolder.Text, courseName, courseUrl, 10);
                }
                Console.WriteLine("***********************************");

                //this.Close();
                driver.Quit();
                scraper.Quit();
            }
*/