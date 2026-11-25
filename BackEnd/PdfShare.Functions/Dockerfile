# Build context: BackEnd/
#   docker build -f PdfShare.Functions/Dockerfile -t pdfshare-func .

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY PdfShare.SharedKernel/PdfShare.SharedKernel.csproj PdfShare.SharedKernel/
COPY PdfShare.Domain/PdfShare.Domain.csproj PdfShare.Domain/
COPY PdfShare.DataAccess.SqlServer/PdfShare.DataAccess.SqlServer.csproj PdfShare.DataAccess.SqlServer/
COPY PdfShare.Infrastructure/PdfShare.Infrastructure.csproj PdfShare.Infrastructure/
COPY PdfShare.Functions/PdfShare.Functions.csproj PdfShare.Functions/
RUN dotnet restore PdfShare.Functions/PdfShare.Functions.csproj

COPY . .
RUN dotnet publish PdfShare.Functions/PdfShare.Functions.csproj -c Release -o /home/site/wwwroot --no-restore

# Azure Functions host + .NET 8 isolated worker. PDFium/SkiaSharp ship their own Linux natives
# (SkiaSharp.NativeAssets.Linux.NoDependencies), so no extra apt packages are needed.
FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0
ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true \
    FUNCTIONS_WORKER_RUNTIME=dotnet-isolated
COPY --from=build /home/site/wwwroot /home/site/wwwroot
