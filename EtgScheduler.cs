namespace etg
{
    
    // tu jest algorytm sortowania, jak na razie jest prosty, bo wybiera z najktrótszym czasem zakonczenia
    // ignoruje koszt, więc to będzie do poprawy(raczej nie) albo
    //  do wyrzucenia w ostateczniej wersji (pewniej)
    

    public class EtgScheduler : IScheduler
    {
        public string Name => "ETG Scheduling";

        public ScheduleResult Schedule(Graph graph)
        {
            var result = new ScheduleResult();
            var taskCount = graph.Tasks.Count;
            var procCount = graph.Procs.Count;

            // Zbuduj mapę poprzedników (graf przechowuje następniki)
            var taskIndex = new Dictionary<Task, int>();
            for (int i = 0; i < taskCount; i++)
            {
                taskIndex[graph.Tasks[i]] = i;
            }

            var predecessors = new List<List<int>>();
            for (int i = 0; i < taskCount; i++)
            {
                predecessors.Add([]);
            }
            for (int i = 0; i < taskCount; i++)
            {
                foreach (var (successor, _) in graph.Tasks[i].Successors)
                {
                    var si = taskIndex[successor];
                    predecessors[si].Add(i);
                }
            }

            // Sortowanie topologiczne (Kahn's algorithm)
            var order = TopologicalSort(taskCount, predecessors, graph);

            // . Zachłanne przypisanie
            var procFreeAt = new int[procCount]; // kiedy każdy procesor będzie wolny
            var taskEndTime = new int[taskCount]; // kiedy każde zadanie się kończy

            foreach (var ti in order)
            {
                var task = graph.Tasks[ti];

                // Najwcześniejszy start = max(koniec wszystkich poprzedników)
                int earliestStart = 0;
                foreach (var pred in predecessors[ti])
                {
                    earliestStart = Math.Max(earliestStart, taskEndTime[pred]);
                }

                List<int> candidateProcessors;

                switch (task.TaskType)
                {
                    case TaskType.DT:
                    case TaskType.CDT:
                        candidateProcessors = GetSpecializedProcessors(graph);
                        break;

                    case TaskType.UT:
                        candidateProcessors = GetGeneralProcessors(graph);
                        break;

                    case TaskType.GT:
                    case TaskType.CGT:
                    default:
                        candidateProcessors = Enumerable.Range(0, procCount).ToList();
                        break;
                }

                if (task.TaskType == TaskType.GT || task.TaskType == TaskType.DT || task.TaskType == TaskType.UT)
                {
                    int bestProc = -1;
                    int bestFinish = int.MaxValue;

                    foreach (var p in candidateProcessors)
                    {
                        int start = Math.Max(
                            earliestStart,
                            procFreeAt[p]);

                        int duration = graph.Times[ti][p];
                        int finish = start + duration;

                        if (finish < bestFinish)
                        {
                            bestFinish = finish;
                            bestProc = p;
                        }
                    }

                    if (bestProc == -1)
                    {
                        throw new InvalidOperationException($"Brak dostępnego procesora dla zadania {task.Name}");
                    }

                    int actualStart = Math.Max(earliestStart, procFreeAt[bestProc]);

                    int actualDuration = graph.Times[ti][bestProc];

                    int actualCost = graph.Costs[ti][bestProc];

                    procFreeAt[bestProc] = actualStart + actualDuration;

                    taskEndTime[ti] = actualStart + actualDuration;

                    result.ScheduledTasks.Add(
                        new ScheduledTask
                        {
                            Task = task,
                            TaskIndex = ti,

                            ProcIndices = [bestProc],

                            StartTime = actualStart,
                            EndTime = actualStart + actualDuration,
                            Duration = actualDuration,
                            Cost = actualCost
                        });
                }

                else if (task.TaskType == TaskType.CDT)
                {
                    if (candidateProcessors.Count == 0)
                    {
                        throw new InvalidOperationException(
                            $"Brak procesorów specjalizowanych dla {task.Name}");
                    }

                    int start = earliestStart;

                    foreach (var p in candidateProcessors)
                    {
                        start = Math.Max(
                            start,
                            procFreeAt[p]);
                    }

                    int duration =
                        candidateProcessors
                            .Select(p => graph.Times[ti][p])
                            .Max();

                    int cost =
                        candidateProcessors
                            .Sum(p => graph.Costs[ti][p]);

                    int finish = start + duration;

                    foreach (var p in candidateProcessors)
                    {
                        procFreeAt[p] = finish;
                    }

                    taskEndTime[ti] = finish;

                    result.ScheduledTasks.Add(
                        new ScheduledTask
                        {
                            Task = task,
                            TaskIndex = ti,

                            ProcIndices =
                                candidateProcessors,

                            StartTime = start,
                            EndTime = finish,
                            Duration = duration,
                            Cost = cost
                        });
                }

                else if (task.TaskType == TaskType.CGT)
                {
                    int start = earliestStart;

                    foreach (var p in candidateProcessors)
                    {
                        start = Math.Max(
                            start,
                            procFreeAt[p]);
                    }

                    int duration =
                        candidateProcessors
                            .Select(p => graph.Times[ti][p])
                            .Max();

                    int cost =
                        candidateProcessors
                            .Sum(p => graph.Costs[ti][p]);

                    int finish = start + duration;

                    foreach (var p in candidateProcessors)
                    {
                        procFreeAt[p] = finish;
                    }

                    taskEndTime[ti] = finish;

                    result.ScheduledTasks.Add(
                        new ScheduledTask
                        {
                            Task = task,
                            TaskIndex = ti,

                            ProcIndices =
                                candidateProcessors,

                            StartTime = start,
                            EndTime = finish,
                            Duration = duration,
                            Cost = cost
                        });
                }

            }

            return result;
        }

       
        private static List<int> TopologicalSort(int taskCount, List<List<int>> predecessors, Graph graph)
        {
            // Oblicz liczbę poprzedników 
            var inDegree = new int[taskCount];
            for (int i = 0; i < taskCount; i++)
            {
                inDegree[i] = predecessors[i].Count;
            }

            // Kolejka priorytetowa — zadania z zerowym in-degree
            // Priorytet: mniejszy indeks = wcześniej (stabilna kolejność)
            var ready = new SortedSet<int>();
            for (int i = 0; i < taskCount; i++)
            {
                if (inDegree[i] == 0)
                {
                    ready.Add(i);
                }
            }

            var order = new List<int>();

            while (ready.Count > 0)
            {
                var current = ready.Min;
                ready.Remove(current);
                order.Add(current);

                // Zmniejsz in-degree następników
                foreach (var (successor, _) in graph.Tasks[current].Successors)
                {
                    var si = graph.Tasks.IndexOf(successor);
                    inDegree[si]--;
                    if (inDegree[si] == 0)
                    {
                        ready.Add(si);
                    }
                }
            }

            if (order.Count != taskCount)
            {
                throw new InvalidOperationException("Graf zawiera cykl — sortowanie topologiczne niemożliwe.");
            }

            return order;
        }

        private static List<int> GetSpecializedProcessors(Graph graph)
        {
            return graph.Procs
                .Select((p, i) => new { p, i })
                .Where(x => x.p.Specialized == 1)
                .Select(x => x.i)
                .ToList();
        }

        private static List<int> GetGeneralProcessors(Graph graph)
        {
            return graph.Procs
                .Select((p, i) => new { p, i })
                .Where(x => x.p.Specialized == 0)
                .Select(x => x.i)
                .ToList();
        }


    }
}