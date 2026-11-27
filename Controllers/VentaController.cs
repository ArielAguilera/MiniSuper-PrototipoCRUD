using Microsoft.AspNetCore.Mvc;
using Npgsql;
using PrototipoCRUD.Data;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Controllers
{
    public class VentaController : Controller
    {
        private readonly ProductoDAO _productoDao = new ProductoDAO();
        private readonly VentaDAO _ventaDao = new VentaDAO();

        // GET: /Venta/Create
        public IActionResult Create()
        {
            ViewBag.Productos = _productoDao.ObtenerTodos();
            return View(new Venta());
        }

        // POST: /Venta/Create
        // PorcentajeDescuento tiene setter privado en Venta (solo se cambia con AplicarDescuento),
        // por eso llega como parámetro aparte y se aplica a la entidad más abajo.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Venta venta, decimal? porcentajeDescuento)
        {
            // Para volver a mostrar lo que escribió el vendedor si hay que corregir algo
            ViewBag.PorcentajeIngresado = porcentajeDescuento;

            // Validación de borde: impedir guardar ventas vacías
            if (venta.Detalles == null || venta.Detalles.Count == 0)
            {
                return VolverConAviso(venta, "Debe agregar al menos un producto antes de confirmar la venta.");
            }

            // CR-01 (trazabilidad): sin vendedor no se registra la venta
            if (string.IsNullOrWhiteSpace(venta.Vendedor))
            {
                return VolverConAviso(venta, "Indica el nombre del vendedor para poder registrar la venta.");
            }

            // Si el campo tenía un texto no numérico (ej. "abc"), pedimos corregirlo en vez de asumir 0%
            if (ModelState.TryGetValue("porcentajeDescuento", out var entrada) && entrada.Errors.Count > 0)
            {
                return VolverConAviso(venta, "El descuento debe ser un número entre 0 y 20 (ej. 10 o 12.5).");
            }

            // CR-01 (regla de negocio): la entidad decide si el porcentaje es válido
            try
            {
                venta.AplicarDescuento(porcentajeDescuento ?? 0m);
            }
            catch (System.ArgumentOutOfRangeException)
            {
                // Mensaje claro y constructivo: el sistema NO se cierra, la venta sigue armada
                return VolverConAviso(venta,
                    $"El descuento máximo que puedes otorgar es {Venta.DescuentoMaximo:0}%. " +
                    $"Ingresaste {porcentajeDescuento:0.##}%. Escribe un valor entre 0 y {Venta.DescuentoMaximo:0} y vuelve a confirmar.");
            }

            try
            {
                int idVenta = _ventaDao.RegistrarVenta(venta);

                string detalleDescuento = venta.PorcentajeDescuento > 0
                    ? $" (descuento {venta.PorcentajeDescuento:0.##}% = Bs. {venta.MontoDescuento:N2}, vendedor: {venta.Vendedor.Trim()})"
                    : string.Empty;

                TempData["Exito"] = $"¡Venta #{idVenta} registrada correctamente por un total de Bs. {venta.TotalFinal:N2}!{detalleDescuento}";
                return RedirectToAction("Create");
            }
            catch (PostgresException pgEx) when (pgEx.SqlState == PostgresErrorCodes.CheckViolation)
            {
                // Segunda línea de defensa: el CHECK de PostgreSQL rechazó los datos
                return VolverConAviso(venta,
                    "La base de datos no aceptó los valores de esta venta (el descuento debe estar entre 0% y 20%). Revisa los datos e inténtalo de nuevo.");
            }
            catch (System.Exception ex)
            {
                return VolverConAviso(venta, $"No se pudo registrar la venta: {ex.Message}");
            }
        }

        // Muestra el formulario otra vez con el aviso y sin perder los datos de la venta
        private IActionResult VolverConAviso(Venta venta, string mensaje)
        {
            TempData["Error"] = mensaje;
            ViewBag.Productos = _productoDao.ObtenerTodos();
            return View(venta);
        }
    }
}
