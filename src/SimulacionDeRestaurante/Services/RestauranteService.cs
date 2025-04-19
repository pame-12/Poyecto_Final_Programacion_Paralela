using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using SimulacionDeRestauranta.Models;

namespace SimulacionDeRestauranta.Services
{
    public class RestauranteService
    {
        //Lista de de los platillos disponibles
        private static string[] menu = { "Yaroa", "Pizza", "Hamburguesa", "Tacos", "Burrito" };
        //Variable para generar numero aleatorios
        private static Random rnd = new Random();
        public ResultadoSimulacion EjecutarSimulacion(int cantidadPedidos)
        {
            //Objto resultado donde se guardaran todas las metricas
            var resultado = new ResultadoSimulacion();
            //Variable para manejar datos compartidos
            object lockObj = new object();

            // Generar los pedidos iniciales
            var pedidosOriginales = GenerarPedidos(cantidadPedidos);


            // Guardar la cantidad total de pedidos
            resultado.TotalPedidos = cantidadPedidos;

            // Contar platos por pedidos
            //Se crea un diccionario
            var estadisticasPlatillos = new Dictionary<string, int>();
            //Recorre la lista de pedidos originales
            foreach (var pedido in pedidosOriginales)
            {
                //Verifica si el plato del pedido ya esta en el diccionario
                if (estadisticasPlatillos.ContainsKey(pedido.Platillo))
                    //Si existe incrementaa el contador
                    estadisticasPlatillos[pedido.Platillo]++;
                else
                    //Si no se agrega al diccionario con un volor de 1
                    estadisticasPlatillos[pedido.Platillo] = 1;
            }
            //Guarda los resultados del conteo de cada platillo
            resultado.EstadisticasPlatillos = estadisticasPlatillos;


            // --------- SIMULACION SECUENCIAL ---------
            //Clonar la lista original de pedidos
            var pedidosSecuencial = ClonarPedidos(pedidosOriginales);
            //Inicializar Variables en 0
            int entregasATiempoSec = 0, entregasGratisSec = 0;
            //Crea e inicializa el cronometro
            var swSecuencial = Stopwatch.StartNew();

            // Procesa y entrega los pedidos uno por uno
            foreach (var pedido in pedidosSecuencial)
            {
                //llama al metodo que simula la preparacion del pedido
                ProcesarPedido(pedido);
                //Llama al metodo que simula la entrega
                SimularEntrega(pedido, ref entregasATiempoSec, ref entregasGratisSec, lockObj);
            }

            //Detiene el cronometro
            swSecuencial.Stop();
            //Tiempo total en segundo
            resultado.TiempoSecuencial = swSecuencial.Elapsed.TotalSeconds;
            //Entregas a tiempo
            resultado.EntregasATiempoSecuencial = entregasATiempoSec;
            //Entregas gratis
            resultado.EntregasGratisSecuencial = entregasGratisSec;

            // --------- SIMULACION PARALELA ---------
            //Lista para simular distitos numeros de procesadores
            int[] nivelesParalelismo = { 2, 4, 6, 8 }; 

            foreach (int procesadores in nivelesParalelismo)
            {
                //Clonar pedidos
                var pedidosParalelo = ClonarPedidos(pedidosOriginales); 
                //Inicializar variables en 0
                int entregasATiempo = 0, entregasGratis = 0;
                //Crea e inicializa el cronometro
                var swParalelo = Stopwatch.StartNew(); 

                // Cola segura para compartir pedidos preparados entre hilos
                var pedidosListos = new ConcurrentQueue<Pedido>();
                //Define el maximo de hilos a usar
                var opciones = new ParallelOptions { MaxDegreeOfParallelism = procesadores };

                // Variable para manejar datos comppertidos
                object lockPreparados = new object();
                //Variable para contar cuantos pedidos fuero preparados
                int pedidosPreparados = 0;

                // --------- Preparacion en paralelo ---------
                Parallel.ForEach(pedidosParalelo, opciones, pedido =>
                {
                    //Llama al metodo que simula el procesamiento de pedido en paralelo
                    ProcesarPedido(pedido); 
                    //Se agregan los pedidos listos a la cola segura
                    pedidosListos.Enqueue(pedido); 

                    // se incrementan los pedidos preparado se usa lock para mas seguridad
                    lock (lockPreparados)
                    {
                        pedidosPreparados++;
                    }
                });

                // --------- Entrega en paralelo ---------
                Parallel.For(0, procesadores, opciones, i =>
                {
                    while (true)
                    {
                        // Se intenta sacar un pedido de la cola y lo guarda en pedido
                        if (pedidosListos.TryDequeue(out var pedido))
                        {
                            //Llama al metodo que simula la entrega paralela
                            SimularEntrega(pedido, ref entregasATiempo, ref entregasGratis, lockObj);
                        }
                        else
                        {
                            // Verifica si todos los pedidos han sido preparados y la cola esta vacia
                            lock (lockPreparados)
                            {
                                if (pedidosPreparados == pedidosParalelo.Count && pedidosListos.IsEmpty)
                                    // Terminar bucle si ya no hay mas pedidos que entregar
                                    break; 
                            }
                            //Espera un poco
                            Thread.Sleep(10); 
                        }
                    }
                });

                swParalelo.Stop();

                // Calculo de las metricas de rendimiento
                double tiempoParalelo = swParalelo.Elapsed.TotalSeconds;
                double speedup = resultado.TiempoSecuencial / tiempoParalelo; 
                double eficiencia = speedup / procesadores;

                // Se guardan los resultados de la simulacion
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
            //Devuelve todos los resultados
            return resultado; 
        }


        
        // Genera una lista de pedidos
        private List<Pedido> GenerarPedidos(int cantidad)
        {
            //Crea la lista de pedidos
            var pedidos = new List<Pedido>();

            for (int i = 1; i <= cantidad; i++)
            {
                //Se agregan pedidos a la lista
                pedidos.Add(new Pedido
                {
                    Id = i,
                    //Se selecciona un plato aleaatorio
                    Platillo = menu[rnd.Next(menu.Length)],
                    HoraPedido = DateTime.Now
                });
            }
            return pedidos;
        }      




        // Crea una copia de los pedidos 
        private List<Pedido> ClonarPedidos(List<Pedido> originales)
        {
            //Tranforma cada elemento en uno nuevo con los mismos valores
            return originales.Select(p => new Pedido
            {
                Id = p.Id,
                Platillo = p.Platillo,
                HoraPedido = p.HoraPedido
            //Convierte el resultado en una lista
            }).ToList();
        }





        //Metodo de procesamiento de los pedidos


        
        

        // Delivery -Raymond Moreno
        
        
        private void SimularEntrega(Pedido pedido, ref int entregasATiempo, ref int entregasGratis, object lockObj)
        {
            int probabilidad = rnd.Next(1, 101);  
            int time = 0;
        
            if (probabilidad <= 20)
            {
                // 20% 5 a 10 min
                time = rnd.Next(5, 11); 
                Console.WriteLine($"Pedido {pedido.Id}: Entregado en {time} minutos");
            }
            else if (probabilidad <= 60)
            {
                // 40% 10 a 15 min
                time = rnd.Next(10, 16); 
                Console.WriteLine($"Pedido {pedido.Id}: Entregado en {time} minutos");
            }
            else if (probabilidad <= 85)
            {
                // 25% 15 a 20 min
                time = rnd.Next(15, 21); 
                Console.WriteLine($"Pedido {pedido.Id}: Entregado en {time} minutos");
            }
            else if (probabilidad <= 95)
            {
                // 10% 20 a 25 min
                time = rnd.Next(20, 26);
                Console.WriteLine($"Pedido {pedido.Id}: Entregado en {time} minutos");
            }
            else
            {
                // 5% más de 30 min
                time = rnd.Next(31, 41); 
                Console.WriteLine($"Pedido {pedido.Id}: Supero los 30 minutos (Salio Gratis) ");
                
            }
        
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
        
