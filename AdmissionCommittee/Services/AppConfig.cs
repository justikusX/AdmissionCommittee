using System.IO;
using System.Text.Json;

namespace AdmissionCommittee.Services;

/// <summary>Чтение строки подключения из appsettings.json (лежит рядом с .exe).</summary>
public static class AppConfig
{
    public static string LoadConnectionString()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (!File.Exists(path))
            throw new FileNotFoundException("Не найден файл настроек appsettings.json", path);

        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.GetProperty("ConnectionStrings").GetProperty("Default").GetString()
               ?? throw new InvalidOperationException("В appsettings.json не задана строка подключения Default");
    }
}
