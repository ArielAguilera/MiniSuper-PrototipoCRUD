using System;

namespace PrototipoCRUD.Data
{
    // Operaciones críticas protegidas (sirven para elegir el mensaje correcto al traducir un error de la BD)
    public enum OperacionDatos
    {
        RegistrarProducto,
        ActualizarProducto,
        EliminarProducto,
        ConsultarProductos,
        RegistrarVenta
    }

    // Tipo de falla ya "traducida" a lenguaje de negocio
    public enum TipoFalla
    {
        CodigoDuplicado,
        ProductoConVentas,
        ProductoInexistente,
        StockInsuficiente,
        ServicioNoDisponible,
        DatosNoValidos,
        Inesperada
    }

    /// <summary>
    /// Excepción de negocio: su Message ya es un texto SEGURO para mostrar al usuario
    /// (sin SqlState, sin nombres de tablas, sin StackTrace). La excepción técnica original
    /// viaja en InnerException y se guarda solo en errores_sistema.log.
    /// </summary>
    public class OperacionFallidaException : InvalidOperationException
    {
        public TipoFalla Tipo { get; }

        public OperacionFallidaException(TipoFalla tipo, string mensajeUsuario, Exception? interna = null)
            : base(mensajeUsuario, interna)
        {
            Tipo = tipo;
        }
    }
}
