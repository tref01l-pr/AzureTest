using Mapster;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PdfShare.DataAccess.SqlServer.Entities.UserEntities;
using PdfShare.DataAccess.SqlServer.Repositories;
using PdfShare.DataAccess.SqlServer.Repositories.Files;
using PdfShare.DataAccess.SqlServer.Repositories.Sessions;
using PdfShare.DataAccess.SqlServer.Repositories.ShareLinks;
using PdfShare.DataAccess.SqlServer.Repositories.Users;
using PdfShare.DataAccess.SqlServer.Services;
using PdfShare.Domain;
using PdfShare.Domain.Interfaces.DataAccessInterfaces;

namespace PdfShare.DataAccess.SqlServer;

public static class DependencyInjection
{
    public static IServiceCollection AddDataAccess(this IServiceCollection services, string connectionString)
    {
        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString, sql =>
        {
            sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.GetName().Name);
            sql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorNumbersToAdd: null);
        }));

        services.AddSingleton(CreateMapperConfig());
        services.AddScoped<IPdfFilesRepository, PdfFilesRepository>();
        services.AddScoped<IShareLinksRepository, ShareLinksRepository>();
        services.AddScoped<ISessionsRepository, SessionsRepository>();
        services.AddScoped<IUsersRepository, UsersRepository>();
        services.AddScoped<IUnitOfWork, DatabaseUnitOfWork>();
        return services;
    }

    public static TypeAdapterConfig CreateMapperConfig()
    {
        var config = new TypeAdapterConfig();
        config.Apply(new DomainMappingProfile(), new DataAccessMappingProfile());
        return config;
    }

    public static IServiceCollection AddIdentityStores(this IServiceCollection services)
    {
        services.AddIdentityCore<UserEntity>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireDigit = true;
                options.SignIn.RequireConfirmedAccount = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            })
            .AddRoles<RoleEntity>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddUserManager<UserManager<UserEntity>>()
            .AddRoleManager<RoleManager<RoleEntity>>();

        services.AddScoped<IIdentityService, IdentityService>();
        return services;
    }
}
