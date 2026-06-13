namespace projet0.Application.Commun.DTOs.TicketDTOs
{
    // Statistiques globales des tickets
    public class TicketDashboardOverviewDTO
    {
        public int TotalTickets { get; set; }
        public int TicketsNonAssigne { get; set; }
        public int TicketsAssignes { get; set; }
        public int TicketsEnCours { get; set; }
        public int TicketsResolus { get; set; }

        // Pourcentages
        public double TauxNonAssigne { get; set; }
        public double TauxAssignes { get; set; }
        public double TauxEnCours { get; set; }
        public double TauxResolus { get; set; }

        // Taux de résolution global
        public double TauxResolutionGlobal { get; set; }  // (Résolus / Total) * 100
    }

    // Statistiques par statut (pour graphique camembert)
    public class TicketStatutStatDTO
    {
        public string Statut { get; set; }  // "Non assigné", "Assigné", "En cours", "Résolu"
        public int Count { get; set; }
        public string Color { get; set; }
        public double Pourcentage { get; set; }
    }

    // Statistiques par jour
    public class TicketJournalierDTO
    {
        public DateTime Date { get; set; }
        public string DateFormatee => Date.ToString("dd/MM/yyyy");
        public string Jour => Date.ToString("dddd", new System.Globalization.CultureInfo("fr-FR"));
        public int Crees { get; set; }
        public int NonAssigne { get; set; }
        public int Assignes { get; set; }
        public int EnCours { get; set; }
        public int Resolus { get; set; }
    }

    // Dashboard complet des tickets
    public class TicketDashboardDTO
    {
        public TicketDashboardOverviewDTO Overview { get; set; }
        public List<TicketStatutStatDTO> StatsParStatut { get; set; }
        public List<TicketJournalierDTO> StatsParJour { get; set; }
        public List<TicketJournalierDTO> StatsParSemaine { get; set; }
        public List<TicketJournalierDTO> StatsParMois { get; set; }

        // Top techniciens
        public List<TopTechnicienDTO> TopTechniciens { get; set; }

        //  Statistiques de résolution globales
        public ResolutionStatsDTO StatsResolution { get; set; }

        //  Résolution par période
        public List<ResolutionParPeriodeDTO> ResolutionParJour { get; set; }
        public List<ResolutionParPeriodeDTO> ResolutionParSemaine { get; set; }
        public List<ResolutionParPeriodeDTO> ResolutionParMois { get; set; }

        //  Statistiques d'assignation par technicien
        public List<TechnicienAssignationDTO> AssignationParTechnicien { get; set; }

        //  Statistiques globales d'assignation
        public AssignationGlobaleDTO AssignationGlobale { get; set; }
    }

    // Top techniciens
    public class TopTechnicienDTO
    {
        public Guid TechnicienId { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string NomComplet => $"{Nom} {Prenom}";
        public int TicketsResolus { get; set; }
        public int TicketsEnCours { get; set; }
        public int TicketsAssignes { get; set; }      //  tickets avec statut "Assigné"
        public int TicketsTotal { get; set; }         //  somme de tous les tickets (Assignés + En cours + Résolus)
        public double TauxResolution { get; set; }
    }

    // Statistiques de résolution
    public class ResolutionStatsDTO
    {
        public double TempsMoyenResolutionHeures { get; set; }
        public double TempsMoyenResolutionJours { get; set; }
        public int TicketsResolusAvantDelai { get; set; }
        public int TicketsResolusApresDelai { get; set; }
        public int TicketsSansDateLimite { get; set; }
        public double TauxRespectDelai { get; set; }  // (AvantDelai / TotalAvecDelai) × 100
    }

    // Statistiques de résolution par période
    public class ResolutionParPeriodeDTO
    {
        public DateTime Date { get; set; }
        public string DateFormatee => Date.ToString("dd/MM/yyyy");

        // Temps moyen de résolution
        public double TempsMoyenResolutionHeures { get; set; }
        public double TempsMoyenResolutionJours { get; set; }

        // Respect des délais
        public int ResolusAvantDelai { get; set; }
        public int ResolusApresDelai { get; set; }
        public int TotalResolusPeriode { get; set; }
        public double TauxRespectDelai { get; set; }
    }

    // Statistiques d'assignation par technicien
    public class TechnicienAssignationDTO
    {
        public Guid TechnicienId { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string NomComplet => $"{Nom} {Prenom}";

        // Tickets assignés à ce technicien
        public int TicketsAssignes { get; set; }
        public int TicketsEnCours { get; set; }
        public int TicketsResolus { get; set; }
        public int TicketsResolusAvantDateLimite { get; set; }
        public int TicketsResolusApresDateLimite { get; set; }
        public int TotalTicketsTechnicien { get; set; }

        // Pourcentages
        public double PourcentageAssignation { get; set; }  // (TotalTechnicien / TotalTicketsAssignesGlobaux) × 100
        public double TauxResolution { get; set; }          // (Resolus / TotalTechnicien) × 100
        public double TauxRespectDelai { get; set; }        //  (AvantDelai / (AvantDelai + ApresDelai)) × 100

    }

    // Statistiques globales d'assignation
    public class AssignationGlobaleDTO
    {
        public int TotalTicketsAvecAssignation { get; set; }  // Tickets avec AssigneeId != null
        public int TotalTicketsSansAssignation { get; set; }  // Tickets avec AssigneeId == null
        
        // Pourcentages
        public double TauxAssignation { get; set; }  // (AvecAssignation / TotalTickets) × 100
    }
}
