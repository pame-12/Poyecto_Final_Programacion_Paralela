
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

   
}
