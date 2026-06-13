using projet0.Domain.Entities;

namespace projet0.Application.Commun.DTOs
{
    public class CommercantDto
    {
        public Guid Id { get; set; }
        public string NomMagasin { get; set; }      // UserName
        public string Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Adresse { get; set; }
        public string? Image { get; set; }
        public UserStatut Statut { get; set; }
        public bool EmailConfirmed { get; set; }
    }
}
