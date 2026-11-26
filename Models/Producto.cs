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
        // Actividad 6: mensajes constructivos (dicen cómo corregir, sin culpar al usuario)
        [Required(ErrorMessage = "Escribe el código del producto (por ejemplo, el código de barras del empaque).")]
        [StringLength(30, ErrorMessage = "El código puede tener hasta 30 caracteres. Revisa que no tenga espacios de más.")]
        [Display(Name = "Código")]
        public string Codigo { get; set; } = string.Empty;

        [Required(ErrorMessage = "Escribe el nombre del producto tal como lo verá el cajero (ej. Leche PIL 1L).")]
        [StringLength(100, ErrorMessage = "El nombre puede tener hasta 100 caracteres. Intenta una versión más corta.")]
        [Display(Name = "Nombre del producto")]
        public string Nombre { get; set; } = string.Empty;

        [Required(ErrorMessage = "Indica el precio de venta en Bs. (ej. 7.50).")]
        [Range(0.01, 999999, ErrorMessage = "Ingresa un precio de venta mayor a Bs. 0.00 (ej. 7.50).")]
        [Display(Name = "Precio de venta (Bs.)")]
        public decimal Precio { get; set; }

        [Required(ErrorMessage = "Indica cuántas unidades hay en tienda.")]
        [Range(0, 100000, ErrorMessage = "El stock debe ser 0 o más unidades. Si todavía no llegó mercadería, escribe 0.")]
        [Display(Name = "Unidades en stock")]
        public int Stock { get; set; }
    }
}
