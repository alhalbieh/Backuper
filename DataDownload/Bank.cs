using DataDownload.Models;
using OpenQA.Selenium;

namespace DataDownload;

public class Bank
{
    private readonly HttpClient _httpClient;

    public Bank(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task Backup(IEnumerable<string> selectedCategories, string downloadPath)
    {
        var cwd = Directory.CreateDirectory(downloadPath);
        Directory.SetCurrentDirectory(cwd.FullName);

        using var log = new StreamWriter("log.txt", true) { AutoFlush = true };
        using var largeFiles = new StreamWriter("large files.txt", false) { AutoFlush = true };

        using var home = Utils.CreateChromeDriver();
        var categories = home.FindElements(By.XPath("//div[@class='content-box']/ul/li"));

        using var pager = Utils.CreateChromeDriver();
        await log.WriteLineAsync($"Beginning backup process ({DateTime.Now})");
        foreach (var category in categories)
        {
            string categoryName = category.Text;
            if (selectedCategories.Contains(categoryName))
            {
                await log.WriteLineAsync($"\tStarting {categoryName} backup ({DateTime.Now})");

                var courses = category.FindElements(By.XPath("ul/li"));
                foreach (var course in courses)
                {
                    string courseName = course.FindElement(By.XPath("span/a")).GetAttribute("innerHTML");
                    string courseUrl = course.FindElement(By.XPath("span/a")).GetAttribute("href");

                    if (courseName == "Topics in Control" || courseName == "Signals" || courseName == "Electromagnetic I")
                    {
                        await log.WriteLineAsync($"\t\tWorking on {courseName}");
                        var subFolders = course.FindElements(By.XPath("ul/li"));

                        var folders = new List<Folder>
                    {
                        new Folder { Path = Path.Combine(categoryName, courseName), Url = courseUrl }
                    };
                        if (subFolders.Count > 0)
                        {
                            folders.RemoveAt(0);
                            foreach (var subFolder in subFolders)
                            {
                                var subAnchor = subFolder.FindElement(By.XPath("span/a"));
                                string subFolderName = subAnchor.GetAttribute("innerHTML");
                                string subFolderUrl = subAnchor.GetAttribute("href");
                                folders.Add(new Folder { Path = Path.Combine(categoryName, courseName, subFolderName), Url = subFolderUrl });
                            }
                        }

                        foreach (var folder in folders)
                        {
                            await log.WriteAsync($"\t\t\tDownloading {folder.Path.Split(@"\")[^1]}: ");
                            try
                            {
                                var scraper = new Scraper(largeFiles, pager);
                                var pdfs = scraper.ScrapeFolder(folder.Path, folder.Url, 20);
                                var downloader = new Downloader(_httpClient);
                                await downloader.Download(folder.Path, pdfs);
                                await log.WriteLineAsync("Done");
                            }

                            catch (Exception e)
                            {
                                await log.WriteLineAsync($"Error, {e.Message}");
                            }
                        }
                        await log.WriteLineAsync($"\t\tFinished {courseName}");
                    }
                }
            }
        }
        await log.WriteLineAsync($"Stopped backup process {DateTime.Now}");
        await log.WriteLineAsync("****************************************");
    }

    public static List<string> GetCategories()
    {
        using var driver = Utils.CreateChromeDriver(login: false);
        var categories = driver.FindElements(By.XPath("//div[@class='content-box']/ul/li"));
        return categories.Select(c => c.Text).ToList();
    }

    public static List<IWebElement> GetCategories(IWebDriver driver)
    {
        var categories = driver.FindElements(By.XPath("//div[@class='content-box']/ul/li"));
        return categories.ToList();
    }

    /*public static Dictionary<> GetCoursesName(IEnumerable<IWebElement> categories)
    {
        foreach (var category in categories)
        {
            var courseFolders = category.FindElements(By.XPath("ul/li"));
            foreach (var courseFolder in courseFolders)
            {
                string courseName = courseFolder.FindElement(By.XPath("span/a")).GetAttribute("innerHTML");
            }
        }
    }*/
}