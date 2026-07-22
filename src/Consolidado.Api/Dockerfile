FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/Cashflow.Contracts/Cashflow.Contracts.csproj src/Cashflow.Contracts/
COPY src/Consolidado.Api/Consolidado.Api.csproj src/Consolidado.Api/
RUN dotnet restore src/Consolidado.Api/Consolidado.Api.csproj
COPY src/ src/
RUN dotnet publish src/Consolidado.Api/Consolidado.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Consolidado.Api.dll"]
