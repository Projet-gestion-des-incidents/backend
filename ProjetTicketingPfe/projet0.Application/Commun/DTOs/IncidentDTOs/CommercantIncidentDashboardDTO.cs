

namespace projet0.Application.Commun.DTOs.IncidentDTOs
{

    public class CommercantIncidentDashboardDTO
    {
        public CommercantIncidentOverviewDTO Overview { get; set; }
        public List<IncidentStatutStatDTO> StatsParStatut { get; set; }
        public List<IncidentJournalierDTO> StatsParJour { get; set; }
        public List<IncidentJournalierDTO> StatsParSemaine { get; set; }
        public List<IncidentJournalierDTO> StatsParMois { get; set; }
        public List<ResolutionParTypeProblemeDTO> ResolutionParTypeProbleme { get; set; }
    }

    public class CommercantIncidentOverviewDTO
    {
        public int TotalIncidents { get; set; }
        public int IncidentsNonTraite { get; set; }
        public int IncidentsEnCours { get; set; }
        public int IncidentsFerme { get; set; }
        public int IncidentsArchives { get; set; }
        public int IncidentsNonArchive { get; set; }

        public double TauxNonTraite { get; set; }
        public double TauxEnCours { get; set; }
        public double TauxFerme { get; set; }
        public double TauxResolution { get; set; }
    }
}
