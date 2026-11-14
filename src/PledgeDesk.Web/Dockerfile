FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/PledgeDesk.Web/PledgeDesk.Web.csproj src/PledgeDesk.Web/
RUN dotnet restore src/PledgeDesk.Web/PledgeDesk.Web.csproj

COPY src/PledgeDesk.Web/ src/PledgeDesk.Web/
RUN dotnet publish src/PledgeDesk.Web/PledgeDesk.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PledgeDesk.Web.dll"]
