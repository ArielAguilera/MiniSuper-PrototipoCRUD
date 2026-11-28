using System;
using Microsoft.AspNetCore.Mvc;
using PrototipoCRUD.Data;
using PrototipoCRUD.Models;

namespace PrototipoCRUD.Controllers
{
    public class ReporteController : Controller
    {
        private readonly ReporteDAO _dao = new ReporteDAO();

        [HttpGet]
        public IActionResult Index()
        {
            var modelo = new ReporteVentasFiltroViewModel
            {
                FechaInicio = DateTime.Today,
                FechaFin = DateTime.Today,
                Granularidad = "Diario"
            };
            return EjecutarConsulta(modelo);
        }

        [HttpPost]
        public IActionResult Index(ReporteVentasFiltroViewModel modelo)
        {
            return EjecutarConsulta(modelo);
        }

        private IActionResult EjecutarConsulta(ReporteVentasFiltroViewModel modelo)
        {
            try
            {
                modelo.Grupos = _dao.ObtenerReporteVentas(modelo.FechaInicio, modelo.FechaFin, modelo.Granularidad);
                modelo.ConsultaEjecutada = true;

                if (modelo.Grupos.Count == 0)
                {
                    modelo.MensajeAviso = $"No existen ventas registradas entre el {modelo.FechaInicio:dd/MM/yyyy} y el {modelo.FechaFin:dd/MM/yyyy}. Intenta ampliar el periodo de búsqueda.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = MensajesUx.Traducir(ex, "Generar reporte de ventas");
            }

            return View(modelo);
        }
    }
}
