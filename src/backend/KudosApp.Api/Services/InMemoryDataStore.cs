using System.Text.Json;

namespace KudosApp.Api.Services;

// Retained only for the static JSON helper used in audit calls.
// All data storage now handled by AppDbContext (EF Core / SQL Server).
public static class JsonHelper
{
    public static string ToJson<T>(T value) => JsonSerializer.Serialize(value);
}
