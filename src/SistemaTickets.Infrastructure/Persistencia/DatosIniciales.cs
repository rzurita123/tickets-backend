using Microsoft.EntityFrameworkCore;
using SistemaTickets.Application.Usuarios;
using SistemaTickets.Domain.Catalogos;
using SistemaTickets.Domain.Usuarios;

namespace SistemaTickets.Infrastructure.Persistencia;

public static class DatosIniciales
{
    private static readonly string[] Localidades =
    [
        "Artigas", "Canelones", "Carmelo", "Durazno", "Florida", "Lagomar",
        "Pando", "Paysandú", "Rosario", "Santa Lucía", "Trinidad",
    ];

    private static readonly (string Codigo, string Nombre, (string Codigo, string Nombre)[] Subsistemas)[] Sistemas =
    [
        ("sis-liquidacion", "Liquidación", [("sub-liq-diaria", "Liquidación diaria"), ("sub-liq-premios", "Premios"), ("sub-liq-reportes", "Reportes"), ("sub-liq-otros", "Otros")]),
        ("sis-caja", "Caja", [("sub-caja-apertura", "Apertura"), ("sub-caja-cierre", "Cierre"), ("sub-caja-movimientos", "Movimientos"), ("sub-caja-otros", "Otros")]),
        ("sis-control-terminales", "Control Terminales", [("sub-term-bloqueo", "Bloqueo"), ("sub-term-sincro", "Sincronización"), ("sub-term-alta", "Alta de terminal"), ("sub-term-impresion", "Impresión"), ("sub-term-otros", "Otros")]),
        ("sis-online", "On-Line", [("sub-online-conexion", "Conexión"), ("sub-online-jugadas", "Registro de jugadas"), ("sub-online-otros", "Otros")]),
        ("sis-envio-archivos", "Envío Archivos", [("sub-env-envio", "Envío"), ("sub-env-recepcion", "Recepción"), ("sub-env-otros", "Otros")]),
        ("sis-correos", "Correos Ormen", [("sub-correo-recepcion", "Recepción"), ("sub-correo-cuenta", "Configuración de cuenta"), ("sub-correo-otros", "Otros")]),
        ("sis-correos-colectiviz", "Correos Colectiviz.", [("sub-colect-otros", "Otros")]),
        ("sis-todos-terminales", "Todos Terminales", [("sub-todos-otros", "Otros")]),
        ("sis-adm-usuarios", "Adm. Usuarios", [("sub-usu-alta", "Alta de usuario"), ("sub-usu-contrasena", "Contraseña"), ("sub-usu-permisos", "Permisos"), ("sub-usu-otros", "Otros")]),
        ("sis-instalaciones", "Instalaciones", [("sub-inst-equipo", "Equipo nuevo"), ("sub-inst-reinstalacion", "Reinstalación"), ("sub-inst-otros", "Otros")]),
    ];

    private static readonly (string Usuario, string Nombre, string Email, Rol Rol, string Cargo, string? Localidad)[] Usuarios =
    [
        ("vpereira", "Valeria Pereira", "vpereira@ormen.example", Rol.Operador, "Mesa de ayuda", null),
        ("nacosta", "Nicolás Acosta", "nacosta@ormen.example", Rol.Operador, "Mesa de ayuda", null),
        ("scabrera", "Silvia Cabrera", "scabrera@ormen.example", Rol.Administrador, "Gerencia de operaciones", null),
        ("ralvarez", "Rodrigo Álvarez", "ralvarez@ormen.example", Rol.Administrador, "Responsable de sistemas", null),
        ("mtechera", "Marcelo Techera", "mtechera@pando.example", Rol.Cliente, "Encargado", "Pando"),
        ("lgomez", "Laura Gómez", "lgomez@lagomar.example", Rol.Cliente, "Encargada", "Lagomar"),
    ];

    public static async Task CargarAsync(ContextoTickets contexto, IHashContrasenas hash, string? contrasenaInicial, CancellationToken cancelacion = default)
    {
        if (!await contexto.Localidades.AnyAsync(cancelacion))
        {
            contexto.Localidades.AddRange(Localidades.Select(nombre => new Localidad { Nombre = nombre }));
            await contexto.SaveChangesAsync(cancelacion);
        }

        if (!await contexto.Sistemas.AnyAsync(cancelacion))
        {
            await CargarCatalogosAsync(contexto, cancelacion);
        }

        if (!string.IsNullOrEmpty(contrasenaInicial) && !await contexto.Usuarios.AnyAsync(cancelacion))
        {
            var hashInicial = hash.Generar(contrasenaInicial);
            var localidades = await contexto.Localidades.ToDictionaryAsync(l => l.Nombre, l => l.Id, cancelacion);
            contexto.Usuarios.AddRange(Usuarios.Select(u => new Usuario
            {
                NombreUsuario = u.Usuario,
                Nombre = u.Nombre,
                Email = u.Email,
                HashContrasena = hashInicial,
                Rol = u.Rol,
                Cargo = u.Cargo,
                LocalidadId = u.Localidad is null ? null : localidades[u.Localidad],
            }));
            await contexto.SaveChangesAsync(cancelacion);
        }
    }

    private static async Task CargarCatalogosAsync(ContextoTickets contexto, CancellationToken cancelacion)
    {
        foreach (var s in Sistemas)
        {
            var sistema = new Sistema { Codigo = s.Codigo, Nombre = s.Nombre };
            contexto.Sistemas.Add(sistema);
            await contexto.SaveChangesAsync(cancelacion);
            contexto.Subsistemas.AddRange(s.Subsistemas.Select(sub =>
                new Subsistema { Codigo = sub.Codigo, Nombre = sub.Nombre, SistemaId = sistema.Id }));
        }

        contexto.TiposProblema.AddRange(
            new TipoProblema { Codigo = "tp-nodef", Nombre = "No definido" },
            new TipoProblema { Codigo = "tp-operacion", Nombre = "Operación" },
            new TipoProblema { Codigo = "tp-software", Nombre = "Software" },
            new TipoProblema { Codigo = "tp-equipamiento", Nombre = "Equipamiento" },
            new TipoProblema { Codigo = "tp-configuracion", Nombre = "Configuración" });

        contexto.Criticidades.AddRange(
            new Criticidad { Codigo = "cri-alta", Nombre = "Alta" },
            new Criticidad { Codigo = "cri-media", Nombre = "Media" },
            new Criticidad { Codigo = "cri-baja", Nombre = "Baja" });

        contexto.GruposSoporte.AddRange(
            new GrupoSoporte { Codigo = "gs-soporte-n2", Nombre = "Soporte técnico (2º nivel)" },
            new GrupoSoporte { Codigo = "gs-infraestructura", Nombre = "Infraestructura y redes" },
            new GrupoSoporte { Codigo = "gs-desarrollo", Nombre = "Desarrollo de sistemas" },
            new GrupoSoporte { Codigo = "gs-proveedor-terminales", Nombre = "Proveedor de terminales" });

        await contexto.SaveChangesAsync(cancelacion);
    }
}
