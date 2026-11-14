using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SistemaTickets.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Criticidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Criticidades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GruposSoporte",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GruposSoporte", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Localidades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Localidades", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ParametrosSugerencia",
                columns: table => new
                {
                    Clave = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Valor = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ParametrosSugerencia", x => x.Clave);
                });

            migrationBuilder.CreateTable(
                name: "Sistemas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sistemas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposProblema",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposProblema", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreUsuario = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HashContrasena = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Rol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Cargo = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LocalidadId = table.Column<int>(type: "int", nullable: true),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Usuarios_Localidades_LocalidadId",
                        column: x => x.LocalidadId,
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Subsistemas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SistemaId = table.Column<int>(type: "int", nullable: false),
                    Codigo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subsistemas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subsistemas_Sistemas_SistemaId",
                        column: x => x.SistemaId,
                        principalTable: "Sistemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosAuditoria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioId = table.Column<int>(type: "int", nullable: true),
                    Operacion = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Entidad = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    EntidadId = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Detalle = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAuditoria", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosAuditoria_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Actividades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    AutorId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Privada = table.Column<bool>(type: "bit", nullable: false),
                    EstadoAnterior = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    EstadoNuevo = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    OperadorId = table.Column<int>(type: "int", nullable: true),
                    DestinoGrupoId = table.Column<int>(type: "int", nullable: true),
                    DestinoUsuarioId = table.Column<int>(type: "int", nullable: true),
                    Detalle = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actividades", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Actividades_GruposSoporte_DestinoGrupoId",
                        column: x => x.DestinoGrupoId,
                        principalTable: "GruposSoporte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Actividades_Usuarios_AutorId",
                        column: x => x.AutorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Actividades_Usuarios_DestinoUsuarioId",
                        column: x => x.DestinoUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Actividades_Usuarios_OperadorId",
                        column: x => x.OperadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Adjuntos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: true),
                    SolucionId = table.Column<int>(type: "int", nullable: true),
                    ActividadId = table.Column<int>(type: "int", nullable: true),
                    Origen = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Clave = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    TipoContenido = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Contenido = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    SubidoPorId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Adjuntos", x => x.Id);
                    table.CheckConstraint("CK_Adjuntos_Pertenencia", "([TicketId] IS NOT NULL AND [SolucionId] IS NULL) OR ([TicketId] IS NULL AND [SolucionId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Adjuntos_Actividades_ActividadId",
                        column: x => x.ActividadId,
                        principalTable: "Actividades",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Adjuntos_Usuarios_SubidoPorId",
                        column: x => x.SubidoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Favoritos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    TicketId = table.Column<int>(type: "int", nullable: true),
                    SolucionId = table.Column<int>(type: "int", nullable: true),
                    Fecha = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Favoritos", x => x.Id);
                    table.CheckConstraint("CK_Favoritos_Elemento", "([TicketId] IS NOT NULL AND [SolucionId] IS NULL) OR ([TicketId] IS NULL AND [SolucionId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_Favoritos_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notificaciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TicketId = table.Column<int>(type: "int", nullable: true),
                    DestinatarioId = table.Column<int>(type: "int", nullable: true),
                    Destino = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Canal = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Asunto = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Cuerpo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Intentos = table.Column<int>(type: "int", nullable: false),
                    CreadaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EnviadaEn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notificaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notificaciones_Usuarios_DestinatarioId",
                        column: x => x.DestinatarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Soluciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Codigo = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Texto = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SistemaId = table.Column<int>(type: "int", nullable: true),
                    SubsistemaId = table.Column<int>(type: "int", nullable: true),
                    PalabrasClave = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TicketOrigenId = table.Column<int>(type: "int", nullable: true),
                    PropuestaPorId = table.Column<int>(type: "int", nullable: false),
                    RevisadaPorId = table.Column<int>(type: "int", nullable: true),
                    MotivoRechazo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreadaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualizadaEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevisadaEn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Soluciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Soluciones_Sistemas_SistemaId",
                        column: x => x.SistemaId,
                        principalTable: "Sistemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Soluciones_Subsistemas_SubsistemaId",
                        column: x => x.SubsistemaId,
                        principalTable: "Subsistemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Soluciones_Usuarios_PropuestaPorId",
                        column: x => x.PropuestaPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Soluciones_Usuarios_RevisadaPorId",
                        column: x => x.RevisadaPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Tickets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Numero = table.Column<int>(type: "int", nullable: true),
                    Titulo = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Estado = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Privado = table.Column<bool>(type: "bit", nullable: false),
                    RegistroDirecto = table.Column<bool>(type: "bit", nullable: false),
                    LocalidadId = table.Column<int>(type: "int", nullable: true),
                    SistemaId = table.Column<int>(type: "int", nullable: true),
                    SubsistemaId = table.Column<int>(type: "int", nullable: true),
                    CriticidadId = table.Column<int>(type: "int", nullable: true),
                    TipoProblemaId = table.Column<int>(type: "int", nullable: true),
                    CreadoPorId = table.Column<int>(type: "int", nullable: false),
                    OperadorId = table.Column<int>(type: "int", nullable: true),
                    ElevadoAGrupoId = table.Column<int>(type: "int", nullable: true),
                    ElevadoAUsuarioId = table.Column<int>(type: "int", nullable: true),
                    Resolucion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResolucionEnDescripcion = table.Column<bool>(type: "bit", nullable: false),
                    SolucionAplicadaId = table.Column<int>(type: "int", nullable: true),
                    CreadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CerradoEn = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Tickets_Criticidades_CriticidadId",
                        column: x => x.CriticidadId,
                        principalTable: "Criticidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tickets_GruposSoporte_ElevadoAGrupoId",
                        column: x => x.ElevadoAGrupoId,
                        principalTable: "GruposSoporte",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tickets_Localidades_LocalidadId",
                        column: x => x.LocalidadId,
                        principalTable: "Localidades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tickets_Sistemas_SistemaId",
                        column: x => x.SistemaId,
                        principalTable: "Sistemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tickets_Soluciones_SolucionAplicadaId",
                        column: x => x.SolucionAplicadaId,
                        principalTable: "Soluciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tickets_Subsistemas_SubsistemaId",
                        column: x => x.SubsistemaId,
                        principalTable: "Subsistemas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tickets_TiposProblema_TipoProblemaId",
                        column: x => x.TipoProblemaId,
                        principalTable: "TiposProblema",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tickets_Usuarios_CreadoPorId",
                        column: x => x.CreadoPorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tickets_Usuarios_ElevadoAUsuarioId",
                        column: x => x.ElevadoAUsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Tickets_Usuarios_OperadorId",
                        column: x => x.OperadorId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actividades_AutorId",
                table: "Actividades",
                column: "AutorId");

            migrationBuilder.CreateIndex(
                name: "IX_Actividades_DestinoGrupoId",
                table: "Actividades",
                column: "DestinoGrupoId");

            migrationBuilder.CreateIndex(
                name: "IX_Actividades_DestinoUsuarioId",
                table: "Actividades",
                column: "DestinoUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Actividades_OperadorId",
                table: "Actividades",
                column: "OperadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Actividades_TicketId",
                table: "Actividades",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Adjuntos_ActividadId",
                table: "Adjuntos",
                column: "ActividadId");

            migrationBuilder.CreateIndex(
                name: "IX_Adjuntos_SolucionId",
                table: "Adjuntos",
                column: "SolucionId");

            migrationBuilder.CreateIndex(
                name: "IX_Adjuntos_SubidoPorId",
                table: "Adjuntos",
                column: "SubidoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Adjuntos_TicketId",
                table: "Adjuntos",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Criticidades_Codigo",
                table: "Criticidades",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_SolucionId",
                table: "Favoritos",
                column: "SolucionId");

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_TicketId",
                table: "Favoritos",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_UsuarioId_SolucionId",
                table: "Favoritos",
                columns: new[] { "UsuarioId", "SolucionId" },
                unique: true,
                filter: "[SolucionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Favoritos_UsuarioId_TicketId",
                table: "Favoritos",
                columns: new[] { "UsuarioId", "TicketId" },
                unique: true,
                filter: "[TicketId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_GruposSoporte_Codigo",
                table: "GruposSoporte",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Localidades_Nombre",
                table: "Localidades",
                column: "Nombre",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_DestinatarioId",
                table: "Notificaciones",
                column: "DestinatarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_Estado",
                table: "Notificaciones",
                column: "Estado");

            migrationBuilder.CreateIndex(
                name: "IX_Notificaciones_TicketId",
                table: "Notificaciones",
                column: "TicketId");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_Entidad_EntidadId",
                table: "RegistrosAuditoria",
                columns: new[] { "Entidad", "EntidadId" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_Fecha",
                table: "RegistrosAuditoria",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_UsuarioId",
                table: "RegistrosAuditoria",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Sistemas_Codigo",
                table: "Sistemas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Soluciones_Codigo",
                table: "Soluciones",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Soluciones_PropuestaPorId",
                table: "Soluciones",
                column: "PropuestaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Soluciones_RevisadaPorId",
                table: "Soluciones",
                column: "RevisadaPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Soluciones_SistemaId",
                table: "Soluciones",
                column: "SistemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Soluciones_SubsistemaId",
                table: "Soluciones",
                column: "SubsistemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Soluciones_TicketOrigenId",
                table: "Soluciones",
                column: "TicketOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_Subsistemas_Codigo",
                table: "Subsistemas",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subsistemas_SistemaId",
                table: "Subsistemas",
                column: "SistemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CreadoPorId",
                table: "Tickets",
                column: "CreadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_CriticidadId",
                table: "Tickets",
                column: "CriticidadId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ElevadoAGrupoId",
                table: "Tickets",
                column: "ElevadoAGrupoId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_ElevadoAUsuarioId",
                table: "Tickets",
                column: "ElevadoAUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_LocalidadId",
                table: "Tickets",
                column: "LocalidadId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_Numero",
                table: "Tickets",
                column: "Numero",
                unique: true,
                filter: "[Numero] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_OperadorId",
                table: "Tickets",
                column: "OperadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SistemaId",
                table: "Tickets",
                column: "SistemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SolucionAplicadaId",
                table: "Tickets",
                column: "SolucionAplicadaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_SubsistemaId",
                table: "Tickets",
                column: "SubsistemaId");

            migrationBuilder.CreateIndex(
                name: "IX_Tickets_TipoProblemaId",
                table: "Tickets",
                column: "TipoProblemaId");

            migrationBuilder.CreateIndex(
                name: "IX_TiposProblema_Codigo",
                table: "TiposProblema",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_LocalidadId",
                table: "Usuarios",
                column: "LocalidadId");

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_NombreUsuario",
                table: "Usuarios",
                column: "NombreUsuario",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Actividades_Tickets_TicketId",
                table: "Actividades",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Adjuntos_Soluciones_SolucionId",
                table: "Adjuntos",
                column: "SolucionId",
                principalTable: "Soluciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Adjuntos_Tickets_TicketId",
                table: "Adjuntos",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Favoritos_Soluciones_SolucionId",
                table: "Favoritos",
                column: "SolucionId",
                principalTable: "Soluciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Favoritos_Tickets_TicketId",
                table: "Favoritos",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Notificaciones_Tickets_TicketId",
                table: "Notificaciones",
                column: "TicketId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Soluciones_Tickets_TicketOrigenId",
                table: "Soluciones",
                column: "TicketOrigenId",
                principalTable: "Tickets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tickets_GruposSoporte_ElevadoAGrupoId",
                table: "Tickets");

            migrationBuilder.DropForeignKey(
                name: "FK_Soluciones_Tickets_TicketOrigenId",
                table: "Soluciones");

            migrationBuilder.DropTable(
                name: "Adjuntos");

            migrationBuilder.DropTable(
                name: "Favoritos");

            migrationBuilder.DropTable(
                name: "Notificaciones");

            migrationBuilder.DropTable(
                name: "ParametrosSugerencia");

            migrationBuilder.DropTable(
                name: "RegistrosAuditoria");

            migrationBuilder.DropTable(
                name: "Actividades");

            migrationBuilder.DropTable(
                name: "GruposSoporte");

            migrationBuilder.DropTable(
                name: "Tickets");

            migrationBuilder.DropTable(
                name: "Criticidades");

            migrationBuilder.DropTable(
                name: "Soluciones");

            migrationBuilder.DropTable(
                name: "TiposProblema");

            migrationBuilder.DropTable(
                name: "Subsistemas");

            migrationBuilder.DropTable(
                name: "Usuarios");

            migrationBuilder.DropTable(
                name: "Sistemas");

            migrationBuilder.DropTable(
                name: "Localidades");
        }
    }
}
