using System;
using System.Collections.Generic;
using System.Linq;

namespace PrototipoCRUD.Models
{
    public class ReporteVentasFiltroViewModel
    {
        public DateTime FechaInicio { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        public DateTime FechaFin { get; set; } = DateTime.Today;
        public string Granularidad { get; set; } = "Diario"; // "Diario" o "Mensual"
        public string UsuarioGenerador { get; set; } = "Administrador / Cajero";

        public List<GrupoReporteVentas> Grupos { get; set; } = new();
        public bool ConsultaEjecutada { get; set; }
        public string? MensajeAviso { get; set; }

        public int TotalOperacionesGeneral => Grupos.Sum(g => g.CantidadOperaciones);
        public decimal TotalDescuentosGeneral => Grupos.Sum(g => g.TotalDescuentos);
        public decimal GranTotalCobrado => Grupos.Sum(g => g.TotalCobrado);
    }

    public class GrupoReporteVentas
    {
        public string Periodo { get; set; } = string.Empty; // YYYY-MM-DD o YYYY-MM
        public List<FilaReporteVenta> Filas { get; set; } = new();

        public int CantidadOperaciones => Filas.Sum(f => f.CantidadVentas);
        public decimal TotalSubtotalBruto => Filas.Sum(f => f.SubtotalBruto);
        public decimal TotalDescuentos => Filas.Sum(f => f.MontoDescuento);
        public decimal TotalCobrado => Filas.Sum(f => f.TotalFinal);
    }

    public class FilaReporteVenta
    {
        public string PeriodoGrupo { get; set; } = string.Empty;
        public int NroVenta { get; set; }          // En modo Diario
        public string Hora { get; set; } = "";     // En modo Diario
        public string FechaDia { get; set; } = ""; // En modo Mensual
        public string Cliente { get; set; } = "Cliente General";
        public string Vendedor { get; set; } = "Sin registrar";
        public int CantidadVentas { get; set; } = 1;
        public decimal SubtotalBruto { get; set; }
        public decimal MontoDescuento { get; set; }
        public decimal TotalFinal { get; set; }
    }
}
