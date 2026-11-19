namespace VetClinic.ConsoleApp.Infrastructure;

/// <summary>Пункт меню: назва та дія, яка виконується при виборі </summary>
public sealed record MenuItem(string Title, Action Execute);
