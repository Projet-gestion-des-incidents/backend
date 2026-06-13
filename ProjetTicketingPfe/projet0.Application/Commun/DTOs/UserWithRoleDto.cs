using projet0.Domain.Entities;

namespace projet0.Application.Commun.DTOs
{
    public class UserWithRoleDto
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } 
        public string Email { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string PhoneNumber { get; set; }
        public string Image { get; set; } 
        public string Role { get; set; }
        public Guid? RoleId { get; set; }
        public UserStatut Statut { get; set; } = UserStatut.Actif;
        public DateTime? BirthDate { get; set; }
        public string? Adresse { get; set; }

    }
}
