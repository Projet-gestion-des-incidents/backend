using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
using projet0.Application.Services.Email;
using projet0.Application.Services.Otp;
using projet0.Domain.Entities;
using projet0.Domain.Enums;
using System.Data;
using System.Diagnostics;


namespace projet0.Application.Services.User
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHostEnvironment _webHostEnvironment;
        private readonly IEmailService _emailService;
        private readonly IOtpService _otpService;
        public UserService(IUserRepository userRepository, ILogger<UserService> logger,UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager, IHostEnvironment webHostEnvironment, IEmailService emailService, IOtpService otpService)
        {
            _userRepository = userRepository;
            _logger = logger;
            _roleManager = roleManager;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment;
            _emailService = emailService;
            _otpService = otpService;  

        }

        // ================= HELPER STOPWATCH =================
        private async Task<T> MeasureAsync<T>(
            string actionName,
            object input,
            Func<Task<T>> action)
        {
            var sw = Stopwatch.StartNew();

            _logger.LogDebug(
                "START {Action} | Input = {@Input}",
                actionName,
                input
            );

            try
            {
                var result = await action();

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "ERROR {Action} | Input = {@Input}",
                    actionName,
                    input
                );
                throw;
            }
            finally
            {
                sw.Stop();

                if (sw.ElapsedMilliseconds > 1000) // seuil configurable
                {
                    _logger.LogWarning(
                        "SLOW {Action} | {Elapsed} ms | Input = {@Input}",
                        actionName,
                        sw.ElapsedMilliseconds,
                        input
                    );
                }
                else
                {
                    _logger.LogDebug(
                        "END {Action} | {Elapsed} ms",
                        actionName,
                        sw.ElapsedMilliseconds
                    );
                }
            }
        }

        // ================= GET ALL =================
        public Task<IEnumerable<ApplicationUser>> GetAllAsync()
            => MeasureAsync(
                actionName: "GetAllUsers",
                input: null, // pas de paramètre
                async () =>
                {
                    var users = await _userRepository.GetAllAsync();

                    _logger.LogDebug(
                        "SUCCESS GetAllUsers | Count = {Count}",
                        users?.Count() ?? 0
                    );

                    return users;
                }
            );

        // ================= GET BY ID =================
        public Task<ApplicationUser> GetByIdAsync(Guid id)
            => MeasureAsync(
                actionName: "GetUserById",
                input: new { UserId = id },
                async () =>
                {
                    var user = await _userRepository.GetByIdAsync(id);

                    if (user == null)
                    {
                        _logger.LogWarning(
                            "NOT_FOUND GetUserById | UserId = {UserId}",
                            id
                        );
                    }
                    else
                    {
                        _logger.LogDebug(
                            "SUCCESS GetUserById | UserId = {UserId} | UserName = {UserName}",
                            user.Id,
                            user.UserName
                        );
                    }

                    return user;
                }
            );   
        

        // ================= UPDATE =================
        public Task<ApiResponse<ApplicationUser>> UpdateAsync(Guid id, UserDto dto)
            => MeasureAsync(
                "UpdateUser",
                new { id, dto },
                async () =>
                {
                    var user = await _userRepository.GetByIdAsync(id);

                    if (user == null)
                    {
                        _logger.LogWarning(
                            "NOT_FOUND UpdateUser | UserId = {UserId}",
                            id
                        );

                        return ApiResponse<ApplicationUser>.Failure(
                          message: UserMessages.UserNotFound,
                          resultCode: 20);
                    }

                    user.UserName = dto.UserName;
                    user.Email = dto.Email;
                    user.Nom = dto.Nom;
                    user.Prenom = dto.Prenom;
                    user.PhoneNumber = dto.PhoneNumber;
                    user.Image = dto.Image;
                    user.Adresse = dto.Adresse; 

                    var result = await _userRepository.UpdateAsync(user);

                    if (!result.Succeeded)
                    {
                        _logger.LogError(
                            "DB_ERROR UpdateUser | UserId = {UserId} | {@Errors}",
                            id,
                            result.Errors
                        );

                        return ApiResponse<ApplicationUser>.Failure(
                        message: UserMessages.UpdateUserError,
                        resultCode: 21);
                    }
                    return ApiResponse<ApplicationUser>.Success(
                                   data: user,
                                   message: "Utilisateur mis à jour avec succès",
                                   resultCode: 0);
                });


        // ================= DESACTIVATE (par admin) =================
        public Task<ApiResponse<string>> DesactivateAsync(Guid id)
            => MeasureAsync("DesactivateUser", new { UserId = id }, async () =>
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("NOT_FOUND DesactivateUser | UserId = {UserId}", id);
                    return ApiResponse<string>.Failure(
                        message: UserMessages.UserNotFound,
                        resultCode: 20);
                }

                // Vérifier si l'utilisateur est déjà désactivé
                var isLockedOut = await _userManager.IsLockedOutAsync(user);
                if (isLockedOut)
                {
                    _logger.LogWarning("ALREADY_DESACTIVATED DesactivateUser | UserId = {UserId}", id);
                    return ApiResponse<string>.Failure(
                        message: "L'utilisateur est déjà désactivé",
                        resultCode: 23);
                }

                // Désactiver l'utilisateur (lockout permanent)
                var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
                if (!result.Succeeded)
                {
                    _logger.LogError("DB_ERROR DesactivateUser | UserId = {UserId} | {@Errors}", id, result.Errors);
                    return ApiResponse<string>.Failure(
                        message: "Erreur lors de la désactivation de l'utilisateur",
                        resultCode: 22);
                }

                // Mettre à jour le statut
                user.Statut = UserStatut.Inactif;
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation(
                    "SUCCESS DesactivateUser | UserId = {UserId} | UserName = {UserName}",
                    user.Id, user.UserName);

                return ApiResponse<string>.Success(
                    message: "Utilisateur désactivé avec succès",
                    resultCode: 0);
            });


        // ================= ACTIVATE (par admin) =================
        public Task<ApiResponse<string>> ActivateAsync(Guid id)
            => MeasureAsync("ActivateUser", new { UserId = id }, async () =>
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user == null)
                {
                    _logger.LogWarning("NOT_FOUND ActivateUser | UserId = {UserId}", id);
                    return ApiResponse<string>.Failure(
                        message: UserMessages.UserNotFound,
                        resultCode: 20);
                }

                // Vérifier si l'utilisateur est vraiment désactivé
                var isLockedOut = await _userManager.IsLockedOutAsync(user);
                if (!isLockedOut)
                {
                    _logger.LogWarning("ALREADY_ACTIVE ActivateUser | UserId = {UserId}", id);
                    return ApiResponse<string>.Failure(
                        message: "L'utilisateur est déjà actif",
                        resultCode: 24);
                }

                // Réactiver l'utilisateur
                var result = await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);

                if (!result.Succeeded)
                {
                    _logger.LogError("DB_ERROR ActivateUser | UserId = {UserId} | {@Errors}", id, result.Errors);
                    return ApiResponse<string>.Failure(
                        message: "Erreur lors de l'activation de l'utilisateur",
                        resultCode: 22);
                }

                // Mettre à jour le statut
                user.Statut = UserStatut.Actif;
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation(
                    "SUCCESS ActivateUser | UserId = {UserId} | UserName = {UserName}",
                    user.Id, user.UserName);

                return ApiResponse<string>.Success(
                    message: "Utilisateur activé avec succès",
                    resultCode: 0);
            });

        public async Task<ApiResponse<PagedResult<UserWithRoleDto>>> GetAllUsersWithRolesAsync(PagedRequest request)
        {
            return await MeasureAsync(
                actionName: "GetAllUsersWithRoles",
                input: request,
                async () =>
                {
                    try
                    {
                        var pagedResult = await _userRepository.GetAllUsersWithRolesAsync(request);

                        _logger.LogInformation(
                            "SUCCESS GetAllUsersWithRoles | Total: {TotalCount} | Page: {Page}/{TotalPages}",
                            pagedResult.TotalCount,
                            pagedResult.Page,
                            pagedResult.TotalPages
                        );

                        return ApiResponse<PagedResult<UserWithRoleDto>>.Success(
                            data: pagedResult,
                            message: pagedResult.TotalCount > 0
                                ? $"{pagedResult.TotalCount} utilisateur(s) trouvé(s)."
                                : "Aucun utilisateur trouvé.",
                            resultCode: 0
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ERROR GetAllUsersWithRoles | Request = {@Request}", request);
                        return ApiResponse<PagedResult<UserWithRoleDto>>.Failure(
                            message: "Erreur lors de la récupération des utilisateurs",
                            resultCode: 32
                        );
                    }
                });
        }

        public async Task<ApiResponse<ApplicationUser>> EditProfileAsync(Guid userId, EditProfileDto dto)
        {
            return await MeasureAsync("EditProfile", new { userId, dto }, async () =>
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponse<ApplicationUser>.Failure(UserMessages.UserNotFound, resultCode: 20);
                }

                bool hasChanges = false;

                // ============================================
                // CAS 1 : L'utilisateur change son EMAIL
                // ============================================
                if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
                {
                    // Vérifier que le nouvel email n'est pas déjà utilisé
                    if (!await _userRepository.IsEmailUniqueAsync(dto.Email, userId))
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            "Cet email est déjà utilisé par un autre compte",
                            resultCode: 10);
                    }

                    // Envoyer OTP sur le NOUVEL email
                    var otpResult = await _otpService.GenerateAndSendOtpToEmailAsync(
                        user,
                        dto.Email,  // Envoyer au nouvel email
                        OtpPurpose.EmailChange);

                    if (otpResult.ResultCode != 0)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            "Erreur lors de l'envoi du code de vérification",
                            resultCode: 42);
                    }

                    // Stocker temporairement le nouvel email (en mémoire, pas en base)
                    // On va utiliser un cache ou retourner un token
                    var changeEmailToken = Guid.NewGuid().ToString();

                    // Retourner un token spécial pour la validation
                    return ApiResponse<ApplicationUser>.Success(
                        data: null,
                        message: $"Un code OTP a été envoyé à {dto.Email}. Veuillez le valider pour confirmer le changement.",
                        resultCode: 42);  // Code spécial "validation email requise"
                }

                // ============================================
                // CAS 2 : L'utilisateur change son MOT DE PASSE
                // ============================================
                if (!string.IsNullOrEmpty(dto.CurrentPassword) && !string.IsNullOrEmpty(dto.NewPassword))
                {
                    // Vérifier l'ancien mot de passe
                    var passwordValid = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
                    if (!passwordValid)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            "Mot de passe actuel incorrect",
                            resultCode: 25);
                    }

                    // Vérifier la confirmation
                    if (dto.NewPassword != dto.ConfirmPassword)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            "Les nouveaux mots de passe ne correspondent pas",
                            resultCode: 26);
                    }

                    // Vérifier la force du mot de passe
                    if (dto.NewPassword.Length < 6)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            "Le mot de passe doit contenir au moins 6 caractères",
                            resultCode: 43);
                    }

                    // Envoyer OTP sur l'email actuel (comme reset password)
                    var otpResult = await _otpService.GenerateAndSendOtpAsync(
                        user,
                        OtpPurpose.ResetPassword);  // Réutilise ResetPassword

                    if (otpResult.ResultCode != 0)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            "Erreur lors de l'envoi du code de vérification",
                            resultCode: 42);
                    }

                    // Retourner un token pour la validation
                    return ApiResponse<ApplicationUser>.Success(
                        data: null,
                        message: $"Un code OTP a été envoyé à {user.Email}. Veuillez le valider pour changer votre mot de passe.",
                        resultCode: 43);  // Code spécial "validation password requise"
                }

                // ============================================
                // CAS 3 : Autres modifications (Nom, Prénom, Téléphone, Adresse, Image)
                // ============================================

                // Adresse
                if (!string.IsNullOrEmpty(dto.Adresse) && dto.Adresse != user.Adresse)
                {
                    user.Adresse = dto.Adresse;
                    hasChanges = true;
                }

                // Nom
                if (!string.IsNullOrEmpty(dto.Nom) && dto.Nom != user.Nom)
                {
                    user.Nom = dto.Nom;
                    hasChanges = true;
                }

                // Prénom
                if (!string.IsNullOrEmpty(dto.Prenom) && dto.Prenom != user.Prenom)
                {
                    user.Prenom = dto.Prenom;
                    hasChanges = true;
                }

                // Téléphone
                if (!string.IsNullOrEmpty(dto.PhoneNumber) && dto.PhoneNumber != user.PhoneNumber)
                {
                    // Vérifier l'unicité du téléphone
                    var existingPhone = await _userManager.Users
                        .FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber && u.Id != userId);
                    if (existingPhone != null)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            "Ce numéro de téléphone est déjà utilisé",
                            resultCode: 12);
                    }
                    user.PhoneNumber = dto.PhoneNumber;
                    hasChanges = true;
                }

                // Date de naissance
                if (dto.BirthDate.HasValue && dto.BirthDate != user.BirthDate)
                {
                    var age = DateTime.Today.Year - dto.BirthDate.Value.Year;
                    if (age < 18)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            "Vous devez avoir au moins 18 ans",
                            resultCode: 40);
                    }
                    user.BirthDate = dto.BirthDate;
                    hasChanges = true;
                }

                // Image
                if (!string.IsNullOrEmpty(dto.Image) && dto.Image != user.Image)
                {
                    user.Image = dto.Image;
                    hasChanges = true;
                }

                // Sauvegarder
                if (hasChanges)
                {
                    var result = await _userRepository.UpdateAsync(user);
                    if (!result.Succeeded)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            UserMessages.UpdateUserError,
                            resultCode: 21);
                    }
                }

                string message = hasChanges ? "Profil mis à jour avec succès" : "Aucune modification détectée";
                return ApiResponse<ApplicationUser>.Success(user, message, 0);
            });
        }

        // Méthode pour supprimer l'ancienne image
        //private async Task DeleteOldImageAsync(string imageUrl)
        //{
        //    try
        //    {
        //        if (string.IsNullOrEmpty(imageUrl) || imageUrl.Contains("default-avatar"))
        //            return;

        //        var webRootPath = _webHostEnvironment.ContentRootPath;
        //        var imagePath = Path.Combine(webRootPath, imageUrl.TrimStart('/'));

        //        if (File.Exists(imagePath))
        //        {
        //            File.Delete(imagePath);
        //            _logger.LogDebug("Old profile image deleted: {ImagePath}", imagePath);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, "Error deleting old profile image: {ImageUrl}", imageUrl);
        //    }
        //}

        //private async Task<string> SaveBase64ImageAsync(string base64String)
        //{
        //    try
        //    {
        //        Console.WriteLine($"Sauvegarde image Base64, longueur: {base64String.Length}");

        //        if (string.IsNullOrEmpty(base64String))
        //            return null;

        //        // Vérifier si c'est un Base64 valide
        //        if (!base64String.Contains(","))
        //        {
        //            // Si le frontend envoie déjà le Base64 propre (sans préfixe)
        //            base64String = "data:image/jpeg;base64," + base64String;
        //        }

        //        var base64Data = base64String.Split(',')[1];

        //        // Déterminer l'extension
        //        string extension = ".jpg";
        //        if (base64String.Contains("data:image/png"))
        //            extension = ".png";
        //        else if (base64String.Contains("data:image/gif"))
        //            extension = ".gif";
        //        else if (base64String.Contains("data:image/webp"))
        //            extension = ".webp";

        //        // Créer un nom unique
        //        var fileName = $"{Guid.NewGuid()}{extension}";

        //        // Chemin de sauvegarde
        //        var webRootPath = _webHostEnvironment.ContentRootPath;
        //        var uploadsFolder = Path.Combine(webRootPath, "uploads", "users");

        //        // Créer le dossier s'il n'existe pas
        //        if (!Directory.Exists(uploadsFolder))
        //        {
        //            Console.WriteLine($"Création du dossier: {uploadsFolder}");
        //            Directory.CreateDirectory(uploadsFolder);
        //        }

        //        var filePath = Path.Combine(uploadsFolder, fileName);

        //        // Convertir Base64 en bytes et sauvegarder
        //        var imageBytes = Convert.FromBase64String(base64Data);
        //        await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

        //        // Retourner l'URL relative
        //        return $"/uploads/users/{fileName}";
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine($"Erreur sauvegarde image: {ex.Message}");
        //        throw;
        //    }
        //}

        public async Task<UserProfileDto> GetMyProfileAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return null;

            return new UserProfileDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Nom = user.Nom,
                Prenom = user.Prenom,
                PhoneNumber = user.PhoneNumber,
                BirthDate = user.BirthDate,
                Image = user.Image,
            };
        }

        // ================= DELETE (SUPPRESSION DÉFINITIVE AVEC CASCADE) =================
     
        public Task<ApiResponse<string>> DeleteAsync(Guid id)
            => MeasureAsync(
                actionName: "DeleteUser",
                input: new { UserId = id },
                async () =>
                {
                    var user = await _userRepository.GetByIdAsync(id);

                    if (user == null)
                    {
                        _logger.LogWarning("NOT_FOUND DeleteUser | UserId = {UserId}", id);
                        return ApiResponse<string>.Failure(
                            message: UserMessages.UserNotFound,
                            resultCode: 20);
                    }

                    // Vérifier si c'est un admin (ne pas supprimer le dernier admin)
                    var userRoles = await _userManager.GetRolesAsync(user);
                    if (userRoles.Contains("Admin"))
                    {
                        var adminCount = (await _userManager.GetUsersInRoleAsync("Admin")).Count;
                        if (adminCount <= 1)
                        {
                            _logger.LogWarning("Cannot delete last admin | UserId = {UserId}", id);
                            return ApiResponse<string>.Failure(
                                message: "Impossible de supprimer le dernier administrateur",
                                resultCode: 30);
                        }
                    }

                    try
                    {
                        // UTILISER LA MÉTHODE DU REPOSITORY AU LIEU DE _context DIRECTEMENT
                        var result = await _userRepository.DeleteUserWithCascadeAsync(user);

                        if (!result.Succeeded)
                        {
                            _logger.LogError(
                                "DB_ERROR DeleteUser | UserId = {UserId} | {@Errors}",
                                id,
                                result.Errors
                            );

                            return ApiResponse<string>.Failure(
                                message: UserMessages.DeleteUserError,
                                resultCode: 22
                            );
                        }

                        _logger.LogInformation(
                            "SUCCESS DeleteUser | UserId = {UserId} | UserName = {UserName} | Email = {Email}",
                            user.Id,
                            user.UserName,
                            user.Email
                        );

                        return ApiResponse<string>.Success(
                            message: "Utilisateur et toutes ses données associées supprimés avec succès",
                            resultCode: 0
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "UNEXPECTED_ERROR DeleteUser | UserId = {UserId}", id);
                        return ApiResponse<string>.Failure(
                            message: "Une erreur inattendue s'est produite lors de la suppression.",
                            resultCode: 99
                        );
                    }
                });


        // ================= SEARCH USERS =================
        public async Task<ApiResponse<PagedResult<UserWithRoleDto>>> SearchUsersAsync(UserSearchRequest request)
        {
            return await MeasureAsync(
                actionName: "SearchUsers",
                input: request,
                async () =>
                {
                    try
                    {
                        // Utilisez directement le repository
                        var (users, totalCount) = await _userRepository.SearchUsersAsync(request);

                        // Utilisez la méthode statique Create
                        var pagedResult = PagedResult<UserWithRoleDto>.Create(
                            items: users.ToList(),
                            totalCount: totalCount,
                            page: Math.Max(1, request.Page),
                            pageSize: Math.Clamp(request.PageSize, 1, 100)
                        );

                        _logger.LogInformation(
                            "SUCCESS SearchUsers | Total: {TotalCount} | Page: {Page}/{TotalPages}",
                            totalCount,
                            pagedResult.Page,
                            pagedResult.TotalPages
                        );

                        return ApiResponse<PagedResult<UserWithRoleDto>>.Success(
                            data: pagedResult,
                            message: totalCount > 0
                                ? $"Recherche terminée. {totalCount} résultat(s) trouvé(s)."
                                : "Aucun résultat trouvé avec les critères spécifiés.",
                            resultCode: 0
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "ERROR SearchUsers | Request = {@Request}", request);
                        return ApiResponse<PagedResult<UserWithRoleDto>>.Failure(
                            message: "Erreur lors de la recherche",
                            resultCode: 31
                        );
                    }
                });
        }

        // Méthode helper pour le tri
        //private IQueryable<ApplicationUser> ApplySorting(
        //    IQueryable<ApplicationUser> query,
        //    string sortBy,
        //    bool sortDescending)
        //{
        //    if (string.IsNullOrWhiteSpace(sortBy))
        //        return query.OrderBy(u => u.Nom); // Tri par défaut

        //    // Normaliser le nom du champ
        //    var normalizedSortBy = sortBy.ToLower().Trim();

        //    return normalizedSortBy switch
        //    {
        //        "username" or "user_name" or "username" =>
        //            sortDescending ? query.OrderByDescending(u => u.UserName) : query.OrderBy(u => u.UserName),

        //        "email" =>
        //            sortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),

        //        "nom" or "name" or "lastname" =>
        //            sortDescending ? query.OrderByDescending(u => u.Nom) : query.OrderBy(u => u.Nom),

        //        "prenom" or "firstname" or "prenom" =>
        //            sortDescending ? query.OrderByDescending(u => u.Prenom) : query.OrderBy(u => u.Prenom),

        //        "birthdate" or "birth_date" or "date" =>
        //            sortDescending ? query.OrderByDescending(u => u.BirthDate) : query.OrderBy(u => u.BirthDate),

        //        "statut" or "status" =>
        //            sortDescending ? query.OrderByDescending(u => u.Statut) : query.OrderBy(u => u.Statut),

        //        _ => query.OrderBy(u => u.Nom) // Tri par défaut
        //    };
        //}

        public async Task<IList<string>> GetUserRolesAsync(Guid userId)
        {
            return await _userRepository.GetUserRolesAsync(userId);
        }

        public async Task<ApiResponse<IEnumerable<TechnicienDto>>> GetTechniciensAsync()
        {
            return await MeasureAsync(
                actionName: "GetTechniciens",
                input: null,
                async () =>
                {
                    try
                    {
                        var techniciens = await _userRepository.GetTechniciensAsync();

                        return ApiResponse<IEnumerable<TechnicienDto>>.Success(
                            data: techniciens,
                            message: $"{techniciens.Count()} technicien(s) trouvé(s)",
                            resultCode: 0
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Erreur lors de la récupération des techniciens");
                        return ApiResponse<IEnumerable<TechnicienDto>>.Failure(
                            message: "Erreur interne du serveur",
                            resultCode: 33
                        );
                    }
                });
        }


        // ================= CREATE TECHNICIEN =================
        public async Task<ApiResponse<ApplicationUser>> CreateTechnicienAsync(CreateTechnicienDto dto)
        {
            return await MeasureAsync("CreateTechnicien", dto, async () =>
            {
                // 1. Validation de l'email
                if (!await _userRepository.IsEmailUniqueAsync(dto.Email))
                {
                    return ApiResponse<ApplicationUser>.Failure(
                        message: "Cet email est déjà utilisé",
                        resultCode: 10);
                }

                // 2. Validation du nom d'utilisateur
                if (!await _userRepository.IsUserNameUniqueAsync(dto.UserName))
                {
                    return ApiResponse<ApplicationUser>.Failure(
                        message: "Ce nom d'utilisateur est déjà pris",
                        resultCode: 11);
                }

                // 3. Récupérer le rôle Technicien
                var role = await _roleManager.FindByNameAsync("Technicien");
                if (role == null)
                {
                    return ApiResponse<ApplicationUser>.Failure(
                        message: "Le rôle Technicien n'existe pas",
                        resultCode: 13);
                }
                string defaultPassword = GenerateRandomPassword();
                // 4. Créer l'utilisateur
                var user = new ApplicationUser
                {
                    UserName = dto.UserName,
                    Email = dto.Email,
                    Nom = dto.Nom,
                    Prenom = dto.Prenom,
                    
                    EmailConfirmed = true,
                    Statut = UserStatut.Actif
                };

                // 4. Générer un mot de passe temporaire sécurisé
                
                var result = await _userRepository.CreateAsync(user, defaultPassword);

                if (!result.Succeeded)
                {
                    return ApiResponse<ApplicationUser>.Failure(
                        message: "Erreur lors de la création du technicien",
                        errors: result.Errors.Select(e => e.Description).ToList(),
                        resultCode: 12);
                }

                // 5. Assigner le rôle
                await _userManager.AddToRoleAsync(user, role.Name);

                // 6. ENVOYER L'EMAIL AVEC LE MOT DE PASSE
                try
                {
                    await _emailService.SendWelcomeEmailAsync(
                        user.Email,
                        user.Nom,
                        user.Prenom,
                        defaultPassword
                    );

                    _logger.LogInformation("✅ Email de bienvenue envoyé à {Email}", user.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Échec envoi email de bienvenue à {Email}", user.Email);
                    // On continue même si l'email échoue - on retourne quand même le mot de passe dans la réponse
                }

                _logger.LogInformation("Technicien créé avec succès | Email: {Email} | Mot de passe: {Password}",
                    user.Email, defaultPassword);

                return ApiResponse<ApplicationUser>.Success(
    data: user,
    message: $"Technicien '{dto.UserName}' créé avec succès. Un email a été envoyé à {user.Email} avec ses identifiants.",
    resultCode: 0);
            });
        }

        // Méthode pour générer un mot de passe temporaire sécurisé
        private string GenerateRandomPassword(int length = 10)
        {
            const string upperCase = "ABCDEFGHJKLMNOPQRSTUVWXYZ";
            const string lowerCase = "abcdefghijkmnopqrstuvwxyz";
            const string digits = "0123456789";
            const string special = "!@#$%^&*";

            var random = new Random();

            // S'assurer d'avoir au moins un de chaque type
            var passwordChars = new List<char>
        {
            upperCase[random.Next(upperCase.Length)],
            lowerCase[random.Next(lowerCase.Length)],
            digits[random.Next(digits.Length)],
            special[random.Next(special.Length)]
        };

            // Remplir le reste
            var allChars = upperCase + lowerCase + digits + special;
            for (int i = passwordChars.Count; i < length; i++)
            {
                passwordChars.Add(allChars[random.Next(allChars.Length)]);
            }

            // Mélanger
            return new string(passwordChars.OrderBy(x => random.Next()).ToArray());
        }

        // ================= CREATE COMMERCANT (MAGASIN) =================
        public async Task<ApiResponse<ApplicationUser>> CreateCommercantAsync(CreateCommercantDto dto)
        {
            return await MeasureAsync("CreateCommercant", dto, async () =>
            {
                // 1. Validation de l'email (UNIQUE pour TOUS les users)
                if (!await _userRepository.IsEmailUniqueAsync(dto.Email))
                {
                    return ApiResponse<ApplicationUser>.Failure(
                        message: "Cet email est déjà utilisé",
                        resultCode: 10);
                }

                // 2. Validation du numéro de téléphone (UNIQUE pour TOUS les users)
                if (!string.IsNullOrEmpty(dto.PhoneNumber))
                {
                    var users = _userManager.Users.ToList();
                    var existingPhone = users.FirstOrDefault(u => u.PhoneNumber == dto.PhoneNumber);
                    if (existingPhone != null)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Ce numéro de téléphone est déjà utilisé",
                            resultCode: 12);
                    }
                }

                // 3. Récupérer le rôle Commercant
                var role = await _roleManager.FindByNameAsync("Commercant");
                if (role == null)
                {
                    return ApiResponse<ApplicationUser>.Failure(
                        message: "Le rôle Commercant n'existe pas",
                        resultCode: 13);
                }

                // 4. Générer un UserName unique pour Identity (obligatoire)
                // Solution : Ajouter un suffixe numérique si nécessaire
                var baseUserName = dto.NomMagasin.Replace(" ", "_");
                var userName = baseUserName;
                var counter = 1;

                while (await _userManager.FindByNameAsync(userName) != null)
                {
                    userName = $"{baseUserName}_{counter}";
                    counter++;
                }
                string defaultPassword = GenerateRandomPassword();

                // 5. Créer l'utilisateur
                var user = new ApplicationUser
                {
                    UserName = userName,                          // UserName technique unique
                    Email = dto.Email,
                    Nom = dto.NomMagasin,                         // Nom du magasin (peut être redondant)
                    Prenom = "Magasin",                           // Valeur par défaut
                    PhoneNumber = dto.PhoneNumber,
                    Adresse = dto.Adresse,
                    EmailConfirmed = true,
                    Statut = UserStatut.Actif
                };
                
                var result = await _userRepository.CreateAsync(user, defaultPassword);

                if (!result.Succeeded)
                {
                    return ApiResponse<ApplicationUser>.Failure(
                        message: "Erreur lors de la création du commerçant",
                        errors: result.Errors.Select(e => e.Description).ToList(),
                        resultCode: 12);
                }

                // 6. Assigner le rôle
                await _userManager.AddToRoleAsync(user, role.Name);
                // Envoyer l'email
                try
                {
                    await _emailService.SendWelcomeEmailAsync(
                        user.Email,
                        user.Nom,
                        "Magasin",
                        defaultPassword
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur envoi email à {Email}", user.Email);
                }

                _logger.LogInformation("Commerçant (magasin) créé avec succès | Nom magasin: {NomMagasin} | UserName technique: {UserName} | Email: {Email}",
                    dto.NomMagasin, user.UserName, user.Email);

                return ApiResponse<ApplicationUser>.Success(
            data: user,
            message: $"Magasin '{dto.NomMagasin}' créé avec succès. Un email a été envoyé à {user.Email} avec ses identifiants.",
            resultCode: 0);
            });
        }

        public async Task<ApiResponse<PagedResult<TechnicienDto>>> GetTechniciensPagedAsync(TechnicienSearchRequest request)
        {
            return await MeasureAsync("GetTechniciensPaged", request, async () =>
            {
                try
                {
                    // 1. Récupérer tous les utilisateurs avec le rôle Technicien
                    var techniciens = await _userManager.GetUsersInRoleAsync("Technicien");

                    // 2. Convertir en IQueryable pour appliquer les filtres
                    var query = techniciens.AsQueryable();

                    // 3. Appliquer les filtres (reste identique)
                    if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                    {
                        var term = request.SearchTerm.ToLower();
                        query = query.Where(t =>
                            t.Nom.ToLower().Contains(term) ||
                            t.Prenom.ToLower().Contains(term) ||
                            t.Email.ToLower().Contains(term) ||
                            (t.UserName != null && t.UserName.ToLower().Contains(term)) ||
                            (t.PhoneNumber != null && t.PhoneNumber.Contains(term)));
                    }

                    if (!string.IsNullOrWhiteSpace(request.Nom))
                    {
                        query = query.Where(t => t.Nom.ToLower().Contains(request.Nom.ToLower()));
                    }

                    if (!string.IsNullOrWhiteSpace(request.Prenom))
                    {
                        query = query.Where(t => t.Prenom.ToLower().Contains(request.Prenom.ToLower()));
                    }

                    if (!string.IsNullOrWhiteSpace(request.Email))
                    {
                        query = query.Where(t => t.Email.ToLower().Contains(request.Email.ToLower()));
                    }

                    if (request.Statut.HasValue)
                    {
                        query = query.Where(t => t.Statut == request.Statut.Value);
                    }

                    if (request.BirthDate.HasValue)
                    {
                        query = query.Where(t => t.BirthDate.HasValue && t.BirthDate.Value.Date == request.BirthDate.Value.Date);
                    }

                    if (request.BirthYear.HasValue)
                    {
                        query = query.Where(t => t.BirthDate.HasValue && t.BirthDate.Value.Year == request.BirthYear.Value);
                    }

                    // 4. Compter le total AVANT pagination
                    var totalCount = query.Count();

                    // 5. Appliquer le tri
                    query = ApplySortingToTechniciens(query, request.SortBy, request.SortDescending);

                    // 6. Appliquer la pagination
                    var page = Math.Max(1, request.Page);
                    var pageSize = Math.Clamp(request.PageSize, 1, 100);
                    var skip = (page - 1) * pageSize;

                    var paginatedTechniciens = query
                        .Skip(skip)
                        .Take(pageSize)
                        .ToList();

                    // 7. Mapper vers DTO (CORRIGÉ - TOUS LES CHAMPS)
                    var dtos = paginatedTechniciens.Select(t => new TechnicienDto
                    {
                        Id = t.Id,
                        Nom = t.Nom,
                        Prenom = t.Prenom,
                        Email = t.Email,
                        UserName = t.UserName,           
                        PhoneNumber = t.PhoneNumber,     
                        Image = t.Image,                 
                        BirthDate = t.BirthDate,         
                        Statut = t.Statut,              
                        EmailConfirmed = t.EmailConfirmed 
                    }).ToList();

                    // 8. Créer le résultat paginé
                    var pagedResult = new PagedResult<TechnicienDto>
                    {
                        Items = dtos,
                        TotalCount = totalCount,
                        Page = page,
                        PageSize = pageSize
                    };

                    _logger.LogInformation("SUCCESS GetTechniciensPaged | Total: {TotalCount} | Page: {Page}/{TotalPages}",
                        totalCount, page, (int)Math.Ceiling((double)totalCount / pageSize));

                    return ApiResponse<PagedResult<TechnicienDto>>.Success(
                        data: pagedResult,
                        message: $"{dtos.Count} technicien(s) trouvé(s) sur {totalCount}",
                        resultCode: 0);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération paginée des techniciens");
                    return ApiResponse<PagedResult<TechnicienDto>>.Failure(
                        message: "Erreur interne du serveur",
                        resultCode: 33);
                }
            });
        }

        // Méthode helper pour le tri des techniciens
        private IQueryable<ApplicationUser> ApplySortingToTechniciens(
            IQueryable<ApplicationUser> query,
            string? sortBy,
            bool descending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query.OrderBy(t => t.Nom).ThenBy(t => t.Prenom);

            var sortByLower = sortBy.ToLower();

            return (sortByLower, descending) switch
            {
                ("nom", false) => query.OrderBy(t => t.Nom),
                ("nom", true) => query.OrderByDescending(t => t.Nom),

                ("prenom", false) => query.OrderBy(t => t.Prenom),
                ("prenom", true) => query.OrderByDescending(t => t.Prenom),

                ("email", false) => query.OrderBy(t => t.Email),
                ("email", true) => query.OrderByDescending(t => t.Email),

                ("username", false) => query.OrderBy(t => t.UserName),
                ("username", true) => query.OrderByDescending(t => t.UserName),

                ("birthdate", false) => query.OrderBy(t => t.BirthDate),
                ("birthdate", true) => query.OrderByDescending(t => t.BirthDate),

                _ => query.OrderBy(t => t.Nom).ThenBy(t => t.Prenom)
            };
        }

        public async Task<ApiResponse<PagedResult<CommercantDto>>> GetCommercantsPagedAsync(CommercantSearchRequest request)
        {
            return await MeasureAsync("GetCommercantsPaged", request, async () =>
            {
                try
                {
                    // 1. Récupérer tous les utilisateurs avec le rôle Commercant
                    var commercants = await _userManager.GetUsersInRoleAsync("Commercant");

                    // 2. Convertir en IQueryable pour appliquer les filtres
                    var query = commercants.AsQueryable();

                    // 3. Appliquer les filtres
                    if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                    {
                        var term = request.SearchTerm.ToLower();
                        query = query.Where(c =>
                            c.UserName.ToLower().Contains(term) ||      // Nom du magasin
                            c.Email.ToLower().Contains(term) ||
                            (c.PhoneNumber != null && c.PhoneNumber.Contains(term)) ||
                            (c.Adresse != null && c.Adresse.ToLower().Contains(term)));
                    }

                    if (!string.IsNullOrWhiteSpace(request.NomMagasin))
                    {
                        query = query.Where(c => c.UserName.ToLower().Contains(request.NomMagasin.ToLower()));
                    }

                    if (!string.IsNullOrWhiteSpace(request.Email))
                    {
                        query = query.Where(c => c.Email.ToLower().Contains(request.Email.ToLower()));
                    }

                    if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                    {
                        query = query.Where(c => c.PhoneNumber != null && c.PhoneNumber.Contains(request.PhoneNumber));
                    }

                    if (!string.IsNullOrWhiteSpace(request.Adresse))
                    {
                        query = query.Where(c => c.Adresse != null && c.Adresse.ToLower().Contains(request.Adresse.ToLower()));
                    }

                    if (request.Statut.HasValue)
                    {
                        query = query.Where(c => c.Statut == request.Statut.Value);
                    }

                    // 4. Compter le total AVANT pagination
                    var totalCount = query.Count();

                    // 5. Appliquer le tri
                    query = ApplySortingToCommercants(query, request.SortBy, request.SortDescending);

                    // 6. Appliquer la pagination
                    var page = Math.Max(1, request.Page);
                    var pageSize = Math.Clamp(request.PageSize, 1, 100);
                    var skip = (page - 1) * pageSize;

                    var paginatedCommercants = query
                        .Skip(skip)
                        .Take(pageSize)
                        .ToList();

                    // 7. Mapper vers DTO
                    var dtos = paginatedCommercants.Select(c => new CommercantDto
                    {
                        Id = c.Id,
                        NomMagasin = c.UserName,
                        Email = c.Email,
                        PhoneNumber = c.PhoneNumber,
                        Adresse = c.Adresse,
                        Statut = c.Statut,
                        
                    }).ToList();

                    // 8. Créer le résultat paginé
                    var pagedResult = new PagedResult<CommercantDto>
                    {
                        Items = dtos,
                        TotalCount = totalCount,
                        Page = page,
                        PageSize = pageSize
                    };

                    _logger.LogInformation("SUCCESS GetCommercantsPaged | Total: {TotalCount} | Page: {Page}/{TotalPages}",
                        totalCount, page, (int)Math.Ceiling((double)totalCount / pageSize));

                    return ApiResponse<PagedResult<CommercantDto>>.Success(
                        data: pagedResult,
                        message: $"{dtos.Count} commerçant(s) trouvé(s) sur {totalCount}",
                        resultCode: 0);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération paginée des commerçants");
                    return ApiResponse<PagedResult<CommercantDto>>.Failure(
                        message: "Erreur interne du serveur",
                        resultCode: 33);
                }
            });
        }

        // Méthode helper pour le tri des commerçants
        private IQueryable<ApplicationUser> ApplySortingToCommercants(
            IQueryable<ApplicationUser> query,
            string? sortBy,
            bool descending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query.OrderBy(c => c.UserName);

            var sortByLower = sortBy.ToLower();

            return (sortByLower, descending) switch
            {
                ("nommagasin", false) => query.OrderBy(c => c.UserName),
                ("nommagasin", true) => query.OrderByDescending(c => c.UserName),

                ("email", false) => query.OrderBy(c => c.Email),
                ("email", true) => query.OrderByDescending(c => c.Email),

                ("phonenumber", false) => query.OrderBy(c => c.PhoneNumber),
                ("phonenumber", true) => query.OrderByDescending(c => c.PhoneNumber),

                ("adresse", false) => query.OrderBy(c => c.Adresse),
                ("adresse", true) => query.OrderByDescending(c => c.Adresse),

                ("statut", false) => query.OrderBy(c => c.Statut),
                ("statut", true) => query.OrderByDescending(c => c.Statut),

                _ => query.OrderBy(c => c.UserName)
            };
        }

        // ================= EDIT TECHNICIEN PROFILE =================

        public async Task<ApiResponse<ApplicationUser>> EditTechnicienProfileAsync(Guid userId, EditTechnicienProfileDto dto)
        {
            return await MeasureAsync("EditTechnicienProfile", new { userId, dto }, async () =>
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponse<ApplicationUser>.Failure(
                        message: UserMessages.UserNotFound,
                        errors: null,
                        resultCode: 20);
                }

                // Vérifier le rôle
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Technicien"))
                {
                    return ApiResponse<ApplicationUser>.Failure(
                        message: "Seul un technicien peut modifier ce profil",
                        errors: null,
                        resultCode: 99);
                }

                // ============================================
                // CAS 1 : Changement d'EMAIL
                // ============================================
                if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
                {
                    // Vérifier l'unicité
                    if (!await _userRepository.IsEmailUniqueAsync(dto.Email, userId))
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Cet email est déjà utilisé par un autre compte",
                            errors: null,
                            resultCode: 10);
                    }

                    // Envoyer OTP sur le NOUVEL email
                    var otpResult = await _otpService.GenerateAndSendOtpToEmailAsync(
                        user,
                        dto.Email,
                        OtpPurpose.EmailChange);

                    if (otpResult.ResultCode != 0)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Erreur lors de l'envoi du code de vérification",
                            errors: null,
                            resultCode: 42);
                    }

                    // Retourner avec code spécial
                    return ApiResponse<ApplicationUser>.Success(
                        data: null,
                        message: $"Un code OTP a été envoyé à {dto.Email}. Veuillez le valider pour confirmer le changement.",
                        resultCode: 42);
                }

                // ============================================
                // CAS 2 : Changement de MOT DE PASSE
                // ============================================
                if (!string.IsNullOrEmpty(dto.CurrentPassword) && !string.IsNullOrEmpty(dto.NewPassword))
                {
                    // Vérifier l'ancien mot de passe
                    var passwordValid = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
                    if (!passwordValid)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Mot de passe actuel incorrect",
                            errors: null,
                            resultCode: 25);
                    }

                    // Vérifier la confirmation
                    if (dto.NewPassword != dto.ConfirmPassword)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Les nouveaux mots de passe ne correspondent pas",
                            errors: null,
                            resultCode: 26);
                    }

                    // Vérifier la force
                    if (dto.NewPassword.Length < 6)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Le mot de passe doit contenir au moins 6 caractères",
                            errors: null,
                            resultCode: 43);
                    }

                    // Envoyer OTP sur l'email actuel
                    var otpResult = await _otpService.GenerateAndSendOtpAsync(
                        user,
                        OtpPurpose.ResetPassword);

                    if (otpResult.ResultCode != 0)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Erreur lors de l'envoi du code de vérification",
                            errors: null,
                            resultCode: 42);
                    }

                    return ApiResponse<ApplicationUser>.Success(
                        data: null,
                        message: $"Un code OTP a été envoyé à {user.Email}. Veuillez le valider pour changer votre mot de passe.",
                        resultCode: 43);
                }

                // ============================================
                // CAS 3 : Autres modifications
                // ============================================
                bool hasChanges = false;

                // Nom
                if (!string.IsNullOrEmpty(dto.Nom) && dto.Nom != user.Nom)
                {
                    user.Nom = dto.Nom;
                    hasChanges = true;
                }

                // Prénom
                if (!string.IsNullOrEmpty(dto.Prenom) && dto.Prenom != user.Prenom)
                {
                    user.Prenom = dto.Prenom;
                    hasChanges = true;
                }

                // Téléphone
                if (!string.IsNullOrEmpty(dto.PhoneNumber) && dto.PhoneNumber != user.PhoneNumber)
                {
                    var existingPhone = await _userManager.Users
                        .FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber && u.Id != userId);
                    if (existingPhone != null)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Ce numéro de téléphone est déjà utilisé",
                            errors: null,
                            resultCode: 12);
                    }
                    user.PhoneNumber = dto.PhoneNumber;
                    hasChanges = true;
                }

                // Date de naissance
                if (dto.BirthDate.HasValue && dto.BirthDate != user.BirthDate)
                {
                    var age = DateTime.Today.Year - dto.BirthDate.Value.Year;
                    if (age < 18)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Vous devez avoir au moins 18 ans",
                            errors: null,
                            resultCode: 40);
                    }
                    user.BirthDate = dto.BirthDate;
                    hasChanges = true;
                }

                // Image
                if (!string.IsNullOrEmpty(dto.Image) && dto.Image != user.Image)
                {
                    user.Image = dto.Image;
                    hasChanges = true;
                }

                // Sauvegarde
                if (hasChanges)
                {
                    var result = await _userRepository.UpdateAsync(user);
                    if (!result.Succeeded)
                    {
                        var errors = result.Errors.Select(e => e.Description).ToList();
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Erreur lors de la mise à jour du profil",
                            errors: errors,
                            resultCode: 21);
                    }
                }

                string message = hasChanges ? "Profil mis à jour avec succès" : "Aucune modification détectée";
                return ApiResponse<ApplicationUser>.Success(user, message, 0);
            });
        }

        // ================= EDIT COMMERCANT PROFILE =================
        
        public async Task<ApiResponse<ApplicationUser>> EditCommercantProfileAsync(Guid userId, EditCommercantProfileDto dto)
        {
            return await MeasureAsync("EditCommercantProfile", new { userId, dto }, async () =>
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponse<ApplicationUser>.Failure(
                        message: UserMessages.UserNotFound,
                        errors: null,
                        resultCode: 20);
                }

                // Vérifier le rôle
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Commercant"))
                {
                    return ApiResponse<ApplicationUser>.Failure(
                        message: "Seul un commerçant peut modifier ce profil",
                        errors: null,
                        resultCode: 99);
                }

                // ============================================
                // CAS 1 : Changement d'EMAIL
                // ============================================
                if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
                {
                    // Vérifier que le nouvel email n'est pas déjà utilisé
                    var existingUser = await _userManager.FindByEmailAsync(dto.Email);
                    if (existingUser != null && existingUser.Id != userId)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Cet email est déjà utilisé par un autre compte",
                            errors: null,
                            resultCode: 10);
                    }

                    // Envoyer OTP sur le NOUVEL email
                    var otpResult = await _otpService.GenerateAndSendOtpToEmailAsync(
                        user,
                        dto.Email,  // Envoyer au nouvel email
                        OtpPurpose.EmailChange);

                    if (otpResult.ResultCode != 0)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Erreur lors de l'envoi du code de vérification",
                            errors: null,
                            resultCode: 42);
                    }

                    // Retourner avec code spécial (pas de changement direct)
                    return ApiResponse<ApplicationUser>.Success(
                        data: null,
                        message: $"Un code OTP a été envoyé à {dto.Email}. Veuillez le valider pour confirmer le changement.",
                        resultCode: 42);  // Code spécial pour validation email
                }

                // ============================================
                // CAS 2 : Changement de MOT DE PASSE
                // ============================================
                if (!string.IsNullOrEmpty(dto.CurrentPassword) && !string.IsNullOrEmpty(dto.NewPassword))
                {
                    // Vérifier l'ancien mot de passe
                    var passwordValid = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
                    if (!passwordValid)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Mot de passe actuel incorrect",
                            errors: null,
                            resultCode: 25);
                    }

                    // Vérifier la confirmation
                    if (dto.NewPassword != dto.ConfirmPassword)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Les nouveaux mots de passe ne correspondent pas",
                            errors: null,
                            resultCode: 26);
                    }

                    // Vérifier la force du mot de passe
                    if (dto.NewPassword.Length < 6)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Le mot de passe doit contenir au moins 6 caractères",
                            errors: null,
                            resultCode: 43);
                    }

                    // Envoyer OTP sur l'email actuel
                    var otpResult = await _otpService.GenerateAndSendOtpAsync(
                        user,
                        OtpPurpose.ResetPassword);

                    if (otpResult.ResultCode != 0)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Erreur lors de l'envoi du code de vérification",
                            errors: null,
                            resultCode: 42);
                    }

                    return ApiResponse<ApplicationUser>.Success(
                        data: null,
                        message: $"Un code OTP a été envoyé à {user.Email}. Veuillez le valider pour changer votre mot de passe.",
                        resultCode: 43);  // Code spécial pour validation password
                }

                // ============================================
                // CAS 3 : Autres modifications (NomMagasin, Téléphone, Adresse, Image)
                // ============================================
                bool hasChanges = false;

                // Nom du magasin
                if (!string.IsNullOrEmpty(dto.NomMagasin) && dto.NomMagasin != user.UserName)
                {
                    // Vérifier l'unicité
                    if (!await _userRepository.IsUserNameUniqueAsync(dto.NomMagasin, userId))
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Ce nom de magasin est déjà utilisé",
                            errors: null,
                            resultCode: 11);
                    }
                    user.UserName = dto.NomMagasin;
                    user.NormalizedUserName = dto.NomMagasin.ToUpper();
                    user.Nom = dto.NomMagasin;
                    hasChanges = true;
                }

                // Téléphone
                if (!string.IsNullOrEmpty(dto.PhoneNumber) && dto.PhoneNumber != user.PhoneNumber)
                {
                    var existingPhone = _userManager.Users.FirstOrDefault(u => u.PhoneNumber == dto.PhoneNumber && u.Id != userId);
                    if (existingPhone != null)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Ce numéro de téléphone est déjà utilisé",
                            errors: null,
                            resultCode: 12);
                    }
                    user.PhoneNumber = dto.PhoneNumber;
                    hasChanges = true;
                }

                // Adresse
                if (!string.IsNullOrEmpty(dto.Adresse) && dto.Adresse != user.Adresse)
                {
                    user.Adresse = dto.Adresse;
                    hasChanges = true;
                }

                // Image
                if (!string.IsNullOrEmpty(dto.Image) && dto.Image != user.Image)
                {
                    user.Image = dto.Image;
                    hasChanges = true;
                }

                // Sauvegarde
                if (hasChanges)
                {
                    var result = await _userRepository.UpdateAsync(user);
                    if (!result.Succeeded)
                    {
                        var errors = result.Errors.Select(e => e.Description).ToList();
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Erreur lors de la mise à jour",
                            errors: errors,
                            resultCode: 21);
                    }
                }

                string message = hasChanges ? "Profil mis à jour avec succès" : "Aucune modification détectée";
                return ApiResponse<ApplicationUser>.Success(user, message, 0);
            });
        }

        // ================= ADMIN UPDATE TECHNICIEN =================
        public async Task<ApiResponse<ApplicationUser>> AdminUpdateTechnicienAsync(Guid userId, AdminUpdateTechnicienDto dto)
        {
            return await MeasureAsync("AdminUpdateTechnicien", new { userId, dto }, async () =>
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponse<ApplicationUser>.Failure(UserMessages.UserNotFound, resultCode: 20);
                }

                // Vérifier le rôle
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Technicien"))
                {
                    return ApiResponse<ApplicationUser>.Failure("Cet utilisateur n'est pas un technicien", resultCode: 99);
                }

                bool hasChanges = false;
                bool emailChanged = false;
                bool userNameChanged = false;

                // 1. VALIDATION DU NOM D'UTILISATEUR (si fourni)
                if (!string.IsNullOrEmpty(dto.UserName))
                {
                    // Vérifier la longueur minimale
                    if (dto.UserName.Length < 4)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            "Le nom d'utilisateur doit contenir au moins 4 caractères",
                            resultCode: 40);
                    }

                    // Vérifier la longueur maximale
                    if (dto.UserName.Length > 30)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            "Le nom d'utilisateur ne peut pas dépasser 30 caractères",
                            resultCode: 40);
                    }

                    // Vérifier l'unicité
                    if (dto.UserName != user.UserName)
                    {
                        if (!await _userRepository.IsUserNameUniqueAsync(dto.UserName, userId))
                        {
                            return ApiResponse<ApplicationUser>.Failure(
                                "Ce nom d'utilisateur est déjà pris",
                                resultCode: 11);
                        }
                    }
                }

                // 2. VALIDATION DE L'EMAIL (si fourni)

                if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
                {
                    // Vérifier l'unicité
                    if (!await _userRepository.IsEmailUniqueAsync(dto.Email, userId))
                    {
                        return ApiResponse<ApplicationUser>.Failure(
        message: "Cet email est déjà utilisé",
        errors: null,
        resultCode: 10);
                    }

                    // Générer un nouveau mot de passe
                    string newPassword = GenerateRandomPassword();

                    // Changer l'email
                    user.Email = dto.Email;
                    user.NormalizedEmail = dto.Email.ToUpper();
                    user.EmailConfirmed = true;

                    // Changer le mot de passe
                    var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                    await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

                    hasChanges = true;
                    emailChanged = true;

                    // Envoyer email avec les nouveaux identifiants (comme register)
                    await _emailService.SendWelcomeEmailAsync(
                        user.Email,  // Nouvel email
                        user.Nom,
                        user.Prenom,
                        newPassword
                    );
                }

                // 3. VALIDATION DU TÉLÉPHONE (si fourni)
                if (!string.IsNullOrEmpty(dto.PhoneNumber))
                {
                    // Vérifier le format (8 chiffres)
                    if (!System.Text.RegularExpressions.Regex.IsMatch(dto.PhoneNumber, @"^[0-9]{8}$"))
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            "Le numéro de téléphone doit contenir exactement 8 chiffres",
                            resultCode: 40);
                    }

                    // Vérifier l'unicité
                    if (dto.PhoneNumber != user.PhoneNumber)
                    {
                        var existingPhone = await _userManager.Users
                            .FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber && u.Id != userId);
                        if (existingPhone != null)
                        {
                            return ApiResponse<ApplicationUser>.Failure(
                                "Ce numéro de téléphone est déjà utilisé",
                                resultCode: 12);
                        }
                    }
                }

                // 4. VALIDATION DE LA DATE DE NAISSANCE (si fournie)
                if (dto.BirthDate.HasValue)
                {
                    var birthDate = dto.BirthDate.Value;
                    var today = DateTime.Today;
                    var age = today.Year - birthDate.Year;
                    if (birthDate.Date > today.AddYears(-age)) age--;

                    // Vérifier l'âge minimum (18 ans)
                    if (age < 18)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            "L'utilisateur doit avoir au moins 18 ans",
                            resultCode: 40);
                    }

                    // Vérifier l'âge maximum (120 ans)
                    if (age > 120)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            "Date de naissance invalide",
                            resultCode: 40);
                    }

                    // Vérifier que la date n'est pas dans le futur
                    if (birthDate.Date > today)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            "La date de naissance ne peut pas être dans le futur",
                            resultCode: 40);
                    }
                }

                // 5. VALIDATION DES CHAMPS TEXTE (longueur)
                if (!string.IsNullOrEmpty(dto.Nom) && (dto.Nom.Length < 4 || dto.Nom.Length > 30))
                {
                    return ApiResponse<ApplicationUser>.Failure(
                        "Le nom doit contenir entre 4 et 30 caractères",
                        resultCode: 40);
                }

                if (!string.IsNullOrEmpty(dto.Prenom) && (dto.Prenom.Length < 4 || dto.Prenom.Length > 30))
                {
                    return ApiResponse<ApplicationUser>.Failure(
                        "Le prénom doit contenir entre 4 et 30 caractères",
                        resultCode: 40);
                }

                // 6. APPLIQUER LES MODIFICATIONS
                // Nom d'utilisateur
                if (!string.IsNullOrEmpty(dto.UserName) && dto.UserName != user.UserName)
                {
                    user.UserName = dto.UserName;
                    user.NormalizedUserName = dto.UserName.ToUpper();
                    hasChanges = true;
                    userNameChanged = true;
                }

                // Email
                if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
                {
                    user.Email = dto.Email;
                    user.NormalizedEmail = dto.Email.ToUpper();
                    user.EmailConfirmed = false;  // Forcer reconfirmation
                    hasChanges = true;
                    emailChanged = true;
                }

                // Téléphone
                if (!string.IsNullOrEmpty(dto.PhoneNumber) && dto.PhoneNumber != user.PhoneNumber)
                {
                    user.PhoneNumber = dto.PhoneNumber;
                    hasChanges = true;
                }

                // Nom
                if (!string.IsNullOrEmpty(dto.Nom) && dto.Nom != user.Nom)
                {
                    user.Nom = dto.Nom;
                    hasChanges = true;
                }

                // Prénom
                if (!string.IsNullOrEmpty(dto.Prenom) && dto.Prenom != user.Prenom)
                {
                    user.Prenom = dto.Prenom;
                    hasChanges = true;
                }

                // Date de naissance
                if (dto.BirthDate.HasValue && dto.BirthDate != user.BirthDate)
                {
                    user.BirthDate = dto.BirthDate;
                    hasChanges = true;
                }

                // Image
                if (!string.IsNullOrEmpty(dto.Image) && dto.Image != user.Image)
                {
                    user.Image = dto.Image;
                    hasChanges = true;
                }

                // 7. SAUVEGARDE
                if (hasChanges)
                {
                    var result = await _userRepository.UpdateAsync(user);
                    if (!result.Succeeded)
                    {
                        var errors = result.Errors.Select(e => e.Description).ToList();
                        return ApiResponse<ApplicationUser>.Failure(
                            "Erreur lors de la mise à jour",
                            errors,
                            resultCode: 21);
                    }
                }

                // 8. MESSAGE DE RETOUR
                string message = hasChanges ? "Technicien mis à jour avec succès" : "Aucune modification détectée";

                if (emailChanged)
                {
                    message = "Technicien mis à jour. Un email de confirmation a été envoyé au nouvel email.";
                }

                if (userNameChanged)
                {
                    message += " Le nom d'utilisateur a été modifié.";
                }

                return ApiResponse<ApplicationUser>.Success(user, message, 0);
            });
        }

        // Méthode utilitaire pour valider l'email
        //private bool IsValidEmail(string email)
        //{
        //    try
        //    {
        //        var addr = new System.Net.Mail.MailAddress(email);
        //        return addr.Address == email;
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //}

        // ================= ADMIN UPDATE COMMERCANT =================
 
        public async Task<ApiResponse<ApplicationUser>> AdminUpdateCommercantAsync(Guid userId, AdminUpdateCommercantDto dto)
        {
            return await MeasureAsync("AdminUpdateCommercant", new { userId, dto }, async () =>
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponse<ApplicationUser>.Failure(UserMessages.UserNotFound, resultCode: 20);
                }

                // Vérifier le rôle
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Commercant"))
                {
                    return ApiResponse<ApplicationUser>.Failure("Cet utilisateur n'est pas un commerçant", resultCode: 99);
                }

                bool hasChanges = false;
                bool emailChanged = false;

                // 1. VALIDATION DU NOM MAGASIN (si fourni)
                if (!string.IsNullOrEmpty(dto.NomMagasin))
                {
                    // Vérifier la longueur (2-20 caractères comme dans CreateCommercantDto)
                    if (dto.NomMagasin.Length < 2)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            "Le nom du magasin doit contenir au moins 2 caractères",
                            resultCode: 40);
                    }

                    if (dto.NomMagasin.Length > 20)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            "Le nom du magasin ne peut pas dépasser 20 caractères",
                            resultCode: 40);
                    }

                    // Vérifier l'unicité
                    if (dto.NomMagasin != user.UserName)
                    {
                        if (!await _userRepository.IsUserNameUniqueAsync(dto.NomMagasin, userId))
                        {
                            return ApiResponse<ApplicationUser>.Failure(
                                "Ce nom de magasin est déjà utilisé",
                                resultCode: 11);
                        }
                    }
                }

                // 2. VALIDATION DE L'EMAIL (si fourni)
                // UserService.cs - Dans AdminUpdateTechnicienAsync

                if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
                {
                    // Vérifier l'unicité
                    if (!await _userRepository.IsEmailUniqueAsync(dto.Email, userId))
                    {
                        return ApiResponse<ApplicationUser>.Failure(
        message: "Cet email est déjà utilisé",
        errors: null,
        resultCode: 10);
                    }

                    // Générer un nouveau mot de passe
                    string newPassword = GenerateRandomPassword();

                    // Changer l'email
                    user.Email = dto.Email;
                    user.NormalizedEmail = dto.Email.ToUpper();
                    user.EmailConfirmed = true;

                    // Changer le mot de passe
                    var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
                    await _userManager.ResetPasswordAsync(user, resetToken, newPassword);

                    hasChanges = true;
                    emailChanged = true;

                    // Envoyer email avec les nouveaux identifiants (comme register)
                    await _emailService.SendWelcomeEmailAsync(
                        user.Email,  // Nouvel email
                        user.Nom,
                        user.Prenom,
                        newPassword
                    );
                }

                // 3. VALIDATION DU TÉLÉPHONE (si fourni)
                if (!string.IsNullOrEmpty(dto.PhoneNumber))
                {
                    if (!System.Text.RegularExpressions.Regex.IsMatch(dto.PhoneNumber, @"^[0-9]{8}$"))
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            "Le numéro de téléphone doit contenir exactement 8 chiffres",
                            resultCode: 40);
                    }

                    if (dto.PhoneNumber != user.PhoneNumber)
                    {
                        var existingPhone = await _userManager.Users
                            .FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber && u.Id != userId);
                        if (existingPhone != null)
                        {
                            return ApiResponse<ApplicationUser>.Failure(
                                "Ce numéro de téléphone est déjà utilisé",
                                resultCode: 12);
                        }
                    }
                }

                // 4. VALIDATION DE L'ADRESSE (si fournie)
                if (!string.IsNullOrEmpty(dto.Adresse) && dto.Adresse.Length > 200)
                {
                    return ApiResponse<ApplicationUser>.Failure(
                        "L'adresse ne peut pas dépasser 200 caractères",
                        resultCode: 40);
                }

                // 5. APPLIQUER LES MODIFICATIONS
                // Nom du magasin (UserName et Nom)
                if (!string.IsNullOrEmpty(dto.NomMagasin) && dto.NomMagasin != user.UserName)
                {
                    user.UserName = dto.NomMagasin;
                    user.NormalizedUserName = dto.NomMagasin.ToUpper();
                    user.Nom = dto.NomMagasin;  // Synchroniser
                    hasChanges = true;
                }

                // Email
                if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
                {
                    user.Email = dto.Email;
                    user.NormalizedEmail = dto.Email.ToUpper();
                    user.EmailConfirmed = false;  // Forcer reconfirmation
                    hasChanges = true;
                    emailChanged = true;
                }

                // Téléphone
                if (!string.IsNullOrEmpty(dto.PhoneNumber) && dto.PhoneNumber != user.PhoneNumber)
                {
                    user.PhoneNumber = dto.PhoneNumber;
                    hasChanges = true;
                }

                // Adresse
                if (!string.IsNullOrEmpty(dto.Adresse) && dto.Adresse != user.Adresse)
                {
                    user.Adresse = dto.Adresse;
                    hasChanges = true;
                }

                // Image
                if (!string.IsNullOrEmpty(dto.Image) && dto.Image != user.Image)
                {
                    user.Image = dto.Image;
                    hasChanges = true;
                }

                // 6. SAUVEGARDE
                if (hasChanges)
                {
                    var result = await _userRepository.UpdateAsync(user);
                    if (!result.Succeeded)
                    {
                        var errors = result.Errors.Select(e => e.Description).ToList();
                        return ApiResponse<ApplicationUser>.Failure(
                            "Erreur lors de la mise à jour",
                            errors,
                            resultCode: 21);
                    }
                }

                // 7. MESSAGE DE RETOUR
                string message = hasChanges ? "Commerçant mis à jour avec succès" : "Aucune modification détectée";

                if (emailChanged)
                {
                    message = "Commerçant mis à jour. Un email de confirmation a été envoyé au nouvel email.";
                }

                return ApiResponse<ApplicationUser>.Success(user, message, 0);
            });
        }

        // ================= GET TECHNICIEN BY ID =================
        public async Task<ApiResponse<TechnicienDto>> GetTechnicienByIdAsync(Guid id)
        {
            return await MeasureAsync("GetTechnicienById", new { UserId = id }, async () =>
            {
                try
                {
                    var user = await _userRepository.GetByIdAsync(id);
                    if (user == null)
                    {
                        return ApiResponse<TechnicienDto>.Failure(
                            message: "Technicien non trouvé",
                            errors: null,
                            resultCode: 20);
                    }

                    // Vérifier que l'utilisateur a bien le rôle Technicien
                    var roles = await _userManager.GetRolesAsync(user);
                    if (!roles.Contains("Technicien"))
                    {
                        return ApiResponse<TechnicienDto>.Failure(
                            message: "Cet utilisateur n'est pas un technicien",
                            errors: null,
                            resultCode: 99);
                    }

                    var technicienDto = new TechnicienDto
                    {
                        Id = user.Id,
                        Nom = user.Nom,
                        Prenom = user.Prenom,
                        Email = user.Email,
                        UserName = user.UserName,
                        PhoneNumber = user.PhoneNumber,
                        Image = user.Image,
                        BirthDate = user.BirthDate,
                        Statut = user.Statut,
                        EmailConfirmed = user.EmailConfirmed
                    };

                    _logger.LogInformation("Technicien trouvé: {Id} - {Nom} {Prenom}", id, user.Nom, user.Prenom);

                    return ApiResponse<TechnicienDto>.Success(
                        data: technicienDto,
                        message: "Technicien récupéré avec succès",
                        resultCode: 0);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération du technicien {UserId}", id);
                    return ApiResponse<TechnicienDto>.Failure(
                        message: "Erreur interne du serveur",
                        errors: null,
                        resultCode: 99);
                }
            });
        }

        // ================= GET COMMERCANT BY ID =================
        public async Task<ApiResponse<CommercantDto>> GetCommercantByIdAsync(Guid id)
        {
            return await MeasureAsync("GetCommercantById", new { UserId = id }, async () =>
            {
                try
                {
                    var user = await _userRepository.GetByIdAsync(id);
                    if (user == null)
                    {
                        return ApiResponse<CommercantDto>.Failure(
                            message: "Commerçant non trouvé",
                            errors: null,
                            resultCode: 20);
                    }

                    // Vérifier que l'utilisateur a bien le rôle Commercant
                    var roles = await _userManager.GetRolesAsync(user);
                    if (!roles.Contains("Commercant"))
                    {
                        return ApiResponse<CommercantDto>.Failure(
                            message: "Cet utilisateur n'est pas un commerçant",
                            errors: null,
                            resultCode: 99);
                    }

                    var commercantDto = new CommercantDto
                    {
                        Id = user.Id,
                        NomMagasin = user.UserName,
                        Email = user.Email,
                        PhoneNumber = user.PhoneNumber,
                        Adresse = user.Adresse,
                        Image = user.Image,
                        Statut = user.Statut,
                        EmailConfirmed = user.EmailConfirmed
                    };

                    _logger.LogInformation("Commerçant trouvé: {Id} - {NomMagasin}", id, user.UserName);

                    return ApiResponse<CommercantDto>.Success(
                        data: commercantDto,
                        message: "Commerçant récupéré avec succès",
                        resultCode: 0);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erreur lors de la récupération du commerçant {UserId}", id);
                    return ApiResponse<CommercantDto>.Failure(
                        message: "Erreur interne du serveur",
                        errors: null,
                        resultCode: 99);
                }
            });
        }

        // ================= EDIT ADMIN PROFILE =================
        public async Task<ApiResponse<ApplicationUser>> EditAdminProfileAsync(Guid userId, EditAdminProfileDto dto)
        {
            return await MeasureAsync("EditAdminProfile", new { userId, dto }, async () =>
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return ApiResponse<ApplicationUser>.Failure(
                        message: UserMessages.UserNotFound,
                        errors: null,
                        resultCode: 20);
                }

                // Vérifier le rôle (Admin uniquement)
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Admin"))
                {
                    return ApiResponse<ApplicationUser>.Failure(
                        message: "Cette API est réservée aux administrateurs",
                        errors: null,
                        resultCode: 99);
                }

                // ============================================
                // CAS 1 : Changement d'EMAIL
                // ============================================
                if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
                {
                    if (!await _userRepository.IsEmailUniqueAsync(dto.Email, userId))
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Cet email est déjà utilisé par un autre compte",
                            errors: null,
                            resultCode: 10);
                    }

                    var otpResult = await _otpService.GenerateAndSendOtpToEmailAsync(
                        user,
                        dto.Email,
                        OtpPurpose.EmailChange);

                    if (otpResult.ResultCode != 0)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Erreur lors de l'envoi du code de vérification",
                            errors: null,
                            resultCode: 42);
                    }

                    return ApiResponse<ApplicationUser>.Success(
                        data: null,
                        message: $"Un code OTP a été envoyé à {dto.Email}. Veuillez le valider pour confirmer le changement.",
                        resultCode: 42);
                }

                // ============================================
                // CAS 2 : Changement de MOT DE PASSE
                // ============================================
                if (!string.IsNullOrEmpty(dto.CurrentPassword) && !string.IsNullOrEmpty(dto.NewPassword))
                {
                    // Vérifier l'ancien mot de passe
                    var passwordValid = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
                    if (!passwordValid)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Mot de passe actuel incorrect",
                            errors: null,
                            resultCode: 25);
                    }

                    // Vérifier la confirmation
                    if (dto.NewPassword != dto.ConfirmPassword)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Les nouveaux mots de passe ne correspondent pas",
                            errors: null,
                            resultCode: 26);
                    }

                    // Vérifier la force du mot de passe (mêmes règles que Register)
                    if (dto.NewPassword.Length < 6)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Le mot de passe doit contenir au moins 6 caractères",
                            errors: null,
                            resultCode: 43);
                    }

                    if (!System.Text.RegularExpressions.Regex.IsMatch(dto.NewPassword, @"\d"))
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Le mot de passe doit contenir au moins un chiffre",
                            errors: null,
                            resultCode: 43);
                    }

                    if (!System.Text.RegularExpressions.Regex.IsMatch(dto.NewPassword, @"[a-z]"))
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Le mot de passe doit contenir au moins une lettre minuscule",
                            errors: null,
                            resultCode: 43);
                    }

                    if (!System.Text.RegularExpressions.Regex.IsMatch(dto.NewPassword, @"[A-Z]"))
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Le mot de passe doit contenir au moins une lettre majuscule",
                            errors: null,
                            resultCode: 43);
                    }

                    // Envoyer OTP sur l'email actuel
                    var otpResult = await _otpService.GenerateAndSendOtpAsync(
                        user,
                        OtpPurpose.ResetPassword);

                    if (otpResult.ResultCode != 0)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Erreur lors de l'envoi du code de vérification",
                            errors: null,
                            resultCode: 42);
                    }

                    return ApiResponse<ApplicationUser>.Success(
                        data: null,
                        message: $"Un code OTP a été envoyé à {user.Email}. Veuillez le valider pour changer votre mot de passe.",
                        resultCode: 43);
                }

                // ============================================
                // CAS 3 : Autres modifications
                // ============================================
                bool hasChanges = false;

                // Nom d'utilisateur
                if (!string.IsNullOrEmpty(dto.UserName) && dto.UserName != user.UserName)
                {
                    if (!await _userRepository.IsUserNameUniqueAsync(dto.UserName, userId))
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Ce nom d'utilisateur est déjà pris",
                            errors: null,
                            resultCode: 11);
                    }
                    user.UserName = dto.UserName;
                    user.NormalizedUserName = dto.UserName.ToUpper();
                    hasChanges = true;
                }

                // Nom
                if (!string.IsNullOrEmpty(dto.Nom) && dto.Nom != user.Nom)
                {
                    user.Nom = dto.Nom;
                    hasChanges = true;
                }

                // Prénom
                if (!string.IsNullOrEmpty(dto.Prenom) && dto.Prenom != user.Prenom)
                {
                    user.Prenom = dto.Prenom;
                    hasChanges = true;
                }

                // Téléphone
                if (!string.IsNullOrEmpty(dto.PhoneNumber) && dto.PhoneNumber != user.PhoneNumber)
                {
                    var existingPhone = await _userManager.Users
                        .FirstOrDefaultAsync(u => u.PhoneNumber == dto.PhoneNumber && u.Id != userId);
                    if (existingPhone != null)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Ce numéro de téléphone est déjà utilisé",
                            errors: null,
                            resultCode: 12);
                    }
                    user.PhoneNumber = dto.PhoneNumber;
                    hasChanges = true;
                }

                // Date de naissance
                if (dto.BirthDate.HasValue && dto.BirthDate != user.BirthDate)
                {
                    // Vérifier l'âge (18 ans minimum)
                    var today = DateTime.Today;
                    var age = today.Year - dto.BirthDate.Value.Year;
                    if (dto.BirthDate.Value.Date > today.AddYears(-age)) age--;

                    if (age < 18)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Vous devez avoir au moins 18 ans",
                            errors: null,
                            resultCode: 40);
                    }

                    if (age > 120)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "La date de naissance n'est pas valide",
                            errors: null,
                            resultCode: 40);
                    }

                    if (dto.BirthDate.Value.Date > today)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "La date de naissance ne peut pas être dans le futur",
                            errors: null,
                            resultCode: 40);
                    }

                    user.BirthDate = dto.BirthDate;
                    hasChanges = true;
                }

                // Image
                if (!string.IsNullOrEmpty(dto.Image) && dto.Image != user.Image)
                {
                    user.Image = dto.Image;
                    hasChanges = true;
                }                

                // Sauvegarde
                if (hasChanges)
                {
                    var result = await _userRepository.UpdateAsync(user);
                    if (!result.Succeeded)
                    {
                        var errors = result.Errors.Select(e => e.Description).ToList();
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Erreur lors de la mise à jour du profil",
                            errors: errors,
                            resultCode: 21);
                    }
                }

                string message = hasChanges ? "Profil administrateur mis à jour avec succès" : "Aucune modification détectée";
                return ApiResponse<ApplicationUser>.Success(user, message, 0);
            });
        }
    }
}

