using Microsoft.AspNetCore.Mvc;
using SimulacionDeRestauranta.Models;
using SimulacionDeRestauranta.Services;
using SimulacionDeRestaurante.Models;
using System.Diagnostics;


namespace SimulacionDeRestaurante.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }
    private static string[] menu = { "Yaroa", "Pizza", "Hamburguesa", "Tacos", "Burrito" };
    public IActionResult Index()
    {
        ViewBag.Menu = menu;
        return View();
    }


    public IActionResult Privacy()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Resultado(SimulacionData data, List<string> menuSeleccionado)
    {
        if (ModelState.IsValid)
        {
            var cantidadPedidos = data.CantidadPeticiones;
            _logger.LogInformation($"Cantidad de pedidos: {cantidadPedidos}");
            _logger.LogInformation($"Menús seleccionados: {string.Join(", ", menuSeleccionado)}");

            var resultadoSimulacion = new RestauranteService().EjecutarSimulacion(cantidadPedidos, menuSeleccionado);
            return View("Resultado", resultadoSimulacion);
        }
        else
        {
            ModelState.AddModelError("", "Por favor, ingrese un número válido de pedidos.");
            ViewBag.Menu = new string[] { "Yaroa", "Pizza", "Hamburguesa", "Tacos", "Burrito" };
            return View("Index", data);
        }
    }


    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
