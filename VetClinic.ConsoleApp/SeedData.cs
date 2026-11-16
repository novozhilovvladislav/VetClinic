using VetClinic.Business.Services;

namespace VetClinic.ConsoleApp;

/// <summary>
/// Початковий довідник процедур. Додається лише під час першого запуску (коли ще немає файлу procedures.json),
/// щоб було з чого обирати при реєстрації візитів. Список можна змінювати у розділі «Процедури».
/// </summary>
internal static class SeedData
{
    public static void AddDefaultProcedures(ProcedureService procedures)
    {
        procedures.Create("Первинний огляд", 350m);
        procedures.Create("Вакцинація", 450m);
        procedures.Create("Чипування", 500m);
        procedures.Create("УЗД", 700m);
        procedures.Create("Видалення зубного каменю", 1200m);
        procedures.Create("Стерилізація", 1800m);
    }
}
