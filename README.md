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

## Uso

```bash
dotnet build
dotnet test
dotnet run --project src/SistemaTickets.Api
```
