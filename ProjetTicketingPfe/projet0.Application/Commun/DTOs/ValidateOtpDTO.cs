using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs
{
    public class ValidateOtpDTO
    {
        public string Email { get; set; } = null!;
        public string Code { get; set; } = null!;
    }

}
