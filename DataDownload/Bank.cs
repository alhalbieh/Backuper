using OpenQA.Selenium;

namespace DataDownload
{
    public class Bank
    {
        public static async Task Backup(IEnumerable<string> selectedCategories, string downloadPath)
        {
            var cwd = Directory.CreateDirectory(downloadPath);
            Directory.SetCurrentDirectory(cwd.FullName);

            using var writer = new StreamWriter("Completed Courses.txt", true);
            using var error = new StreamWriter("Errors.txt", true);
            using var largeFiles = new StreamWriter("Large Files.txt", true);
            writer.AutoFlush = true;
            error.AutoFlush = true;
            largeFiles.AutoFlush = true;

            using var home = Utils.CreateChromeDriver();
            var categories = home.FindElements(By.XPath("//div[@class='content-box']/ul/li"));

            using var downloader = new Downloader(largeFiles);
            foreach (var category in categories)
            {
                string categoryName = category.Text;
                if (selectedCategories.Contains(categoryName))
                {
                    await writer.WriteLineAsync($"Starting {categoryName} backup ({DateTime.Now})");
                    await error.WriteLineAsync($"Starting {categoryName} backup ({DateTime.Now})");
                    await largeFiles.WriteLineAsync($"Starting {categoryName} backup ({DateTime.Now})");
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
                                await downloader.DownloadCourseWithSubs(categoryName, courseName, subFolders, 20);
                            else
                                await downloader.DownloadCourse(categoryName, courseName, courseUrl, 20);
                            await writer.WriteLineAsync("Done");
                        }
                        catch (Exception e)
                        {
                            await writer.WriteLineAsync("Error, check 'Errors.txt'");
                            await error.WriteLineAsync($"{categoryName}: {e.Message}");
                        }
                    }
                    await writer.WriteLineAsync($"Finished {categoryName} backup ({DateTime.Now})");
                    await writer.WriteLineAsync("**********************************************");
                    await error.WriteLineAsync($"Finished {categoryName} backup ({DateTime.Now})");
                    await error.WriteLineAsync("**********************************************");
                    await largeFiles.WriteLineAsync($"Finished {categoryName} backup ({DateTime.Now})");
                    await largeFiles.WriteLineAsync("**********************************************");
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
}