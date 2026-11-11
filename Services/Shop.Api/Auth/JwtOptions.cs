namespace Shop.Api.Auth;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "road-to-highload";
    public string Audience { get; set; } = "road-to-highload";
    public string SigningKey { get; set; } = "L3MhjlIqnox+YNgiJgISw7FDZaxELQMz4RjelwUOFzIQfNeTHaaY05xaD/sOc+kw";
    public int ExpiryMinutes { get; set; } = 60;
}
