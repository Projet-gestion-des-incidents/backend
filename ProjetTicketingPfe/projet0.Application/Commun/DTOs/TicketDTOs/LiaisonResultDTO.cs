namespace projet0.Application.Commun.DTOs.TicketDTOs
{
    public class LiaisonResultDTO
    {
        public int LiensAjoutes { get; set; }
        public int LiensDejaExistants { get; set; }
        public int IncidentsNonTrouves { get; set; }
        public int TotalDemande { get; set; }
        public List<string> Details { get; set; } = new();
    }
}
