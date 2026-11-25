using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PdfShare.DataAccess.SqlServer;
using PdfShare.Domain.Options;
using PdfShare.Functions.Services;
using PdfShare.Infrastructure;

DotNetEnv.Env.TraversePath().NoClobber().Load();

var builder = FunctionsApplication.CreateBuilder(args);

var config = builder.Configuration;
var databaseUrl = config["DATABASE_URL"]
    ?? throw new InvalidOperationException("DATABASE_URL is required.");

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Warning);

builder.Services
    .AddDataAccess(databaseUrl)
    .AddInfrastructure(new BlobStorageOptions
    {
        ConnectionString = NullIfEmpty(config["BLOB_CONNECTION_STRING"]),
        BlobServiceUri = ToUri(config["BLOB_SERVICE_URI"]),
        QueueServiceUri = ToUri(config["QUEUE_SERVICE_URI"]),
    })
    .AddSingleton<IPdfAnalyzer, PdfAnalyzer>();

builder.Build().Run();

static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

static Uri? ToUri(string? value) => NullIfEmpty(value) is { } v ? new Uri(v, UriKind.Absolute) : null;
