using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using projet0.Application.Common.Models.Pagination;
using projet0.Application.Commun.DTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
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


        public UserService(IUserRepository userRepository, ILogger<UserService> logger, UserManager<ApplicationUser> userManager, RoleManager<IdentityRole<Guid>> roleManager, IHostEnvironment webHostEnvironment) 
        {
            _userRepository = userRepository;
            _logger = logger;
            _roleManager = roleManager;
            _userManager = userManager;
            _webHostEnvironment = webHostEnvironment; 
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

        // ================= CREATE =================
        public Task<ApiResponse<ApplicationUser>> CreateAsync(UserDto dto)
    => MeasureAsync(
        actionName: "CreateUser",
        input: dto,
        async () =>
        {
            // 1. Validation de l'email
            if (!await _userRepository.IsEmailUniqueAsync(dto.Email))
            {
                _logger.LogWarning(
                    "BUSINESS_RULE EmailAlreadyUsed | {Email}",
                    dto.Email
                );

                return ApiResponse<ApplicationUser>.Failure(
                    message: UserMessages.EmailAlreadyUsed,
                    resultCode: 10);
            }

            // 2. Validation du nom d'utilisateur
            if (!await _userRepository.IsUserNameUniqueAsync(dto.UserName))
            {
                _logger.LogWarning(
                    "BUSINESS_RULE UserNameAlreadyUsed | {UserName}",
                    dto.UserName
                );

                return ApiResponse<ApplicationUser>.Failure(
                    message: UserMessages.UserNameAlreadyUsed,
                    resultCode: 11
                );
            }

            // 3. Vérification du rôle
            var role = await _roleManager.FindByIdAsync(dto.RoleId.ToString());
            if (role == null)
            {
                return ApiResponse<ApplicationUser>.Failure(
                    message: "Le rôle spécifié n'existe pas",
                    resultCode: 13);
            }

            // 4. Empêcher la création d'Admin
            if (role.Name == "Admin")
            {
                return ApiResponse<ApplicationUser>.Failure(
                    message: "Impossible de créer un utilisateur Admin via ce service",
                    resultCode: 14);
            }

            // 5. Gestion de l'image
            string imageUrl = null;
            if (!string.IsNullOrEmpty(dto.Image))
            {
                try
                {
                    // Accepter soit Base64, soit URL existante
                    if (dto.Image.StartsWith("data:image"))
                    {
                        imageUrl = await SaveBase64ImageAsync(dto.Image);
                    }
                    else
                    {
                        // Si c'est déjà une URL, l'utiliser directement
                        imageUrl = dto.Image;
                    }
                    _logger.LogDebug("Image sauvegardée, URL: {ImageUrl}", imageUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erreur sauvegarde image: {Message}", ex.Message);
                    // Continuer sans image
                }
            }

            // 6. Création de l'utilisateur
            var user = new ApplicationUser
            {
                UserName = dto.UserName,
                Email = dto.Email,
                Nom = dto.Nom,
                Prenom = dto.Prenom,
                PhoneNumber = dto.PhoneNumber,
                BirthDate = dto.BirthDate,
                Image = imageUrl,
                EmailConfirmed = true // Email confirmé automatiquement pour les utilisateurs créés par admin
            };

            // 7. Mot de passe par défaut
            string defaultPassword = "Azerty123";

            var result = await _userRepository.CreateAsync(user, defaultPassword);

            if (!result.Succeeded)
            {
                _logger.LogError(
                    "DB_ERROR CreateUser failed | {@Errors}",
                    result.Errors
                );

                // Vérifier si l'erreur est liée au mot de passe
                var passwordError = result.Errors.FirstOrDefault(e => e.Code.Contains("Password"));
                if (passwordError != null)
                {
                    return ApiResponse<ApplicationUser>.Failure(
                        message: $"Le mot de passe par défaut ne respecte pas les règles de sécurité. {passwordError.Description}",
                        errors: result.Errors.Select(e => e.Description).ToList(),
                        resultCode: 16
                    );
                }

                return ApiResponse<ApplicationUser>.Failure(
                    message: UserMessages.CreateUserError,
                    errors: result.Errors.Select(e => e.Description).ToList(),
                    resultCode: 12
                );
            }

            // 8. Assignation du rôle
            var roleResult = await _userManager.AddToRoleAsync(user, role.Name);

            if (!roleResult.Succeeded)
            {
                _logger.LogWarning(
                    "Role assignment failed for user {UserId} | Role: {RoleName} | Errors: {@Errors}",
                    user.Id,
                    role.Name,
                    roleResult.Errors
                );

                // Option: Vous pouvez décider de supprimer l'utilisateur si le rôle échoue
                // await _userManager.DeleteAsync(user);
                // return ApiResponse<ApplicationUser>.Failure(
                //     message: "Échec de l'assignation du rôle",
                //     errors: roleResult.Errors.Select(e => e.Description).ToList(),
                //     resultCode: 15
                // );
            }

            _logger.LogInformation(
                "SUCCESS CreateUser | UserId = {UserId} | Email: {Email} | Role: {RoleName} | DefaultPassword: {Password}",
                user.Id,
                user.Email,
                role.Name,
                defaultPassword
            );

            // 9. Retourner la réponse avec des informations supplémentaires
            var responseData = new
            {
                User = user,
                DefaultPassword = defaultPassword,
                Message = $"Utilisateur créé avec succès. Mot de passe par défaut: {defaultPassword}"
            };

            return ApiResponse<ApplicationUser>.Success(
                data: user,
                message: $"Utilisateur créé avec succès. Mot de passe par défaut: {defaultPassword}",
                resultCode: 0
            );

            // Dans CreateAsync, après la création réussie :
            /*try
            {
                // Envoyer un email avec le mot de passe par défaut
                var emailBody = $@"
        Bonjour {user.Nom} {user.Prenom},
        
        Votre compte a été créé avec succès.
        
        Identifiants de connexion :
        Email: {user.Email}
        Mot de passe temporaire: {defaultPassword}
        
        Veuillez changer votre mot de passe dès votre première connexion.
        
        Cordialement,
        L'équipe d'administration
    ";

                // _emailService.SendEmailAsync(user.Email, "Votre compte a été créé", emailBody);
                _logger.LogInformation("Email de bienvenue envoyé à {Email}", user.Email);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Erreur lors de l'envoi de l'email de bienvenue");
            }*/
        });

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
                    //user.Age = dto.Age;

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


        public async Task<IEnumerable<UserWithRoleDto>> GetAllUsersWithRolesAsync()
        {
            // on utilise le repository qui utilise UserManager
            var usersWithRoles = await _userRepository.GetAllUsersWithRolesAsync();

            // optionnel : on peut logger
            _logger.LogDebug("SUCCESS GetAllUsersWithRoles | Count = {Count}", usersWithRoles?.Count() ?? 0);

            return usersWithRoles;
        }
        public async Task<ApiResponse<ApplicationUser>> EditProfileAsync(Guid userId, EditProfileDto dto)
        {
            return await MeasureAsync(
                "EditProfile",
                new { userId, dto },
                async () =>
                {
                    // 1. Récupérer l'utilisateur
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user == null)
                    {
                        _logger.LogWarning("User not found | UserId = {UserId}", userId);
                        return ApiResponse<ApplicationUser>.Failure(
                            message: UserMessages.UserNotFound,
                            resultCode: 20
                        );
                    }

                    // 2. Journaliser la requête reçue (pour debug)
                    _logger.LogDebug("EditProfile request received for user {UserId}", userId);
                    _logger.LogDebug("Image data received (first 100 chars): {ImagePreview}",
                        dto.Image?.Length > 100 ? dto.Image.Substring(0, 100) + "..." : dto.Image);

                    // 3. Vérifier l'unicité de l'email si modifié
                    if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
                    {
                        if (!await _userRepository.IsEmailUniqueAsync(dto.Email, userId))
                        {
                            _logger.LogWarning("EmailAlreadyUsed EditProfile | UserId = {UserId} | Email: {Email}",
                                userId, dto.Email);
                            return ApiResponse<ApplicationUser>.Failure(
                                message: UserMessages.EmailAlreadyUsed,
                                resultCode: 10
                            );
                        }
                    }

                    // 4. Vérifier l'unicité du nom d'utilisateur si modifié
                    if (!string.IsNullOrEmpty(dto.UserName) && dto.UserName != user.UserName)
                    {
                        if (!await _userRepository.IsUserNameUniqueAsync(dto.UserName, userId))
                        {
                            _logger.LogWarning("UserNameAlreadyUsed EditProfile | UserId = {UserId} | UserName: {UserName}",
                                userId, dto.UserName);
                            return ApiResponse<ApplicationUser>.Failure(
                                message: UserMessages.UserNameAlreadyUsed,
                                resultCode: 11
                            );
                        }
                    }

                    // 5. Vérifier le mot de passe si fourni
                    if (!string.IsNullOrEmpty(dto.CurrentPassword) &&
                        !string.IsNullOrEmpty(dto.NewPassword))
                    {
                        // Vérifier l'ancien mot de passe
                        var passwordValid = await _userManager.CheckPasswordAsync(user, dto.CurrentPassword);
                        if (!passwordValid)
                        {
                            _logger.LogWarning("Invalid current password | UserId = {UserId}", userId);
                            return ApiResponse<ApplicationUser>.Failure(
                                message: "Le mot de passe actuel est incorrect",
                                resultCode: 25
                            );
                        }

                        // Vérifier que newPassword et confirmPassword correspondent
                        if (dto.NewPassword != dto.ConfirmPassword)
                        {
                            _logger.LogWarning("Passwords don't match | UserId = {UserId}", userId);
                            return ApiResponse<ApplicationUser>.Failure(
                                message: "Le nouveau mot de passe et la confirmation ne correspondent pas",
                                resultCode: 26
                            );
                        }

                        // Changer le mot de passe
                        var changePasswordResult = await _userManager.ChangePasswordAsync(
                            user,
                            dto.CurrentPassword,
                            dto.NewPassword
                        );

                        if (!changePasswordResult.Succeeded)
                        {
                            _logger.LogError("Password change failed | UserId = {UserId} | Errors: {@Errors}",
                                userId, changePasswordResult.Errors);
                            return ApiResponse<ApplicationUser>.Failure(
                                message: "Erreur lors du changement de mot de passe",
                                errors: changePasswordResult.Errors.Select(e => e.Description).ToList(),
                                resultCode: 27
                            );
                        }
                    }

                    // 6. Mise à jour des informations de base
                    bool hasChanges = false;
                    bool emailChanged = false;

                    if (!string.IsNullOrEmpty(dto.UserName) && dto.UserName != user.UserName)
                    {
                        user.UserName = dto.UserName;
                        hasChanges = true;
                    }

                    if (!string.IsNullOrEmpty(dto.Email) && dto.Email != user.Email)
                    {
                        user.Email = dto.Email;
                        user.EmailConfirmed = false; // Réinitialiser la confirmation si email changé
                        hasChanges = true;
                        emailChanged = true;
                    }

                    if (!string.IsNullOrEmpty(dto.Nom) && dto.Nom != user.Nom)
                    {
                        user.Nom = dto.Nom;
                        hasChanges = true;
                    }

                    if (!string.IsNullOrEmpty(dto.Prenom) && dto.Prenom != user.Prenom)
                    {
                        user.Prenom = dto.Prenom;
                        hasChanges = true;
                    }

                    if (!string.IsNullOrEmpty(dto.PhoneNumber) && dto.PhoneNumber != user.PhoneNumber)
                    {
                        user.PhoneNumber = dto.PhoneNumber;
                        hasChanges = true;
                    }

                    if (dto.BirthDate.HasValue && dto.BirthDate != user.BirthDate)
                    {
                        user.BirthDate = dto.BirthDate.Value;
                        hasChanges = true;
                    }

                    // 7. Gestion de l'image de profil (version simplifiée)
                    if (!string.IsNullOrEmpty(dto.Image) && dto.Image != user.Image)
                    {
                        // Accepter n'importe quelle chaîne non vide comme image
                        user.Image = dto.Image;
                        hasChanges = true;
                        _logger.LogDebug("Profile image updated for user {UserId}", userId);
                    }

                    // 8. Sauvegarder les modifications si nécessaire
                    if (hasChanges)
                    {
                        try
                        {
                            var result = await _userRepository.UpdateAsync(user);
                            if (!result.Succeeded)
                            {
                                _logger.LogError("DB_ERROR EditProfile | UserId = {UserId} | {@Errors}",
                                    userId, result.Errors);

                                // Journaliser les erreurs spécifiques
                                foreach (var error in result.Errors)
                                {
                                    _logger.LogError("Identity error: {Code} - {Description}",
                                        error.Code, error.Description);
                                }

                                return ApiResponse<ApplicationUser>.Failure(
                                    message: UserMessages.UpdateUserError,
                                    errors: result.Errors.Select(e => e.Description).ToList(),
                                    resultCode: 21
                                );
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Exception during user update | UserId = {UserId}", userId);
                            return ApiResponse<ApplicationUser>.Failure(
                                message: "Erreur technique lors de la mise à jour du profil",
                                resultCode: 28
                            );
                        }
                    }

                    // 9. Si l'email a été changé, envoyer un email de confirmation
                    if (emailChanged)
                    {
                        try
                        {
                            // Générer un token de confirmation
                            var emailToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                            // Ici vous pouvez envoyer l'email de confirmation
                            // _emailService.SendEmailConfirmationAsync(user.Email, emailToken);

                            _logger.LogInformation("Email changed, confirmation required | UserId = {UserId}", userId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to generate email confirmation token | UserId = {UserId}", userId);
                        }
                    }

                    _logger.LogInformation(
                        "SUCCESS EditProfile | UserId = {UserId} | HasChanges: {HasChanges} | EmailChanged: {EmailChanged} | ImageUpdated: {ImageUpdated}",
                        userId, hasChanges, emailChanged, !string.IsNullOrEmpty(dto.Image)
                    );

                    return ApiResponse<ApplicationUser>.Success(
                        data: user,
                        message: hasChanges
                            ? "Profil mis à jour avec succès"
                            : "Aucune modification détectée",
                        resultCode: 0
                    );
                });
        }

        // Méthode pour supprimer l'ancienne image
        private async Task DeleteOldImageAsync(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl) || imageUrl.Contains("default-avatar"))
                    return;

                var webRootPath = _webHostEnvironment.ContentRootPath;
                var imagePath = Path.Combine(webRootPath, imageUrl.TrimStart('/'));

                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                    _logger.LogDebug("Old profile image deleted: {ImagePath}", imagePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting old profile image: {ImageUrl}", imageUrl);
            }
        }
        private async Task<string> SaveBase64ImageAsync(string base64String)
        {
            try
            {
                Console.WriteLine($"Sauvegarde image Base64, longueur: {base64String.Length}");

                if (string.IsNullOrEmpty(base64String))
                    return null;

                // Vérifier si c'est un Base64 valide
                if (!base64String.Contains(","))
                {
                    // Si le frontend envoie déjà le Base64 propre (sans préfixe)
                    base64String = "data:image/jpeg;base64," + base64String;
                }

                var base64Data = base64String.Split(',')[1];

                // Déterminer l'extension
                string extension = ".jpg";
                if (base64String.Contains("data:image/png"))
                    extension = ".png";
                else if (base64String.Contains("data:image/gif"))
                    extension = ".gif";
                else if (base64String.Contains("data:image/webp"))
                    extension = ".webp";

                // Créer un nom unique
                var fileName = $"{Guid.NewGuid()}{extension}";

                // Chemin de sauvegarde
                var webRootPath = _webHostEnvironment.ContentRootPath;
                var uploadsFolder = Path.Combine(webRootPath, "uploads", "users");

                // Créer le dossier s'il n'existe pas
                if (!Directory.Exists(uploadsFolder))
                {
                    Console.WriteLine($"Création du dossier: {uploadsFolder}");
                    Directory.CreateDirectory(uploadsFolder);
                }

                var filePath = Path.Combine(uploadsFolder, fileName);

                // Convertir Base64 en bytes et sauvegarder
                var imageBytes = Convert.FromBase64String(base64Data);
                await System.IO.File.WriteAllBytesAsync(filePath, imageBytes);

                // Retourner l'URL relative
                return $"/uploads/users/{fileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur sauvegarde image: {ex.Message}");
                throw;
            }
        }
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
                Image = user.Image
            };
        }

        // ================= DELETE (SUPPRESSION DÉFINITIVE) =================
        public Task<ApiResponse<string>> DeleteAsync(Guid id)
            => MeasureAsync(
                actionName: "DeleteUser",
                input: new { UserId = id },
                async () =>
                {
                    var user = await _userRepository.GetByIdAsync(id);

                    if (user == null)
                    {
                        _logger.LogWarning(
                            "NOT_FOUND DeleteUser | UserId = {UserId}",
                            id
                        );

                        return ApiResponse<string>.Failure(
                            message: UserMessages.UserNotFound,
                            resultCode: 20);
                    }

                    // OPTION 1: Suppression définitive (recommandée pour les admins)
                    var result = await _userManager.DeleteAsync(user);

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
                        message: "Utilisateur supprimé définitivement avec succès",
                        resultCode: 0
                    );
                });

        // ================= SEARCH USERS (avec rôles) =================
        public async Task<ApiResponse<PagedResult<UserWithRoleDto>>> SearchUsersAsync(UserSearchRequest request)
        {
            return await MeasureAsync(
                actionName: "SearchUsers",
                input: request,
                async () =>
                {
                    try
                    {
                        // 1. Créer la query de base
                        var query = _userManager.Users.AsQueryable();

                        // 2. Recherche globale
                        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
                        {
                            var term = request.SearchTerm.ToLower();
                            query = query.Where(u =>
                                (u.Nom != null && u.Nom.ToLower().Contains(term)) ||
                                (u.Prenom != null && u.Prenom.ToLower().Contains(term)) ||
                                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                                (u.UserName != null && u.UserName.ToLower().Contains(term)));
                        }

                        // 3. Filtres additionnels
                        if (request.Statut.HasValue)
                            query = query.Where(u => u.Statut == request.Statut.Value);

                        if (!string.IsNullOrWhiteSpace(request.UserName))
                            query = query.Where(u => u.UserName.Contains(request.UserName));

                        if (!string.IsNullOrWhiteSpace(request.Email))
                            query = query.Where(u => u.Email.Contains(request.Email));

                        if (!string.IsNullOrWhiteSpace(request.Nom))
                            query = query.Where(u => u.Nom.Contains(request.Nom));

                        if (!string.IsNullOrWhiteSpace(request.Prenom))
                            query = query.Where(u => u.Prenom.Contains(request.Prenom));

                        if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                            query = query.Where(u => u.PhoneNumber != null && u.PhoneNumber.Contains(request.PhoneNumber));

                        if (request.BirthDate.HasValue)
                        {
                             var year = request.BirthDate.Value.Year;
                             query = query.Where(u => u.BirthDate.HasValue && 
                                                    u.BirthDate.Value.Year == year);
                        }

                        // 4. APPLIQUER LE TRI AVANT PAGINATION
                        query = ApplySorting(query, request.SortBy, request.SortDescending);

                        // 5. Récupérer TOUS les utilisateurs filtrés
                        var allFilteredUsers = await query.ToListAsync();

                        // 6. Filtrer par rôle (insensible à la casse)
                        var usersWithRoles = new List<(ApplicationUser User, string RoleName)>();

                        foreach (var user in allFilteredUsers)
                        {
                            var roles = await _userManager.GetRolesAsync(user);
                            var roleName = roles.FirstOrDefault() ?? "USER";

                            // Filtre par rôle insensible à la casse
                            if (string.IsNullOrWhiteSpace(request.Role) ||
                                string.Equals(roleName, request.Role, StringComparison.OrdinalIgnoreCase))
                            {
                                usersWithRoles.Add((user, roleName));
                            }
                        }

                        // 7. Pagination
                        var totalCount = usersWithRoles.Count;
                        var page = Math.Max(1, request.Page);
                        var pageSize = Math.Clamp(request.PageSize, 1, 100);

                        var skip = (page - 1) * pageSize;
                        if (skip >= totalCount && totalCount > 0)
                        {
                            page = (int)Math.Ceiling(totalCount / (double)pageSize);
                            skip = (page - 1) * pageSize;
                        }

                        var paginatedUsers = usersWithRoles
                            .Skip(skip)
                            .Take(pageSize)
                            .ToList();

                        // 8. Convertir en DTO
                        var userDtos = paginatedUsers.Select(x => new UserWithRoleDto
                        {
                            Id = x.User.Id,
                            UserName = x.User.UserName,
                            Nom = x.User.Nom,
                            Prenom = x.User.Prenom,
                            Email = x.User.Email,
                            PhoneNumber = x.User.PhoneNumber,
                            Image = x.User.Image,
                            Role = x.RoleName,
                            Statut = x.User.Statut
                        }).ToList();

                        // 9. Créer le résultat
                        var pagedResult = new PagedResult<UserWithRoleDto>
                        {
                            Items = userDtos,
                            TotalCount = totalCount,
                            Page = page,
                            PageSize = pageSize
                        };

                        _logger.LogInformation(
                            "SUCCESS SearchUsers | Role: {Role} | Sort: {SortBy} {SortDescending} | Total: {TotalCount}",
                            request.Role, request.SortBy, request.SortDescending, totalCount
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
        private IQueryable<ApplicationUser> ApplySorting(
            IQueryable<ApplicationUser> query,
            string sortBy,
            bool sortDescending)
        {
            if (string.IsNullOrWhiteSpace(sortBy))
                return query.OrderBy(u => u.Nom); // Tri par défaut

            // Normaliser le nom du champ
            var normalizedSortBy = sortBy.ToLower().Trim();

            return normalizedSortBy switch
            {
                "username" or "user_name" or "username" =>
                    sortDescending ? query.OrderByDescending(u => u.UserName) : query.OrderBy(u => u.UserName),

                "email" =>
                    sortDescending ? query.OrderByDescending(u => u.Email) : query.OrderBy(u => u.Email),

                "nom" or "name" or "lastname" =>
                    sortDescending ? query.OrderByDescending(u => u.Nom) : query.OrderBy(u => u.Nom),

                "prenom" or "firstname" or "prenom" =>
                    sortDescending ? query.OrderByDescending(u => u.Prenom) : query.OrderBy(u => u.Prenom),

                "birthdate" or "birth_date" or "date" =>
                    sortDescending ? query.OrderByDescending(u => u.BirthDate) : query.OrderBy(u => u.BirthDate),

                "statut" or "status" =>
                    sortDescending ? query.OrderByDescending(u => u.Statut) : query.OrderBy(u => u.Statut),

                

                _ => query.OrderBy(u => u.Nom) // Tri par défaut
            };
        }

        public Task<ApiResponse<IEnumerable<UserWithRoleDto>>> SearchUsersAsync(string searchTerm)
        {
            throw new NotImplementedException();
        }


    }
}

