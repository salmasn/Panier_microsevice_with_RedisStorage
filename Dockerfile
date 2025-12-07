# Étape 1 : Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copier le .csproj et restaurer les dépendances
COPY *.csproj ./
RUN dotnet restore

# Copier tout le code source et builder
COPY . ./
RUN dotnet publish -c Release -o out

# Étape 2 : Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/out .

# Railway utilise la variable PORT
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PanierService.dll"]