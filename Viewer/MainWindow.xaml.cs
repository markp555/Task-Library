using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenQA.Selenium;

namespace Viewer
{
    public class Account
    {
        public string Username { get; set; }

        public string Password { get; set; }
    }

    public class Contest
    {
        public string Username { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
    }

    public class Task
    {
        public string Tag { get; set; }
        public bool Solved { get; set; }
        public string Username { get; set; }
        public Contest Contest { get; set; }
        public string Name { get; set; }
        public string SearchName { get; set; }
    }

    public class AccountsList : ObservableCollection<Account> { }
    public class TasksList : ObservableCollection<Task> { }
    public class ContestsList : ObservableCollection<Contest> { }

    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public TasksList Tasks => (TasksList)Resources["tasks"];
        public ContestsList Contests => (ContestsList)Resources["contests"];
        public AccountsList Accounts => (AccountsList)Resources["accounts"];

        private List<Task> tasks;
        private List<Contest> contests;
        private HashSet<string> contest_names;

        public MainWindow()
        {
            InitializeComponent();
            tasks = new List<Task>();
            contests = new List<Contest>();
            contest_names = new HashSet<string>();
            //Tasks.Add(new Task { Name = "A+B", Solved = true });
            System.IO.StreamReader reader = new System.IO.StreamReader(@"P:\contests.dat");
            while (!reader.EndOfStream)
            {
                string info = reader.ReadLine();
                string contest_name = reader.ReadLine();
                string task_name = reader.ReadLine();
                var subinfo = info.Split(new char[] { ' ' });
                Contest contest = new Contest { Name = contest_name, Link = subinfo[2], Username = subinfo[1] };
                Task task = new Task { Name = task_name, Tag = subinfo[0], Contest = contest, Username = subinfo[1], Solved = subinfo[3] == "1", SearchName = task_name == null ? "-" : task_name.ToLower() };
                tasks.Add(task);
                Tasks.Add(task);
                if (!contest_names.Contains(contest.Name))
                {
                    contests.Add(contest);
                    contest_names.Add(contest.Name);
                }
            }
            reader = new System.IO.StreamReader(@"P:\contests_login.dat");
            while (!reader.EndOfStream)
            {
                string info = reader.ReadLine();
                string[] subinfo = info.Split(new char[] { ' ' });
                if (subinfo.Count() == 2)
                {
                    Account account = new Account { Username = subinfo[0], Password = subinfo[1] };
                    Accounts.Add(account);
                }
            }
            foreach (var contest in contests)
            {
                Contests.Add(contest);
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (listViewMoveList.SelectedItem is Account account)
            {
                Clipboard.SetText(account.Username);
            }
        }

        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
            if (listViewMoveList.SelectedItem is Account account)
            {
                Clipboard.SetText(account.Password);
            }
        }

        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
            if (listViewContestsList.SelectedItem is Contest contest)
            {
                Clipboard.SetText(contest.Link);
            }
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            if (listViewContestsList.SelectedItem is Contest contest)
            {
                Account account = Accounts.First((Account acc) => acc.Username == contest.Username);
                if (account == null)
                    return;
                Login(contest.Link, contest.Username, account.Password);
            }
        }

        private void Login(string link, string username, string password)
        {
            OpenQA.Selenium.Chrome.ChromeOptions options = new OpenQA.Selenium.Chrome.ChromeOptions
            {
                BinaryLocation = @"C:\Program Files\Google\Chrome\Application\chrome.exe"
            };
            IWebDriver driver = new OpenQA.Selenium.Chrome.ChromeDriver(options);
            driver.Navigate().GoToUrl(link);
            IWebElement username_input = driver.FindElement(By.CssSelector("input[type=text]"));
            username_input.SendKeys(username);
            IWebElement password_input = driver.FindElement(By.CssSelector("input[type=password]"));
            password_input.SendKeys(password);
            IWebElement login_btn = driver.FindElement(By.CssSelector("input[type=submit]"));
            login_btn.Click();
            MessageBox.Show("Contest opened");
            driver.Quit();
        }

        private void filterBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Reload();
        }

        private void filterUsername_Checked(object sender, RoutedEventArgs e)
        {
            Reload();
        }

        private void Reload()
        {
            Tasks.Clear();
            string term = filterBox.Text.ToLower();
            foreach (var task in tasks)
            {
                if (task.SearchName.IndexOf(term) != -1)
                {
                    if ((bool)filterUsername.IsChecked)
                    {
                        if (listViewMoveList.SelectedItem is Account account && task.Username == account.Username)
                        {
                            Tasks.Add(task);
                        }
                    }
                    else if ((bool)filterContest.IsChecked)
                    {
                        if (listViewContestsList.SelectedItem is Contest contest && task.Contest.Name == contest.Name)
                        {
                            Tasks.Add(task);
                        }
                    }
                    else
                    {
                        Tasks.Add(task);
                    }
                }
            }
        }

        private void filterContest_Checked(object sender, RoutedEventArgs e)
        {
            Reload();
        }

        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            if (listViewTasksList.SelectedItem is Task task)
            {
                Clipboard.SetText(task.Contest.Link);
            }
        }

        private void MenuItem_Click_5(object sender, RoutedEventArgs e)
        {
            if (listViewTasksList.SelectedItem is Task task)
            {
                Account account = Accounts.First((Account acc) => acc.Username == task.Username);
                if (account == null)
                    return;
                Login(task.Contest.Link, task.Username, account.Password);
            }
        }
    }
}
