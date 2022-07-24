using DataDownload.Models;
using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System.Collections.ObjectModel;

namespace DataDownload;

public class Scraper
{
    private readonly IWebDriver _pager;
    private readonly StreamWriter _largeFiles;
    public Scraper(StreamWriter largeFiles, IWebDriver pager)
    {
        _largeFiles = largeFiles;
        _pager = pager;
    }

    public List<Pdf> ScrapeFolder(string folderPath, string folderUrl, float maxSize)
    {
        _pager.Navigate().GoToUrl(folderUrl);
        using var opener = Utils.CreateChromeDriver(Directory.CreateDirectory(folderPath).FullName);
        opener.Navigate().GoToUrl(_pager.Url);

        var pdfs = new List<Pdf>();
        while (true)
        {
            var rows = _pager.FindElements(By.XPath("//tbody/tr")).ToList();
            if (rows.Count == 0)
                break;
            rows.RemoveAt(0);
            pdfs.AddRange(ScrapeTable(new ReadOnlyCollection<IWebElement>(rows)));

            var buttons = _pager.FindElements(By.XPath("//span[@class='next']"));
            if (buttons.Count > 0)
                new Actions(_pager).MoveToElement(buttons[0]).Click().Perform();
            else
                break;
        }
        return pdfs;

        List<Pdf> ScrapeTable(ReadOnlyCollection<IWebElement> rows)
        {
            var pdfs = new List<Pdf>();
            foreach (var row in rows)
            {
                var cells = row.FindElements(By.XPath("td"));
                string fileSizeText = cells[2].Text;
                string buttonUrl = cells[^1].FindElement(By.XPath("a")).GetAttribute("href");
                float fileSize = float.Parse(fileSizeText);
                if (fileSize <= maxSize)
                {
                    if (fileSize > 0)
                    {
                        string oldUrl = opener.Url.ToString();
                        opener.Navigate().GoToUrl(buttonUrl);
                        string newUrl = opener.Url.ToString();
                        //Check if zip file or pdf, since pdf files will open a new page
                        if (oldUrl != newUrl)
                        {
                            opener.Navigate().Back();
                            string name = newUrl.Split(@"/")[^1];
                            pdfs.Add(new Pdf { Name = name, Url = newUrl });
                        }
                    }
                }
                else
                {
                    _largeFiles.Write($"{buttonUrl},     {folderPath}");
                    _largeFiles.WriteLine();
                }
            }
            return pdfs;
        }
    }
}






























/*
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
    */