using System;
using Npgsql;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Data
{
    /// <summary>
    /// Traduce errores técnicos de PostgreSQL/Npgsql a mensajes de negocio:
    /// lenguaje claro, tono respetuoso (sin culpar) y con orientación para resolver.
    /// </summary>
    public static class TraductorErrores
    {
        public const string MensajeSinConexion =
            "No pudimos comunicarnos con la base de datos del sistema en este momento. " +
            "Tus datos no se modificaron. Verifica que el servicio de datos esté activo e inténtalo de nuevo en unos segundos.";

        // Error de la base de datos con código SQLSTATE (23505 = único, 23503 = clave foránea, ...)
        public static OperacionFallidaException Traducir(PostgresException ex, OperacionDatos operacion, string? dato = null)
        {
            return TraducirSqlState(ex.SqlState, operacion, dato, ex);
        }

        // Servidor apagado, red caída, tiempo de espera agotado, etc.
        public static OperacionFallidaException TraducirRed(NpgsqlException ex)
        {
            return new OperacionFallidaException(TipoFalla.ServicioNoDisponible, MensajeSinConexion, ex);
        }

        // Separado para poder probarlo sin necesidad de una base de datos
        public static OperacionFallidaException TraducirSqlState(
            string? sqlState, OperacionDatos operacion, string? dato = null, Exception? interna = null)
        {
            string codigoSql = sqlState ?? string.Empty;

            switch (codigoSql)
            {
                case "23505": // unique_violation
                    string quien = string.IsNullOrWhiteSpace(dato) ? "Ese código" : $"El código '{dato}'";
                    return new OperacionFallidaException(TipoFalla.CodigoDuplicado,
                        $"{quien} ya pertenece a otro producto. Ingresa un código distinto o edita el producto existente.",
                        interna);

                case "23503": // foreign_key_violation
                    if (operacion == OperacionDatos.EliminarProducto)
                    {
                        return new OperacionFallidaException(TipoFalla.ProductoConVentas,
                            "No se puede eliminar este producto porque ya forma parte de ventas registradas. " +
                            "Para conservar el historial, déjalo en el catálogo; si ya no lo vendes, puedes poner su stock en 0.",
                            interna);
                    }
                    return new OperacionFallidaException(TipoFalla.DatosNoValidos,
                        "Uno de los productos de la venta ya no existe en el catálogo. " +
                        "Actualiza la página, revisa el carrito e inténtalo de nuevo.",
                        interna);

                case "23514": // check_violation
                    if (operacion == OperacionDatos.RegistrarVenta)
                    {
                        return new OperacionFallidaException(TipoFalla.DatosNoValidos,
                            $"El descuento de la venta debe estar entre 0% y {Venta.DescuentoMaximo:0}%. Corrígelo y vuelve a confirmar.",
                            interna);
                    }
                    return new OperacionFallidaException(TipoFalla.DatosNoValidos,
                        "Algún valor está fuera del rango permitido (el precio debe ser mayor a Bs. 0.00 y el stock, 0 o más). " +
                        "Corrígelo e inténtalo de nuevo.",
                        interna);

                case "23502": // not_null_violation
                case "22001": // string_data_right_truncation (texto demasiado largo)
                case "22003": // numeric_value_out_of_range
                    return new OperacionFallidaException(TipoFalla.DatosNoValidos,
                        "Algún dato falta, es demasiado largo o está fuera del rango permitido. Revisa los campos e inténtalo de nuevo.",
                        interna);
            }

            // Clases 08 (conexión), 28 (autenticación), 53 (recursos), 57 (servidor apagándose) y base inexistente
            if (codigoSql.StartsWith("08") || codigoSql.StartsWith("28") ||
                codigoSql.StartsWith("53") || codigoSql.StartsWith("57") || codigoSql == "3D000")
            {
                return new OperacionFallidaException(TipoFalla.ServicioNoDisponible, MensajeSinConexion, interna);
            }

            return new OperacionFallidaException(TipoFalla.Inesperada,
                "No pudimos completar la operación por una restricción de datos. " +
                "Revisa la información e inténtalo de nuevo; si el problema continúa, contacta a soporte.",
                interna);
        }
    }
}
