using OpenQA.Selenium;

namespace DataDownload
{
    public class Bank
    {
        /*
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        */
        public static async Task Backup(IEnumerable<string> selectedCategories, string downloadPath)
        {
            using var home = Utils.CreateChromeDriver();
            var cwd = Directory.CreateDirectory(downloadPath);
            Directory.SetCurrentDirectory(cwd.FullName);
            using var downloader = new Downloader();

            var categories = home.FindElements(By.XPath("//div[@class='content-box']/ul/li"));
            foreach (var category in categories)
            {
                using var writer = new StreamWriter("Completed Courses.txt", true);
                using var error = new StreamWriter("Errors.txt", true);
                string categoryName = category.Text;
                if (selectedCategories.Contains(categoryName))
                {
                    await writer.WriteLineAsync($"Starting {categoryName} backup ({DateTime.Now})");

                    var courseFolders = category.FindElements(By.XPath("ul/li"));
                    foreach (var courseFolder in courseFolders)
                    {
                        string courseName = courseFolder.FindElement(By.XPath("span/a")).GetAttribute("innerHTML");
                        string courseUrl = courseFolder.FindElement(By.XPath("span/a")).GetAttribute("href");

                        await writer.WriteAsync($"Working on {courseName}: ");
                        var subFolders = courseFolder.FindElements(By.XPath("ul/li"));
                        try
                        {

                            if (subFolders.Count > 0)
                                await downloader.DownloadCourseWithSubsAsync(categoryName, courseName, subFolders, 20);
                            else
                                await downloader.DownloadCourseAsync(categoryName, courseName, courseUrl, 20);
                            await writer.WriteLineAsync("Done");
                        }
                        catch (Exception e)
                        {
                            await writer.WriteLineAsync("Not completed, see 'Errors.txt' for details");
                            await error.WriteLineAsync(e.Message);
                        }
                    }
                    await writer.WriteLineAsync($"Finished {categoryName} backup ({DateTime.Now})");
                    await writer.WriteLineAsync("**********************************************");
                }
            }
        }

        public static List<IWebElement> GetCategoriesName()
        {
            using var driver = Utils.CreateChromeDriver();
            var categories = driver.FindElements(By.XPath("//div[@class='content-box']/ul/li"));
            return categories.ToList();
        }

        public static List<IWebElement> GetCategories(IWebDriver driver)
        {
            var categories = driver.FindElements(By.XPath("//div[@class='content-box']/ul/li"));
            return categories.ToList();
        }
    }
}