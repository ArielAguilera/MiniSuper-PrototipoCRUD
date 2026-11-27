using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace PrototipoCRUD.Models
{
    public class Venta
    {
        // Regla de negocio CR-01: descuento máximo permitido por política de la empresa
        public const decimal DescuentoMinimo = 0m;
        public const decimal DescuentoMaximo = 20m;

        public int Id { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string Cliente { get; set; } = "Cliente General";

        // CR-01 (Trazabilidad): vendedor que registra la venta y otorga el descuento
        [Required(ErrorMessage = "Indica el nombre del vendedor que registra la venta (ej. Carlos Pérez).")]
        [StringLength(100, ErrorMessage = "El nombre del vendedor puede tener hasta 100 caracteres.")]
        [Display(Name = "Vendedor")]
        public string Vendedor { get; set; } = string.Empty;

        // Lista de productos agregados a esta venta
        public List<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();

        // CR-01: el setter es privado, la única forma de cambiarlo es AplicarDescuento()
        // (así es imposible guardar un porcentaje fuera de 0%–20% desde otra capa).
        public decimal PorcentajeDescuento { get; private set; }

        // Suma automáticamente los subtotales de todos los ítems usando LINQ
        public decimal SubtotalBruto => Detalles.Sum(d => d.Subtotal);

        public decimal MontoDescuento =>
            Math.Round(SubtotalBruto * (PorcentajeDescuento / 100m), 2, MidpointRounding.AwayFromZero);

        public decimal TotalFinal => SubtotalBruto - MontoDescuento;

        // Compatibilidad hacia atrás (regresión): el código anterior usa venta.Total.
        // Sin descuento, Total == SubtotalBruto, igual que antes del cambio.
        public decimal Total
        {
            get => TotalFinal;
            set { }
        }

        /// <summary>
        /// Aplica un descuento porcentual a la venta. Regla CR-01: entre 0% y 20% (ambos inclusive).
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Si el porcentaje es menor a 0 o mayor a 20.</exception>
        public void AplicarDescuento(decimal porcentaje)
        {
            if (porcentaje < DescuentoMinimo || porcentaje > DescuentoMaximo)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(porcentaje),
                    porcentaje,
                    "El descuento permitido debe estar entre 0% y un máximo de 20%.");
            }

            PorcentajeDescuento = porcentaje;
        }
    }
}
