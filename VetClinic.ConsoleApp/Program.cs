using System.Text;
using VetClinic.Business.Services;
using VetClinic.ConsoleApp;
using VetClinic.ConsoleApp.Infrastructure;
using VetClinic.ConsoleApp.Screens;
using VetClinic.Domain.Entities;
using VetClinic.Domain.Exceptions;
using VetClinic.Persistence;


ConfigureConsole();

// Каталог для файлів даних
var dataDirectory = args.Length > 0
    ? Path.GetFullPath(args[0])
    : Path.Combine(AppContext.BaseDirectory, "Data");

try
{
    var proceduresFile = Path.Combine(dataDirectory, "procedures.json");
    var veterinariansFile = Path.Combine(dataDirectory, "veterinarians.json");

    // Довідники заповнюються прикладами лише тоді, коли відповідного файлу ще немає
    var seedProcedures = !File.Exists(proceduresFile);
    var seedVeterinarians = !File.Exists(veterinariansFile);

    // Persistence
    var ownerRepository = new JsonFileRepository<Owner>(Path.Combine(dataDirectory, "owners.json"));
    var petRepository = new JsonFileRepository<Pet>(Path.Combine(dataDirectory, "pets.json"));
    var procedureRepository = new JsonFileRepository<Procedure>(proceduresFile);
    var visitRepository = new JsonFileRepository<Visit>(Path.Combine(dataDirectory, "visits.json"));
    var veterinarianRepository = new JsonFileRepository<Veterinarian>(veterinariansFile);
    var passportRepository = new JsonFileRepository<PetPassport>(Path.Combine(dataDirectory, "passports.json"));

    // Business Logic
    var ownerService = new OwnerService(ownerRepository, petRepository);
    var petService = new PetService(petRepository, ownerRepository, visitRepository, passportRepository);
    var procedureService = new ProcedureService(procedureRepository);
    var veterinarianService = new VeterinarianService(veterinarianRepository, visitRepository);
    var visitService = new VisitService(
        visitRepository, ownerRepository, petRepository, procedureRepository, veterinarianRepository);
    var passportService = new PetPassportService(passportRepository, petRepository, ownerRepository);
    var statisticsService = new StatisticsService(visitRepository, veterinarianRepository);

    if (seedProcedures)
    {
        SeedData.AddDefaultProcedures(procedureService);
    }

    if (seedVeterinarians)
    {
        SeedData.AddDefaultVeterinarians(veterinarianService);
    }

    // Presentation
    var pickers = new Pickers(ownerService, petService, procedureService, veterinarianService);
    var mainMenu = new MainMenu(
        new OwnersScreen(ownerService, pickers),
        new PetsScreen(petService, ownerService, pickers),
        new PassportsScreen(passportService, procedureService, pickers),
        new VeterinariansScreen(veterinarianService, pickers),
        new ProceduresScreen(procedureService, pickers),
        new VisitsScreen(visitService, procedureService, passportService, pickers),
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
