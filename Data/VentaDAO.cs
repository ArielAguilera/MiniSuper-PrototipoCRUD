using System;
using Npgsql;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Data
{
    public class VentaDAO
    {
        public int RegistrarVenta(Venta venta)
        {
            // ------------------------------------------------------------------
            // ACTIVIDAD 9 — Cláusulas de guarda (antes de abrir la conexión)
            // ------------------------------------------------------------------
            if (venta == null)
            {
                throw new ArgumentNullException(nameof(venta), "No se recibieron los datos de la venta.");
            }
            if (venta.Detalles == null || venta.Detalles.Count == 0)
            {
                throw new ArgumentException("La venta debe incluir al menos un producto.");
            }
            if (venta.PorcentajeDescuento < Venta.DescuentoMinimo || venta.PorcentajeDescuento > Venta.DescuentoMaximo)
            {
                throw new ArgumentOutOfRangeException(nameof(venta.PorcentajeDescuento), venta.PorcentajeDescuento,
                    "El descuento permitido debe estar entre 0% y un máximo de 20%.");
            }
            foreach (var linea in venta.Detalles)
            {
                if (linea == null)
                {
                    throw new ArgumentException("La venta contiene una línea de producto vacía.");
                }
                if (linea.IdProducto <= 0)
                {
                    throw new ArgumentException("Una de las líneas de la venta no tiene un producto seleccionado.");
                }
                if (linea.Cantidad <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(linea.Cantidad), linea.Cantidad,
                        "La cantidad de cada producto debe ser al menos 1.");
                }
                if (linea.PrecioUnitario < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(linea.PrecioUnitario), linea.PrecioUnitario,
                        "El precio unitario no puede ser negativo.");
                }
            }

            NpgsqlConnection? conn = null;
            NpgsqlTransaction? transaccion = null;

            try
            {
                conn = new NpgsqlConnection(Conexion.Cadena);
                conn.Open();

                // Iniciamos la transacción atómica (Todo o Nada)
                transaccion = conn.BeginTransaction();

                // 1. Insertar la Cabecera (Maestro) y recuperar el ID generado con RETURNING id
                //    CR-01: ahora también guarda el vendedor y los campos del descuento.
                //    La columna "total" se mantiene (= total final) para no romper lo anterior.
                string sqlVenta = @"
                    INSERT INTO ventas
                        (fecha, cliente, total,
                         vendedor, subtotal_bruto, porcentaje_descuento, monto_descuento, total_final)
                    VALUES
                        (@fecha, @cliente, @total,
                         @vendedor, @subtotalBruto, @porcentajeDescuento, @montoDescuento, @totalFinal)
                    RETURNING id;";

                int idVentaGenerado;
                using (var cmdVenta = new NpgsqlCommand(sqlVenta, conn, transaccion))
                {
                    cmdVenta.Parameters.AddWithValue("@fecha", DateTime.Now);
                    cmdVenta.Parameters.AddWithValue("@cliente",
                        string.IsNullOrWhiteSpace(venta.Cliente) ? "Cliente General" : venta.Cliente.Trim());
                    cmdVenta.Parameters.AddWithValue("@total", venta.TotalFinal);

                    // Si por alguna razón llega vacío (código antiguo), se conserva la fila con un valor explícito
                    cmdVenta.Parameters.AddWithValue("@vendedor",
                        string.IsNullOrWhiteSpace(venta.Vendedor) ? "Sin registrar" : venta.Vendedor.Trim());
                    cmdVenta.Parameters.AddWithValue("@subtotalBruto", venta.SubtotalBruto);
                    cmdVenta.Parameters.AddWithValue("@porcentajeDescuento", venta.PorcentajeDescuento);
                    cmdVenta.Parameters.AddWithValue("@montoDescuento", venta.MontoDescuento);
                    cmdVenta.Parameters.AddWithValue("@totalFinal", venta.TotalFinal);

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
                            string nombre = string.IsNullOrWhiteSpace(item.NombreProducto)
                                ? "uno de los productos"
                                : $"«{item.NombreProducto}»";
                            throw new OperacionFallidaException(TipoFalla.StockInsuficiente,
                                $"No hay stock suficiente de {nombre} para completar esta venta. " +
                                "Reduce la cantidad o actualiza la página para ver el inventario disponible; no se registró nada.");
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
            catch (PostgresException exPg)                  // 1) la BD rechazó la orden (CHECK 23514, FK 23503, ...)
            {
                RevertirSinOcultarErrores(transaccion);
                RegistroErrores.RegistrarLogTecnico(exPg, "Registrar venta");
                throw TraductorErrores.Traducir(exPg, OperacionDatos.RegistrarVenta);
            }
            catch (NpgsqlException exRed)                   // 2) PostgreSQL apagado o red caída
            {
                RevertirSinOcultarErrores(transaccion);
                RegistroErrores.RegistrarLogTecnico(exRed, "Registrar venta");
                throw TraductorErrores.TraducirRed(exRed);
            }
            catch (OperacionFallidaException)               // 3) regla de negocio (ej. stock insuficiente)
            {
                RevertirSinOcultarErrores(transaccion);
                throw;
            }
            catch (Exception ex)                            // 4) cualquier otra: se registra y se re-lanza con throw;
            {
                RevertirSinOcultarErrores(transaccion);
                RegistroErrores.RegistrarLogTecnico(ex, "Registrar venta");
                throw;
            }
            finally
            {
                // Limpieza garantizada de transacción y conexión
                transaccion?.Dispose();
                conn?.Dispose();
            }
        }

        // Si algo falló, revertimos cualquier cambio parcial. Si el propio rollback falla
        // (por ejemplo, la conexión ya se cayó) lo registramos y seguimos con el error original.
        private static void RevertirSinOcultarErrores(NpgsqlTransaction? transaccion)
        {
            if (transaccion == null)
            {
                return;
            }

            try
            {
                transaccion.Rollback();
            }
            catch (Exception exRollback)
            {
                RegistroErrores.RegistrarLogTecnico(exRollback, "Rollback de la venta");
            }
        }
    }
}
