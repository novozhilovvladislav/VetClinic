using VetClinic.Business.Services;

namespace VetClinic.ConsoleApp;


internal static class SeedData
{
    public static void AddDefaultProcedures(ProcedureService procedures)
    {
        procedures.Create("Первинний огляд", 350m, 120m, new[] { "діагностика" });
        procedures.Create("Вакцинація", 450m, 250m, new[] { "вакцинація", "профілактика" });
        procedures.Create("Чипування", 500m, 220m, new[] { "профілактика" });
        procedures.Create("УЗД", 700m, 200m, new[] { "діагностика" });
        procedures.Create("Видалення зубного каменю", 1200m, 400m, new[] { "стоматологія" });
        procedures.Create("Стерилізація", 1800m, 700m, new[] { "хірургія" });
    }

    public static void AddDefaultVeterinarians(VeterinarianService veterinarians)
    {
        veterinarians.Create("Петренко Олена", "терапевт");
        veterinarians.Create("Коваленко Андрій", "хірург");
        veterinarians.Create("Мельник Ірина", "стоматолог");
    }
}
