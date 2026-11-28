using Microsoft.AspNetCore.Mvc;
using PrototipoCRUD.Data;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Controllers
{
    // Controlador: recibe las peticiones del navegador, llama al DAO
    // y decide qué vista mostrar. No tiene SQL.
    //
    // ACTIVIDAD 9: cada acción está protegida con try-catch. El detalle técnico lo registra
    // la capa de datos en errores_sistema.log; aquí solo se muestra un mensaje UX orientador.
    public class ProductoController : Controller
    {
        private ProductoDAO _dao = new ProductoDAO();

        private const string MensajeCodigoRepetido =
            "Este código ya pertenece a otro producto del catálogo. Verifica el código del empaque o edita el producto existente.";

        // READ: GET /Producto  -> muestra la lista de productos
        public IActionResult Index()
        {
            try
            {
                var productos = _dao.ObtenerTodos();
                return View(productos);
            }
            catch (Exception ex)
            {
                // Si la BD no responde, la pantalla sigue viva y explica qué hacer
                ViewBag.ErrorCarga = MensajesUx.Traducir(ex, "Consultar listado de productos");
                return View(new List<Producto>());
            }
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
            try
            {
                string codigo = (producto.Codigo ?? string.Empty).Trim();

                // Validamos que el código no se repita (la BD también lo controla con UNIQUE)
                if (codigo.Length > 0 && _dao.ExisteCodigo(codigo, 0))
                {
                    ModelState.AddModelError("Codigo", MensajeCodigoRepetido);
                }

                // Si hay errores de validación volvemos a mostrar el formulario con los mensajes
                if (!ModelState.IsValid)
                {
                    return View(producto);
                }

                producto.Codigo = codigo;
                producto.Nombre = (producto.Nombre ?? string.Empty).Trim();
                _dao.Insertar(producto);
                TempData["Mensaje"] = "Producto registrado correctamente";
                return RedirectToAction("Index");
            }
            catch (OperacionFallidaException ex) when (ex.Tipo == TipoFalla.CodigoDuplicado)
            {
                // Otra persona registró el mismo código justo antes: lo señalamos en el campo
                ModelState.AddModelError("Codigo", ex.Message);
                return View(producto);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, MensajesUx.Traducir(ex, "Registrar producto"));
                return View(producto);
            }
        }

        // ACTIVIDAD 6: CREATE desde la ventana modal "Nuevo Producto"
        // POST /Producto/CrearModal
        // El modal envía el formulario con JavaScript (fetch) y esta acción
        // responde en JSON. Así el modal NO se cierra si hay errores y el
        // usuario no pierde lo que escribió.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrearModal(Producto producto)
        {
            try
            {
                string codigo = (producto.Codigo ?? string.Empty).Trim();

                // 1. Regla de negocio: el código no se puede repetir
                if (codigo.Length > 0 && _dao.ExisteCodigo(codigo, 0))
                {
                    ModelState.AddModelError("Codigo", MensajeCodigoRepetido);
                }

                // 2. Si hay errores, devolvemos un mensaje por cada campo (sin excepciones técnicas)
                if (!ModelState.IsValid)
                {
                    var errores = new Dictionary<string, string>();
                    foreach (var campo in ModelState)
                    {
                        if (campo.Value.Errors.Count > 0)
                        {
                            // Tomamos solo el primer mensaje de cada campo para no saturar
                            errores[campo.Key] = campo.Value.Errors[0].ErrorMessage;
                        }
                    }
                    return Json(new { ok = false, errores = errores });
                }

                // 3. Todo correcto: guardamos con el mismo DAO de la Actividad 5
                producto.Codigo = codigo;
                producto.Nombre = (producto.Nombre ?? string.Empty).Trim();
                _dao.Insertar(producto);

                // El mensaje se muestra en la lista cuando la página se recarga
                TempData["Mensaje"] = $"Listo: \"{producto.Nombre}\" se agregó al catálogo.";
                return Json(new { ok = true });
            }
            catch (OperacionFallidaException ex) when (ex.Tipo == TipoFalla.CodigoDuplicado)
            {
                var errores = new Dictionary<string, string> { ["Codigo"] = ex.Message };
                return Json(new { ok = false, errores = errores });
            }
            catch (Exception ex)
            {
                // Mensaje general dentro del modal (el modal no se cierra y no se pierde lo escrito)
                return Json(new { ok = false, errorGeneral = MensajesUx.Traducir(ex, "Registrar producto (modal)") });
            }
        }

        // UPDATE: GET /Producto/Edit/5 -> muestra el formulario con los datos actuales
        public IActionResult Edit(int id)
        {
            try
            {
                var producto = _dao.ObtenerPorId(id);
                if (producto == null)
                {
                    // Referencia nula controlada: en lugar de una página en blanco (404) explicamos qué pasó
                    TempData["Error"] = "No encontramos ese producto; puede que ya no exista. Revisa la lista e inténtalo de nuevo.";
                    return RedirectToAction("Index");
                }
                return View(producto);
            }
            catch (Exception ex)
            {
                TempData["Error"] = MensajesUx.Traducir(ex, "Abrir edición de producto");
                return RedirectToAction("Index");
            }
        }

        // UPDATE: POST /Producto/Edit/5 -> guarda los cambios
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Producto producto)
        {
            try
            {
                string codigo = (producto.Codigo ?? string.Empty).Trim();

                if (codigo.Length > 0 && _dao.ExisteCodigo(codigo, producto.Id))
                {
                    ModelState.AddModelError("Codigo", MensajeCodigoRepetido);
                }

                if (!ModelState.IsValid)
                {
                    return View(producto);
                }

                producto.Codigo = codigo;
                producto.Nombre = (producto.Nombre ?? string.Empty).Trim();
                _dao.Actualizar(producto);
                TempData["Mensaje"] = "Producto actualizado correctamente";
                return RedirectToAction("Index");
            }
            catch (OperacionFallidaException ex) when (ex.Tipo == TipoFalla.CodigoDuplicado)
            {
                ModelState.AddModelError("Codigo", ex.Message);
                return View(producto);
            }
            catch (OperacionFallidaException ex) when (ex.Tipo == TipoFalla.ProductoInexistente)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, MensajesUx.Traducir(ex, "Actualizar producto"));
                return View(producto);
            }
        }

        // DELETE: POST /Producto/Delete/5 -> elimina el producto
        // Se usa POST (y no un simple link GET) para que no se borre por accidente
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                _dao.Eliminar(id);
                TempData["Mensaje"] = "Producto eliminado correctamente";
            }
            catch (Exception ex)
            {
                // Ej.: el producto ya tiene ventas registradas (clave foránea) o la BD no responde
                TempData["Error"] = MensajesUx.Traducir(ex, "Eliminar producto");
            }
            return RedirectToAction("Index");
        }
    }
}
