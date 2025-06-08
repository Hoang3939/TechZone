using System.Threading.Tasks;

namespace ShopDienTu.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string message);
    }
}