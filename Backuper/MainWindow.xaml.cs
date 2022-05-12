using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.IO;
using System.Windows;
namespace Backuper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            //InitializeComponent();

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
                Directory.CreateDirectory(mainFolder.Text);

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
            }
            this.Close();
            driver.Quit();
            scraper.Quit();

        }
    }
}
