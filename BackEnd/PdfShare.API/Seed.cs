using Microsoft.AspNetCore.Identity;
using PdfShare.DataAccess.SqlServer.Entities.UserEntities;
using PdfShare.Domain;
using PdfShare.Domain.Interfaces.DataAccessInterfaces;

namespace PdfShare.API;

public static class Seed
{
    public static readonly string[] DemoUsers = ["alice@pdfshare.local", "bob@pdfshare.local"];

    public static async Task SeedDataAsync(IServiceProvider services, bool seedDemoUsers, string? demoPassword, ILogger logger)
    {
        var roleManager = services.GetRequiredService<RoleManager<RoleEntity>>();
        foreach (var role in Enum.GetNames<Roles>())
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new RoleEntity { Name = role });
            }
        }

        if (!seedDemoUsers)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(demoPassword))
        {
            logger.LogWarning("SEED_DEMO_USERS is on but SEED_DEMO_PASSWORD is empty: demo users were not created");
            return;
        }

        var identity = services.GetRequiredService<IIdentityService>();
        foreach (var email in DemoUsers)
        {
            var result = await identity.RegisterAsync(email, demoPassword, Roles.User);
            if (result.IsSuccess)
            {
                logger.LogInformation("Demo user {Email} created", email);
            }
        }
    }
}
