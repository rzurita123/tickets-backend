namespace SistemaTickets.Infrastructure.Persistencia;

internal record SolucionInicial(
    string Codigo, string Titulo, string Texto, string? Sistema, string? Subsistema, string[] PalabrasClave,
    string PropuestaPor, string RevisadaPor, bool ConImagen);

internal static class SolucionesIniciales
{
    public static readonly SolucionInicial[] Lista =
    [
        new("sol-liq-reprocesar", "Reprocesar la liquidación diaria que no aparece",
            "1. Confirmar con la agencia la fecha de la liquidación que falta.\n2. Verificar en el sistema central si el proceso de liquidación terminó.\n3. Si no terminó o terminó con error, reprocesar la liquidación de esa fecha.\n4. Pedir a la agencia que vuelva a ingresar y confirme que la ve.",
            "sis-liquidacion", "sub-liq-diaria", ["liquidación", "no aparece", "no figura", "liquidación del día"], "ralvarez", "ralvarez", false),
        new("sol-liq-anulacion", "Diferencia en la liquidación por anulación fuera de horario",
            "Cuando una anulación se registra después del cierre, impacta en la liquidación del día siguiente.\n1. Pedir a la agencia la fecha y el importe de la diferencia.\n2. Buscar anulaciones registradas después del cierre de ese día.\n3. Mostrar al cliente en qué liquidación figura.",
            "sis-liquidacion", "sub-liq-diaria", ["diferencia", "no coincide", "anulación", "liquidación"], "scabrera", "scabrera", false),
        new("sol-liq-premio", "Premio pagado después del cierre",
            "Los premios pagados después del cierre figuran en la liquidación del día siguiente.\n1. Pedir fecha y hora del pago.\n2. Confirmar si fue posterior al cierre.\n3. Indicar al cliente en qué liquidación lo va a ver.",
            "sis-liquidacion", "sub-liq-premios", ["premio", "pagado", "no figura", "descontado"], "nacosta", "scabrera", false),
        new("sol-navegador", "Borrar la caché del navegador",
            "Solución general: sirve cuando una pantalla no carga, un botón no responde o una descarga falla.\n1. Cerrar todas las ventanas del navegador.\n2. Borrar la caché y las cookies del último día.\n3. Volver a ingresar y repetir la operación.\n4. Si persiste, probar con otro navegador.",
            null, null, ["exportar", "descargar", "no carga", "navegador", "botón no responde", "pantalla en blanco"], "ralvarez", "ralvarez", false),
        new("sol-caja-anterior", "Caja del día anterior sin cerrar",
            "1. Ingresar con el usuario encargado de la agencia.\n2. Verificar si quedó abierta la caja del día anterior.\n3. Hacer el cierre pendiente.\n4. Abrir la caja del día.",
            "sis-caja", "sub-caja-apertura", ["abrir caja", "apertura", "caja anterior", "sin cerrar"], "scabrera", "scabrera", false),
        new("sol-caja-duplicado", "Movimiento duplicado en el cierre de caja",
            "1. Pedir a la agencia el importe de la diferencia.\n2. Revisar los movimientos del día buscando registros repetidos por ese importe.\n3. Anular el movimiento duplicado.\n4. Volver a hacer el cierre.",
            "sis-caja", "sub-caja-cierre", ["diferencia", "cierre de caja", "faltante", "sobrante", "duplicado"], "vpereira", "scabrera", false),
        new("sol-caja-anular", "Anular y volver a ingresar un movimiento",
            "1. Buscar el movimiento en el detalle del día.\n2. Anularlo indicando el motivo.\n3. Ingresarlo de nuevo con el importe correcto.",
            "sis-caja", "sub-caja-movimientos", ["importe", "movimiento", "anular", "corregir", "equivocado"], "scabrera", "scabrera", false),
        new("sol-term-desbloqueo", "Desbloquear una terminal por intentos fallidos",
            "1. Buscar la terminal en Control Terminales.\n2. Confirmar que el bloqueo es por intentos fallidos.\n3. Desbloquearla.\n4. Pedir a la agencia que ingrese con el usuario correcto.",
            "sis-control-terminales", "sub-term-bloqueo", ["bloqueada", "bloqueo", "no permite vender", "intentos"], "ralvarez", "ralvarez", false),
        new("sol-term-reinicio", "Reiniciar la terminal y verificar la conexión",
            "1. Apagar la terminal y esperar 30 segundos.\n2. Encenderla y verificar que tenga conexión.\n3. Forzar la sincronización.\n4. Si no sincroniza, revisar la conexión de la agencia.",
            "sis-control-terminales", "sub-term-sincro", ["sincroniza", "sincronización", "desactualizada", "reiniciar"], "ralvarez", "ralvarez", false),
        new("sol-term-alta", "Alta de una terminal nueva",
            "1. Pedir el número de serie de la terminal.\n2. Registrarla en Control Terminales asociada a la agencia.\n3. Confirmar con la agencia la primera conexión.",
            "sis-control-terminales", "sub-term-alta", ["alta", "terminal nueva", "número de serie", "habilitar"], "ralvarez", "ralvarez", false),
        new("sol-term-impresora", "La impresora de la terminal no imprime",
            "1. Apagar la terminal.\n2. Retirar el rollo y volver a colocarlo con el lado térmico hacia el cabezal.\n3. Encender la terminal y hacer una impresión de prueba.\n4. Si imprime en blanco, el rollo está colocado al revés.",
            "sis-control-terminales", "sub-term-impresion", ["impresora", "no imprime", "imprime en blanco", "papel", "rollo", "comprobante"], "vpereira", "scabrera", true),
        new("sol-online-router", "Sin conexión: reiniciar el router de la agencia",
            "1. Confirmar si el problema ocurre en todas las terminales.\n2. Pedir a la agencia que apague el router, espere un minuto y lo vuelva a encender.\n3. Verificar la conexión con On-Line.\n4. Si sigue sin conexión, la agencia debe contactar a su proveedor de internet.",
            "sis-online", "sub-online-conexion", ["sin conexión", "conexión", "internet", "router", "no conecta"], "nacosta", "ralvarez", false),
        new("sol-env-tamano", "El archivo de cierre supera el tamaño permitido",
            "1. Revisar si el archivo incluye días anteriores.\n2. Generarlo de nuevo sólo con el día actual.\n3. Enviarlo y confirmar la recepción.",
            "sis-envio-archivos", "sub-env-envio", ["archivo", "envío", "error de envío", "tamaño"], "ralvarez", "ralvarez", false),
        new("sol-correo-spam", "Los correos llegan a la carpeta de no deseado",
            "1. Pedir a la agencia que revise la carpeta de correo no deseado.\n2. Marcar el remitente como seguro.\n3. Confirmar que el próximo correo llegue a la bandeja de entrada.",
            "sis-correos", "sub-correo-recepcion", ["correo", "no llegan", "no deseado", "spam", "recibir"], "vpereira", "scabrera", false),
        new("sol-correo-config", "Configurar la cuenta de correo en un equipo",
            "1. Abrir el programa de correo del equipo.\n2. Agregar la cuenta con los datos de servidor de ORMEN.\n3. Enviar un correo de prueba y verificar la recepción.",
            "sis-correos", "sub-correo-cuenta", ["configurar", "cuenta", "correo", "equipo nuevo"], "ralvarez", "ralvarez", false),
        new("sol-usu-desbloqueo", "Desbloquear usuario y generar contraseña temporal",
            "1. Verificar la identidad del encargado de la agencia.\n2. Desbloquear el usuario.\n3. Generar una contraseña temporal.\n4. Indicar que la cambie en el primer ingreso.",
            "sis-adm-usuarios", "sub-usu-contrasena", ["usuario bloqueado", "contraseña", "intentos", "no puede ingresar"], "nacosta", "ralvarez", false),
        new("sol-usu-alta", "Alta de usuario de agencia",
            "1. Pedir nombre y documento de la persona al encargado.\n2. Crear el usuario con el perfil que corresponda.\n3. Enviar los datos de acceso al encargado.",
            "sis-adm-usuarios", "sub-usu-alta", ["alta", "usuario nuevo", "empleado", "empleada"], "ralvarez", "ralvarez", false),
        new("sol-inst-remota", "Instalación del sistema por acceso remoto",
            "1. Coordinar el acceso remoto con la agencia.\n2. Instalar el sistema.\n3. Restaurar la configuración de la agencia.\n4. Verificar el ingreso con un usuario de la agencia.",
            "sis-instalaciones", "sub-inst-equipo", ["instalar", "instalación", "equipo nuevo", "computadora", "acceso remoto", "formatear"], "ralvarez", "ralvarez", false),
    ];

    public const string ImagenPasos = "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"720\" height=\"260\" viewBox=\"0 0 720 260\"> <rect width=\"720\" height=\"260\" fill=\"#F7F9FB\"/> <g font-family=\"Arial, Helvetica, sans-serif\"> <rect x=\"24\" y=\"56\" width=\"196\" height=\"124\" rx=\"12\" fill=\"#FFFFFF\" stroke=\"#C9D3DD\"/> <circle cx=\"56\" cy=\"90\" r=\"17\" fill=\"#1D78B4\"/> <text x=\"56\" y=\"96\" text-anchor=\"middle\" font-size=\"17\" font-weight=\"bold\" fill=\"#FFFFFF\">1</text> <text x=\"40\" y=\"138\" font-size=\"17\" fill=\"#1C3A5A\">Apagar la</text> <text x=\"40\" y=\"160\" font-size=\"17\" fill=\"#1C3A5A\">terminal</text> <rect x=\"262\" y=\"56\" width=\"196\" height=\"124\" rx=\"12\" fill=\"#FFFFFF\" stroke=\"#C9D3DD\"/> <circle cx=\"294\" cy=\"90\" r=\"17\" fill=\"#1D78B4\"/> <text x=\"294\" y=\"96\" text-anchor=\"middle\" font-size=\"17\" font-weight=\"bold\" fill=\"#FFFFFF\">2</text> <text x=\"278\" y=\"138\" font-size=\"17\" fill=\"#1C3A5A\">Retirar y volver</text> <text x=\"278\" y=\"160\" font-size=\"17\" fill=\"#1C3A5A\">a colocar el rollo</text> <rect x=\"500\" y=\"56\" width=\"196\" height=\"124\" rx=\"12\" fill=\"#FFFFFF\" stroke=\"#C9D3DD\"/> <circle cx=\"532\" cy=\"90\" r=\"17\" fill=\"#1D78B4\"/> <text x=\"532\" y=\"96\" text-anchor=\"middle\" font-size=\"17\" font-weight=\"bold\" fill=\"#FFFFFF\">3</text> <text x=\"516\" y=\"138\" font-size=\"17\" fill=\"#1C3A5A\">Encender y hacer</text> <text x=\"516\" y=\"160\" font-size=\"17\" fill=\"#1C3A5A\">una prueba</text> <path d=\"M226 118h28M246 110l8 8-8 8M464 118h28M484 110l8 8-8 8\" stroke=\"#D6B169\" stroke-width=\"3\" fill=\"none\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/> <text x=\"24\" y=\"236\" font-size=\"13\" fill=\"#5F6E7C\">Imagen de ejemplo (demo)</text> </g> </svg>";
}
