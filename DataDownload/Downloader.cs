using OpenQA.Selenium;
using OpenQA.Selenium.Interactions;
using System.Collections.ObjectModel;

namespace DataDownload
{
    public class Downloader : IDisposable
    {
        private readonly string largeFilesTxt = "Large Files.txt";
        private readonly IWebDriver scrapper = Utils.CreateChromeDriver();
        private readonly HttpClient httpClient = new HttpClient();

        public async Task DownloadCourseWithSubsAsync(string categoryName, string courseName,
                                                      ReadOnlyCollection<IWebElement> subFolders, float maxSize = float.PositiveInfinity)
        {
            foreach (var subFolder in subFolders)
            {
                var subAnchor = subFolder.FindElement(By.XPath("span/a"));
                var subFolderName = subAnchor.GetAttribute("innerHTML");
                var downloadFolder = Directory.CreateDirectory(Path.Combine(categoryName, courseName, subFolderName)).FullName;
                scrapper.Navigate().GoToUrl(subAnchor.GetAttribute("href"));
                await DownloadFolderAsync(maxSize, downloadFolder);
            }
        }

        public async Task DownloadCourseAsync(string categoryName, string courseName, string courseUrl,
                                              float maxSize = float.PositiveInfinity)
        {
            var downloadFolder = Directory.CreateDirectory(Path.Combine(categoryName, courseName)).FullName;
            scrapper.Navigate().GoToUrl(courseUrl);
            await DownloadFolderAsync(maxSize, downloadFolder);
        }

        private async Task DownloadFolderAsync(float maxSize, string downloadFolder)
        {
            var pdfNames = new List<string>();
            var pdfUrls = new List<string>();

            var opener = Utils.CreateChromeDriver(downloadFolder);
            opener.Navigate().GoToUrl(scrapper.Url);

            while (true)
            {
                var rowsList = scrapper.FindElements(By.XPath("//tbody/tr")).ToList();
                if (rowsList.Count > 0)
                    rowsList.RemoveAt(0);
                var rows = new ReadOnlyCollection<IWebElement>(rowsList);

                var outputs = await Task.Run(() => ScrapPage(opener, rows, downloadFolder, maxSize));
                pdfNames.AddRange(outputs.Item1);
                pdfUrls.AddRange(outputs.Item2);

                var buttons = scrapper.FindElements(By.XPath("//span[@class='next']"));
                if (buttons.Count > 0)
                    await Task.Run(() => new Actions(scrapper).MoveToElement(buttons[0]).Click().Perform());
                else
                    break;
            }
            opener.Quit();
            await DownloadPdfsAsync(downloadFolder, pdfNames, pdfUrls);
        }
        public (List<string>, List<string>) ScrapPage(IWebDriver opener, ReadOnlyCollection<IWebElement> rows, string downloadFolder, float maxSize)
        {
            var pdfNames = new List<string>();
            var pdfUrls = new List<string>();
            using var writer = new StreamWriter(largeFilesTxt, true);

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
                    //Check if zip file or pdf, since pdf files will open a new page
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
                    writer.Write($"{buttonUrl},     {downloadFolder}");
                    writer.WriteLine();
                }
            }
            return (pdfNames, pdfUrls);
        }
        private async Task DownloadPdfsAsync(string downloadFolder, List<string> pdfNames, List<string> pdfUrls)
        {
            var rarNames = Directory.GetFiles(downloadFolder).ToList();
            if (rarNames.Count > 0)
            {
                rarNames.ForEach(name => name = name.Split(@"\")[^1]);
                var allNames = rarNames.Concat(pdfNames).ToList();
                pdfNames = allNames.RenameDuplicates().GetRange(rarNames.Count, pdfNames.Count);
            }
            else
                pdfNames = pdfNames.RenameDuplicates();

            var responseTasks = pdfUrls.Select(u => httpClient.GetAsync(u));
            var responses = await Task.WhenAll(responseTasks);
            var streamTasks = responses.Select(r => r.Content.ReadAsStreamAsync());
            var streams = await Task.WhenAll(streamTasks);

            var pdfFullNames = pdfNames.Select(n => Path.Combine(downloadFolder, n));
            var fileStreams = pdfFullNames.Select(n => File.Create(n));
            var downloadTasks = streams.Zip(fileStreams, (s, fs) => s.CopyToAsync(fs));
            await Task.WhenAll(downloadTasks);
        }

        public void Dispose()
        {
            scrapper.Quit();
            httpClient.Dispose();
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