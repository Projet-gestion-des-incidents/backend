using Microsoft.Extensions.Logging;
using projet0.Application.Commun.DTOs;
using projet0.Application.Commun.Ressources;
using projet0.Application.Interfaces;
using projet0.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace projet0.Application.Services.User
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserService> _logger;

        public UserService(IUserRepository userRepository, ILogger<UserService> logger)
        {
            _userRepository = userRepository;
            _logger = logger;
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

                    var user = new ApplicationUser
                    {
                        UserName = dto.UserName,
                        Email = dto.Email,
                        Nom = dto.Nom,
                        Prenom = dto.Prenom,
                        Age = dto.Age
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

                    _logger.LogDebug(
                        "SUCCESS CreateUser | UserId = {UserId}",
                        user.Id
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
                    user.Age = dto.Age;

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

                    var result = await _userRepository.DeleteAsync(user);

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

        public async Task<IEnumerable<UserWithRoleDto>> GetAllUsersWithRolesAsync()
        {
            // on utilise le repository qui utilise UserManager
            var usersWithRoles = await _userRepository.GetAllUsersWithRolesAsync();

            // optionnel : on peut logger
            _logger.LogDebug("SUCCESS GetAllUsersWithRoles | Count = {Count}", usersWithRoles?.Count() ?? 0);

            return usersWithRoles;
        }
    }
}
