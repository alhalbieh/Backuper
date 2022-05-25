using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Backuper
{
    public class Downloader
    {
        private IWebDriver driver;
        private Regex regex = new(@"\d+\.?\d+");
        private bool isAdmin;

        public Downloader(IWebDriver driver, bool isAdmin)
        {
            this.driver = driver;
            this.isAdmin = isAdmin;
        }

        public void CustomDownload(string mainName, string courseName, ReadOnlyCollection<IWebElement> subFolders, float maxSize = float.PositiveInfinity)
        {
            foreach (IWebElement subFolder in subFolders)
            {
                IWebElement subAnchor = subFolder.FindElement(By.XPath("span/a"));
                DirectoryInfo path = Directory.CreateDirectory(Path.Combine(mainName, courseName, subAnchor.GetAttribute("innerHTML")));
                driver.Navigate().GoToUrl(subAnchor.GetAttribute("href"));
                if (isAdmin)
                    DoProcessAdmin(maxSize, path);
                else
                    DoProcess(maxSize, path);
            }
        }

        public void CustomDownload(string mainName, string courseName, string courseUrl, float maxSize = float.PositiveInfinity)
        {
            DirectoryInfo path = Directory.CreateDirectory(Path.Combine(mainName, courseName));
            driver.Navigate().GoToUrl(courseUrl);
            if (isAdmin)
                DoProcessAdmin(maxSize, path);
            else
                DoProcess(maxSize, path);
        }

        private void DoProcess(float maxSize, DirectoryInfo path)
        {
            while (true)
            {
                var pdfItems = driver.FindElements(By.XPath("//ul[@class='fileListing']/li"));
                DownloadList(pdfItems, path.FullName, maxSize);
                var buttons = driver.FindElements(By.Id("nextLink"));
                if (buttons.Count > 0 && buttons[0].GetAttribute("class") != "button icon arrowright disable")
                    new Actions(driver).MoveToElement(buttons[0]).Click().Perform();
                else
                    break;
            }
        }

        private void DownloadList(ReadOnlyCollection<IWebElement> pdfItems, string folder, float maxSize)
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
                var rows = driver.FindElements(By.XPath("//tbody/tr"));
                //DownloadTable(rows, path.FullName, maxSize);
                var buttons = driver.FindElements(By.XPath("//span[@class='Next']"));
                if (buttons.Count > 0)
                    new Actions(driver).MoveToElement(buttons[0]).Click().Perform();
                else
                    break;
            }
        }

        private void DownloadTable(ReadOnlyCollection<IWebElement> pdfItems, string folder, float maxSize)
        {

        }
    }
}


