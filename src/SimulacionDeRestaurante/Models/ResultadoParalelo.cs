namespace SimulacionDeRestauranta.Models
{
    public class ResultadoParalelo
    {
        public int Procesadores { get; set; }
        public double Tiempo { get; set; }
        public double Speedup { get; set; }
        public double Eficiencia { get; set; }
        public int EntregasATiempo { get; set; }
        public int EntregasGratis { get; set; }
    }
}
