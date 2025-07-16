using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;

namespace CF_Parser
{
    class Program
    {
        static void Main(string[] args)
        {
            OpenQA.Selenium.Chrome.ChromeOptions options = new OpenQA.Selenium.Chrome.ChromeOptions
            {
                BinaryLocation = @"C:\Program Files\Google\Chrome\Application\chrome.exe"
            };
            //options.AddAdditionalChromeOption("excludeSwitches", "enable-automation");
            options.AddArgument("--disable-blink-features=AutomationControlled");
            options.AddArgument("--no-sandbox");
            options.AddArgument("start-maximized");
            options.AddArgument("enable-automation");
            options.AddArgument("--disable-infobars");
            options.AddArgument("--disable-dev-shm-usage");
            options.AddArgument("--disable-browser-side-navigation");
            options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/109.0.0.0 Safari/537.36");
            string username = Console.ReadLine();
            IWebDriver driver = new OpenQA.Selenium.Chrome.ChromeDriver(options);
            driver.Navigate().GoToUrl("https://codeforces.com/enter");
            System.Threading.Thread.Sleep(1000);
            Console.ReadLine();
            //driver.Navigate().GoToUrl()
            var groups = driver.FindElements(By.CssSelector("a.groupName"));
            List<string> group_links = new List<string>();
            foreach (var group in groups)
            {
                group_links.Add(group.GetAttribute("href"));
            }
            System.IO.StreamWriter writer = new System.IO.StreamWriter(@"P:\contests.dat", true);
            foreach (var group in group_links)
            {
                Console.WriteLine(group);
                driver.Navigate().GoToUrl(group);
                var contests = driver.FindElements(By.CssSelector("a"));
                List<string> links = new List<string>();
                foreach (var contest in contests)
                {
                    if (contest.Text.IndexOf("Войти") == -1 && contest.Text.IndexOf("Enter") == -1)
                        continue;
                    links.Add(contest.GetAttribute("href"));
                }
                foreach (var contest in links)
                {
                    driver.Navigate().GoToUrl(contest);
                    Console.ReadLine();
                }
            }
            writer.Close();
            Console.ReadLine();
            driver.Quit();
        }
    }
}
