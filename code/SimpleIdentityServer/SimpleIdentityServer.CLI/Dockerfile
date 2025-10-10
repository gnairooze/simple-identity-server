FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "SimpleIdentityServer.CLI.csproj"
RUN dotnet build "SimpleIdentityServer.CLI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SimpleIdentityServer.CLI.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
COPY simple-task.sh .
RUN chmod +x simple-task.sh .

ENTRYPOINT ["./simple-task.sh"]