using System.Net.Mime;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PdfShare.API.Extensions;
using PdfShare.Domain.DTO.ShareLinks.Requests;
using PdfShare.Domain.DTO.ShareLinks.Responses;
using PdfShare.Domain.Interfaces.CoreInterfaces;

namespace PdfShare.API.Controllers;

/// <summary>Share links management. Only the owner of the file can use these endpoints.</summary>
[Authorize]
[Route("")]
public sealed class ShareLinksController(IShareLinksService shareLinksService) : BaseController
{
    /// <summary>Creates a public link, or a restricted link for the selected users.</summary>
    [HttpPost("files/{fileId:guid}/links")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(ShareLinkDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> Create(Guid fileId, [FromBody] CreateShareLinkRequest request, CancellationToken ct)
    {
        if (UserId.IsFailure)
        {
            return UserId.ToProblemDetails();
        }

        var result = await shareLinksService.CreateAsync(UserId.Value, fileId, request, ct);
        return result.IsFailure
            ? result.ToProblemDetails()
            : Results.Created($"/s/{result.Value.Token}", result.Value);
    }

    /// <summary>All links of a file, including revoked ones.</summary>
    [HttpGet("files/{fileId:guid}/links")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<ShareLinkDto>))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetByFile(Guid fileId, CancellationToken ct)
    {
        if (UserId.IsFailure)
        {
            return UserId.ToProblemDetails();
        }

        var result = await shareLinksService.GetByFileAsync(UserId.Value, fileId, ct);
        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    /// <summary>Replaces the user list of a restricted link.</summary>
    [HttpPut("links/{id:guid}/users")]
    [Consumes(MediaTypeNames.Application.Json)]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ShareLinkDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> UpdateUsers(Guid id, [FromBody] UpdateShareLinkUsersRequest request, CancellationToken ct)
    {
        if (UserId.IsFailure)
        {
            return UserId.ToProblemDetails();
        }

        var result = await shareLinksService.UpdateUsersAsync(UserId.Value, id, request, ct);
        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    /// <summary>Revokes a link. It stops working immediately.</summary>
    [HttpDelete("links/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> Revoke(Guid id, CancellationToken ct)
    {
        if (UserId.IsFailure)
        {
            return UserId.ToProblemDetails();
        }

        var result = await shareLinksService.RevokeAsync(UserId.Value, id, ct);
        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }
}
