# ZafraFlow API image for the VPS demo (see docs/operations/vps-deploy.md).
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY AgroFlow.Domain/AgroFlow.Domain.csproj AgroFlow.Domain/
COPY AgroFlow.Application/AgroFlow.Application.csproj AgroFlow.Application/
COPY AgroFlow.Data/AgroFlow.Data.csproj AgroFlow.Data/
COPY AgroFlow.Api/AgroFlow.Api.csproj AgroFlow.Api/
RUN dotnet restore AgroFlow.Api/AgroFlow.Api.csproj
COPY AgroFlow.Domain/ AgroFlow.Domain/
COPY AgroFlow.Application/ AgroFlow.Application/
COPY AgroFlow.Data/ AgroFlow.Data/
COPY AgroFlow.Api/ AgroFlow.Api/
RUN dotnet publish AgroFlow.Api/AgroFlow.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
# The SQLite file lives on a named volume mounted here.
RUN mkdir -p /app/data
EXPOSE 8080
ENTRYPOINT ["dotnet", "AgroFlow.Api.dll"]
