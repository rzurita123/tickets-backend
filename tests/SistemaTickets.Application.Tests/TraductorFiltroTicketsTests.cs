using SistemaTickets.Application.Catalogos;
using SistemaTickets.Application.Tickets;
using SistemaTickets.Application.Usuarios;
using SistemaTickets.Domain.Tickets;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Application.Tests;

public class TraductorFiltroTicketsTests
{
    private static TraductorFiltroTickets Crear() => new(new CatalogoFalso(), new UsuariosVacios(), new OpcionesListadoTickets());

    private static FiltroTickets Filtro(string? estado = null, int pagina = 1, int porPagina = 15) =>
        new(estado, null, null, null, null, null, null, pagina, porPagina);

    [Fact]
    public async Task En_curso_incluye_abierto_en_proceso_y_bloqueado()
    {
        var criterios = await Crear().TraducirAsync(Filtro("en-curso"));

        Assert.Equal(new[] { EstadoTicket.Abierto, EstadoTicket.EnProceso, EstadoTicket.Bloqueado }, criterios.Estados);
    }

    [Fact]
    public async Task Un_estado_desconocido_es_un_error_de_negocio()
    {
        await Assert.ThrowsAsync<ErrorTicket>(() => Crear().TraducirAsync(Filtro("XXX")));
    }

    [Fact]
    public async Task Una_persona_que_atiende_inexistente_es_un_error_de_negocio()
    {
        var filtro = Filtro() with { Atiende = "nadie" };

        await Assert.ThrowsAsync<ErrorTicket>(() => Crear().TraducirAsync(filtro));
    }

    [Fact]
    public async Task Sin_asignar_se_traduce_a_sin_operador()
    {
        var criterios = await Crear().TraducirAsync(Filtro() with { Atiende = "sin" });

        Assert.True(criterios.SinOperador);
        Assert.Null(criterios.OperadorId);
    }

    [Theory]
    [InlineData(0, 15)]
    [InlineData(40, 40)]
    [InlineData(5000, 100)]
    public async Task La_cantidad_por_pagina_se_acota(int pedido, int esperado)
    {
        var criterios = await Crear().TraducirAsync(Filtro(porPagina: pedido));

        Assert.Equal(esperado, criterios.PorPagina);
    }

    [Fact]
    public async Task La_pagina_nunca_es_menor_a_uno()
    {
        var criterios = await Crear().TraducirAsync(Filtro(pagina: -3));

        Assert.Equal(1, criterios.Pagina);
    }

    private sealed class CatalogoFalso : ICatalogoLectura
    {
        public Task<int?> IdAsync(TipoCatalogo tipo, string? codigo, CancellationToken cancelacion = default) =>
            Task.FromResult<int?>(string.IsNullOrEmpty(codigo) ? null : 1);

        public Task<string?> CodigoAsync(TipoCatalogo tipo, int? id, CancellationToken cancelacion = default) =>
            Task.FromResult<string?>(null);

        public void Invalidar(TipoCatalogo tipo)
        {
        }
    }

    private sealed class UsuariosVacios : IRepositorioUsuarios
    {
        public Task<Usuario?> BuscarPorNombreUsuarioAsync(string nombreUsuario, CancellationToken cancelacion = default) =>
            Task.FromResult<Usuario?>(null);

        public Task<Usuario?> BuscarPorIdAsync(int id, CancellationToken cancelacion = default) =>
            Task.FromResult<Usuario?>(null);

        public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken cancelacion = default) =>
            Task.FromResult<IReadOnlyList<Usuario>>([]);
    }
}