namespace PdfShare.Domain.DTO.Users;

public sealed class UserDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
}
