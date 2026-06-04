FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY WorldCupBets.sln ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY src/WorldCupBets.Domain/WorldCupBets.Domain.csproj src/WorldCupBets.Domain/
COPY src/WorldCupBets.Application/WorldCupBets.Application.csproj src/WorldCupBets.Application/
COPY src/WorldCupBets.Infrastructure/WorldCupBets.Infrastructure.csproj src/WorldCupBets.Infrastructure/
COPY src/WorldCupBets.WebApi/WorldCupBets.WebApi.csproj src/WorldCupBets.WebApi/
RUN dotnet restore src/WorldCupBets.WebApi/WorldCupBets.WebApi.csproj

COPY src ./src
RUN dotnet publish src/WorldCupBets.WebApi/WorldCupBets.WebApi.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 \
    && rm -rf /var/lib/apt/lists/*
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish ./
ENTRYPOINT ["dotnet", "WorldCupBets.WebApi.dll"]
