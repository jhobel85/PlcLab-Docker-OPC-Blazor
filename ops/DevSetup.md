# Developer Setup

1. Install **.NET 9 SDK**.
2. Install **Docker Desktop** or compatible engine.
3. Clone the repo and restore:
   ```bash
   dotnet restore
   ```
4. PlcLab.OPC - Install required package 
dotnet add package Opc.Ua.Core
dotnet add package Opc.Ua.Client
dotnet add package Opc.Ua.Configuration 
dotnet add package Opc.Ua.Security.Certificates 

dotnet clean
dotnet nuget locals all --clear
dotnet restore

5. Run the app:
   ```bash
   dotnet run -p src/PlcLab.Web
   ```
