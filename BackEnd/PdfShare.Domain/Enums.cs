namespace PdfShare.Domain;

public enum Roles
{
    Admin,
    User,
}

public enum FileStatus
{
    Processing,
    Ready,
    Rejected,
}

public enum ShareLinkType
{
    Public,
    Restricted,
}
