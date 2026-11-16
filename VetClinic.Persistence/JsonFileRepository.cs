using System.Text;
using System.Text.Json;
using VetClinic.Domain.Entities;

namespace VetClinic.Persistence;

/// <summary>
/// Репозиторій, що зберігає колекцію сутностей в одному JSON-файлі.
/// Дані читаються з файлу один раз під час створення, кожна зміна одразу записується на диск.
/// </summary>
public sealed class JsonFileRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly string _filePath;
    private readonly object _sync = new object();
    private List<T> _items;

    public JsonFileRepository(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        _filePath = filePath;
        _items = Load();
    }

    public IReadOnlyList<T> GetAll()
    {
        lock (_sync)
        {
            return _items.Select(Clone).ToList();
        }
    }

    public T? GetById(Guid id)
    {
        lock (_sync)
        {
            var found = _items.FirstOrDefault(i => i.Id == id);
            return found is null ? null : Clone(found);
        }
    }

    public void Add(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        lock (_sync)
        {
            if (_items.Any(i => i.Id == entity.Id))
            {
                throw new DataStorageException("Запис із таким ідентифікатором уже існує.");
            }

            var updated = new List<T>(_items) { Clone(entity) };
            Save(updated);
            _items = updated;
        }
    }

    public void Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);

        lock (_sync)
        {
            var index = _items.FindIndex(i => i.Id == entity.Id);
            if (index < 0)
            {
                throw new DataStorageException("Запис для оновлення не знайдено.");
            }

            var updated = new List<T>(_items);
            updated[index] = Clone(entity);
            Save(updated);
            _items = updated;
        }
    }

    public bool Remove(Guid id)
    {
        lock (_sync)
        {
            var index = _items.FindIndex(i => i.Id == id);
            if (index < 0)
            {
                return false;
            }

            var updated = new List<T>(_items);
            updated.RemoveAt(index);
            Save(updated);
            _items = updated;
            return true;
        }
    }

    private List<T> Load()
    {
        if (!File.Exists(_filePath))
        {
            return new List<T>();
        }

        try
        {
            var json = File.ReadAllText(_filePath, Encoding.UTF8);
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<T>();
            }

            return JsonSerializer.Deserialize<List<T>>(json, JsonStorageOptions.Default) ?? new List<T>();
        }
        catch (Exception ex)
        {
            // Пошкоджений JSON, значення, що не проходять валідацію сутностей, проблеми з доступом до файлу.
            throw new DataStorageException(
                $"Не вдалося прочитати файл даних «{_filePath}»: {ex.Message}", ex);
        }
    }

    private void Save(List<T> items)
    {
        try
        {
            var directory = Path.GetDirectoryName(Path.GetFullPath(_filePath));
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(items, JsonStorageOptions.Default);

            var tempPath = _filePath + ".tmp";
            File.WriteAllText(tempPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Move(tempPath, _filePath, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
        {
            throw new DataStorageException(
                $"Не вдалося записати файл даних «{_filePath}»: {ex.Message}", ex);
        }
    }

    private static T Clone(T entity)
    {
        var json = JsonSerializer.Serialize(entity, JsonStorageOptions.Default);
        return JsonSerializer.Deserialize<T>(json, JsonStorageOptions.Default)!;
    }
}
