using AppInsightExceptionsMailer.Mail.Pocos;
using System.Threading.Tasks;

namespace AppInsightExceptionsMailer.Mail.Interfaces
{
    public interface IMailService
    {
        Task SendEmailAsync(MailRequest mailRequest);
    }
}
