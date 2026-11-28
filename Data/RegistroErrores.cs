using System;
using System.IO;
using System.Text;
using Npgsql;

namespace PrototipoCRUD.Data
{
    /// <summary>
    /// Log técnico interno (para el desarrollador). El usuario NUNCA ve este contenido.
    /// Archivo: errores_sistema.log (en la carpeta del proyecto; Program.cs fija la ruta).
    /// </summary>
    public static class RegistroErrores
    {
        private static readonly object _bloqueo = new object();
        private const string MarcaRegistrado = "LogTecnicoRegistrado";

        public static string RutaLog { get; set; } =
            Path.Combine(Directory.GetCurrentDirectory(), "errores_sistema.log");

        /// <summary>
        /// Registra fecha, operación, tipo de excepción, mensaje y StackTrace.
        /// Nunca lanza excepciones: si el archivo no se puede escribir, el detalle va a la consola.
        /// Si la misma excepción ya se registró (porque se re-lanzó con throw;) no se duplica.
        /// </summary>
        public static void RegistrarLogTecnico(Exception ex, string operacion = "No especificada")
        {
            if (ex == null || ex.Data.Contains(MarcaRegistrado))
            {
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Operación: {operacion}");
            sb.AppendLine($"Tipo: {ex.GetType().FullName}");

            if (ex is PostgresException pg)
            {
                sb.AppendLine($"SqlState: {pg.SqlState} | Tabla: {pg.TableName} | Restricción: {pg.ConstraintName}");
            }

            sb.AppendLine($"Mensaje: {ex.Message}");
            sb.AppendLine($"StackTrace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                sb.AppendLine($"Excepción interna: {ex.InnerException.GetType().FullName}: {ex.InnerException.Message}");
            }

            sb.AppendLine("---");

            try
            {
                lock (_bloqueo)
                {
                    File.AppendAllText(RutaLog, sb.ToString(), Encoding.UTF8);
                }
                ex.Data[MarcaRegistrado] = true;
            }
            catch (Exception exLog) when (exLog is IOException || exLog is UnauthorizedAccessException)
            {
                // El log falló: no ocultamos el problema, queda visible en la consola del desarrollador
                Console.Error.WriteLine($"[LOG] No se pudo escribir en '{RutaLog}': {exLog.Message}");
                Console.Error.WriteLine(sb.ToString());
            }
        }
    }
}
