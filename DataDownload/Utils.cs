using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace DataDownload
{
    public static class Utils
    {
        public static void LoginZenon(this IWebDriver driver)
        {
            driver.Navigate().GoToUrl("https://bank.engzenon.com/login");
            driver.FindElement(By.Id("UserUsername")).SendKeys("AhmadHalabieh");
            Thread.Sleep(250);
            driver.FindElement(By.Id("UserPassword")).SendKeys("5fcf01ebb");
            Thread.Sleep(250);
            driver.FindElement(By.XPath("//button[@class='btn-icon submit']")).Click();
        }

        public static IWebDriver CreateChromeDriver(string defaultDownloadPath = "")
        {
            var chromeOptions = new ChromeOptions();
            if (!string.IsNullOrWhiteSpace(defaultDownloadPath))
                chromeOptions.AddUserProfilePreference("download.default_directory", $"{defaultDownloadPath}");
            chromeOptions.AddUserProfilePreference("intl.accept_languages", "nl");
            chromeOptions.AddUserProfilePreference("disable-popup-blocking", "true");
            chromeOptions.AddArgument("headless");
            /*
            var chromeDriverService = ChromeDriverService.CreateDefaultService();
            chromeDriverService.HideCommandPromptWindow = true;
            var chromeDriver = new ChromeDriver(chromeDriverService, chromeOptions);*/
            var chromeDriver = new ChromeDriver(chromeOptions);

            chromeDriver.LoginZenon();
            return chromeDriver;
        }

        public static List<string> RenameDuplicatesBig(this List<string> oldNames)
        {
            //var x = "asd".Split('.');

            /*var namesNoExt = oldNames.Select(x => x.Split('.').Length > 1 ? string.Join('.', x.Split('.')[..^1]) : x);
            var exts = oldNames.Select(x => x.Split('.').Length > 1 ? x.Split('.')[^1] : "");

            foreach (var oldName in oldNames)
            {
                var newName = oldName;
                if (newNames.Contains(newName))
                {
                    int j = 1;
                    while (newNames.Contains($"{newName} ({j})"))
                        j++;
                    newName = $"{newName} ({j})";
                }
                newNames.Add(newName);
            }
            return newNames;

            if(namesNoExt.Contains() && exts.Contains())*/
            return oldNames;
        }

        public static List<string> RenameDuplicates(this List<string> oldNames)
        {
            var newNames = new List<string>();
            foreach (var oldName in oldNames)
            {
                var newName = oldName;
                if (newNames.Contains(newName))
                {
                    int j = 1;
                    while (newNames.Contains(Rename(newName, j)))
                        j++;
                    newName = Rename(newName, j);
                }
                newNames.Add(newName);
            }
            return newNames;

            string Rename(string str, int count)
            {
                int ind = str.Length - 1;
                for (int i = str.Length - 1; i >= 0; i--)
                    if (str[i] == '.')
                        ind = i;
                return str[..ind] + $" ({count})" + str[ind..];
            }
        }
    }
}
