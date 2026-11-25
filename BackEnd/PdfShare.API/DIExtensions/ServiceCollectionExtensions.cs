using System.Reflection;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PdfShare.API.Extensions;
using PdfShare.Domain.Constants;
using PdfShare.Domain.Interfaces.DataAccessInterfaces;
using PdfShare.Domain.Options;
using PdfShare.SharedKernel;
using Swashbuckle.AspNetCore.Filters;

namespace PdfShare.API.DIExtensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, JWTSecretOptions jwt)
    {
        services.AddOptions<JWTSecretOptions>().Configure(o =>
        {
            o.Secret = jwt.Secret;
            o.AccessTokenLifetime = jwt.AccessTokenLifetime;
            o.RefreshTokenLifetime = jwt.RefreshTokenLifetime;
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false; // tokens already use ClaimTypes.* names
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                    NameClaimType = ClaimTypes.Name,
                    RoleClaimType = ClaimTypes.Role,
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var sessionClaim = context.Principal?.FindFirst(ClaimNames.SessionId)?.Value;
                        if (!Guid.TryParse(sessionClaim, out var sessionId))
                        {
                            context.Fail("Missing session.");
                            return;
                        }

                        var sessions = context.HttpContext.RequestServices.GetRequiredService<ISessionsRepository>();
                        var time = context.HttpContext.RequestServices.GetRequiredService<TimeProvider>();
                        if (!await sessions.ExistsAsync(sessionId, time.GetUtcNow(), context.HttpContext.RequestAborted))
                        {
                            context.Fail("Session revoked.");
                        }
                    },
                };
            });

        services.AddAuthorization();
        return services;
    }

    public static IMvcBuilder ConfigureValidationProblems(this IMvcBuilder builder) =>
        builder.ConfigureApiBehaviorOptions(options =>
            options.InvalidModelStateResponseFactory = context =>
            {
                var errors = context.ModelState
                    .Where(entry => entry.Value is { Errors.Count: > 0 })
                    .SelectMany(entry => entry.Value!.Errors.Select(e => new Error(
                        $"Validation.{(string.IsNullOrEmpty(entry.Key) ? "Body" : entry.Key)}",
                        string.IsNullOrEmpty(e.ErrorMessage) ? "Invalid value." : e.ErrorMessage)))
                    .ToArray();

                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Bad Request",
                    Detail = errors.FirstOrDefault()?.Description,
                    Extensions = { ["errors"] = errors },
                };
                return new BadRequestObjectResult(problem) { ContentTypes = { "application/problem+json" } };
            });

    public static IServiceCollection AddSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "PdfShare API", Version = "v1" });
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Description = "Paste the access token as: Bearer {token}",
                In = ParameterLocation.Header,
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
            });
            options.OperationFilter<SecurityRequirementsOperationFilter>();

            var xml = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
            if (File.Exists(xml))
            {
                options.IncludeXmlComments(xml);
            }
        });
        return services;
    }
}
