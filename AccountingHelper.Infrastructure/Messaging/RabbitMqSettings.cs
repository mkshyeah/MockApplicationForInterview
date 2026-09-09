namespace AccountingHelper.Infrastructure.Messaging;

public sealed class RabbitMqSettings
{
    public const string SectionName = "RabbitMq";
    
    public string Host { get; set; } = null!;
    public ushort Port { get; set; }
    public string VirtualHost { get; set; } = "/";
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
}