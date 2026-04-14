using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace projet0.Application.Commun
{
    public class ResetPasswordDTO
    {
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Le code OTP est requis")]
        [MinLength(6, ErrorMessage = "Le code OTP doit contenir 6 chiffres")]
        [MaxLength(6, ErrorMessage = "Le code OTP doit contenir 6 chiffres")]
        public string OtpCode { get; set; }

        [Required(ErrorMessage = "Le nouveau mot de passe est requis")]
        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
        [Compare("NewPassword", ErrorMessage = "Les mots de passe ne correspondent pas")]
        public string ConfirmPassword { get; set; }
    }
}