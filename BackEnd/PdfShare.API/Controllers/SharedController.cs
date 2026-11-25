using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PdfShare.API.Extensions;
using PdfShare.Domain.DTO.Files.Responses;
using PdfShare.Domain.DTO.ShareLinks.Responses;
using PdfShare.Domain.Interfaces.CoreInterfaces;

namespace PdfShare.API.Controllers;

[AllowAnonymous]
[Route("s")]
public sealed class SharedController(IShareLinksService shareLinksService) : BaseController
{
    /// <summary>File info shown on the share page.</summary>
    [HttpGet("{token}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(SharedFileInfoDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IResult> GetInfo(string token, CancellationToken ct)
    {
        var result = await shareLinksService.GetSharedFileInfoAsync(token, OptionalUserId, ct);
        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    [HttpGet("{token}/download-url")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DownloadUrlDto))]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IResult> GetDownloadUrl(string token, CancellationToken ct)
    {
        var result = await shareLinksService.GetSharedDownloadUrlAsync(token, OptionalUserId, ct);
        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }
}
