using VetClinic.ConsoleApp.Infrastructure;

namespace VetClinic.ConsoleApp.Screens;

/// <summary>Головне меню застосунку </summary>
public sealed class MainMenu
{
    private readonly OwnersScreen _owners;
    private readonly PetsScreen _pets;
    private readonly PassportsScreen _passports;
    private readonly VeterinariansScreen _veterinarians;
    private readonly ProceduresScreen _procedures;
    private readonly VisitsScreen _visits;
    private readonly ReportsScreen _reports;

    public MainMenu(
        OwnersScreen owners,
        PetsScreen pets,
        PassportsScreen passports,
        VeterinariansScreen veterinarians,
        ProceduresScreen procedures,
        VisitsScreen visits,
        ReportsScreen reports)
    {
        _owners = owners;
        _pets = pets;
        _passports = passports;
        _veterinarians = veterinarians;
        _procedures = procedures;
        _visits = visits;
        _reports = reports;
    }

    public void Run() => Ui.Menu("Ветеринарна клініка — головне меню", new[]
    {
        new MenuItem("Власники", _owners.Show),
        new MenuItem("Тварини", _pets.Show),
        new MenuItem("Паспорти тварин", _passports.Show),
        new MenuItem("Лікарі", _veterinarians.Show),
        new MenuItem("Процедури (послуги)", _procedures.Show),
        new MenuItem("Візити", _visits.Show),
        new MenuItem("Статистика", _reports.Show)
    }, exitTitle: "Вихід");
}
