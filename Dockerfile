FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["Gastitis.Domain/Gastitis.Domain.csproj", "Gastitis.Domain/"]
COPY ["Gastitis.Application/Gastitis.Application.csproj", "Gastitis.Application/"]
COPY ["Gastitis.Infrastructure/Gastitis.Infrastructure.csproj", "Gastitis.Infrastructure/"]
COPY ["Gastitis.API/Gastitis.API.csproj", "Gastitis.API/"]

RUN dotnet restore Gastitis.API/Gastitis.API.csproj

COPY . .

RUN dotnet publish "Gastitis.API/Gastitis.API.csproj" -c Release -o /app/publish /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Gastitis.API.dll"]

