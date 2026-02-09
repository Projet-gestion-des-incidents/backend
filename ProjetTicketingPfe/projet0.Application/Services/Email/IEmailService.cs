using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Services.Email
{
    public interface IEmailService
    {
        Task SendAsync(string to, string subject, string body);

    }

}
