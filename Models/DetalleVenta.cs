namespace PrototipoCRUD.Models
{
    public class DetalleVenta
    {
        public int Id { get; set; }
        public int IdVenta { get; set; }
        public int IdProducto { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }

        // Propiedad calculada automáticamente: Cantidad × PrecioUnitario
        public decimal Subtotal => Cantidad * PrecioUnitario;
    }
}
