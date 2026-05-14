namespace KudosApp.Api.Infrastructure;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Key { get; set; } = "ReplaceThisWithASecureKeyForDevOnly123456789";
    public string Issuer { get; set; } = "KudosApp";
    public string Audience { get; set; } = "KudosUsers";
    public int ExpiryMinutes { get; set; } = 120;
}
