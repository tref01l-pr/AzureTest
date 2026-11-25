using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PdfShare.API.Extensions;
using PdfShare.Domain.Constants;
using PdfShare.Domain.DTO.Common;
using PdfShare.Domain.DTO.Files.Requests;
using PdfShare.Domain.DTO.Files.Responses;
using PdfShare.Domain.Interfaces.CoreInterfaces;

namespace PdfShare.API.Controllers;

/// <summary>The signed-in user's own PDF files.</summary>
[Authorize]
[Route("files")]
public sealed class FilesController(IFilesService filesService) : BaseController
{
    private const long MaxRequestBytes = FileConstraints.MaxFileSizeBytes + (1024 * 1024);

    /// <summary>Uploads a PDF (max 20 MB).</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(PdfFileDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IResult> Upload(IFormFile file, CancellationToken ct)
    {
        if (UserId.IsFailure)
        {
            return UserId.ToProblemDetails();
        }

        await using var stream = file.OpenReadStream();
        var result = await filesService.UploadAsync(
            UserId.Value,
            new UploadPdfCommand(stream, file.FileName, file.ContentType, file.Length),
            ct);

        return result.IsFailure
            ? result.ToProblemDetails()
            : Results.Created($"/files/{result.Value.Id}", result.Value);
    }

    /// <summary>Lists the user's files, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PagedResult<PdfFileDto>))]
    public async Task<IResult> GetMyFiles([FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (UserId.IsFailure)
        {
            return UserId.ToProblemDetails();
        }

        return Results.Ok(await filesService.GetMyFilesAsync(UserId.Value, page, pageSize, ct));
    }

    /// <summary>Files other users shared with me through restricted links (download only).</summary>
    [HttpGet("shared-with-me")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<SharedFileDto>))]
    public async Task<IResult> GetSharedWithMe(CancellationToken ct)
    {
        if (UserId.IsFailure)
        {
            return UserId.ToProblemDetails();
        }

        return Results.Ok(await filesService.GetSharedWithMeAsync(UserId.Value, ct));
    }

    /// <summary>Short-lived (5 min) read-only download URL for the owner.</summary>
    [HttpGet("{id:guid}/download-url")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DownloadUrlDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetDownloadUrl(Guid id, CancellationToken ct)
    {
        if (UserId.IsFailure)
        {
            return UserId.ToProblemDetails();
        }

        var result = await filesService.GetDownloadUrlAsync(UserId.Value, id, ct);
        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    /// <summary>Short-lived URL of the first-page preview (PNG).</summary>
    [HttpGet("{id:guid}/thumbnail-url")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(DownloadUrlDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> GetThumbnailUrl(Guid id, CancellationToken ct)
    {
        if (UserId.IsFailure)
        {
            return UserId.ToProblemDetails();
        }

        var result = await filesService.GetThumbnailUrlAsync(UserId.Value, id, ct);
        return result.IsFailure ? result.ToProblemDetails() : Results.Ok(result.Value);
    }

    /// <summary>Deletes the file, its preview and all of its share links.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IResult> Delete(Guid id, CancellationToken ct)
    {
        if (UserId.IsFailure)
        {
            return UserId.ToProblemDetails();
        }

        var result = await filesService.DeleteAsync(UserId.Value, id, ct);
        return result.IsFailure ? result.ToProblemDetails() : Results.NoContent();
    }
}
