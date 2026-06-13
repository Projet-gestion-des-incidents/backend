using System.ComponentModel.DataAnnotations;

namespace projet0.Application.Commun.DTOs
{
    public class ConfirmEmailChangeDto
    {
        [Required(ErrorMessage = "Le nouvel email est requis")]
        [EmailAddress(ErrorMessage = "Format d'email invalide")]
        public string NewEmail { get; set; }

        [Required(ErrorMessage = "Le code OTP est requis")]
        [MinLength(6, ErrorMessage = "Le code OTP doit contenir 6 chiffres")]
        [MaxLength(6, ErrorMessage = "Le code OTP doit contenir 6 chiffres")]
        public string OtpCode { get; set; }
    }
}
