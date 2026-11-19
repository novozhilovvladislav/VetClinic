Сутності Owner, Pet, Visit, Procedure - Domain/Entities. CRUD процедур - ProcedureService, ProceduresScreen. Реєстрація візиту з прив’язкою власника й тварини та вибором процедур - VisitService.Register, VisitsScreen.Register. Унікальний ID візиту - Visit.Id

Збереження у файли - JsonFileRepository<T>. Статуси та історія змін - VisitStatus, Visit.StatusHistory, StatusChange. Закриття візиту (перевірка процедур, вартість, час завершення) - Visit.Complete, Visit.TotalCost, Visit.CompletedAt. Виручка за день - StatisticsService.GetDailyReport.

 Кабінет - Visit.Room. Собівартість, теги та блокування процедури - Procedure. Статистика по періодах - GetPeriodReport, DateRange. Статистика по кабінетах - GetCabinetStatistics.

Лікар - Veterinarian, VeterinarianService, Visit.VeterinarianId. Паспорт тварини - PetPassport та його записи, PetPassportService. Статистика по лікарях - GetVeterinarianStatistics; по процедурах - GetMostFrequentProcedures, GetMostProfitableProcedures.
