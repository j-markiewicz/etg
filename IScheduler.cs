namespace etg
{

    /// Interfejs schedulera, mozna zmienić implementację na inny algorytm.
    /// gdybyś chcieli miec wybór ze względu na czas albo coś innego, albo kilka algorytmów sortowania
    /// i użytkownik będzie mógł sobie wybierac
   
    
    // idk,ale chyba najwazniejsza rzecz, myśle że musimy to ustalić w piątek, jak będzie to sortować 
    
    public class ScheduleResult
    {
        public List<ScheduledTask> ScheduledTasks { get; set; } = [];
        public int Makespan => ScheduledTasks.Count > 0 ? ScheduledTasks.Max(t => t.EndTime) : 0;
        public int TotalCost => ScheduledTasks.Sum(t => t.Cost);
    }

    public class ScheduledTask
    {
        public Task Task { get; set; } = null!;
            
        public int TaskIndex { get; set; }

        public List<int> ProcIndices { get; set; } = [];

        public int StartTime { get; set; }
        public int EndTime { get; set; }
        public int Duration { get; set; }

        public int Cost { get; set; }
    }


    /// Interfejs schedulera — podmień implementację na inny algorytm.

    public interface IScheduler
    {
        string Name { get; }
        ScheduleResult Schedule(Graph graph);
    }
}
