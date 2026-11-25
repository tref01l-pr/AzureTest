# Build context: BackEnd/
#   docker build -f PdfShare.API/Dockerfile -t pdfshare-api .

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (cached layer): only build props + project files.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY PdfShare.SharedKernel/PdfShare.SharedKernel.csproj PdfShare.SharedKernel/
COPY PdfShare.Domain/PdfShare.Domain.csproj PdfShare.Domain/
COPY PdfShare.Core/PdfShare.Core.csproj PdfShare.Core/
COPY PdfShare.DataAccess.SqlServer/PdfShare.DataAccess.SqlServer.csproj PdfShare.DataAccess.SqlServer/
COPY PdfShare.Infrastructure/PdfShare.Infrastructure.csproj PdfShare.Infrastructure/
COPY PdfShare.API/PdfShare.API.csproj PdfShare.API/
RUN dotnet restore PdfShare.API/PdfShare.API.csproj

COPY . .
RUN dotnet publish PdfShare.API/PdfShare.API.csproj -c Release -o /app --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080
# Non-root user shipped with the .NET 8 images.
USER $APP_UID
COPY --from=build /app .
ENTRYPOINT ["dotnet", "PdfShare.API.dll"]
