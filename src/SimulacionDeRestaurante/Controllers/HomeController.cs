using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SimulacionDeRestauranta.Models;
using SimulacionDeRestauranta.Services;
using SimulacionDeRestaurante.Models;

namespace SimulacionDeRestaurante.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {

        return View();
    }


    public IActionResult Privacy()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Resultado(SimulacionData data)
    {

        if (ModelState.IsValid)
        {
            var cantidadPedidos = data.CantidadPeticiones;
            _logger.LogInformation($"Cantidad de pedidos: {cantidadPedidos}");
            var resultadoSimulacion = new RestauranteService().EjecutarSimulacion(cantidadPedidos);
            return View("Resultado", resultadoSimulacion);
        }
        else
        {
            // Manejar el caso en que el modelo no es válido
            // Puedes redirigir a una vista de error o mostrar un mensaje de error
            ModelState.AddModelError("", "Por favor, ingrese un número válido de pedidos.");
            return View("Index", data);
        }

    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}