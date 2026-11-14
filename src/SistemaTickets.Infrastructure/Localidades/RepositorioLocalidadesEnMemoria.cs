using SistemaTickets.Application.Localidades;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Infrastructure.Localidades;

public class RepositorioLocalidadesEnMemoria : IRepositorioLocalidades
{
    private static readonly string[] Nombres =
    [
        "Artigas", "Canelones", "Carmelo", "Durazno", "Florida", "Lagomar",
        "Pando", "Paysandú", "Rosario", "Santa Lucía", "Trinidad",
    ];

    private readonly IReadOnlyList<Localidad> _localidades =
        [.. Nombres.Select((nombre, i) => new Localidad { Id = i + 1, Nombre = nombre })];

    public Task<IReadOnlyList<Localidad>> ListarAsync(CancellationToken cancelacion = default) =>
        Task.FromResult(_localidades);
}
