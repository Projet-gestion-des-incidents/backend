using projet0.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace projet0.Application.Commun.DTOs.IncidentDTOs
{
    public class IncidentDashboardOverviewDTO
    {
        public int TotalIncidents { get; set; }
        public int IncidentsNonTraite { get; set; }
        public int IncidentsEnCours { get; set; }
        public int IncidentsFerme { get; set; }
        public int IncidentsNonTraiteLiesTicket { get; set; }     // Non traités + liés à au moins un ticket
        public int IncidentsNonTraiteSansTicket { get; set; }

        // Pourcentages
        public double TauxNonTraite { get; set; }
        public double TauxEnCours { get; set; }
        public double TauxFerme { get; set; }
        public double TauxNonTraiteLiesTicket { get; set; }       // % des non traités liés à un ticket
        public double TauxNonTraiteSansTicket { get; set; }       // % des non traités sans ticket
    }

    // Statistiques par statut 
    public class IncidentStatutStatDTO
    {
        public string Statut { get; set; }  // "Non traité", "En cours", "Fermé"
        public int Count { get; set; }
        public string Color { get; set; }   
        public double Pourcentage { get; set; }
    }

    // Statistiques par jour
    public class IncidentJournalierDTO
    {
        public DateTime Date { get; set; }
        public string DateFormatee => Date.ToString("dd/MM/yyyy");
        public string Jour => Date.ToString("dddd", new System.Globalization.CultureInfo("fr-FR"));

        // Nombre d'incidents créés ce jour
        public int Crees { get; set; }

        // Répartition par statut pour ce jour
        public int NonTraite { get; set; }
        public int EnCours { get; set; }
        public int Ferme { get; set; }
    }

    public class IncidentDashboardDTO
    {
        public IncidentDashboardOverviewDTO Overview { get; set; }
        public List<IncidentStatutStatDTO> StatsParStatut { get; set; }
        public List<IncidentJournalierDTO> StatsParJour { get; set; }
        public List<IncidentJournalierDTO> StatsParSemaine { get; set; }
        public List<IncidentJournalierDTO> StatsParMois { get; set; }

        //  Statistiques de résolution globales
        public ResolutionIncidentStatsDTO StatsResolution { get; set; }

        //  Temps moyen par sévérité
        public List<ResolutionParSeveriteDTO> ResolutionParSeverite { get; set; }

        //  Temps moyen par type de problème 
        public List<ResolutionParTypeProblemeDTO> ResolutionParTypeProbleme { get; set; }
    }

    // Statistiques de résolution globales
    public class ResolutionIncidentStatsDTO
    {
        public double TempsMoyenResolutionHeures { get; set; }
        public double TempsMoyenResolutionJours { get; set; }
        public int IncidentsResolus { get; set; }           // Nombre d'incidents résolus
        public int IncidentsNonResolus { get; set; }        // Incidents non encore résolus
        public double TauxResolution { get; set; }          // (Resolus / Total) × 100
    }

    // Statistiques de résolution par sévérité
    public class ResolutionParSeveriteDTO
    {
        public string Severite { get; set; }                // "Non définie", "Faible", "Moyenne", "Forte"
        public int NombreIncidents { get; set; }            // Nombre d'incidents avec cette sévérité
        public int NombreResolus { get; set; }              // Dont combien sont résolus
        public double TempsMoyenResolutionHeures { get; set; }
        public double TempsMoyenResolutionJours { get; set; }
        public double TauxResolution { get; set; }          // (Resolus / NombreIncidents) × 100
        public string Color { get; set; }                   // Pour le graphique
    }

    // Statistiques de résolution par type de problème
    public class ResolutionParTypeProblemeDTO
    {
        public string TypeProbleme { get; set; }            // Libellé du type
        public TypeProbleme TypeProblemeEnum { get; set; }  // Valeur enum
        public int NombreIncidents { get; set; }            // Nombre d'incidents de ce type
        public int NombreResolus { get; set; }              // Dont combien sont résolus
        public double TempsMoyenResolutionHeures { get; set; }
        public double TempsMoyenResolutionJours { get; set; }
        public double TauxResolution { get; set; }          // (Resolus / NombreIncidents) × 100
        public double PourcentageTotal { get; set; }        // (NombreIncidents / TotalIncidents) × 100
        public string Color { get; set; }                   // Pour le graphique
    }

}
