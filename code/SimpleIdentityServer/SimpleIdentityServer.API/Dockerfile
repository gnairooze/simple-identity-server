FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SimpleIdentityServer.API.csproj", "./"]
RUN dotnet restore "SimpleIdentityServer.API.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "SimpleIdentityServer.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SimpleIdentityServer.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SimpleIdentityServer.API.dll"] 