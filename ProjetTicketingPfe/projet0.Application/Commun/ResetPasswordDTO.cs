using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun
{
    public class ResetPasswordDTO
    {
        public string Email { get; set; }
        public string OtpCode { get; set; }
        public string NewPassword { get; set; }
    }
}
