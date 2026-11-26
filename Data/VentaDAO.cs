using System;
using Npgsql;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Data
{
    public class VentaDAO
    {
        public int RegistrarVenta(Venta venta)
        {
            using var conn = new NpgsqlConnection(Conexion.Cadena);
            conn.Open();

            // Iniciamos la transacción atómica (Todo o Nada)
            using var transaccion = conn.BeginTransaction();

            try
            {
                // 1. Insertar la Cabecera (Maestro) y recuperar el ID generado con RETURNING id
                string sqlVenta = @"
                    INSERT INTO ventas (fecha, cliente, total)
                    VALUES (@fecha, @cliente, @total)
                    RETURNING id;";

                int idVentaGenerado;
                using (var cmdVenta = new NpgsqlCommand(sqlVenta, conn, transaccion))
                {
                    cmdVenta.Parameters.AddWithValue("@fecha", DateTime.Now);
                    cmdVenta.Parameters.AddWithValue("@cliente",
                        string.IsNullOrWhiteSpace(venta.Cliente) ? "Cliente General" : venta.Cliente.Trim());
                    cmdVenta.Parameters.AddWithValue("@total", venta.Total);

                    idVentaGenerado = Convert.ToInt32(cmdVenta.ExecuteScalar());
                }

                // 2. Recorrer la lista de productos (Detalle), insertarlos y descontar stock
                string sqlDetalle = @"
                    INSERT INTO detalle_ventas (id_venta, id_producto, cantidad, precio_unitario, subtotal)
                    VALUES (@idVenta, @idProducto, @cantidad, @precioUnitario, @subtotal);";

                string sqlStock = @"
                    UPDATE productos
                    SET stock = stock - @cantidad
                    WHERE id = @idProducto AND stock >= @cantidad;";

                foreach (var item in venta.Detalles)
                {
                    // Descontar stock verificando que alcance
                    using (var cmdStock = new NpgsqlCommand(sqlStock, conn, transaccion))
                    {
                        cmdStock.Parameters.AddWithValue("@cantidad", item.Cantidad);
                        cmdStock.Parameters.AddWithValue("@idProducto", item.IdProducto);

                        int filasAfectadas = cmdStock.ExecuteNonQuery();
                        if (filasAfectadas == 0)
                        {
                            throw new Exception($"Stock insuficiente para el producto '{item.NombreProducto}'.");
                        }
                    }

                    // Guardar línea de detalle
                    using (var cmdDet = new NpgsqlCommand(sqlDetalle, conn, transaccion))
                    {
                        cmdDet.Parameters.AddWithValue("@idVenta", idVentaGenerado);
                        cmdDet.Parameters.AddWithValue("@idProducto", item.IdProducto);
                        cmdDet.Parameters.AddWithValue("@cantidad", item.Cantidad);
                        cmdDet.Parameters.AddWithValue("@precioUnitario", item.PrecioUnitario);
                        cmdDet.Parameters.AddWithValue("@subtotal", item.Subtotal);

                        cmdDet.ExecuteNonQuery();
                    }
                }

                // 3. Si todo salió bien, confirmamos permanentemente en PostgreSQL
                transaccion.Commit();
                return idVentaGenerado;
            }
            catch
            {
                // Si algo falló, revertimos cualquier cambio parcial en la base de datos
                transaccion.Rollback();
                throw;
            }
        }
    }
}
