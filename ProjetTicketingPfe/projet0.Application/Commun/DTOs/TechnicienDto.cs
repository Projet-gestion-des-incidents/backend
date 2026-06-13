using projet0.Domain.Entities;

namespace projet0.Application.Commun.DTOs
{
    public class TechnicienDto
    {
        public Guid Id { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Email { get; set; }
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Image { get; set; }
        public DateTime? BirthDate { get; set; }
        public UserStatut Statut { get; set; }
        public bool EmailConfirmed { get; set; }
        public string NomComplet => $"{Nom} {Prenom}";
    }
}
