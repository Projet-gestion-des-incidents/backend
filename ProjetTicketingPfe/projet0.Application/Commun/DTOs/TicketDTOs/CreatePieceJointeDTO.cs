using Microsoft.AspNetCore.Http;

namespace projet0.Application.Commun.DTOs.Ticket
{
    public class CreatePieceJointeDTO
    {
        public string NomFichier { get; set; }
               
        public string ContenuBase64 { get; set; } // Pour les fichiers encodés en base64

        public IFormFile? Fichier { get; set; }    // Pour l'upload de fichiers
    }
}
