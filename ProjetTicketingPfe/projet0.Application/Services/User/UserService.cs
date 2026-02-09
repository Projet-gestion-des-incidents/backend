using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using projet0.Application.Commun.DTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using System;
using Microsoft.Extensions.Hosting; 
using System.Linq; 
using System.IO;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;

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
                    // Vérifier que le rôle existe
                    var role = await _roleManager.FindByIdAsync(dto.RoleId.ToString());
                    if (role == null)
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Le rôle spécifié n'existe pas",
                            resultCode: 13);
                    }

                    // Optionnel : Empêcher la création d'Admin par ce service
                    if (role.Name == "Admin")
                    {
                        return ApiResponse<ApplicationUser>.Failure(
                            message: "Impossible de créer un utilisateur Admin via ce service",
                            resultCode: 14);
                    }

                    string imageUrl = null;
                    if (!string.IsNullOrEmpty(dto.Image))
                    {
                        try
                        {
                            // Appeler une méthode pour sauvegarder l'image Base64
                            imageUrl = await SaveBase64ImageAsync(dto.Image);
                            Console.WriteLine($"Image sauvegardée, URL: {imageUrl}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Erreur sauvegarde image: {ex.Message}");
                            // Vous pouvez décider de continuer sans image ou retourner une erreur
                        }
                    }
                    var user = new ApplicationUser
                    {
                        UserName = dto.UserName,
                        Email = dto.Email,
                        Nom = dto.Nom,
                        Prenom = dto.Prenom,

                        PhoneNumber = dto.PhoneNumber,
                        BirthDate = dto.BirthDate,
                        Image = imageUrl // ICI : ASSIGNER L'URL DE L'IMAGE


                    };

                    var result = await _userRepository.CreateAsync(user, dto.Password);

                    if (!result.Succeeded)
                    {
                        _logger.LogError(
                            "DB_ERROR CreateUser failed | {@Errors}",
                            result.Errors
                        );
                        return ApiResponse<ApplicationUser>.Failure(
                                           message: UserMessages.CreateUserError,
                                           errors: result.Errors.Select(e => e.Description).ToList(),
                                           resultCode: 12);
                    }



                    var roleResult = await _userManager.AddToRoleAsync(user, role.Name);
                    if (!roleResult.Succeeded)
                    {
                        _logger.LogWarning(
                            "Role assignment failed for user {UserId} | Role: {RoleName}",
                            user.Id,
                            role.Name
                        );
                    }



                    var roleAssignmentResult = await _userManager.AddToRoleAsync(user, role.Name);

                    if (!roleAssignmentResult.Succeeded)
                    {
                        _logger.LogWarning(
                            "Role assignment failed for user {UserId} | Role: {RoleName} | Errors: {@Errors}",
                            user.Id,
                            role.Name,
                            roleAssignmentResult.Errors
                        );

                        // Option 1: Supprimer l'utilisateur si le rôle échoue
                        // await _userManager.DeleteAsync(user);
                        // return ApiResponse<ApplicationUser>.Failure(
                        //     message: "Échec de l'assignation du rôle",
                        //     errors: roleAssignmentResult.Errors.Select(e => e.Description).ToList(),
                        //     resultCode: 15
                        // );

                        // Option 2: Continuer mais logger l'erreur (choisi celle-ci pour le moment)
                        // L'utilisateur est créé mais sans rôle
                    }

                    // 3. Optionnel : Confirmer automatiquement l'email pour les utilisateurs créés par admin
                    user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(user);

                    _logger.LogDebug(
                        "SUCCESS CreateUser | UserId = {UserId} | Email: {Email} | Role: {RoleName}",
                        user.Id,
                        user.Email,
                        role.Name
                    );

                    return ApiResponse<ApplicationUser>.Success(
                        data: user,
                        message: "Utilisateur créé avec succès",
                        resultCode: 0
                    );
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

        // ================= DELETE =================
        public Task<ApiResponse<string>> DeleteAsync(Guid id)
            => MeasureAsync(
                actionName: "DeleteUser",
                input: new { UserId = id },
                async () =>
                {
                    var user = await _userRepository.GetByIdAsync(id);

                    if (user == null || user.IsDeleted)
                    {
                        _logger.LogWarning(
                            "NOT_FOUND DeleteUser | UserId = {UserId}",
                            id
                        );

                        return ApiResponse<string>.Failure(
                            message: UserMessages.UserNotFound,
                            resultCode: 20);
                    }

                    var result = await _userRepository.SoftDeleteAsync(user);

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

                    _logger.LogDebug(
                        "SUCCESS DeleteUser | UserId = {UserId} | UserName = {UserName}",
                        user.Id,
                        user.UserName
                    );

                    return ApiResponse<string>.Success(
                        message: "Utilisateur supprimé avec succès",
                        resultCode: 0
                    );
                });
        public async Task<ApiResponse<string>> ActivateAsync(Guid id)
        {
            var user = await _userRepository.GetByIdAsync(id);

            if (user == null)
                return ApiResponse<string>.Failure(
    message: "Utilisateur introuvable",
    errors: null,
    resultCode: 20
);


            var result = await _userRepository.RestoreAsync(user);

            if (!result.Succeeded)
                return ApiResponse<string>.Failure(
                     message: "Erreur activation", errors: null,
    resultCode: 22);

            return ApiResponse<string>.Success(
    message: "Utilisateur activé",
    resultCode: 0
);

        }

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
                    var user = await _userRepository.GetByIdAsync(userId);
                    if (user == null)
                    {
                        _logger.LogWarning("User not found | UserId = {UserId}", userId);
                        return ApiResponse<ApplicationUser>.Failure(
                            message: UserMessages.UserNotFound,
                            resultCode: 20
                        );
                    }

                    // Mise à jour des champs modifiables
                    user.UserName = dto.UserName ?? user.UserName;
                    user.Email = dto.Email ?? user.Email;
                    user.Nom = dto.Nom ?? user.Nom;
                    user.Prenom = dto.Prenom ?? user.Prenom;
                    user.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;
                    //user.BirthDate = dto.BirthDate ?? user.BirthDate;

                    // Gestion de l'image
                    //if (!string.IsNullOrEmpty(dto.Image))
                    //{
                    //    user.Image = await SaveBase64ImageAsync(dto.Image);
                    //}

                    var result = await _userRepository.UpdateAsync(user);
                    if (!result.Succeeded)
                    {
                        _logger.LogError("DB_ERROR EditProfile | UserId = {UserId} | {@Errors}", userId, result.Errors);
                        return ApiResponse<ApplicationUser>.Failure(
                            message: UserMessages.UpdateUserError,
                            resultCode: 21
                        );
                    }

                    return ApiResponse<ApplicationUser>.Success(
                        data: user,
                        message: "Profil mis à jour avec succès",
                        resultCode: 0
                    );
                });
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
    }
}
