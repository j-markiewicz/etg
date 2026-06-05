using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

namespace etg
{
    public class HeftScheduler : IScheduler
    {
        public string Name => "HEFT Scheduling";
        private const double TimeWeight = 0.8;
        private const double CostWeight = 0.2;

        public ScheduleResult Schedule(Graph graph)
        {
            var missingSpecializedTasks = new List<string>();
            var missingGeneralTasks = new List<string>();
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

            var ranks = ComputeUpwardRanks(graph);

            var order =
                Enumerable.Range(0, taskCount)
                    .OrderByDescending(i => ranks[i])
                    .ToList();


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

                        if (candidateProcessors.Count == 0)
                        {
                            candidateProcessors =
                                Enumerable.Range(0, procCount).ToList();

                            missingSpecializedTasks.Add(task.Name);
                        }

                        break;

                    case TaskType.UT:

                        candidateProcessors = GetGeneralProcessors(graph);

                        if (candidateProcessors.Count == 0)
                        {
                            candidateProcessors = Enumerable.Range(0, procCount).ToList();

                            missingGeneralTasks.Add(task.Name);
                        }

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
                    double bestScore = double.MaxValue;

                    foreach (var p in candidateProcessors)
                    {
                        int start = Math.Max(earliestStart, procFreeAt[p]);

                        int duration = graph.Times[ti][p];

                        int finish = start + duration;

                        int cost = graph.Costs[ti][p];

                        double score = TimeWeight * finish + CostWeight * cost;

                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestProc = p;
                        }
                    }

                    if (bestProc == -1)
                    {
                        throw new InvalidOperationException($"Nie udało się przypisać procesora do zadania {task.Name}");
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

                    int start = earliestStart;

                    foreach (var p in candidateProcessors)
                    {
                        start = Math.Max(start, procFreeAt[p]);
                    }

                    int duration = candidateProcessors.Select(p => graph.Times[ti][p]).Max();

                    int cost = candidateProcessors.Sum(p => graph.Costs[ti][p]);

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

                            ProcIndices = candidateProcessors,

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


            if (missingSpecializedTasks.Any())
            {
                result.Warnings.Add($"Zadania {string.Join(", ", missingSpecializedTasks)} wymagają procesora specjalizowanego, ale żaden nie istnieje. Zostaną wykonane na procesorach ogólnych.");
            }

            if (missingGeneralTasks.Any())
            {
                result.Warnings.Add($"Zadania {string.Join(", ", missingGeneralTasks)} wymagają procesora ogólnego, ale żaden nie istnieje. Zostaną wykonane na procesorach specjalizowanych.");
            }

            return result;
        }


        private Dictionary<int, double> ComputeUpwardRanks(Graph graph)
        {
            var taskIndex =
                graph.Tasks
                    .Select((t, i) => new { t, i })
                    .ToDictionary(x => x.t, x => x.i);

            var ranks =
                new Dictionary<int, double>();

            double Rank(int ti)
            {
                if (ranks.ContainsKey(ti))
                {
                    return ranks[ti];
                }

                double avgTime =
                    graph.Times[ti].Average();

                if (graph.Tasks[ti].Successors.Count == 0)
                {
                    ranks[ti] = avgTime;
                    return avgTime;
                }

                double maxSucc = 0;

                foreach (var (succ, comm) in graph.Tasks[ti].Successors)
                {
                    int si = taskIndex[succ];

                    maxSucc = Math.Max(
                        maxSucc,
                        comm + Rank(si));
                }

                ranks[ti] =
                    avgTime + maxSucc;

                return ranks[ti];
            }

            for (int i = 0; i < graph.Tasks.Count; i++)
            {
                Rank(i);
            }

            return ranks;
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
