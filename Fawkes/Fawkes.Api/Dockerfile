# Build context is the Fawkes/ folder (solution root), so all referenced projects are available:
#   docker build -f Fawkes.Api/Dockerfile -t fawkes-api .

# Use the official .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY Fawkes.Core/Fawkes.Core.csproj Fawkes.Core/
COPY Fawkes.Store/Fawkes.Store.csproj Fawkes.Store/
COPY Fawkes.Api/Fawkes.Api.csproj Fawkes.Api/
RUN dotnet restore Fawkes.Api/Fawkes.Api.csproj

# Copy the sources and publish the application
COPY Fawkes.Core/ Fawkes.Core/
COPY Fawkes.Store/ Fawkes.Store/
COPY Fawkes.Api/ Fawkes.Api/
RUN dotnet publish Fawkes.Api/Fawkes.Api.csproj -c Release -o /out --no-restore

# Use the ASP.NET Core runtime image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /fawkes-api

# Commit-SHA des Builds, wird von GET /version ausgegeben
ARG GIT_SHA=dev
ENV BUILD_SHA=$GIT_SHA

# Copy published files from the build stage
COPY --from=build /out .
EXPOSE 80

# Set the entry point for the container
ENTRYPOINT ["dotnet", "Fawkes.Api.dll"]
