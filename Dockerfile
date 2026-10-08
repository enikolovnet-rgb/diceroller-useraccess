# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# The local feed only exists on a developer machine; in the image the BuildingBlocks packages come from GitHub Packages.
COPY global.json nuget.config Directory.Build.props Directory.Packages.props .editorconfig ./
RUN dotnet nuget disable source local && dotnet nuget enable source github

COPY src/DiceRoller.UserAccess.Domain/DiceRoller.UserAccess.Domain.csproj src/DiceRoller.UserAccess.Domain/
COPY src/DiceRoller.UserAccess.Application/DiceRoller.UserAccess.Application.csproj src/DiceRoller.UserAccess.Application/
COPY src/DiceRoller.UserAccess.Infrastructure/DiceRoller.UserAccess.Infrastructure.csproj src/DiceRoller.UserAccess.Infrastructure/
COPY src/DiceRoller.UserAccess.Api/DiceRoller.UserAccess.Api.csproj src/DiceRoller.UserAccess.Api/

# The token is a BuildKit secret: it is only visible to this RUN and never written to a layer or to nuget.config.
RUN --mount=type=secret,id=nuget_token,env=NUGET_AUTH_TOKEN,required=true \
    NuGetPackageSourceCredentials_github="Username=docker;Password=${NUGET_AUTH_TOKEN}" \
    dotnet restore src/DiceRoller.UserAccess.Api/DiceRoller.UserAccess.Api.csproj

COPY src/ src/
RUN dotnet publish src/DiceRoller.UserAccess.Api/DiceRoller.UserAccess.Api.csproj -c Release --no-restore -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /data/photos \
    && chown "$APP_UID" /data/photos

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    PhotoStorage__RootPath=/data/photos
VOLUME /data/photos
EXPOSE 8080

USER $APP_UID

HEALTHCHECK --interval=10s --timeout=3s --start-period=30s --retries=3 \
    CMD curl -fsS http://localhost:8080/health/live || exit 1

ENTRYPOINT ["dotnet", "DiceRoller.UserAccess.Api.dll"]
