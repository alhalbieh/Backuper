using OpenQA.Selenium;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Backuper
{
    public static class Utils
    {
        public static void Login(IWebDriver driver)
        {
            driver.Navigate().GoToUrl("https://bank.engzenon.com/login");
            driver.FindElement(By.Id("UserUsername")).SendKeys("AhmadHalabieh");
            Thread.Sleep(250);
            driver.FindElement(By.Id("UserPassword")).SendKeys("5fcf01ebb");
            Thread.Sleep(250);
            driver.FindElement(By.XPath("//button[@class='btn-icon submit']")).Click();
        }

        public static List<string> RenameDuplicates(List<string> list)
        {
            var newNames = new List<string>();

            for (int i = 0; i < list.Count; i++)
            {
                var newName = list[i];
                if (newNames.Contains(newName))
                {
                    int j = 1;
                    while (newNames.Contains($"{newName} ({j})"))
                    {
                        j++;
                    }

                    newName = $"{newName} ({j})";
                }
                newNames.Add(newName);
            }
            return newNames;
        }
    }
}
