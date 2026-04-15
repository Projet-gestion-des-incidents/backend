using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace projet0.Application.Commun.DTOs
{
    public class ConfirmPasswordChangeDto
    {
        [Required(ErrorMessage = "Le nouveau mot de passe est requis")]
        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Le code OTP est requis")]
        [MinLength(6, ErrorMessage = "Le code OTP doit contenir 6 chiffres")]
        [MaxLength(6, ErrorMessage = "Le code OTP doit contenir 6 chiffres")]
        public string OtpCode { get; set; }
    }
}
