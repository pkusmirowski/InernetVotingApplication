# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY InternetVotingApplication.csproj ./
RUN dotnet restore InternetVotingApplication.csproj
COPY . .
RUN dotnet publish InternetVotingApplication.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
# Election dates are entered and compared in Polish local time; the base image runs on UTC.
ENV TZ=Europe/Warsaw
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InternetVotingApplication.dll"]
