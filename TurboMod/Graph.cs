namespace TommoJProductions.TurboMod
{
    public class Graph
    {
        // Written, 09.10.2022

        GraphData[] data;

        public Graph(GraphData[] graphData)
        {
            data = graphData;
        }

        public float cal(float current)
        {
            for (int i = 0; i < data.Length; i++)
            {
                if (current < data[i].to)
                {
                    if (i > 0)
                        return data[i].graphCal(current, data[i - 1].end, data[i - 1].to);
                    else
                        return data[i].graphCal(current, 0, 0);
                }
            }
            return data[data.Length - 1].graphCal(current, data[data.Length - 2].end, data[data.Length - 2].to);
        }
    }
}
