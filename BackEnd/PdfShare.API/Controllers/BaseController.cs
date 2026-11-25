using System.Net.Mime;
using System.Security.Claims;
using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Mvc;
using PdfShare.Domain.Constants;
using PdfShare.Domain.Errors;

namespace PdfShare.API.Controllers;

[ApiController]
[Produces(MediaTypeNames.Application.Json)]
public abstract class BaseController : ControllerBase
{
    /// <summary>Current user id from the JWT, or a failure when the request is anonymous.</summary>
    protected Result<Guid> UserId =>
        OptionalUserId is { } id ? id : AuthErrors.MissingUserId().ToResult<Guid>();

    /// <summary>Current user id, or null for anonymous visitors (used by public share links).</summary>
    protected Guid? OptionalUserId =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    protected Guid? SessionId =>
        Guid.TryParse(User.FindFirstValue(ClaimNames.SessionId), out var id) ? id : null;
}
