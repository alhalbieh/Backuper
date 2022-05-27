using OpenQA.Selenium;
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
        private IWebDriver scrapper;
        private IWebDriver opener;
        private Regex regex = new(@"\d+\.?\d+");
        private bool isAdmin;

        public Downloader(IWebDriver driver, IWebDriver opener, bool isAdmin)
        {
            this.scrapper = driver;
            this.opener = opener;
            this.isAdmin = isAdmin;
        }

        public void CustomDownload(string mainName, string courseName, ReadOnlyCollection<IWebElement> subFolders,
                                   float maxSize = float.PositiveInfinity)
        {
            foreach (var subFolder in subFolders)
            {
                var subAnchor = subFolder.FindElement(By.XPath("span/a"));
                var path = Directory.CreateDirectory(Path.Combine(mainName, courseName, subAnchor.GetAttribute("innerHTML")));
                scrapper.Navigate().GoToUrl(subAnchor.GetAttribute("href"));
                opener.Navigate().GoToUrl(subAnchor.GetAttribute("href"));
                if (isAdmin)
                {
                    DoProcessAdmin(maxSize, path);
                }
                else
                {
                    DoProcess(maxSize, path);
                }
            }
        }

        public void CustomDownload(string mainName, string courseName, string courseUrl, float maxSize = float.PositiveInfinity)
        {
            var path = Directory.CreateDirectory(Path.Combine(mainName, courseName));
            scrapper.Navigate().GoToUrl(courseUrl);
            opener.Navigate().GoToUrl(courseUrl);
            if (isAdmin)
            {
                DoProcessAdmin(maxSize, path);
            }
            else
            {
                DoProcess(maxSize, path);
            }
        }

        private void DoProcess(float maxSize, DirectoryInfo path)
        {
            while (true)
            {
                var pdfItems = scrapper.FindElements(By.XPath("//ul[@class='fileListing']/li"));
                DownloadList(pdfItems, path.FullName, maxSize);
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

        private void DownloadList(ReadOnlyCollection<IWebElement> pdfItems, string folder, float maxSize)
        {
            List<Task> tasks = new();
            List<Tuple<string, string>> pathes = new();
            foreach (IWebElement? pdfItem in pdfItems)
            {
                string fileSizeText = pdfItem.FindElement(By.XPath("span[@class='filesize']")).Text;
                float fileSize = float.Parse(regex.Matches(fileSizeText)[0].Value);
                if (fileSize <= maxSize)
                {
                    IWebElement pdfAnchor = pdfItem.FindElement(By.XPath("a"));
                    string url = pdfAnchor.GetAttribute("href");
                    string name = url.Split('/')[^1];
                    string path = Path.Combine(folder, name);

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

        private void DoProcessAdmin(float maxSize, DirectoryInfo path)
        {
            while (true)
            {
                var rowsList = scrapper.FindElements(By.XPath("//tbody/tr")).ToList();
                if (rowsList.Count > 0)
                {
                    rowsList.RemoveAt(0);
                }

                var rows = new ReadOnlyCollection<IWebElement>(rowsList);

                DownloadTable(rows, path.FullName, maxSize);
                scrapper.Navigate().Refresh();
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
        }

        private void DownloadTable(ReadOnlyCollection<IWebElement> rows, string folder, float maxSize)
        {
            var tasks = new List<Task>();
            var pathes = new List<Tuple<string, string>>();
            foreach (var row in rows)
            {
                var cells = row.FindElements(By.XPath("td"));
                string name = cells[1].Text;
                string fileSizeText = cells[2].Text;
                string url = cells[^1].FindElement(By.XPath("a")).GetAttribute("href");

                float fileSize = float.Parse(fileSizeText);
                if (fileSize <= maxSize)
                {
                    opener.Navigate().GoToUrl(url);
                    Console.WriteLine(opener.Url.ToString());
                    /*
                    string path = Path.Combine(folder, name);

                    pathes.Add(new(url, name));
                    int i = 1;
                    while (Directory.Exists(path))
                    {
                        path = $"{path}_{i}";
                        i++;
                    }

                    WebClient webClient = new();
                    webClient.DownloadFile(url, path);
                    */
                }
            }
            //Task.WaitAll(tasks.ToArray());
        }
    }
}


