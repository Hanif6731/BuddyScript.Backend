FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=9384
EXPOSE 9384

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["BuddyScript.Backend.csproj", "./"]
RUN dotnet restore "BuddyScript.Backend.csproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "BuddyScript.Backend.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "BuddyScript.Backend.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "BuddyScript.Backend.dll"]
