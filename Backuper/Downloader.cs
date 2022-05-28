using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Backuper
{
    public class Downloader
    {
        private string largeFilesText = "Large Files.txt";
        private string doneFoldersText = "Progress.txt";
        private IWebDriver scrapper;
        private Regex regex = new(@"\d+\.?\d+");
        private bool isAdmin;

        public Downloader(IWebDriver driver, bool isAdmin)
        {
            this.scrapper = driver;
            this.isAdmin = isAdmin;
        }

        public void DownloadCourseWithSubs(string categoryName, string courseName,
                                           ReadOnlyCollection<IWebElement> subFolders,
                                           float maxSize = float.PositiveInfinity)
        {
            using var writer = new StreamWriter(doneFoldersText, true);
            writer.WriteLine(courseName);
            foreach (var subFolder in subFolders)
            {
                var subAnchor = subFolder.FindElement(By.XPath("span/a"));
                var subFolderName = subAnchor.GetAttribute("innerHTML");
                var downloadFolder = Directory.CreateDirectory(Path.Combine(categoryName, courseName, subFolderName)).FullName;
                scrapper.Navigate().GoToUrl(subAnchor.GetAttribute("href"));
                writer.Write($" {subFolderName}: ");
                if (isAdmin)
                {
                    DownloadFolderAdmin(maxSize, downloadFolder);
                }
                else
                {
                    DownloadFolder(maxSize, downloadFolder);
                }
                writer.WriteLine("Done");
            }
        }

        public void DownloadCourse(string categoryName, string courseName, string courseUrl, float maxSize = float.PositiveInfinity)
        {
            using var writer = new StreamWriter(doneFoldersText, true);
            var downloadFolder = Directory.CreateDirectory(Path.Combine(categoryName, courseName)).FullName;
            scrapper.Navigate().GoToUrl(courseUrl);

            writer.Write($"{courseName}: ");
            if (isAdmin)
            {
                DownloadFolderAdmin(maxSize, downloadFolder);
            }
            else
            {
                DownloadFolder(maxSize, downloadFolder);
            }
            writer.WriteLine("Done");
        }

        private void DownloadFolder(float maxSize, string downloadFolder)
        {
            while (true)
            {
                var pdfItems = scrapper.FindElements(By.XPath("//ul[@class='fileListing']/li"));
                DownloadPage(pdfItems, downloadFolder, maxSize);
                var buttons = scrapper.FindElements(By.Id("nextLink"));
                if (buttons.Count > 0 && buttons[0].GetAttribute("class") != "button icon arrowright disable")
                {
                    new Actions(scrapper).MoveToElement(buttons[0]).Click().Perform();
                }
                else
                {
                    break;
                }
            }
        }

        private void DownloadPage(ReadOnlyCollection<IWebElement> pdfItems, string downloadFolder, float maxSize)
        {
            List<Task> tasks = new();
            List<Tuple<string, string>> pathes = new();
            foreach (var pdfItem in pdfItems)
            {
                string fileSizeText = pdfItem.FindElement(By.XPath("span[@class='filesize']")).Text;
                float fileSize = float.Parse(regex.Matches(fileSizeText)[0].Value);
                if (fileSize <= maxSize)
                {
                    IWebElement pdfAnchor = pdfItem.FindElement(By.XPath("a"));
                    string url = pdfAnchor.GetAttribute("href");
                    string name = url.Split('/')[^1];
                    string path = Path.Combine(downloadFolder, name);

                    pathes.Add(new(url, name));
                    int i = 1;
                    while (Directory.Exists(path))
                    {
                        path = $"{path}_{i}";
                        i++;
                    }

                    WebClient webClient = new();
                    webClient.DownloadFile(url, path);
                }
            }
            Task.WaitAll(tasks.ToArray());
        }

        private void DownloadFolderAdmin(float maxSize, string downloadFolder)
        {
            var pdfNames = new List<string>();
            var pdfUrls = new List<string>();

            var chromeOptions = new ChromeOptions();
            chromeOptions.AddUserProfilePreference("download.default_directory", $"{downloadFolder}");
            chromeOptions.AddUserProfilePreference("intl.accept_languages", "nl");
            chromeOptions.AddUserProfilePreference("disable-popup-blocking", "true");

            var opener = new ChromeDriver(chromeOptions);
            Utils.Login(opener);
            opener.Navigate().GoToUrl(scrapper.Url);

            while (true)
            {
                var rowsList = scrapper.FindElements(By.XPath("//tbody/tr")).ToList();
                if (rowsList.Count > 0)
                {
                    rowsList.RemoveAt(0);
                }
                var rows = new ReadOnlyCollection<IWebElement>(rowsList);

                var outputs = ScrapPageAdmin(rows, maxSize, opener);
                pdfNames.AddRange(outputs.Item1);
                pdfUrls.AddRange(outputs.Item2);

                var buttons = scrapper.FindElements(By.XPath("//span[@class='next']"));
                if (buttons.Count > 0)
                {
                    new Actions(scrapper).MoveToElement(buttons[0]).Click().Perform();
                }
                else
                {
                    break;
                }
            }

            opener.Quit();
            DownloadPdfsAsync(downloadFolder, pdfNames, pdfUrls);
        }

        private (List<string>, List<string>) ScrapPageAdmin(ReadOnlyCollection<IWebElement> rows, float maxSize, IWebDriver opener)
        {
            var pdfNames = new List<string>();
            var pdfUrls = new List<string>();
            foreach (var row in rows)
            {
                var cells = row.FindElements(By.XPath("td"));
                string fileSizeText = cells[2].Text;
                string buttonUrl = cells[^1].FindElement(By.XPath("a")).GetAttribute("href");
                float fileSize = float.Parse(fileSizeText);
                if (fileSize <= maxSize && fileSize > 0)
                {
                    string oldUrl = opener.Url.ToString();
                    opener.Navigate().GoToUrl(buttonUrl);
                    string newUrl = opener.Url.ToString();
                    if (oldUrl != newUrl)
                    {
                        opener.Navigate().Back();
                        string name = newUrl.Split(@"/")[^1];
                        pdfNames.Add(name);
                        pdfUrls.Add(newUrl);
                    }
                }
                else
                {
                    using var writer = new StreamWriter(largeFilesText, true);
                    writer.WriteLineAsync(buttonUrl);
                }
            }
            return (pdfNames, pdfUrls);
        }

        private void DownloadPdfsAsync(string downloadFolder, List<string> pdfNames, List<string> pdfUrls)
        {
            var rarNames = Directory.GetFiles(downloadFolder).ToList();
            if (rarNames.Count > 0)
            {
                rarNames.ForEach(name => name = name.Split(@"\")[^1]);
                var allNames = rarNames.Concat(pdfNames).ToList();
                pdfNames = Utils.RenameDuplicates(allNames).GetRange(rarNames.Count, pdfNames.Count);
            }
            else
            {
                pdfNames = Utils.RenameDuplicates(pdfNames);
            }

            var tasks = new List<Task>();
            for (int i = 0; i < pdfNames.Count; i++)
            {
                var webClient = new WebClient();
                var pdfFullName = Path.Combine(downloadFolder, pdfNames[i]);
                var pdfUrl = pdfUrls[i];
                tasks.Add(Task.Run(() => webClient.DownloadFile(pdfUrl, pdfFullName)));
            }
            Task.WaitAll(tasks.ToArray());
        }
    }
}


