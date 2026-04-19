# ===== Development stage =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS dev
WORKDIR /app
EXPOSE 5141
ENV ASPNETCORE_URLS=http://+:5141
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "watch", "run", "--project", "PettyCash.Api"]

# ===== Build stage =====
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore PettyCash.Api/PettyCash.Api.csproj
RUN dotnet publish PettyCash.Api -c Release -o /app/publish

# ===== Production stage =====
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS prod
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 5141
ENV ASPNETCORE_URLS=http://+:5141
ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "PettyCash.Api.dll"]
