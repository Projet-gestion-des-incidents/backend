using Microsoft.AspNetCore.Identity;
using System;

namespace projet0.Domain.Entities
{
    public class ApplicationUser : IdentityUser<Guid>
    { 
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public int Age { get; set; }
<<<<<<< Updated upstream
        public string? Image { get; set; }
=======
        public string? Image { get; set; } // ✅ nullable
>>>>>>> Stashed changes

        public string? Phone { get; set; }
        public string? Role { get; set; }

        public DateTime? BirthDate { get; set; }

    }
}