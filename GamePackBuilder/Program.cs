using Spectre.Console;

AnsiConsole.Write(
    new FigletText("GamePackBuilder")
        .LeftJustified()
        .Color(Color.Cyan1));

AnsiConsole.MarkupLine("[green]Спектр работает![/]");
AnsiConsole.MarkupLine("[yellow]Проверка цветов:[/] [red]красный[/] [blue]синий[/] [magenta]розовый[/]");

AnsiConsole.WriteLine();
AnsiConsole.Write(new Rule("[cyan]Готов к работе[/]").RuleStyle("cyan"));

AnsiConsole.MarkupLine("\n[grey]Нажми любую клавишу для выхода...[/]");
Console.ReadKey();