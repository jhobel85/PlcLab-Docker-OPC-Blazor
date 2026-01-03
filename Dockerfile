# Use the official .NET 9 SDK image to build the app
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy the solution and project files
COPY PlcLab-Docker-OPC-Blazor.sln ./
COPY Directory.Packages.props ./
COPY src/PlcLab.Domain/PlcLab.Domain.csproj src/PlcLab.Domain/
COPY src/PlcLab.Application/PlcLab.Application.csproj src/PlcLab.Application/
COPY src/PlcLab.Infrastructure/PlcLab.Infrastructure.csproj src/PlcLab.Infrastructure/
COPY src/PlcLab.OPC/PlcLab.OPC.csproj src/PlcLab.OPC/
COPY src/PlcLab.Web/PlcLab.Web.csproj src/PlcLab.Web/

# Clear NuGet cache to avoid Windows path issues
RUN dotnet nuget locals all --clear

# Restore dependencies
RUN dotnet restore PlcLab-Docker-OPC-Blazor.sln

# Copy the rest of the source code
COPY src/ ./

# Build the web app
WORKDIR /src/PlcLab.Web
RUN dotnet build -c Release

# Publish the app
RUN dotnet publish -c Release -o /app/publish

# Use the official .NET 9 runtime image for the final stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
# Create PKI directories for OPC UA certificates
RUN mkdir -p /app/pki/trusted /app/pki/rejected
COPY --from=build /app/publish .

# Expose the port
EXPOSE 8080

# Set the entry point
ENTRYPOINT ["dotnet", "PlcLab.Web.dll"]