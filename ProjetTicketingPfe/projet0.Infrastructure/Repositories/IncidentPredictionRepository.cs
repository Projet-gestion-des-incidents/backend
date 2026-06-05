// projet0.Infrastructure/Repositories/IncidentPredictionRepository.cs
//
// CORRECTION PRINCIPALE :
//   ModelOutput utilisait "ConfidenceLowerBound" / "ConfidenceUpperBound"
//   mais ForecastBySsa déclare les colonnes "LowerBound" / "UpperBound".
//   Le nom de la propriété C# doit correspondre EXACTEMENT au columnName déclaré.
//
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using projet0.Application.Commun.DTOs.IncidentDTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
using projet0.Domain.Enums;
using projet0.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace projet0.Infrastructure.Repositories
{
    public class IncidentPredictionRepository : IIncidentPredictionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<IncidentPredictionRepository> _logger;
        private readonly MLContext _mlContext;

        // Seuil minimum de points non-nuls pour tenter une prédiction.
        // En dessous, on retourne 0 (évite SSA sur séries quasi-plates).
        private const int MinNonZeroPoints = 5;

        public IncidentPredictionRepository(
            ApplicationDbContext context,
            ILogger<IncidentPredictionRepository> logger)
        {
            _context = context;
            _logger = logger;
            _mlContext = new MLContext(seed: 42);
        }

        // ------------------------------------------------------------------ //
        //  GetHistoricalDataAsync                                              //
        // ------------------------------------------------------------------ //
        public async Task<ApiResponse<List<DailyIncidentCountDTO>>> GetHistoricalDataAsync(
            int monthsBack = 4)
        {
            try
            {
                var startDate = DateTime.Today.AddMonths(-monthsBack);

                var incidents = await _context.Incidents
                    .Where(i => i.DateDetection >= startDate)
                    .ToListAsync();

                var dailyCounts = incidents
                    .GroupBy(i => i.DateDetection.Date)
                    .Select(g => new DailyIncidentCountDTO
                    {
                        Date = g.Key,
                        TotalIncidents = g.Count(),
                        PaiementRefuse = g.Count(i => i.TypeProbleme == TypeProbleme.PaiementRefuse),
                        TerminalHorsLigne = g.Count(i => i.TypeProbleme == TypeProbleme.TerminalHorsLigne),
                        Lenteur = g.Count(i => i.TypeProbleme == TypeProbleme.Lenteur),
                        BugAffichage = g.Count(i => i.TypeProbleme == TypeProbleme.BugAffichage),
                        ConnexionReseau = g.Count(i => i.TypeProbleme == TypeProbleme.ConnexionReseau),
                        ErreurFluxTransactionnel = g.Count(i => i.TypeProbleme == TypeProbleme.ErreurFluxTransactionnel),
                        ProblemeLogicielTPE = g.Count(i => i.TypeProbleme == TypeProbleme.ProblemeLogicielTPE),
                        Autre = g.Count(i => i.TypeProbleme == TypeProbleme.Autre)
                    })
                    .OrderBy(d => d.Date)
                    .ToList();

                // Remplir tous les jours (y compris jours sans incident = 0)
                var allDates = new List<DailyIncidentCountDTO>();
                for (var date = startDate; date <= DateTime.Today; date = date.AddDays(1))
                {
                    var existing = dailyCounts.FirstOrDefault(d => d.Date == date);
                    allDates.Add(existing ?? new DailyIncidentCountDTO { Date = date });
                }

                return ApiResponse<List<DailyIncidentCountDTO>>.Success(allDates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des données historiques");
                return ApiResponse<List<DailyIncidentCountDTO>>.Failure(
                    "Erreur lors de la récupération des données");
            }
        }

        // ------------------------------------------------------------------ //
        //  PredictNextWeekAndMonthAsync                                        //
        // ------------------------------------------------------------------ //
        public async Task<ApiResponse<IncidentPredictionResponseDTO>> PredictNextWeekAndMonthAsync()
        {
            try
            {
                var historicalResponse = await GetHistoricalDataAsync(4);
                if (!historicalResponse.IsSuccess || historicalResponse.Data == null)
                    return ApiResponse<IncidentPredictionResponseDTO>.Failure(
                        "Impossible de récupérer les données historiques");

                var historicalData = historicalResponse.Data;

                var predictions = new IncidentPredictionResponseDTO
                {
                    PredictionSemaine = new List<PredictionResultDTO>(),
                    PredictionMois = new List<PredictionResultDTO>(),
                    DateGeneration = DateTime.Now
                };

                var types = new[]
                {
                    "TotalIncidents", "PaiementRefuse", "TerminalHorsLigne", "Lenteur",
                    "BugAffichage",   "ConnexionReseau", "ErreurFluxTransactionnel",
                    "ProblemeLogicielTPE", "Autre"
                };

                foreach (var type in types)
                {
                    var typePredictions = PredictForType(historicalData, type);
                    predictions.PredictionSemaine.AddRange(
                        typePredictions.Where(p => p.Periode == "Semaine"));
                    predictions.PredictionMois.AddRange(
                        typePredictions.Where(p => p.Periode == "Mois"));
                }

                predictions.PredictionSemaine = MergePredictionsByDate(predictions.PredictionSemaine);
                predictions.PredictionMois = MergePredictionsByDate(predictions.PredictionMois);

                return ApiResponse<IncidentPredictionResponseDTO>.Success(predictions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la prédiction");
                return ApiResponse<IncidentPredictionResponseDTO>.Failure(
                    "Erreur lors de la génération des prédictions");
            }
        }

        // ------------------------------------------------------------------ //
        //  PredictForType  — méthode principale                               //
        // ------------------------------------------------------------------ //
        private List<PredictionResultDTO> PredictForType(
            List<DailyIncidentCountDTO> historicalData, string type)
        {
            var results = new List<PredictionResultDTO>();

            var values = historicalData
                .Select(d => (float)GetValueByType(d, type))
                .ToArray();

            // Garde-fou 1 : pas assez de points total
            if (values.Length < 14)
            {
                _logger.LogWarning(
                    "Pas assez de données pour {Type}: {Count} jours (minimum 14)", type, values.Length);
                return results;
            }

            // Garde-fou 2 : série trop éparse (signal insuffisant pour SSA)
            int nonZeroCount = values.Count(v => v > 0);
            if (nonZeroCount < MinNonZeroPoints)
            {
                _logger.LogWarning(
                    "Signal insuffisant pour {Type}: seulement {NonZero} jours avec incidents", type, nonZeroCount);
                return BuildZeroPredictions(historicalData.Last().Date, type);
            }

            try
            {
                // Préparation du DataView
                var inputData = historicalData
                    .Select((d, i) => new ModelInput
                    {
                        Index = i,
                        Value = (float)GetValueByType(d, type)
                    })
                    .ToList();

                var dataView = _mlContext.Data.LoadFromEnumerable(inputData);

                // windowSize = 7 → pattern hebdomadaire
                // seriesLength = toute la série disponible
                // horizon = 30 → prédit 30 jours (couvre semaine ET mois)
                int seriesLen = values.Length;
                int windowSize = Math.Min(7, seriesLen / 4);   // sécurité si série courte

                var forecastingPipeline = _mlContext.Forecasting.ForecastBySsa(
                    outputColumnName: "Forecast",
                    inputColumnName: "Value",
                    windowSize: windowSize,
                    seriesLength: seriesLen,
                    trainSize: seriesLen,
                    horizon: 30,
                    confidenceLevel: 0.95f,
                    confidenceLowerBoundColumn: "LowerBound",   // ← nom réel de la colonne ML.NET
                    confidenceUpperBoundColumn: "UpperBound");  // ← nom réel de la colonne ML.NET

                var transformer = forecastingPipeline.Fit(dataView);

                // *** CORRECTION DU BUG ***
                // CreateTimeSeriesEngine<TIn, TOut> mappe les colonnes par nom.
                // ModelOutput DOIT avoir des propriétés nommées exactement
                // "Forecast", "LowerBound", "UpperBound".
                var forecastEngine = transformer.CreateTimeSeriesEngine<ModelInput, ModelOutput>(_mlContext);
                var forecast = forecastEngine.Predict();

                var lastDate = historicalData.Last().Date;

                // --- Prédictions jours 1-7 (Semaine) ---
                for (int i = 1; i <= 7 && i - 1 < forecast.Forecast.Length; i++)
                {
                    int idx = i - 1;
                    results.Add(new PredictionResultDTO
                    {
                        Date = lastDate.AddDays(i),
                        Periode = "Semaine",
                        TotalIncidents = Math.Max(0, (int)Math.Round(forecast.Forecast[idx])),
                        IncidentsParType = new Dictionary<string, int>
                        {
                            { type, Math.Max(0, (int)Math.Round(forecast.Forecast[idx])) }
                        },
                        ConfidenceLower = Math.Max(0, forecast.LowerBound?[idx] ?? 0),
                        ConfidenceUpper = Math.Max(0, forecast.UpperBound?[idx] ?? 0)
                    });
                }

                // --- Prédictions semaines 1-4 (Mois) : total de la semaine ---
                for (int week = 0; week < 4; week++)
                {
                    var weekStart = lastDate.AddDays(week * 7 + 1);
                    double weekTotal = 0;
                    double lowerTotal = 0;
                    double upperTotal = 0;
                    int validDays = 0;

                    for (int day = 0; day < 7; day++)
                    {
                        int idx = week * 7 + day;
                        if (idx < forecast.Forecast.Length)
                        {
                            weekTotal += Math.Max(0, forecast.Forecast[idx]);
                            lowerTotal += Math.Max(0, forecast.LowerBound?[idx] ?? 0);
                            upperTotal += Math.Max(0, forecast.UpperBound?[idx] ?? 0);
                            validDays++;
                        }
                    }

                    results.Add(new PredictionResultDTO
                    {
                        Date = weekStart,
                        Periode = "Mois",
                        // TotalIncidents = total de la semaine (pas la moyenne)
                        TotalIncidents = (int)Math.Round(weekTotal),
                        IncidentsParType = new Dictionary<string, int>
                                {
                                    { type, (int)Math.Round(weekTotal) }
                                },
                        ConfidenceLower = Math.Round(lowerTotal, 1),
                        ConfidenceUpper = Math.Round(upperTotal, 1)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erreur prédiction pour {Type}", type);
            }

            return results;
        }

        // ------------------------------------------------------------------ //
        //  Helpers                                                             //
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Retourne des prédictions à zéro pour les types avec signal insuffisant.
        /// </summary>
        private List<PredictionResultDTO> BuildZeroPredictions(DateTime lastDate, string type)
        {
            var results = new List<PredictionResultDTO>();
            for (int i = 1; i <= 7; i++)
            {
                results.Add(new PredictionResultDTO
                {
                    Date = lastDate.AddDays(i),
                    Periode = "Semaine",
                    TotalIncidents = 0,
                    IncidentsParType = new Dictionary<string, int> { { type, 0 } },
                    ConfidenceLower = 0,
                    ConfidenceUpper = 0
                });
            }
            for (int week = 0; week < 4; week++)
            {
                results.Add(new PredictionResultDTO
                {
                    Date = lastDate.AddDays(week * 7 + 1),
                    Periode = "Mois",
                    TotalIncidents = 0,
                    IncidentsParType = new Dictionary<string, int> { { type, 0 } },
                    ConfidenceLower = 0,
                    ConfidenceUpper = 0
                });
            }
            return results;
        }

        private double GetValueByType(DailyIncidentCountDTO data, string type) => type switch
        {
            "TotalIncidents" => data.TotalIncidents,
            "PaiementRefuse" => data.PaiementRefuse,
            "TerminalHorsLigne" => data.TerminalHorsLigne,
            "Lenteur" => data.Lenteur,
            "BugAffichage" => data.BugAffichage,
            "ConnexionReseau" => data.ConnexionReseau,
            "ErreurFluxTransactionnel" => data.ErreurFluxTransactionnel,
            "ProblemeLogicielTPE" => data.ProblemeLogicielTPE,
            "Autre" => data.Autre,
            _ => 0
        };

        private List<PredictionResultDTO> MergePredictionsByDate(
            List<PredictionResultDTO> predictions)
        {
            return predictions
                .GroupBy(p => p.Date)
                .Select(g =>
                {
                    var mergedTypes = g
                        .SelectMany(p => p.IncidentsParType)
                        .GroupBy(kv => kv.Key)
                        .ToDictionary(g2 => g2.Key, g2 => g2.Sum(kv => kv.Value));

                    // ✅ Total = somme des types individuels UNIQUEMENT (exclut "TotalIncidents")
                    int totalReel = mergedTypes
                        .Where(kv => kv.Key != "TotalIncidents")
                        .Sum(kv => kv.Value);

                    return new PredictionResultDTO
                    {
                        Date = g.Key,
                        Periode = g.First().Periode,
                        TotalIncidents = totalReel,
                        IncidentsParType = mergedTypes,
                        ConfidenceLower = Math.Round(g
                            .Where(p => p.IncidentsParType.ContainsKey("TotalIncidents"))
                            .Sum(p => p.ConfidenceLower), 1),
                        ConfidenceUpper = Math.Round(g
                            .Where(p => p.IncidentsParType.ContainsKey("TotalIncidents"))
                            .Sum(p => p.ConfidenceUpper), 1)
                    };
                })
                .OrderBy(p => p.Date)
                .ToList();
        }

        // ------------------------------------------------------------------ //
        //  Classes internes ML.NET                                             //
        // ------------------------------------------------------------------ //
        private class ModelInput
        {
            public int Index { get; set; }
            public float Value { get; set; }
        }

        /// <summary>
        /// CORRECTION : les noms des propriétés doivent correspondre EXACTEMENT
        /// aux columnNames déclarés dans ForecastBySsa :
        ///   outputColumnName              → "Forecast"
        ///   confidenceLowerBoundColumn    → "LowerBound"
        ///   confidenceUpperBoundColumn    → "UpperBound"
        ///
        /// L'ancienne version utilisait "ConfidenceLowerBound" / "ConfidenceUpperBound"
        /// ce qui causait :
        ///   ArgumentOutOfRangeException: Could not find column 'ConfidenceLowerBound'
        /// </summary>
        private class ModelOutput
        {
            public float[]? Forecast { get; set; } = Array.Empty<float>();
            public float[]? LowerBound { get; set; }  // ← était "ConfidenceLowerBound"
            public float[]? UpperBound { get; set; }  // ← était "ConfidenceUpperBound"
        }
    }
}