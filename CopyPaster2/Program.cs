// See https://aka.ms/new-console-template for more information
using CopyPaster2;
using Spectre.Console;
using TaskStatus = CopyPaster2.TaskStatus;

Console.WriteLine("Hello, World!");

const string dbPath = "Data Source=ejudge_data.db";
const string backLabel = "← Back (Esc)";

using var db = new DatabaseWrapper(dbPath);
await db.InitializeAsync();

AnsiConsole.Write(new FigletText("Contest Manager").LeftJustified());
AnsiConsole.MarkupLine("[grey]Нажмите Enter для входа в меню...[/]");
Console.ReadLine();

while (true)
{
    var choice = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("[bold]Главное меню[/]")
            .HighlightStyle("bold cyan1")
            .AddChoices("Add Credentials", "Parse Contest", "Search Problems", "Submit Contest", "Exit"));

    switch (choice)
    {
        case "Add Credentials": await AddCredentialsScreenAsync(); break;
        case "Parse Contest": await ParseContestScreenAsync(); break;
        case "Search Problems": await SearchProblemsScreenAsync(); break;
        case "Submit Contest": await SubmitContestScreenAsync(); break;
        case "Exit": return;
    }
}

async Task AddCredentialsScreenAsync()
{
    while (true)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold green]Add Credentials[/]").RuleStyle("green"));

        // Список уже добавленных
        var users = await db.GetAllUsernamesAsync();
        if (users.Count > 0)
        {
            var table = new Table()
                .Border(TableBorder.Rounded)
                .BorderColor(Color.Grey)
                .AddColumn("[bold]User[/]")
                .AddColumn("[bold]Password[/]");
            foreach (var u in users)
            {
                table.AddRow(Markup.Escape(u));
            }
            AnsiConsole.Write(table);
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]Пока нет сохранённых пользователей.[/]");
        }

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Действие:")
                .AddChoices("➕ Добавить нового", "🔑 Показать пароль", backLabel));

        if (action == backLabel) return;

        if (action == "➕ Добавить нового")
        {
            var user = AnsiConsole.Ask<string>("Имя пользователя:");
            var pass = AnsiConsole.Ask<string>("Пароль:");
            await db.AddCredentialsAsync(new Credentials(user, pass));
            AnsiConsole.Confirm($"[green]✓[/] Пользователь [bold]{Markup.Escape(user)}[/] добавлен.");
        }
        else if (action == "🔑 Показать пароль")
        {
            if (users.Count == 0) { AnsiConsole.MarkupLine("[yellow]Нет пользователей.[/]"); continue; }
            var pick = AnsiConsole.Prompt(
                new SelectionPrompt<string>().Title("Выберите пользователя:").AddChoices(users));
            var c = await db.GetCredentialsByUsernameAsync(pick);
            AnsiConsole.Confirm($"Пароль для [bold]{Markup.Escape(pick)}[/]: [yellow]{Markup.Escape(c!.Password)}[/]");
        }
    }
}

// ===== 2. Parse Contest =====
async Task ParseContestScreenAsync()
{
    AnsiConsole.Clear();
    AnsiConsole.Write(new Rule("[bold blue]Parse Contest[/]").RuleStyle("blue"));

    var url = AnsiConsole.Ask<string>("Ссылка на контест:");
    var users = await db.GetAllUsernamesAsync();

    var username = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Пользователь для входа:")
            .AddChoices(users.Concat([backLabel])));
    if (username == backLabel) return;

    // Запуск с прогресс-баром и логами
    await AnsiConsole.Progress()
        .AutoClear(true)
        .Columns(new ProgressColumn[]
        {
                new TaskDescriptionColumn { Alignment = Justify.Left },
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn(),
        })
        .StartAsync(async ctx =>
        {
            var task = ctx.AddTask("[cyan]Парсинг[/]", maxValue: 1.0);

            var progress = new Progress<double>(v =>
            {
                task.Value = v;
                if (v >= 1.0) task.Description = "[bold green]Готово[/]";
            });

            var log = new Progress<string>(msg =>
            {
                task.Description = $"[cyan]{Markup.Escape(msg)}[/]";
            });

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            try
            {
                ContestWorker cw = new(db);
                await cw.ParseContestAsync(url, username, progress, log, cts.Token);
            }
            catch (OperationCanceledException)
            {
                task.Description = "[red]Отменено пользователем[/]";
            }
        });

    AnsiConsole.Confirm("[green]Парсинг завершён.[/]");
}

// ===== 3. Search Problems =====
async Task SearchProblemsScreenAsync()
{
    while (true)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[bold magenta]Search Problems[/]").RuleStyle("magenta"));

        var query = AnsiConsole.Ask<string>("Запрос (по названию / условию):");
        var onlyOk = AnsiConsole.Confirm("Только задачи со статусом OK?", defaultValue: false);

        var results = await db.SearchTasksAsync(query, onlyOk);

        // Список найденных задач + пункт "Назад"
        var choices = results
            .Select(t => $"{t.TaskName}  [{StatusColor(t.Status)}]{t.Status}[/]  ({Markup.Escape(t.Contest ?? "")})")
            .ToList();
        choices.Add(backLabel);

        while (true)
        {
            var pick = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title($"Найдено: {results.Count}. Выберите задачу:")
                    .PageSize(10)
                    .AddChoices(choices));

            if (pick == backLabel) break;

            var idx = choices.IndexOf(pick);
            var task = results[idx];
            // await ShowTaskTabsAsync(task);
        }
    }
}

// ===== 4. Submit Contest =====
async Task SubmitContestScreenAsync()
{
    AnsiConsole.Clear();
    AnsiConsole.Write(new Rule("[bold yellow]Submit Contest[/]").RuleStyle("yellow"));

    var url = AnsiConsole.Ask<string>("Ссылка на контест:");
    var users = await db.GetAllUsernamesAsync();

    var username = AnsiConsole.Prompt(
        new SelectionPrompt<string>()
            .Title("Пользователь:")
            .AddChoices(users.Concat(new[] { backLabel })));
    if (username == backLabel) return;

    await AnsiConsole.Progress()
        .AutoClear(true)
        .Columns(new ProgressColumn[]
        {
                new TaskDescriptionColumn { Alignment = Justify.Left },
                new ProgressBarColumn(),
                new PercentageColumn(),
                new SpinnerColumn(),
        })
        .StartAsync(async ctx =>
        {
            var task = ctx.AddTask("[yellow]Отправка[/]", maxValue: 1.0);
            var progress = new Progress<double>(v => task.Value = v);
            var log = new Progress<string>(msg => task.Description = $"[yellow]{Markup.Escape(msg)}[/]");

            using var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            try
            {
                ContestWorker cw = new(db);
                await cw.SubmitContestAsync(url, username, progress, log, cts.Token);
            }
            catch (OperationCanceledException)
            {
                task.Description = "[red]Отменено[/]";
            }
        });
}

// ===== Утилиты =====
static string StatusColor(TaskStatus s) => s switch
{
    TaskStatus.OK => "green",
    TaskStatus.Error => "red",
    TaskStatus.NotTried => "yellow",
    _ => "white"
};

