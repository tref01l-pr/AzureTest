using PdfShare.SharedKernel;

namespace PdfShare.Domain.Errors;

public static class AuthErrors
{
    public static Error InvalidCredentials() => new("Auth.InvalidCredentials", "Invalid email or password.");
    public static Error LockedOut() => new("Auth.LockedOut", "Too many failed attempts. Try again later.");
    public static Error EmailTaken() => new("Auth.Conflict", "A user with this email already exists.");
    public static Error RegistrationFailed(string details) => new("Auth.RegistrationFailed", details);
    public static Error InvalidRefreshToken() => new("Auth.Unauthorized", "Session expired. Please sign in again.");
    public static Error UserNotFound() => new("User.NotFound", "User not found.");
    public static Error ChangePasswordFailed(string details) => new("Auth.ChangePasswordFailed", details);
    public static Error MissingUserId() => new("Auth.Unauthorized", "User is not authenticated.");
}

public static class FileErrors
{
    public static Error NotFound() => new("File.NotFound", "File not found.");
    public static Error Empty() => new("File.Empty", "File is empty.");
    public static Error TooLarge(long maxBytes) => new("File.TooLarge", $"File exceeds the {maxBytes / 1024 / 1024} MB limit.");
    public static Error InvalidType() => new("File.InvalidType", "Only PDF files are allowed.");
    public static Error InvalidName() => new("File.InvalidName", "File name is invalid.");
    public static Error NotReady() => new("File.Conflict", "File is still being processed or was rejected.");
    public static Error NoThumbnail() => new("Thumbnail.NotFound", "Thumbnail is not available.");
}

public static class ShareLinkErrors
{
    public static Error NotFound() => new("ShareLink.NotFound", "Link not found or revoked.");
    public static Error Expired() => new("ShareLink.Expired", "Link has expired.");
    public static Error LoginRequired() => new("ShareLink.Unauthorized", "Sign in to access this file.");
    public static Error Forbidden() => new("ShareLink.Forbidden", "You don't have access to this file.");
    public static Error RestrictedRequiresUsers() => new("ShareLink.UsersRequired", "Choose at least one user for a restricted link.");
    public static Error PublicCannotHaveUsers() => new("ShareLink.UsersNotAllowed", "A public link cannot have a user list.");
    public static Error TooManyUsers(int max) => new("ShareLink.TooManyUsers", $"A link can be shared with at most {max} users.");
    public static Error InvalidExpiration() => new("ShareLink.InvalidExpiration", "Expiration must be in the future.");
    public static Error UnknownUsers() => new("ShareLink.UnknownUsers", "Some of the selected users don't exist.");
    public static Error NotRestricted() => new("ShareLink.NotRestricted", "Only restricted links have a user list.");
    public static Error FileRejected() => new("ShareLink.FileRejected", "A rejected file cannot be shared.");
}
