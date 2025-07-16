using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenQA.Selenium;

namespace Ejudge_Parser
{
    class Program
    {
        static void Main(string[] args)
        {
            OpenQA.Selenium.Chrome.ChromeOptions options = new OpenQA.Selenium.Chrome.ChromeOptions
            {
                BinaryLocation = @"C:\Program Files\Google\Chrome\Application\chrome.exe"
            };
            IWebDriver driver = new OpenQA.Selenium.Chrome.ChromeDriver(options);
            Console.Write("Ejudge parent contest page: ");
            string parent = Console.ReadLine();
            Console.Write("Tag: ");
            string tag = Console.ReadLine();
            Console.Write("Username: ");
            string user = Console.ReadLine();
            Console.Write("Password: ");
            string pass = Console.ReadLine();
            System.IO.StreamWriter writer = new System.IO.StreamWriter(@"P:\contests.dat", true);
            driver.Navigate().GoToUrl(parent);
            var contests_list = driver.FindElements(By.XPath("//a"));
            List<string> contests = new List<string>();
            foreach (var link in contests_list)
            {
                string contest = link.GetAttribute("href");
                if (contest.IndexOf("ejudge") == -1)
                    continue;
                contests.Add(contest);
                //IWebElement info = driver.FindElement(By.CssSelector("#probNavTaskArea-ins > table > tbody > tr:nth-child(2)"))
                //Console.ReadLine();
            }
            foreach (var contest in contests)
            {
                Console.Write("Parsing ");
                Console.WriteLine(contest);
                driver.Navigate().GoToUrl(contest);
                IWebElement username_input = driver.FindElement(By.CssSelector("#l12 > form > div > table > tbody > tr > td:nth-child(1) > div > input[type=text]"));
                username_input.SendKeys(user);
                IWebElement password_input = driver.FindElement(By.CssSelector("#l12 > form > div > table > tbody > tr > td:nth-child(2) > div > input[type=password]"));
                password_input.SendKeys(pass);
                IWebElement login_btn = driver.FindElement(By.CssSelector("#l12 > form > div > table > tbody > tr > td:nth-child(4) > div > input[type=submit]"));
                login_btn.Click();
                System.Threading.Thread.Sleep(250);
                string contest_name = driver.Title;
                IWebElement itog_btn = driver.FindElement(By.CssSelector("#main-menu > ul > li:nth-child(2) > div > a"));
                itog_btn.Click();
                var problems = driver.FindElements(By.CssSelector("#probNavTaskArea-ins > table > tbody > tr"));
                foreach (var problem in problems)
                {
                    var problem_info = problem.FindElements(By.XPath(".//td"));
                    if (problem_info.Count < 3)
                        continue;
                    string problem_name = problem_info[1].Text;
                    bool isSolved = problem.GetAttribute("class") == "green-tr";
                    writer.Write(tag);
                    writer.Write(" ");
                    writer.Write(user);
                    writer.Write(" ");
                    writer.Write(contest);
                    writer.Write(" ");
                    writer.WriteLine(isSolved ? 1:0);
                    writer.WriteLine(contest_name);
                    writer.WriteLine(problem_name);
                    //Console.Write(isSolved);
                    //Console.WriteLine(problem_name);
                }
            }
            //driver.Navigate().GoToUrl("https://ejudge.cpm-inf.ru/cgi-bin/new-client?contest_id=67101");
            //Console.ReadLine();
            //while (!Console.KeyAvailable)
            //{
            //    string old_url = driver.Url;
            //    IWebElement form = driver.FindElement(By.CssSelector("#ej-main-submit-tab > form > table > tbody > tr:nth-child(3) > td:nth-child(2) > input[type=file]"));
            //    form.SendKeys(@"P:\code.cpp");
            //    IWebElement btn = driver.FindElement(By.CssSelector("#ej-main-submit-tab > form > table > tbody > tr:nth-child(4) > td:nth-child(2) > input[type=submit]"));
            //    btn.Click();
            //    System.Threading.Thread.Sleep(250);
            //    driver.Navigate().GoToUrl(old_url);
            //    System.IO.StreamReader reader = new System.IO.StreamReader(@"P:\code.cpp");
            //    string file = reader.ReadToEnd();
            //    file += "~";
            //    reader.Close();
            //    System.IO.StreamWriter writer = new System.IO.StreamWriter(@"P:\code.cpp");
            //    writer.Write(file);
            //    writer.Close();
            //}
            Console.ReadLine();
            driver.Quit();
        }
    }
}
