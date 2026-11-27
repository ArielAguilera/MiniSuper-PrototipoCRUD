using System;
using Xunit;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Tests
{
    public class VentaDescuentoTests
    {
        // ================= PRUEBAS OBLIGATORIAS (Actividad 8) =================

        [Fact] // 1. Caso Válido: Descuento dentro del límite (15%)
        public void AplicarDescuento_PorcentajeValido_CalculaTotalCorrectamente()
        {
            var venta = new Venta { Vendedor = "Carlos Pérez" };
            venta.Detalles.Add(new DetalleVenta { Cantidad = 2, PrecioUnitario = 100m }); // Bruto: 200

            venta.AplicarDescuento(15m); // 15% de 200 = 30

            Assert.Equal(30m, venta.MontoDescuento);
            Assert.Equal(170m, venta.TotalFinal);
        }

        [Fact] // 2. Caso Borde: Exactamente el límite (20%)
        public void AplicarDescuento_LimiteMaximo20_EsAceptado()
        {
            var venta = new Venta { Vendedor = "Ana Gómez" };
            venta.Detalles.Add(new DetalleVenta { Cantidad = 1, PrecioUnitario = 100m });

            venta.AplicarDescuento(20m);

            Assert.Equal(80m, venta.TotalFinal);
        }

        [Fact] // 3. Caso Inválido: Supera el 20% (ej. 25%)
        public void AplicarDescuento_MayorAl20PorCiento_LanzaExcepcion()
        {
            var venta = new Venta { Vendedor = "Carlos Pérez" };

            Assert.Throws<ArgumentOutOfRangeException>(() => venta.AplicarDescuento(25m));
        }

        [Fact] // 4. Caso Inválido: Descuento negativo (-5%)
        public void AplicarDescuento_Negativo_LanzaExcepcion()
        {
            var venta = new Venta();

            Assert.Throws<ArgumentOutOfRangeException>(() => venta.AplicarDescuento(-5m));
        }

        // ================= PRUEBAS ADICIONALES (borde y regresión) =================

        [Fact] // Borde inferior: 0% es válido y no cambia el total
        public void AplicarDescuento_Cero_NoModificaElTotal()
        {
            var venta = new Venta { Vendedor = "Ana Gómez" };
            venta.Detalles.Add(new DetalleVenta { Cantidad = 3, PrecioUnitario = 10m });

            venta.AplicarDescuento(0m);

            Assert.Equal(0m, venta.MontoDescuento);
            Assert.Equal(30m, venta.TotalFinal);
        }

        [Fact] // Borde superior: apenas por encima del límite debe rechazarse
        public void AplicarDescuento_20ConUnCentesimo_LanzaExcepcion()
        {
            var venta = new Venta();

            Assert.Throws<ArgumentOutOfRangeException>(() => venta.AplicarDescuento(20.01m));
        }

        [Fact] // Si el descuento es inválido, el porcentaje anterior no se pierde ni se corrompe
        public void AplicarDescuento_Invalido_NoCambiaElPorcentajeAnterior()
        {
            var venta = new Venta();
            venta.AplicarDescuento(10m);

            Assert.Throws<ArgumentOutOfRangeException>(() => venta.AplicarDescuento(30m));

            Assert.Equal(10m, venta.PorcentajeDescuento);
        }

        [Fact] // Regresión: una venta sin descuento se comporta como antes (Total == suma de subtotales)
        public void VentaSinDescuento_TotalIgualAlSubtotalBruto()
        {
            var venta = new Venta();
            venta.Detalles.Add(new DetalleVenta { Cantidad = 2, PrecioUnitario = 7.50m });
            venta.Detalles.Add(new DetalleVenta { Cantidad = 1, PrecioUnitario = 9.00m });

            Assert.Equal(24m, venta.SubtotalBruto);
            Assert.Equal(0m, venta.PorcentajeDescuento);
            Assert.Equal(24m, venta.Total);
            Assert.Equal(24m, venta.TotalFinal);
        }
    }
}
