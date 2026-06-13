namespace projet0.Application.Commun.DTOs.TicketDTOs
{
    // Dashboard pour technicien
    public class TicketTechnicienDashboardDTO
    {
        public TicketTechnicienOverviewDTO Overview { get; set; }
        public List<TicketStatutStatDTO> StatsParStatut { get; set; }
        public List<TicketJournalierDTO> StatsParJour { get; set; }
        public List<TicketJournalierDTO> StatsParSemaine { get; set; }
        public List<TicketJournalierDTO> StatsParMois { get; set; }
        public List<TicketTechnicienResolutionDTO> StatsResolution { get; set; }
    }

    public class TicketTechnicienOverviewDTO
    {
        public int TotalTickets { get; set; }
        public int TicketsAssignes { get; set; }      // Statut = Assigné
        public int TicketsEnCours { get; set; }       // Statut = En cours
        public int TicketsResolus { get; set; }       // Statut = Résolu
        public int TicketsNonArchive { get; set; }    // Non archivés
        public int TicketsArchives { get; set; }      // Archivés

        public double TauxAssignes { get; set; }
        public double TauxEnCours { get; set; }
        public double TauxResolus { get; set; }
        public double TauxResolution { get; set; }
    }

    public class TicketTechnicienResolutionDTO
    {
        public string Periode { get; set; }  // "Jour", "Semaine", "Mois"
        public DateTime Date { get; set; }
        public int TicketsResolus { get; set; }
        public double TempsMoyenResolutionHeures { get; set; }
        public double TempsMoyenResolutionJours { get; set; }
    }
}
