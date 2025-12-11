FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ApplicationService/ApplicationService.csproj", "ApplicationService/"]
COPY ["Contracts/Contracts.csproj", "Contracts/"]
RUN dotnet restore "ApplicationService/ApplicationService.csproj"
COPY . .
WORKDIR "/src/ApplicationService"
RUN dotnet build "ApplicationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ApplicationService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ApplicationService.dll"]
