using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PdfShare.Core.Services.Access;
using PdfShare.Core.Services.Authentication;
using PdfShare.Core.Services.Files;
using PdfShare.Core.Services.ShareLinks;
using PdfShare.Core.Services.Users;
using PdfShare.Domain.Interfaces.CoreInterfaces;

namespace PdfShare.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<IFileAccessService, FileAccessService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IFilesService, FilesService>();
        services.AddScoped<IShareLinksService, ShareLinksService>();
        services.AddScoped<IUsersService, UsersService>();
        return services;
    }
}
