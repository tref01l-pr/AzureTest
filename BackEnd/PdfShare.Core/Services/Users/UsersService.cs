using PdfShare.Domain.DTO.Users;
using PdfShare.Domain.Interfaces.CoreInterfaces;
using PdfShare.Domain.Interfaces.DataAccessInterfaces;

namespace PdfShare.Core.Services.Users;

public sealed class UsersService(IUsersRepository usersRepository) : IUsersService
{
    public const int MinQueryLength = 2;
    public const int MaxResults = 10;

    public async Task<IReadOnlyList<UserDto>> SearchAsync(string? emailFragment, Guid currentUserId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(emailFragment) || emailFragment.Trim().Length < MinQueryLength)
        {
            return [];
        }

        return await usersRepository.SearchByEmailAsync(emailFragment, currentUserId, MaxResults, ct);
    }
}
