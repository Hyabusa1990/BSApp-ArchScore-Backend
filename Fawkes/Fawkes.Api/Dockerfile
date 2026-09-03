# Use the official .NET SDK image for building the application
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /fawkes-api

# Copy project files and restore dependencies
COPY *.csproj .
RUN dotnet restore

# Copy all files and publish the application
COPY . .
RUN dotnet publish -c Release -o /out

# Use the ASP.NET Core runtime image for running the application
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /fawkes-api

# Copy published files from the build stage
COPY --from=build /out .
EXPOSE 80

# Set the entry point for the container
ENTRYPOINT ["dotnet", "Fawkes.Api.dll"]
