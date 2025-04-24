namespace SimulacionDeRestauranta.Models
{
    public class ResultadoSimulacion
    {
        public double TiempoSecuencial { get; set; }
        public int EntregasATiempoSecuencial { get; set; }
        public int EntregasGratisSecuencial { get; set; }
        public List<ResultadoParalelo> ResultadosParalelos { get; set; } = new List<ResultadoParalelo>();
        public int TotalPedidos { get; set; }
        public Dictionary<string, int> EstadisticasPlatillos { get; set; } = new Dictionary<string, int>();
    }
}
