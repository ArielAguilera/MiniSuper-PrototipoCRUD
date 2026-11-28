using System;
using System.Collections.Generic;
using Npgsql;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Data
{
    // DAO = Data Access Object. Aquí van TODAS las consultas SQL de productos.
    // El controlador nunca escribe SQL, solo llama a estos métodos.
    //
    // ACTIVIDAD 9: cada método (1) valida sus parámetros con cláusulas de guarda y
    // (2) ejecuta el SQL dentro de EjecutarConsulta(), que concentra el try-catch-finally
    // ordenado de lo más específico a lo más general.
    public class ProductoDAO
    {
        private readonly string cadenaConexion = Conexion.Cadena;

        // CREATE: inserta un producto nuevo
        public void Insertar(Producto p)
        {
            ValidarProducto(p);

            EjecutarAccion(OperacionDatos.RegistrarProducto, p.Codigo, conexion =>
            {
                string sql = "INSERT INTO productos (codigo, nombre, precio, stock) " +
                             "VALUES (@codigo, @nombre, @precio, @stock)";
                using var comando = new NpgsqlCommand(sql, conexion);
                // Usamos parámetros (@...) para evitar inyección SQL
                comando.Parameters.AddWithValue("codigo", p.Codigo);
                comando.Parameters.AddWithValue("nombre", p.Nombre);
                comando.Parameters.AddWithValue("precio", p.Precio);
                comando.Parameters.AddWithValue("stock", p.Stock);
                comando.ExecuteNonQuery();
            });
        }

        // READ: devuelve todos los productos ordenados por nombre
        public List<Producto> ObtenerTodos()
        {
            return EjecutarConsulta<List<Producto>>(OperacionDatos.ConsultarProductos, null, conexion =>
            {
                var lista = new List<Producto>();
                string sql = "SELECT id, codigo, nombre, precio, stock FROM productos ORDER BY nombre";
                using var comando = new NpgsqlCommand(sql, conexion);
                using var reader = comando.ExecuteReader();
                while (reader.Read())
                {
                    lista.Add(LeerProducto(reader));
                }
                return lista;
            });
        }

        // READ: busca un solo producto por su id (se usa para Editar)
        // Si no lo encuentra devuelve null
        public Producto? ObtenerPorId(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), id, "El identificador del producto no es válido.");
            }

            return EjecutarConsulta<Producto?>(OperacionDatos.ConsultarProductos, null, conexion =>
            {
                string sql = "SELECT id, codigo, nombre, precio, stock FROM productos WHERE id = @id";
                using var comando = new NpgsqlCommand(sql, conexion);
                comando.Parameters.AddWithValue("id", id);
                using var reader = comando.ExecuteReader();
                return reader.Read() ? LeerProducto(reader) : null;
            });
        }

        // UPDATE: actualiza los datos de un producto existente
        public void Actualizar(Producto p)
        {
            ValidarProducto(p);
            if (p.Id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(p.Id), p.Id, "El identificador del producto no es válido.");
            }

            EjecutarAccion(OperacionDatos.ActualizarProducto, p.Codigo, conexion =>
            {
                string sql = "UPDATE productos SET codigo = @codigo, nombre = @nombre, " +
                             "precio = @precio, stock = @stock WHERE id = @id";
                using var comando = new NpgsqlCommand(sql, conexion);
                comando.Parameters.AddWithValue("codigo", p.Codigo);
                comando.Parameters.AddWithValue("nombre", p.Nombre);
                comando.Parameters.AddWithValue("precio", p.Precio);
                comando.Parameters.AddWithValue("stock", p.Stock);
                comando.Parameters.AddWithValue("id", p.Id);

                if (comando.ExecuteNonQuery() == 0)
                {
                    throw new OperacionFallidaException(TipoFalla.ProductoInexistente,
                        "El producto que intentas modificar ya no está en el catálogo (quizá otra persona lo eliminó). " +
                        "Actualiza la lista para ver los cambios.");
                }
            });
        }

        // DELETE: elimina un producto por su id
        public void Eliminar(int id)
        {
            if (id <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(id), id, "El identificador del producto no es válido.");
            }

            EjecutarAccion(OperacionDatos.EliminarProducto, null, conexion =>
            {
                string sql = "DELETE FROM productos WHERE id = @id";
                using var comando = new NpgsqlCommand(sql, conexion);
                comando.Parameters.AddWithValue("id", id);

                if (comando.ExecuteNonQuery() == 0)
                {
                    throw new OperacionFallidaException(TipoFalla.ProductoInexistente,
                        "Ese producto ya no está en el catálogo (quizá otra persona lo eliminó). " +
                        "Actualiza la lista para ver los cambios.");
                }
            });
        }

        // Revisa si ya existe otro producto con el mismo código.
        // idActual sirve para que al EDITAR no se compare consigo mismo (en Crear se manda 0).
        public bool ExisteCodigo(string codigo, int idActual)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                throw new ArgumentException("El código del producto es obligatorio.");
            }

            return EjecutarConsulta<bool>(OperacionDatos.ConsultarProductos, null, conexion =>
            {
                string sql = "SELECT COUNT(*) FROM productos WHERE codigo = @codigo AND id <> @id";
                using var comando = new NpgsqlCommand(sql, conexion);
                comando.Parameters.AddWithValue("codigo", codigo);
                comando.Parameters.AddWithValue("id", idActual);
                return Convert.ToInt64(comando.ExecuteScalar()) > 0;
            });
        }

        // ---------------------------------------------------------------------
        // ACTIVIDAD 9 — Cláusulas de guarda: validan ANTES de abrir la conexión
        // ---------------------------------------------------------------------
        private static void ValidarProducto(Producto p)
        {
            if (p == null)
            {
                throw new ArgumentNullException(nameof(p), "No se recibieron los datos del producto.");
            }
            if (string.IsNullOrWhiteSpace(p.Codigo))
            {
                throw new ArgumentException("El código del producto es obligatorio.");
            }
            if (string.IsNullOrWhiteSpace(p.Nombre))
            {
                throw new ArgumentException("El nombre del producto es obligatorio.");
            }
            if (p.Precio <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(p.Precio), p.Precio, "El precio debe ser mayor a cero.");
            }
            if (p.Stock < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(p.Stock), p.Stock, "El stock no puede ser negativo.");
            }
        }

        // ---------------------------------------------------------------------
        // ACTIVIDAD 9 — try-catch-finally ordenado (de lo específico a lo general)
        // ---------------------------------------------------------------------
        private void EjecutarAccion(OperacionDatos operacion, string? dato, Action<NpgsqlConnection> accion)
        {
            EjecutarConsulta<object?>(operacion, dato, conexion =>
            {
                accion(conexion);
                return null;
            });
        }

        private T EjecutarConsulta<T>(OperacionDatos operacion, string? dato, Func<NpgsqlConnection, T> accion)
        {
            NpgsqlConnection? conexion = null;
            try
            {
                // Bloque TRY breve: abrir la conexión y ejecutar la acción pedida
                conexion = new NpgsqlConnection(cadenaConexion);
                conexion.Open();
                return accion(conexion);
            }
            catch (PostgresException exPg)              // 1) la más específica: la BD rechazó la orden (SqlState)
            {
                RegistroErrores.RegistrarLogTecnico(exPg, operacion.ToString());
                throw TraductorErrores.Traducir(exPg, operacion, dato);
            }
            catch (NpgsqlException exRed)               // 2) servidor apagado, red caída, timeout...
            {
                RegistroErrores.RegistrarLogTecnico(exRed, operacion.ToString());
                throw TraductorErrores.TraducirRed(exRed);
            }
            catch (OperacionFallidaException)           // 3) regla de negocio ya traducida (ej. producto inexistente)
            {
                throw;
            }
            catch (Exception ex)                        // 4) la más general, siempre al final
            {
                RegistroErrores.RegistrarLogTecnico(ex, operacion.ToString());
                throw;                                  // "throw;" (sin ex) conserva el Stack Trace original
            }
            finally
            {
                // Limpieza garantizada, ocurra o no un error
                conexion?.Dispose();
            }
        }

        // Método de apoyo: convierte la fila actual del reader en un objeto Producto
        private Producto LeerProducto(NpgsqlDataReader reader)
        {
            return new Producto
            {
                Id = reader.GetInt32(0),
                Codigo = reader.GetString(1),
                Nombre = reader.GetString(2),
                Precio = reader.GetDecimal(3),
                Stock = reader.GetInt32(4)
            };
        }
    }
}
