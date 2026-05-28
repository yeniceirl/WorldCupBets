FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY WorldCupBets.sln ./
COPY src/WorldCupBets.Domain/WorldCupBets.Domain.csproj src/WorldCupBets.Domain/
COPY src/WorldCupBets.Application/WorldCupBets.Application.csproj src/WorldCupBets.Application/
COPY src/WorldCupBets.Infrastructure/WorldCupBets.Infrastructure.csproj src/WorldCupBets.Infrastructure/
COPY src/WorldCupBets.WebApi/WorldCupBets.WebApi.csproj src/WorldCupBets.WebApi/
RUN dotnet restore WorldCupBets.sln

COPY . .
RUN dotnet publish src/WorldCupBets.WebApi/WorldCupBets.WebApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "WorldCupBets.WebApi.dll"]
