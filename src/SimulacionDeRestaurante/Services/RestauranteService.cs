using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Concurrent;

using SimulacionDeRestaurante.Models;
public class ProcesadorPedidos
{
    private readonly Random rnd = new Random();
    private readonly string[] menu = { "Yaroa", "Pizza", "Hamburguesa", "Tacos", "Burrito" };

    public async IAsyncEnumerable<Resultado> Procesamiento(int numeroPedidos)
    {
        var pedidosOriginales = GenerarPedidos(numeroPedidos);
        List<int> chefs = new() { 2, 4, 6, 8 };
        Stopwatch sw = new Stopwatch();
        double tiempoSecuencial;

        // Secuencial
        sw.Start();
        var resultadoSecuencial = await Task.Run(() => Secuencial(ClonarPedidos(pedidosOriginales)));
        sw.Stop();
        tiempoSecuencial = sw.ElapsedMilliseconds;

        foreach (var numChef in chefs)
        {
            sw.Restart();
            var resultadoParalelo = await Task.Run(() => Paralelo(ClonarPedidos(pedidosOriginales), numChef));
            sw.Stop();

            double tiempoParalelo = sw.ElapsedMilliseconds;
            double speedup = tiempoSecuencial / tiempoParalelo;
            double eficiencia = speedup / numChef;
            string recomendacion = "";

            if (eficiencia > 0.7)
            {
                recomendacion = $"Se recomienda utilizar {numChef} chefs.";
            }
            else if (eficiencia > 0.5 && eficiencia <= 0.7)
            {
                recomendacion = $"Se recomienda utilizar {numChef} chefs, pero se puede mejorar.";
            }
            else
            {
                recomendacion = $"No se recomienda utilizar {numChef} chefs, ya que la eficiencia es baja.";
            }

            yield return new Resultado
            {
                NumeroChefs = numChef,
                TiempoSecuencial = tiempoSecuencial,
                TiempoParalelo = tiempoParalelo,
                Speedup = speedup,
                Eficiencia = eficiencia,
                Recomendacion = recomendacion,
                EntregasATiempo = resultadoParalelo.EntregasATiempo,
                EntregasGratis = resultadoParalelo.EntregasGratis
            };
        }
    }

    private List<Pedido> GenerarPedidos(int cantidad)
    {
        var pedidos = new List<Pedido>();
        for (int i = 1; i <= cantidad; i++)
        {
            pedidos.Add(new Pedido
            {
                Id = i,
                Platillo = menu[rnd.Next(menu.Length)],
                HoraPedido = DateTime.Now
            });
        }
        return pedidos;
    }

    private List<Pedido> ClonarPedidos(List<Pedido> originales)
    {
        return originales.Select(p => new Pedido
        {
            Id = p.Id,
            Platillo = p.Platillo,
            HoraPedido = p.HoraPedido
        }).ToList();
    }

    private Resultado Secuencial(List<Pedido> pedidos)
    {
        int entregasATiempo = 0;
        int entregasGratis = 0;
        object lockObj = new();

        foreach (var pedido in pedidos)
        {
            SimularEntrega(pedido, ref entregasATiempo, ref entregasGratis, lockObj);
        }

        return new Resultado
        {
            EntregasATiempo = entregasATiempo,
            EntregasGratis = entregasGratis
        };
    }

    private Resultado Paralelo(List<Pedido> pedidos, int numChefs)
    {
        int entregasATiempo = 0;
        int entregasGratis = 0;
        object lockObj = new();
        var pedidosListos = new ConcurrentQueue<Pedido>();
        int pedidosPreparados = 0;
        object lockPreparados = new object();
        var opciones = new ParallelOptions { MaxDegreeOfParallelism = numChefs };

        // Preparación en paralelo
        Parallel.ForEach(pedidos, opciones, pedido =>
        {
            Thread.Sleep(rnd.Next(50, 201)); 
            pedidosListos.Enqueue(pedido);
            lock (lockPreparados)
            {
                pedidosPreparados++;
            }
        });

        Parallel.For(0, numChefs, opciones, _ =>
        {
            while (true)
            {
                if (pedidosListos.TryDequeue(out var pedido))
                {
                    SimularEntrega(pedido, ref entregasATiempo, ref entregasGratis, lockObj);
                }
                else
                {
                    lock (lockPreparados)
                    {
                        if (pedidosPreparados == pedidos.Count && pedidosListos.IsEmpty)
                            break;
                    }
                    Thread.Sleep(10);
                }
            }
        });

        return new Resultado
        {
            EntregasATiempo = entregasATiempo,
            EntregasGratis = entregasGratis
        };
    }

    private void SimularEntrega(Pedido pedido, ref int entregasATiempo, ref int entregasGratis, object lockObj)
    {
        int probabilidad = rnd.Next(1, 101);
        int time;

        if (probabilidad <= 20)
        {
            time = rnd.Next(5, 11);
            Console.WriteLine($"Pedido {pedido.Id}: Entregado en {time} minutos");
        }
        else if (probabilidad <= 60)
        {
            time = rnd.Next(10, 16);
            Console.WriteLine($"Pedido {pedido.Id}: Entregado en {time} minutos");
        }
        else if (probabilidad <= 85)
        {
            time = rnd.Next(15, 21);
            Console.WriteLine($"Pedido {pedido.Id}: Entregado en {time} minutos");
        }
        else if (probabilidad <= 95)
        {
            time = rnd.Next(20, 26);
            Console.WriteLine($"Pedido {pedido.Id}: Entregado en {time} minutos");
        }
        else
        {
            time = rnd.Next(31, 41);
            Console.WriteLine($"Pedido {pedido.Id}: Superó los 30 minutos (Salió gratis)");
        }

        pedido.TiempoEntrega = time;
        pedido.EsGratis = time > 30;

        lock (lockObj)
        {
            if (pedido.EsGratis)
                entregasGratis++;
            else
                entregasATiempo++;
        }
    }
}
