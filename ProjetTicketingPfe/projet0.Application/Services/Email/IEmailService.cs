using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Services.Email
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);
        Task SendWelcomeEmailAsync(string to, string nom, string prenom, string defaultPassword);
        Task SendPasswordChangeConfirmationAsync(string email);  // ✅ AJOUTER
    }

}
