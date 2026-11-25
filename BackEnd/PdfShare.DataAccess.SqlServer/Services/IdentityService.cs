using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PdfShare.DataAccess.SqlServer.Entities.UserEntities;
using PdfShare.Domain;
using PdfShare.Domain.DTO.Authentication.Responses;
using PdfShare.Domain.Errors;
using PdfShare.Domain.Interfaces.DataAccessInterfaces;

namespace PdfShare.DataAccess.SqlServer.Services;

public sealed class IdentityService(
    UserManager<UserEntity> userManager,
    RoleManager<RoleEntity> roleManager,
    TimeProvider timeProvider) : IIdentityService
{
    public async Task<Result<UserCredentialsDto>> RegisterAsync(string email, string password, Roles role, CancellationToken ct = default)
    {
        if (await userManager.FindByEmailAsync(email) is not null)
        {
            return AuthErrors.EmailTaken().ToResult<UserCredentialsDto>();
        }

        var roleName = role.ToString();
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new RoleEntity { Name = roleName });
        }

        var user = new UserEntity
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = email,
            CreatedAt = timeProvider.GetUtcNow(),
        };

        var created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            return ToError(created, AuthErrors.RegistrationFailed).ToResult<UserCredentialsDto>();
        }

        var addedToRole = await userManager.AddToRoleAsync(user, roleName);
        if (!addedToRole.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return ToError(addedToRole, AuthErrors.RegistrationFailed).ToResult<UserCredentialsDto>();
        }

        return new UserCredentialsDto(user.Id, email, roleName);
    }

    public async Task<Result<UserCredentialsDto>> CheckPasswordAsync(string email, string password, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user is null)
        {
            return AuthErrors.InvalidCredentials().ToResult<UserCredentialsDto>();
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            return AuthErrors.LockedOut().ToResult<UserCredentialsDto>();
        }

        if (!await userManager.CheckPasswordAsync(user, password))
        {
            await userManager.AccessFailedAsync(user);
            return AuthErrors.InvalidCredentials().ToResult<UserCredentialsDto>();
        }

        await userManager.ResetAccessFailedCountAsync(user);
        return await ToCredentialsAsync(user);
    }

    public async Task<Result<UserCredentialsDto>> GetCredentialsAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        return user is null
            ? AuthErrors.UserNotFound().ToResult<UserCredentialsDto>()
            : await ToCredentialsAsync(user);
    }

    public async Task<Result> ChangePasswordAsync(Guid userId, string currentPassword, string newPassword, CancellationToken ct = default)
    {
        var user = await userManager.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            return AuthErrors.UserNotFound();
        }

        var changed = await userManager.ChangePasswordAsync(user, currentPassword, newPassword);
        return changed.Succeeded ? Result.Success() : ToError(changed, AuthErrors.ChangePasswordFailed);
    }

    private async Task<UserCredentialsDto> ToCredentialsAsync(UserEntity user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new UserCredentialsDto(user.Id, user.Email!, roles.FirstOrDefault() ?? nameof(Roles.User));
    }

    private static PdfShare.SharedKernel.Error ToError(IdentityResult result, Func<string, PdfShare.SharedKernel.Error> factory) =>
        factory(string.Join(" ", result.Errors.Select(e => e.Description)));
}
