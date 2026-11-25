using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PdfShare.API.Extensions;
using PdfShare.Domain.DTO.Users;
using PdfShare.Domain.Interfaces.CoreInterfaces;

namespace PdfShare.API.Controllers;

/// <summary>User lookup for choosing who can access a restricted link.</summary>
[Authorize]
[Route("users")]
public sealed class UsersController(IUsersService usersService) : BaseController
{
    /// <summary>Finds users whose email starts with <paramref name="email"/> (min 2 chars, max 10 results).</summary>
    [HttpGet("search")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<UserDto>))]
    public async Task<IResult> Search([FromQuery] string? email, CancellationToken ct)
    {
        if (UserId.IsFailure)
        {
            return UserId.ToProblemDetails();
        }

        return Results.Ok(await usersService.SearchAsync(email, UserId.Value, ct));
    }
}
