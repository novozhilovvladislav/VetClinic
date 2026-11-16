using System.Text;
using VetClinic.Business.Services;
using VetClinic.ConsoleApp;
using VetClinic.ConsoleApp.Infrastructure;
using VetClinic.ConsoleApp.Screens;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Exceptions;
using VetClinic.Persistence;

//   Presentation (екрани)  →  Business (сервіси)  →  Persistence (репозиторії у JSON-файлах)  →  Domain (сутності)

ConfigureConsole();

// Каталог для файлів даних: за замовчуванням «Data» поруч із програмою,
var dataDirectory = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.Combine(AppContext.BaseDirectory, "Data");

try
{
    var proceduresFile = Path.Combine(dataDirectory, "procedures.json");
    var isFirstRun = !File.Exists(proceduresFile);

    // Persistence
    var ownerRepository = new JsonFileRepository<Owner>(Path.Combine(dataDirectory, "owners.json"));
    var petRepository = new JsonFileRepository<Pet>(Path.Combine(dataDirectory, "pets.json"));
    var procedureRepository = new JsonFileRepository<Procedure>(proceduresFile);
    var visitRepository = new JsonFileRepository<Visit>(Path.Combine(dataDirectory, "visits.json"));

    // Business Logic
    var ownerService = new OwnerService(ownerRepository, petRepository);
    var petService = new PetService(petRepository, ownerRepository, visitRepository);
    var procedureService = new ProcedureService(procedureRepository);
    var visitService = new VisitService(visitRepository, ownerRepository, petRepository, procedureRepository);
    var statisticsService = new StatisticsService(visitRepository);

    if (isFirstRun)
    {
        SeedData.AddDefaultProcedures(procedureService);
    }

    // Presentation
    var pickers = new Pickers(ownerService, petService, procedureService);
    var mainMenu = new MainMenu(
        new OwnersScreen(ownerService, pickers),
        new PetsScreen(petService, ownerService, pickers),
        new ProceduresScreen(procedureService, pickers),
        new VisitsScreen(visitService, procedureService, pickers),
        new ReportsScreen(statisticsService));

    Ui.Header("Система управління ветеринарною клінікою");
    Ui.Info($"Файли даних: {dataDirectory}");
    Ui.Pause();

    mainMenu.Run();

    Console.WriteLine("До побачення!");
    return 0;
}
catch (VetClinicException ex)
{
    // Наприклад, пошкоджений файл даних
    Ui.Error($"Помилка запуску: {ex.Message}");
    Ui.Pause();
    return 1;
}

static void ConfigureConsole()
{
    try
    {
        Console.OutputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        if (Console.IsInputRedirected)
        {
            Console.InputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }

        Console.Title = "Ветеринарна клініка";
    }
    catch (Exception)
    {
    }
}
