using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using SimulacionDeRestauranta.Models;

namespace SimulacionDeRestauranta.Services
{
    public class RestauranteService
    {
        private static string[] menu = { "Yaroa", "Pizza", "Hamburguesa", "Tacos", "Burrito" };
        private static Random rnd = new Random();

        public ResultadoSimulacion EjecutarSimulacion(int cantidadPedidos)
        {
            var resultado = new ResultadoSimulacion();
            object lockObj = new object();
            var pedidosOriginales = GenerarPedidos(cantidadPedidos);
            resultado.TotalPedidos = cantidadPedidos;
            var estadisticasPlatillos = new Dictionary<string, int>();

            foreach (var pedido in pedidosOriginales)
            {
                if (estadisticasPlatillos.ContainsKey(pedido.Platillo))
                    estadisticasPlatillos[pedido.Platillo]++;
                else
                    estadisticasPlatillos[pedido.Platillo] = 1;
            }
            resultado.EstadisticasPlatillos = estadisticasPlatillos;

            var pedidosSecuencial = ClonarPedidos(pedidosOriginales);
            int entregasATiempoSec = 0, entregasGratisSec = 0;
            var swSecuencial = Stopwatch.StartNew();

            foreach (var pedido in pedidosSecuencial)
            {
                ProcesarPedido(pedido);
                SimularEntrega(pedido, ref entregasATiempoSec, ref entregasGratisSec, lockObj);
            }

            swSecuencial.Stop();
            resultado.TiempoSecuencial = swSecuencial.Elapsed.TotalSeconds;
            resultado.EntregasATiempoSecuencial = entregasATiempoSec;
            resultado.EntregasGratisSecuencial = entregasGratisSec;

            int[] nivelesParalelismo = { 2, 4, 6, 8 };

            foreach (int procesadores in nivelesParalelismo)
            {
                var pedidosParalelo = ClonarPedidos(pedidosOriginales);
                int entregasATiempo = 0, entregasGratis = 0;
                var swParalelo = Stopwatch.StartNew();
                var pedidosListos = new ConcurrentQueue<Pedido>();
                var opciones = new ParallelOptions { MaxDegreeOfParallelism = procesadores };
                object lockPreparados = new object();
                int pedidosPreparados = 0;

                Parallel.ForEach(pedidosParalelo, opciones, pedido =>
                {
                    ProcesarPedido(pedido);
                    pedidosListos.Enqueue(pedido);
                    lock (lockPreparados)
                    {
                        pedidosPreparados++;
                    }
                });

                Parallel.For(0, procesadores, opciones, i =>
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
                                if (pedidosPreparados == pedidosParalelo.Count && pedidosListos.IsEmpty)
                                    break;
                            }
                            Thread.Sleep(10);
                        }
                    }
                });

                swParalelo.Stop();
                double tiempoParalelo = swParalelo.Elapsed.TotalSeconds;
                double speedup = resultado.TiempoSecuencial / tiempoParalelo;
                double eficiencia = speedup / procesadores;

                resultado.ResultadosParalelos.Add(new ResultadoParalelo
                {
                    Procesadores = procesadores,
                    Tiempo = tiempoParalelo,
                    Speedup = speedup,
                    Eficiencia = eficiencia,
                    EntregasATiempo = entregasATiempo,
                    EntregasGratis = entregasGratis
                });
            }
            return resultado;
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

        private void SimularEntrega(Pedido pedido, ref int entregasATiempo, ref int entregasGratis, object lockObj)
        {
            int probabilidad = rnd.Next(1, 101);
            int time = 0;

            if (probabilidad <= 20)
                time = rnd.Next(5, 11);
            else if (probabilidad <= 60)
                time = rnd.Next(10, 16);
            else if (probabilidad <= 85)
                time = rnd.Next(15, 21);
            else if (probabilidad <= 95)
                time = rnd.Next(20, 26);
            else
                time = rnd.Next(31, 41);

            Console.WriteLine($"Pedido {pedido.Id}: Entregado en {time} minutos");
            pedido.TiempoEntrega = time;

            if (time > 30)
            {
                pedido.EsGratis = true;
                lock (lockObj)
                {
                    entregasGratis++;
                }
            }
            else
            {
                lock (lockObj)
                {
                    entregasATiempo++;
                }
            }
        }
    }
}
