namespace projet0.Domain.Entities
{
    public class PieceJointe
    {
        public Guid Id { get; set; }
        public string NomFichier { get; set; }
        public string? ContentType { get; set; }
        public DateTime DateAjout { get; set; }

        public Guid UploadedById { get; set; }

        // Navigation Properties
        public virtual CommentaireTicket Commentaire { get; set; }
        public virtual ApplicationUser UploadedBy { get; set; }
        public virtual Incident Incident { get; set; }


        // Foreign Keys 
        public Guid? IncidentId { get; set; }
        public Guid? CommentaireId { get; set; }



    }
}
