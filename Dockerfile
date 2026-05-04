FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY src/TapitAI.Domain/TapitAI.Domain.csproj             src/TapitAI.Domain/
COPY src/TapitAI.Application/TapitAI.Application.csproj   src/TapitAI.Application/
COPY src/TapitAI.Infrastructure/TapitAI.Infrastructure.csproj src/TapitAI.Infrastructure/
COPY src/TapitAI.API/TapitAI.API.csproj                   src/TapitAI.API/

RUN dotnet restore src/TapitAI.API/TapitAI.API.csproj

COPY src/ src/

RUN dotnet publish src/TapitAI.API/TapitAI.API.csproj \
    -c Release \
    --no-restore \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "TapitAI.API.dll"]
