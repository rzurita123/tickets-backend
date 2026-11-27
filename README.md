# Sistema de tickets

Backend del sistema de tickets: API REST en ASP.NET Core.

## Estructura

```
SistemaTickets.sln
src/
  SistemaTickets.Domain/          entidades y reglas del dominio
  SistemaTickets.Application/     casos de uso; depende de Domain
  SistemaTickets.Infrastructure/  persistencia e integraciones; depende de Application
  SistemaTickets.Api/             controladores, seguridad y frontend; depende de Application e Infrastructure
tests/
  SistemaTickets.Domain.Tests/
  SistemaTickets.Application.Tests/
  SistemaTickets.Api.Tests/
```

## Requisitos

- .NET SDK 10
- SQL Server LocalDB
- `dotnet tool install --global dotnet-ef`

## Base de datos

En desarrollo usa LocalDB (base `SistemaTickets`). Al arrancar, la Api aplica las migraciones y carga los datos iniciales si la base está vacía.

Los perfiles de inicio `http` y `https` también cargan los registros diarios de ejemplo en desarrollo, incluso si la base ya tiene tickets. Se registran como tickets privados, visibles para el personal de ORMEN. La referencia en la actividad de cada ticket evita duplicados al reiniciar.

```bash
dotnet ef migrations add <Nombre> -p src/SistemaTickets.Infrastructure -s src/SistemaTickets.Api -o Persistencia/Migraciones
dotnet ef database drop -p src/SistemaTickets.Infrastructure -s src/SistemaTickets.Api
```

## Uso

```bash
dotnet build
dotnet test
dotnet run --project src/SistemaTickets.Api
```
