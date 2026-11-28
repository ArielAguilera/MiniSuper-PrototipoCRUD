using System;
using System.IO;
using Xunit;
using PrototipoCRUD.Controllers;
using PrototipoCRUD.Data;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Tests
{
    // Actividad 9: pruebas de las cláusulas de guarda, de la traducción de errores a mensajes UX
    // y del log técnico. Ninguna necesita PostgreSQL: las guardas se ejecutan ANTES de abrir la conexión.
    public class ProteccionExcepcionesTests
    {
        // ---------- Cláusulas de guarda (ProductoDAO) ----------

        [Fact]
        public void ProductoDAO_Insertar_ProductoNulo_LanzaArgumentNullException()
        {
            var dao = new ProductoDAO();

            Assert.Throws<ArgumentNullException>(() => dao.Insertar(null!));
        }

        [Fact]
        public void ProductoDAO_Insertar_PrecioCero_LanzaArgumentOutOfRangeException()
        {
            var dao = new ProductoDAO();
            var producto = new Producto { Codigo = "7771", Nombre = "Leche", Precio = 0m, Stock = 1 };

            Assert.Throws<ArgumentOutOfRangeException>(() => dao.Insertar(producto));
        }

        [Fact]
        public void ProductoDAO_Insertar_SinNombre_LanzaArgumentException()
        {
            var dao = new ProductoDAO();
            var producto = new Producto { Codigo = "7771", Nombre = "   ", Precio = 5m, Stock = 1 };

            Assert.Throws<ArgumentException>(() => dao.Insertar(producto));
        }

        [Fact]
        public void ProductoDAO_Eliminar_IdInvalido_LanzaArgumentOutOfRangeException()
        {
            var dao = new ProductoDAO();

            Assert.Throws<ArgumentOutOfRangeException>(() => dao.Eliminar(0));
        }

        // ---------- Cláusulas de guarda (VentaDAO) ----------

        [Fact]
        public void VentaDAO_RegistrarVenta_VentaNula_LanzaArgumentNullException()
        {
            var dao = new VentaDAO();

            Assert.Throws<ArgumentNullException>(() => dao.RegistrarVenta(null!));
        }

        [Fact]
        public void VentaDAO_RegistrarVenta_SinProductos_LanzaArgumentException()
        {
            var dao = new VentaDAO();
            var venta = new Venta { Vendedor = "Carlos Pérez" };

            Assert.Throws<ArgumentException>(() => dao.RegistrarVenta(venta));
        }

        [Fact]
        public void VentaDAO_RegistrarVenta_CantidadCero_LanzaArgumentOutOfRangeException()
        {
            var dao = new VentaDAO();
            var venta = new Venta { Vendedor = "Carlos Pérez" };
            venta.Detalles.Add(new DetalleVenta { IdProducto = 1, Cantidad = 0, PrecioUnitario = 10m });

            Assert.Throws<ArgumentOutOfRangeException>(() => dao.RegistrarVenta(venta));
        }

        // ---------- Traducción de errores SQL a mensajes de negocio ----------

        [Fact]
        public void Traductor_ClaveUnica23505_DaMensajeDeCodigoDuplicadoSinJergaTecnica()
        {
            var ex = TraductorErrores.TraducirSqlState("23505", OperacionDatos.RegistrarProducto, "7771234500011");

            Assert.Equal(TipoFalla.CodigoDuplicado, ex.Tipo);
            Assert.Contains("7771234500011", ex.Message);
            Assert.DoesNotContain("23505", ex.Message);
            Assert.DoesNotContain("constraint", ex.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Traductor_ClaveForanea23503_AlEliminarProducto_ExplicaQueTieneVentas()
        {
            var ex = TraductorErrores.TraducirSqlState("23503", OperacionDatos.EliminarProducto);

            Assert.Equal(TipoFalla.ProductoConVentas, ex.Tipo);
            Assert.Contains("ventas", ex.Message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("23503", ex.Message);
            Assert.DoesNotContain("detalle_ventas", ex.Message);
        }

        [Fact]
        public void Traductor_CheckDescuento23514_MencionaElLimiteDel20()
        {
            var ex = TraductorErrores.TraducirSqlState("23514", OperacionDatos.RegistrarVenta);

            Assert.Contains("20%", ex.Message);
        }

        [Fact]
        public void Traductor_ErrorDeConexion08006_DaMensajeDeServicioNoDisponible()
        {
            var ex = TraductorErrores.TraducirSqlState("08006", OperacionDatos.RegistrarVenta);

            Assert.Equal(TipoFalla.ServicioNoDisponible, ex.Tipo);
            Assert.Equal(TraductorErrores.MensajeSinConexion, ex.Message);
        }

        // ---------- Mensajes UX y log técnico ----------
        // Estas pruebas escriben el log en un archivo temporal (no ensucian errores_sistema.log)

        [Fact]
        public void MensajesUx_ArgumentNull_NoMuestraNombreDeParametro()
        {
            var ex = new ArgumentNullException("p", "No se recibieron los datos del producto.");

            string mensaje = MensajesUx.Traducir(ex, "prueba");

            Assert.DoesNotContain("Parameter", mensaje);
            Assert.DoesNotContain("'p'", mensaje);
        }

        [Fact]
        public void MensajesUx_PrecioFueraDeRango_PideUnPrecioMayorACero()
        {
            var ex = new ArgumentOutOfRangeException("Precio", 0m, "El precio debe ser mayor a cero.");

            string mensaje = MensajesUx.Traducir(ex, "prueba");

            Assert.Contains("precio", mensaje, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Actual value", mensaje);
        }

        [Fact]
        public void MensajesUx_ExcepcionInesperada_NoFiltraDetalleTecnicoYLoGuardaEnElLog()
        {
            string rutaAnterior = RegistroErrores.RutaLog;
            string rutaTemporal = Path.Combine(Path.GetTempPath(), $"errores_prueba_{Guid.NewGuid():N}.log");
            RegistroErrores.RutaLog = rutaTemporal;

            try
            {
                Exception capturada;
                try
                {
                    throw new InvalidOperationException("SELECT * FROM tabla_secreta falló");
                }
                catch (Exception ex)
                {
                    capturada = ex;
                }

                string mensaje = MensajesUx.Traducir(capturada, "Prueba de log");

                Assert.Equal(MensajesUx.Inesperado, mensaje);
                Assert.DoesNotContain("SELECT", mensaje);

                string contenidoLog = File.ReadAllText(rutaTemporal);
                Assert.Contains("InvalidOperationException", contenidoLog);
                Assert.Contains("SELECT * FROM tabla_secreta", contenidoLog);
                Assert.Contains("StackTrace", contenidoLog);
            }
            finally
            {
                RegistroErrores.RutaLog = rutaAnterior;
                if (File.Exists(rutaTemporal))
                {
                    File.Delete(rutaTemporal);
                }
            }
        }

        [Fact]
        public void RegistroErrores_LaMismaExcepcionSeRegistraUnaSolaVez()
        {
            string rutaAnterior = RegistroErrores.RutaLog;
            string rutaTemporal = Path.Combine(Path.GetTempPath(), $"errores_prueba_{Guid.NewGuid():N}.log");
            RegistroErrores.RutaLog = rutaTemporal;

            try
            {
                var ex = new InvalidOperationException("falla de prueba");

                RegistroErrores.RegistrarLogTecnico(ex, "Operación A");
                RegistroErrores.RegistrarLogTecnico(ex, "Operación B (re-lanzada)");

                string contenidoLog = File.ReadAllText(rutaTemporal);
                Assert.Contains("Operación A", contenidoLog);
                Assert.DoesNotContain("Operación B", contenidoLog);
            }
            finally
            {
                RegistroErrores.RutaLog = rutaAnterior;
                if (File.Exists(rutaTemporal))
                {
                    File.Delete(rutaTemporal);
                }
            }
        }
    }
}
