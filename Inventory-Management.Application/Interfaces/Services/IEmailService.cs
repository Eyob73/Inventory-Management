namespace Inventory_Management.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendEmailAsync(string to, string subject, string body);
}
