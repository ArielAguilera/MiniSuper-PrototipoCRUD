using System;
using System.Text.RegularExpressions;
using PrototipoCRUD.Data;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Controllers
{
    /// <summary>
    /// Traduce cualquier excepción a un mensaje UX para la pantalla:
    /// lenguaje de negocio, tono respetuoso (sin culpar) y orientado a la solución.
    /// Jamás devuelve SqlState, nombres de tablas ni StackTrace.
    /// </summary>
    public static class MensajesUx
    {
        public const string DatosNoRecibidos =
            "No recibimos los datos del formulario. Completa los campos e inténtalo de nuevo.";

        public const string Inesperado =
            "Ocurrió un inconveniente interno al procesar la solicitud. Tus datos no se modificaron; " +
            "inténtalo de nuevo en unos segundos o contacta a soporte.";

        // El orden importa: de la excepción más específica a la más general
        // (ArgumentNull y ArgumentOutOfRange son "hijas" de ArgumentException).
        public static string Traducir(Exception ex, string operacion)
        {
            switch (ex)
            {
                case ArgumentNullException:
                    return DatosNoRecibidos;

                case ArgumentOutOfRangeException fueraDeRango:
                    return DeFueraDeRango(fueraDeRango);

                case ArgumentException argumento:
                    return QuitarNombreDeParametro(argumento.Message);

                case OperacionFallidaException negocio:
                    // Su Message ya fue redactado para el usuario en la capa de datos
                    return negocio.Message;

                default:
                    // Red de seguridad final: se guarda el detalle técnico y el usuario ve un aviso amable
                    RegistroErrores.RegistrarLogTecnico(ex, operacion);
                    return Inesperado;
            }
        }

        private static string DeFueraDeRango(ArgumentOutOfRangeException ex)
        {
            switch (ex.ParamName)
            {
                case "Precio":
                    return "Ingresa un precio de venta mayor a Bs. 0.00 (ej. 7.50).";
                case "Stock":
                    return "El stock debe ser 0 o más unidades. Si todavía no llegó mercadería, escribe 0.";
                case "id":
                case "Id":
                    return "No encontramos ese producto. Vuelve a la lista y selecciónalo de nuevo.";
                case "Cantidad":
                    return "La cantidad de cada producto debe ser al menos 1.";
                case "PorcentajeDescuento":
                case "porcentaje":
                    return $"El descuento debe estar entre 0% y {Venta.DescuentoMaximo:0}%. Corrígelo y vuelve a confirmar.";
                default:
                    return "Alguno de los valores está fuera del rango permitido. Revísalos e inténtalo de nuevo.";
            }
        }

        // .NET agrega "(Parameter 'x')" al mensaje de algunas excepciones: es jerga técnica
        private static string QuitarNombreDeParametro(string mensaje)
        {
            return Regex.Replace(mensaje, @"\s*\((Parameter|Parámetro) '[^']*'\)", string.Empty).Trim();
        }
    }
}
