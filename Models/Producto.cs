using System.ComponentModel.DataAnnotations;

namespace PrototipoCRUD.Models
{
    // Modelo que representa un producto del catálogo del Minisúper El Ahorro.
    // Cada propiedad corresponde a una columna de la tabla "productos".
    public class Producto
    {
        public int Id { get; set; }

        // Código único del producto (por ejemplo el código de barras).
        // Lo usaremos en las siguientes actividades para buscar productos en la venta.
        [Required(ErrorMessage = "El código es obligatorio")]
        [StringLength(30, ErrorMessage = "El código no puede tener más de 30 caracteres")]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [StringLength(100, ErrorMessage = "El nombre no puede tener más de 100 caracteres")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "El precio es obligatorio")]
        [Range(0.01, 999999, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "El stock es obligatorio")]
        [Range(0, 100000, ErrorMessage = "El stock no puede ser negativo")]
        public int Stock { get; set; }
    }
}
