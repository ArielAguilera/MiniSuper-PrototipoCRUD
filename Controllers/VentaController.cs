using Microsoft.AspNetCore.Mvc;
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
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Venta venta)
        {
            // Validación de borde: impedir guardar ventas vacías
            if (venta.Detalles == null || venta.Detalles.Count == 0)
            {
                TempData["Error"] = "Debe agregar al menos un producto antes de confirmar la venta.";
                ViewBag.Productos = _productoDao.ObtenerTodos();
                return View(venta);
            }

            try
            {
                int idVenta = _ventaDao.RegistrarVenta(venta);
                TempData["Exito"] = $"¡Venta #{idVenta} registrada correctamente por un total de Bs. {venta.Total:N2}!";
                return RedirectToAction("Create");
            }
            catch (System.Exception ex)
            {
                TempData["Error"] = $"No se pudo registrar la venta: {ex.Message}";
                ViewBag.Productos = _productoDao.ObtenerTodos();
                return View(venta);
            }
        }
    }
}
