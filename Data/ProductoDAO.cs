using System.Collections.Generic;
using Npgsql;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Data
{
    // DAO = Data Access Object. Aquí van TODAS las consultas SQL de productos.
    // El controlador nunca escribe SQL, solo llama a estos métodos.
    public class ProductoDAO
    {
        private readonly string cadenaConexion = Conexion.Cadena;

        // CREATE: inserta un producto nuevo
        public void Insertar(Producto p)
        {
            using (var conexion = new NpgsqlConnection(cadenaConexion))
            {
                conexion.Open();
                string sql = "INSERT INTO productos (codigo, nombre, precio, stock) " +
                             "VALUES (@codigo, @nombre, @precio, @stock)";
                using (var comando = new NpgsqlCommand(sql, conexion))
                {
                    // Usamos parámetros (@...) para evitar inyección SQL
                    comando.Parameters.AddWithValue("codigo", p.Codigo);
                    comando.Parameters.AddWithValue("nombre", p.Nombre);
                    comando.Parameters.AddWithValue("precio", p.Precio);
                    comando.Parameters.AddWithValue("stock", p.Stock);
                    comando.ExecuteNonQuery();
                }
            }
        }

        // READ: devuelve todos los productos ordenados por nombre
        public List<Producto> ObtenerTodos()
        {
            var lista = new List<Producto>();
            using (var conexion = new NpgsqlConnection(cadenaConexion))
            {
                conexion.Open();
                string sql = "SELECT id, codigo, nombre, precio, stock FROM productos ORDER BY nombre";
                using (var comando = new NpgsqlCommand(sql, conexion))
                using (var reader = comando.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        lista.Add(LeerProducto(reader));
                    }
                }
            }
            return lista;
        }

        // READ: busca un solo producto por su id (se usa para Editar)
        // Si no lo encuentra devuelve null
        public Producto? ObtenerPorId(int id)
        {
            Producto? producto = null;
            using (var conexion = new NpgsqlConnection(cadenaConexion))
            {
                conexion.Open();
                string sql = "SELECT id, codigo, nombre, precio, stock FROM productos WHERE id = @id";
                using (var comando = new NpgsqlCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue("id", id);
                    using (var reader = comando.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            producto = LeerProducto(reader);
                        }
                    }
                }
            }
            return producto;
        }

        // UPDATE: actualiza los datos de un producto existente
        public void Actualizar(Producto p)
        {
            using (var conexion = new NpgsqlConnection(cadenaConexion))
            {
                conexion.Open();
                string sql = "UPDATE productos SET codigo = @codigo, nombre = @nombre, " +
                             "precio = @precio, stock = @stock WHERE id = @id";
                using (var comando = new NpgsqlCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue("codigo", p.Codigo);
                    comando.Parameters.AddWithValue("nombre", p.Nombre);
                    comando.Parameters.AddWithValue("precio", p.Precio);
                    comando.Parameters.AddWithValue("stock", p.Stock);
                    comando.Parameters.AddWithValue("id", p.Id);
                    comando.ExecuteNonQuery();
                }
            }
        }

        // DELETE: elimina un producto por su id
        public void Eliminar(int id)
        {
            using (var conexion = new NpgsqlConnection(cadenaConexion))
            {
                conexion.Open();
                string sql = "DELETE FROM productos WHERE id = @id";
                using (var comando = new NpgsqlCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue("id", id);
                    comando.ExecuteNonQuery();
                }
            }
        }

        // Revisa si ya existe otro producto con el mismo código.
        // idActual sirve para que al EDITAR no se compare consigo mismo (en Crear se manda 0).
        public bool ExisteCodigo(string codigo, int idActual)
        {
            using (var conexion = new NpgsqlConnection(cadenaConexion))
            {
                conexion.Open();
                string sql = "SELECT COUNT(*) FROM productos WHERE codigo = @codigo AND id <> @id";
                using (var comando = new NpgsqlCommand(sql, conexion))
                {
                    comando.Parameters.AddWithValue("codigo", codigo);
                    comando.Parameters.AddWithValue("id", idActual);
                    long cantidad = (long)comando.ExecuteScalar()!;
                    return cantidad > 0;
                }
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
