using Microsoft.EntityFrameworkCore;
using SistemaTickets.Application.Usuarios;
using SistemaTickets.Domain.Catalogos;
using SistemaTickets.Domain.Soluciones;
using SistemaTickets.Domain.Tickets;
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

    private static readonly (string Clave, int Valor)[] Parametros =
    [
        ("pesoSistema", 3), ("pesoSubsistema", 2), ("pesoPalabraClave", 2),
        ("pesoLocalidad", 1), ("pesoPalabraComun", 1), ("maxResultados", 6),
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

        if (await contexto.Usuarios.AnyAsync(cancelacion) && !await contexto.Soluciones.AnyAsync(cancelacion))
        {
            await CargarSolucionesAsync(contexto, cancelacion);
        }

        if (!await contexto.ParametrosSugerencia.AnyAsync(cancelacion))
        {
            contexto.ParametrosSugerencia.AddRange(Parametros.Select(p => new ParametroSugerencia { Clave = p.Clave, Valor = p.Valor }));
            await contexto.SaveChangesAsync(cancelacion);
        }

        if (!await contexto.Tickets.AnyAsync(cancelacion))
        {
            await CargarTicketsAsync(contexto, cancelacion);
        }
    }

    private static async Task CargarSolucionesAsync(ContextoTickets contexto, CancellationToken cancelacion)
    {
        var usuarios = await contexto.Usuarios.ToDictionaryAsync(u => u.NombreUsuario, u => u.Id, cancelacion);
        var sistemas = await contexto.Sistemas.ToDictionaryAsync(s => s.Codigo, s => s.Id, cancelacion);
        var subsistemas = await contexto.Subsistemas.ToDictionaryAsync(s => s.Codigo, s => s.Id, cancelacion);
        var ahora = DateTime.UtcNow;
        var lista = SolucionesIniciales.Lista;

        for (var i = 0; i < lista.Length; i++)
        {
            var s = lista[i];
            var creada = ahora.AddDays(-(300 - i * 2));
            var revisada = s.PropuestaPor == s.RevisadaPor ? creada : creada.AddHours(20);
            var solucion = new Solucion
            {
                Codigo = s.Codigo,
                Titulo = s.Titulo,
                Texto = s.Texto,
                Estado = EstadoSolucion.Aprobada,
                SistemaId = s.Sistema is null ? null : sistemas[s.Sistema],
                SubsistemaId = s.Subsistema is null ? null : subsistemas[s.Subsistema],
                PalabrasClave = [.. s.PalabrasClave],
                PropuestaPorId = usuarios[s.PropuestaPor],
                RevisadaPorId = usuarios[s.RevisadaPor],
                CreadaEn = creada,
                RevisadaEn = revisada,
                ActualizadaEn = revisada,
            };
            if (s.ConImagen)
            {
                solucion.Adjuntos.Add(new Adjunto
                {
                    Origen = OrigenAdjunto.Solucion,
                    Clave = "img-" + s.Codigo,
                    Nombre = "pasos-impresora.svg",
                    TipoContenido = "image/svg+xml",
                    Contenido = System.Text.Encoding.UTF8.GetBytes(SolucionesIniciales.ImagenPasos),
                    SubidoPorId = usuarios[s.PropuestaPor],
                    Fecha = creada,
                });
            }

            contexto.Soluciones.Add(solucion);
        }

        await contexto.SaveChangesAsync(cancelacion);
    }


    private static async Task CargarTicketsAsync(ContextoTickets contexto, CancellationToken cancelacion)
    {
        var usuarios = await contexto.Usuarios.ToDictionaryAsync(u => u.NombreUsuario, u => u, cancelacion);
        var localidades = await contexto.Localidades.ToDictionaryAsync(l => l.Nombre, l => l.Id, cancelacion);
        var sistemas = await contexto.Sistemas.ToDictionaryAsync(s => s.Codigo, s => s.Id, cancelacion);
        var subsistemas = await contexto.Subsistemas.ToDictionaryAsync(s => s.Codigo, s => s.Id, cancelacion);
        var criticidades = await contexto.Criticidades.ToDictionaryAsync(c => c.Codigo, c => c.Id, cancelacion);
        var tipos = await contexto.TiposProblema.ToDictionaryAsync(t => t.Codigo, t => t.Id, cancelacion);

        var ahora = DateTime.UtcNow;
        var numero = (await contexto.Tickets.MaxAsync(t => t.Numero, cancelacion) ?? 1000) + 1;

        Ticket Nuevo(
            string titulo,
            string descripcion,
            string creador,
            string localidad,
            string sistema,
            string subsistema,
            string criticidad,
            string tipo,
            int diasAtras)
        {
            var fecha = ahora.AddDays(-diasAtras);
            var ticket = Ticket.NuevoBorrador(usuarios[creador].Id, fecha);

            ticket.Titulo = titulo;
            ticket.Descripcion = descripcion;
            ticket.LocalidadId = localidades[localidad];
            ticket.SistemaId = sistemas[sistema];
            ticket.SubsistemaId = subsistemas[subsistema];
            ticket.CriticidadId = criticidades[criticidad];
            ticket.TipoProblemaId = tipos[tipo];

            return ticket;
        }

        Ticket Registrar(Ticket ticket, int diasAtras)
        {
            ticket.Registrar(numero++, false, false, ticket.CreadoPorId, ahora.AddDays(-diasAtras));
            return ticket;
        }

        var tickets = new List<Ticket>();

        // 4 tickets abiertos, pendientes de asignación.
        tickets.Add(Registrar(Nuevo(
            "Liquidación diaria con diferencias",
            "Se detectan diferencias en los importes de la liquidación diaria.",
            "mtechera", "Pando", "sis-liquidacion", "sub-liq-diaria",
            "cri-alta", "tp-operacion", 1), 1));

        tickets.Add(Registrar(Nuevo(
            "Error al registrar movimientos de caja",
            "El sistema muestra un error al intentar guardar movimientos.",
            "lgomez", "Lagomar", "sis-caja", "sub-caja-movimientos",
            "cri-media", "tp-software", 2), 2));

        tickets.Add(Registrar(Nuevo(
            "Terminal bloqueada",
            "Una terminal permanece bloqueada y no permite operar.",
            "mtechera", "Pando", "sis-control-terminales", "sub-term-bloqueo",
            "cri-alta", "tp-equipamiento", 3), 3));

        tickets.Add(Registrar(Nuevo(
            "Problemas de conexión On-Line",
            "Se reportan cortes intermitentes en la conexión.",
            "lgomez", "Lagomar", "sis-online", "sub-online-conexion",
            "cri-baja", "tp-configuracion", 4), 4));

        // 3 tickets en proceso.
        var t5 = Registrar(Nuevo(
            "Falla en envío de archivos",
            "Los archivos no están llegando al destino esperado.",
            "mtechera", "Pando", "sis-envio-archivos", "sub-env-envio",
            "cri-media", "tp-operacion", 2), 2);
        t5.Asignar(usuarios["vpereira"].Id, usuarios["vpereira"].Id, ahora.AddDays(-2).AddHours(1));
        tickets.Add(t5);

        var t6 = Registrar(Nuevo(
            "Problemas con recepción de correos",
            "No se reciben algunos correos de la cuenta institucional.",
            "lgomez", "Lagomar", "sis-correos", "sub-correo-recepcion",
            "cri-baja", "tp-software", 3), 3);
        t6.Asignar(usuarios["nacosta"].Id, usuarios["nacosta"].Id, ahora.AddDays(-3).AddHours(1));
        tickets.Add(t6);

        var t7 = Registrar(Nuevo(
            "Error en permisos de usuarios",
            "Un usuario no puede acceder a las opciones que tiene habilitadas.",
            "mtechera", "Pando", "sis-adm-usuarios", "sub-usu-permisos",
            "cri-alta", "tp-configuracion", 1), 1);
        t7.Asignar(usuarios["vpereira"].Id, usuarios["vpereira"].Id, ahora.AddDays(-1).AddHours(1));
        tickets.Add(t7);

        // 2 tickets bloqueados.
        var t8 = Registrar(Nuevo(
            "Falla de sincronización de terminales",
            "Se requiere intervención externa para completar la sincronización.",
            "mtechera", "Pando", "sis-control-terminales", "sub-term-sincro",
            "cri-alta", "tp-equipamiento", 5), 5);
        t8.Asignar(usuarios["nacosta"].Id, usuarios["nacosta"].Id, ahora.AddDays(-5).AddHours(1));
        t8.Bloquear("Pendiente de intervención del proveedor.", false,
            usuarios["nacosta"].Id, ahora.AddDays(-4));
        tickets.Add(t8);

        var t9 = Registrar(Nuevo(
            "Error en cierre de caja",
            "El cierre de caja no coincide con los movimientos registrados.",
            "lgomez", "Lagomar", "sis-caja", "sub-caja-cierre",
            "cri-media", "tp-operacion", 4), 4);
        t9.Asignar(usuarios["vpereira"].Id, usuarios["vpereira"].Id, ahora.AddDays(-4).AddHours(1));
        t9.Bloquear("Se necesita información adicional de la localidad.", false,
            usuarios["vpereira"].Id, ahora.AddDays(-3));
        tickets.Add(t9);

        // 2 tickets cerrados.
        var t10 = Registrar(Nuevo(
            "Error en reportes de liquidación",
            "El reporte mensual presenta datos incompletos.",
            "lgomez", "Lagomar", "sis-liquidacion", "sub-liq-reportes",
            "cri-baja", "tp-software", 10), 10);
        t10.Asignar(usuarios["nacosta"].Id, usuarios["nacosta"].Id, ahora.AddDays(-10).AddHours(1));
        t10.Cerrar("Se corrigió la consulta que generaba el reporte.", null,
            tipos["tp-software"], [], usuarios["nacosta"].Id, ahora.AddDays(-8));
        tickets.Add(t10);

        var t11 = Registrar(Nuevo(
            "Instalación de equipo nuevo",
            "Se solicita configurar un equipo para un puesto de trabajo.",
            "mtechera", "Pando", "sis-instalaciones", "sub-inst-equipo",
            "cri-media", "tp-equipamiento", 8), 8);
        t11.Asignar(usuarios["vpereira"].Id, usuarios["vpereira"].Id, ahora.AddDays(-8).AddHours(1));
        t11.Cerrar("El equipo quedó instalado y operativo.", null,
            tipos["tp-equipamiento"], [], usuarios["vpereira"].Id, ahora.AddDays(-6));
        tickets.Add(t11);

        // 1 borrador sin número.
        tickets.Add(Nuevo(
            "Configuración de cuenta de correo",
            "Borrador de solicitud para configurar una cuenta de correo.",
            "lgomez", "Lagomar", "sis-correos", "sub-correo-cuenta",
            "cri-baja", "tp-configuracion", 0));

        contexto.Tickets.AddRange(tickets);
        await contexto.SaveChangesAsync(cancelacion);
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
