FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/Cashflow.Contracts/Cashflow.Contracts.csproj src/Cashflow.Contracts/
COPY src/Lancamentos.Api/Lancamentos.Api.csproj src/Lancamentos.Api/
RUN dotnet restore src/Lancamentos.Api/Lancamentos.Api.csproj
COPY src/ src/
RUN dotnet publish src/Lancamentos.Api/Lancamentos.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Lancamentos.Api.dll"]
