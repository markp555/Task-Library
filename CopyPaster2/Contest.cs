using PuppeteerSharp;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Xml.Linq;

namespace CopyPaster2
{
    public class ContestWorker(DatabaseWrapper database)
    {
        public DatabaseWrapper Database { get; set; } = database;

        // Парсинг контеста. IProgress<string> — для логов, IProgress<double> — для прогресса.
        public async Task ParseContestAsync(
            string url, string username,
            IProgress<double> progress, IProgress<string> log, CancellationToken ct)
        {
            var options = new LaunchOptions
            {
                // Укажите точный путь к исполняемому файлу Chrome на вашем компьютере
                ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                Headless = false // false, если хотите видеть браузер на экране
            };

            // 2. Запускаем браузер, используя локальный путь
            using var browser = await Puppeteer.LaunchAsync(options);
            using var page = await browser.NewPageAsync();

            await page.GoToAsync(url);
            if (!url.Contains("SID="))
            {
                var sel = await page.QuerySelectorAsync("input[type=text]");
                await sel.FocusAsync();
                await sel.TypeAsync(username);
                sel = await page.QuerySelectorAsync("input[type=password]");
                await sel.FocusAsync();
                var creds = await Database.GetCredentialsByUsernameAsync(username);
                await sel.TypeAsync(creds?.Password ?? throw new ApplicationException("no password stored for given account"));
                sel = await page.QuerySelectorAsync("input[type=submit]");
                var navTask = page.WaitForNavigationAsync();
                await sel.ClickAsync();
                await navTask;
                if (!page.Url.Contains("SID="))
                {
                    throw new ApplicationException("Unable to login into ejudge");
                }
                log.Report("Logged into contest");
            }
            var uri = new Uri(page.Url);
            var queryParsed = HttpUtility.ParseQueryString(uri.Query);
            queryParsed["action"] = "137";
            await page.GoToAsync(uri.GetLeftPart(UriPartial.Path) + "?" + queryParsed);
            log.Report("Navigated to summary");
            var taskList = await page.QuerySelectorAllAsync("#probNavTopList > td > ul > li");
            List<string> tasks = [];
            foreach (var task in taskList)
            {
                var taskurl = await task.EvaluateFunctionAsync<string>("el => el.querySelector('a').href");
                tasks.Add(taskurl);
            }
            double total = taskList.Length, cur = 0.0;
            foreach (var taskurl in tasks)
            {
                try
                {
                    await page.GoToAsync(taskurl);
                    var taskName = await page.QuerySelectorAsync("#probNavTaskArea-ins > h3:nth-child(3)");
                    var text = await taskName.EvaluateFunctionAsync<string>("el => el.textContent");
                    // var statement = await page.ScreenshotBase64Async();
                    var statementSource = await page.QuerySelectorAsync("#probNavTaskArea-ins");
                    var statement = await statementSource.EvaluateFunctionAsync<string>("el => el.outerText");
                    log.Report($"Task: {text}");
                    var submissions = await page.QuerySelectorAllAsync("#ej-main-submit-tab > table > tbody > tr");
                    TaskStatus tstatus = TaskStatus.NotTried;
                    string solution = "";
                    for (int i = 1; i < submissions.Length; i++)
                    {
                        var statusBar = await submissions[i].QuerySelectorAsync("td:nth-child(6)");
                        string status = await statusBar.EvaluateFunctionAsync<string>("el => el.textContent");
                        if (status == "OK")
                        {
                            var submBar = await submissions[i].QuerySelectorAsync("td:nth-child(8)");
                            string submurl = await submBar.EvaluateFunctionAsync<string>("el => el.querySelector('a').href");
                            await page.GoToAsync(submurl);
                            var codeblock = await page.QuerySelectorAsync("#l13 > div.l14 > pre > code");
                            solution = await codeblock.EvaluateFunctionAsync<string>("el => el.textContent");
                            tstatus = TaskStatus.OK;
                            break;
                        }
                        tstatus = TaskStatus.Error;
                    }
                    TaskRecord newTask = new(-1, text, url, username, solution, statement, tstatus);
                    await Database.AddTaskAsync(newTask);
                    cur += 1.0 / total;
                    progress.Report(cur);
                }
                catch (Exception ex)
                {
                    log.Report(ex.ToString());
                    cur += 1.0 / total;
                    progress.Report(cur);
                }
            }
        }

        public async Task SubmitContestAsync(
            string url, string username,
            IProgress<double> progress, IProgress<string> log, CancellationToken ct)
        {
            var options = new LaunchOptions
            {
                // Укажите точный путь к исполняемому файлу Chrome на вашем компьютере
                ExecutablePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe",
                Headless = false // false, если хотите видеть браузер на экране
            };

            // 2. Запускаем браузер, используя локальный путь
            using var browser = await Puppeteer.LaunchAsync(options);
            using var page = await browser.NewPageAsync();

            await page.GoToAsync(url);
            if (!url.Contains("SID="))
            {
                var sel = await page.QuerySelectorAsync("input[type=text]");
                await sel.FocusAsync();
                await sel.TypeAsync(username);
                sel = await page.QuerySelectorAsync("input[type=password]");
                await sel.FocusAsync();
                var creds = await Database.GetCredentialsByUsernameAsync(username);
                await sel.TypeAsync(creds?.Password ?? throw new ApplicationException("no password stored for given account"));
                sel = await page.QuerySelectorAsync("input[type=submit]");
                var navTask = page.WaitForNavigationAsync();
                await sel.ClickAsync();
                await navTask;
                if (!page.Url.Contains("SID="))
                {
                    throw new ApplicationException("Unable to login into ejudge");
                }
                log.Report("Logged into contest");
            }
            var uri = new Uri(page.Url);
            var queryParsed = HttpUtility.ParseQueryString(uri.Query);
            queryParsed["action"] = "137";
            await page.GoToAsync(uri.GetLeftPart(UriPartial.Path) + "?" + queryParsed);
            log.Report("Navigated to summary");
            var taskList = await page.QuerySelectorAllAsync("#probNavTopList > td > ul > li");
            List<string> tasks = [];
            foreach (var task in taskList)
            {
                var taskurl = await task.EvaluateFunctionAsync<string>("el => el.querySelector('a').href");
                tasks.Add(taskurl);
            }
            double total = taskList.Length, cur = 0.0;
            foreach (var taskurl in tasks)
            {
                try
                {
                    await page.GoToAsync(taskurl);
                    var taskName = await page.QuerySelectorAsync("#probNavTaskArea-ins > h3:nth-child(3)");
                    var text = await taskName.EvaluateFunctionAsync<string>("el => el.textContent");
                    text = text[(text.IndexOf(':') + 1)..].Replace("[", "[[");
                    log.Report("TASK " + text);
                    var submissions = await page.QuerySelectorAllAsync("#ej-main-submit-tab > table > tbody > tr");
                    if (submissions.Length > 1)
                    {
                        log.Report("Skipped due to not empty submissions list");
                        cur += 1.0 / total;
                        progress.Report(cur);
                        continue;
                    }
                    var res = await Database.SearchTasksAsync(text, true);
                    if (res.Count > 0)
                    {
                        var target = await page.QuerySelectorAsync("textarea");
                        await target.FocusAsync();
                        // await target.TypeAsync(res[0].Submission);
                        await page.EvaluateFunctionAsync(@"
                        (element, text) => {
                            if (element) {
                                element.value = text;
                                // Триггерим события изменения, чтобы сайт понял, что текст обновился
                                element.dispatchEvent(new Event('input', { bubbles: true }));
                                element.dispatchEvent(new Event('change', { bubbles: true }));
                            }
                        }", target, res[0].Submission);
                        var sendbtn = await page.QuerySelectorAsync("input[type=submit]");
                        if (await AnsiConsole.ConfirmAsync($"Submit {Markup.Escape(res[0].TaskName)} -> {Markup.Escape(text)}", cancellationToken: ct))
                        {
                            var tx = page.WaitForNavigationAsync();
                            await sendbtn.ClickAsync();
                            await tx;
                            log.Report("Submitted code!");
                        }
                        else {
                            log.Report("Canceled by user!");
                        }
                    }
                    else
                    {
                        log.Report("Task not found in archive");
                    }
                    cur += 1.0 / total;
                    progress.Report(cur);
                }
                catch (Exception ex)
                {
                    log.Report(ex.ToString());
                    cur += 1.0 / total;
                    progress.Report(cur);
                }
            }
        }
    }
}
