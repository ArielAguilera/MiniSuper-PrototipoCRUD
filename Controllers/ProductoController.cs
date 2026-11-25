using Microsoft.AspNetCore.Mvc;
using PrototipoCRUD.Data;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Controllers
{
    // Controlador: recibe las peticiones del navegador, llama al DAO
    // y decide qué vista mostrar. No tiene SQL.
    public class ProductoController : Controller
    {
        private ProductoDAO _dao = new ProductoDAO();

        // READ: GET /Producto  -> muestra la lista de productos
        public IActionResult Index()
        {
            var productos = _dao.ObtenerTodos();
            return View(productos);
        }

        // CREATE: GET /Producto/Create -> muestra el formulario vacío
        public IActionResult Create()
        {
            return View();
        }

        // CREATE: POST /Producto/Create -> recibe el formulario y guarda
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Producto producto)
        {
            // Validamos que el código no se repita (la BD también lo controla con UNIQUE)
            if (!string.IsNullOrWhiteSpace(producto.Codigo) && _dao.ExisteCodigo(producto.Codigo, 0))
            {
                ModelState.AddModelError("Codigo", "Ya existe un producto con ese código");
            }

            // Si hay errores de validación volvemos a mostrar el formulario con los mensajes
            if (!ModelState.IsValid)
            {
                return View(producto);
            }

            _dao.Insertar(producto);
            TempData["Mensaje"] = "Producto registrado correctamente";
            return RedirectToAction("Index");
        }

        // UPDATE: GET /Producto/Edit/5 -> muestra el formulario con los datos actuales
        public IActionResult Edit(int id)
        {
            var producto = _dao.ObtenerPorId(id);
            if (producto == null)
            {
                return NotFound(); // el id no existe en la BD
            }
            return View(producto);
        }

        // UPDATE: POST /Producto/Edit/5 -> guarda los cambios
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Producto producto)
        {
            if (!string.IsNullOrWhiteSpace(producto.Codigo) && _dao.ExisteCodigo(producto.Codigo, producto.Id))
            {
                ModelState.AddModelError("Codigo", "Ya existe otro producto con ese código");
            }

            if (!ModelState.IsValid)
            {
                return View(producto);
            }

            _dao.Actualizar(producto);
            TempData["Mensaje"] = "Producto actualizado correctamente";
            return RedirectToAction("Index");
        }

        // DELETE: POST /Producto/Delete/5 -> elimina el producto
        // Se usa POST (y no un simple link GET) para que no se borre por accidente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            _dao.Eliminar(id);
            TempData["Mensaje"] = "Producto eliminado correctamente";
            return RedirectToAction("Index");
        }
    }
}
