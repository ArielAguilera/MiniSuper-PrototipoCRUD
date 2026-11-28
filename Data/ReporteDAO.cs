using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Data
{
    public class ReporteDAO
    {
        public List<GrupoReporteVentas> ObtenerReporteVentas(DateTime inicio, DateTime fin, string granularidad)
        {
            if (fin.Date < inicio.Date)
                throw new ArgumentException("La fecha final no puede ser anterior a la fecha inicial. Ajusta el rango e inténtalo de nuevo.");

            var filas = new List<FilaReporteVenta>();
            NpgsqlConnection? conexion = null;

            try
            {
                conexion = new NpgsqlConnection(Conexion.Cadena);
                conexion.Open();

                bool esMensual = string.Equals(granularidad, "Mensual", StringComparison.OrdinalIgnoreCase);

                string sql = esMensual
                    ? @"SELECT TO_CHAR(v.fecha, 'YYYY-MM') AS periodo_grupo,
                               TO_CHAR(v.fecha::date, 'YYYY-MM-DD') AS fecha_dia,
                               COUNT(v.id) AS cantidad_ventas,
                               COALESCE(SUM(v.subtotal_bruto), 0) AS subtotal_bruto,
                               COALESCE(SUM(v.monto_descuento), 0) AS monto_descuento,
                               COALESCE(SUM(v.total_final), 0) AS total_final
                          FROM ventas v
                         WHERE v.fecha::date BETWEEN @inicio AND @fin
                         GROUP BY TO_CHAR(v.fecha, 'YYYY-MM'), v.fecha::date
                         ORDER BY periodo_grupo ASC, v.fecha::date ASC;"
                    : @"SELECT TO_CHAR(v.fecha, 'YYYY-MM-DD') AS periodo_grupo,
                               v.id AS nro_venta,
                               TO_CHAR(v.fecha, 'HH24:MI') AS hora,
                               COALESCE(NULLIF(v.cliente, ''), 'Cliente General') AS cliente,
                               COALESCE(NULLIF(v.vendedor, ''), 'Sin registrar') AS vendedor,
                               COALESCE(v.subtotal_bruto, v.total_final) AS subtotal_bruto,
                               COALESCE(v.monto_descuento, 0) AS monto_descuento,
                               v.total_final
                          FROM ventas v
                         WHERE v.fecha::date BETWEEN @inicio AND @fin
                         ORDER BY v.fecha ASC;";

                using var cmd = new NpgsqlCommand(sql, conexion);
                cmd.Parameters.AddWithValue("inicio", inicio.Date);
                cmd.Parameters.AddWithValue("fin", fin.Date);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (esMensual)
                    {
                        filas.Add(new FilaReporteVenta
                        {
                            PeriodoGrupo = reader.GetString(0),
                            FechaDia = reader.GetString(1),
                            CantidadVentas = Convert.ToInt32(reader.GetInt64(2)),
                            SubtotalBruto = reader.GetDecimal(3),
                            MontoDescuento = reader.GetDecimal(4),
                            TotalFinal = reader.GetDecimal(5)
                        });
                    }
                    else
                    {
                        filas.Add(new FilaReporteVenta
                        {
                            PeriodoGrupo = reader.GetString(0),
                            NroVenta = reader.GetInt32(1),
                            Hora = reader.GetString(2),
                            Cliente = reader.GetString(3),
                            Vendedor = reader.GetString(4),
                            SubtotalBruto = reader.GetDecimal(5),
                            MontoDescuento = reader.GetDecimal(6),
                            TotalFinal = reader.GetDecimal(7),
                            CantidadVentas = 1
                        });
                    }
                }
            }
            catch (PostgresException exPg)
            {
                RegistroErrores.RegistrarLogTecnico(exPg, "ObtenerReporteVentas");
                throw new InvalidOperationException("No se pudo generar el reporte por un problema al consultar las ventas en la base de datos.", exPg);
            }
            catch (NpgsqlException exRed)
            {
                RegistroErrores.RegistrarLogTecnico(exRed, "ObtenerReporteVentas");
                throw TraductorErrores.TraducirRed(exRed);
            }
            catch (Exception ex)
            {
                RegistroErrores.RegistrarLogTecnico(ex, "ObtenerReporteVentas");
                throw;
            }
            finally
            {
                conexion?.Dispose();
            }

            return filas
                .GroupBy(f => f.PeriodoGrupo)
                .Select(g => new GrupoReporteVentas
                {
                    Periodo = g.Key,
                    Filas = g.ToList()
                })
                .ToList();
        }
    }
}
