using System;
using System.Collections.Generic;
using System.Linq;

namespace PrototipoCRUD.Models
{
    public class Venta
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; } = DateTime.Now;
        public string Cliente { get; set; } = "Cliente General";

        // Lista de productos agregados a esta venta
        public List<DetalleVenta> Detalles { get; set; } = new List<DetalleVenta>();

        // Suma automáticamente los subtotales de todos los ítems usando LINQ
        public decimal Total
        {
            get => Detalles.Sum(d => d.Subtotal);
            set { }
        }
    }
}
