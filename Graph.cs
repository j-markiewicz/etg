using System.IO;
using System.Text.RegularExpressions;

namespace etg {
    public enum TaskType
    {
        GT,   // General Task - dowolny zasób
        UT,   // Universal Task - tylko zasób uniwersalny
        DT,   // Dedicated Task - tylko zasób specjalistyczny
        CDT,  // Common Dedicated Task - wiele zasobów specjalistycznych
        CGT   // Common General Task - wiele zasobów dowolnego typu
    }

    public partial class Graph(List<Task> tasks, List<Proc> procs, List<List<int>> times, List<List<int>> costs) {
        public List<Task> Tasks = tasks;
        public List<Proc> Procs = procs;
        public List<List<int>> Times = times;
        public List<List<int>> Costs = costs;

        private static TaskType ParseTaskType(string name) {
            if (name.StartsWith("CDT")) return TaskType.CDT;
            if (name.StartsWith("CGT")) return TaskType.CGT;
            if (name.StartsWith("GT")) return TaskType.GT;
            if (name.StartsWith("UT")) return TaskType.UT;
            if (name.StartsWith("DT")) return TaskType.DT;
            throw new ArgumentException($"Unknown task type in '{name}'");
        }

        public static Graph Parse(string str) {
            var reader = new StringReader(str);

            // @tasks N
            var line = reader.ReadLine()?.Trim();
            var match = TasksLine().Match(line ?? "");

            if (!match.Success) {
                throw new ArgumentException($"Parsing failed:\nMissing or invalid @tasks line near '{line}'");
            }

            var nTasks = int.Parse(match.Groups["nTasks"].Value);
            var tasks = new List<Task>();
            var taskSuccessors = new List<List<(int, int)>>();

            // tasks
            for (int i = 0; i < nTasks; i++) {
                line = reader.ReadLine()?.Trim();
                match = TaskLine().Match(line ?? "");

                if (!match.Success) {
                    throw new ArgumentException($"Parsing failed:\nMissing or invalid task near '{line}'");
                }

                var name = match.Groups["name"].Value;
                var successorCount = int.Parse(match.Groups["successorCount"].Value);
                var successorsStr = match.Groups["successors"].Value.Trim();
                var successors = successorsStr != "" ? successorsStr.Split(" ", StringSplitOptions.RemoveEmptyEntries).Select(req => {
                    var groups = TaskSuccessor().Match(req).Groups;
                    return (
                        int.Parse(groups["index"].Value),
                        int.Parse(groups["commWeight"].Value)
                    );
                }).ToList() : [];

                var taskType = ParseTaskType(name);
                tasks.Add(new Task(name, taskType, successorCount));
                taskSuccessors.Add(successors);
            }

            // @proc M
            line = reader.ReadLine()?.Trim();
            match = ProcLine().Match(line ?? "");

            if (!match.Success) {
                throw new ArgumentException($"Parsing failed:\nMissing or invalid @proc line near '{line}'");
            }

            var nProcs = int.Parse(match.Groups["nProcs"].Value);
            var procs = new List<Proc>();

            // procs
            for (int i = 0; i < nProcs; i++) {
                line = reader.ReadLine()?.Trim();
                match = ProcRegex().Match(line ?? "");

                if (!match.Success) {
                    throw new ArgumentException($"Parsing failed:\nMissing or invalid proc near '{line}'");
                }

                var speed = int.Parse(match.Groups[1].Value);
                // Druga kolumna (universal) ignorowana
                var specialized = int.Parse(match.Groups[3].Value);
                procs.Add(new Proc(speed, specialized));
            }

            // @times
            line = reader.ReadLine()?.Trim();
            match = TimesLine().Match(line ?? "");

            if (!match.Success) {
                throw new ArgumentException($"Parsing failed:\nMissing or invalid @times line near '{line}'");
            }

            var times = new List<List<int>>();
            for (int i = 0; i < nTasks; i++) {
                var vals = reader.ReadLine()?.Trim()?.Split(" ", StringSplitOptions.RemoveEmptyEntries)?.Select(v => int.Parse(v))?.ToList();

                if (vals is null || vals.Count != nProcs) {
                    throw new ArgumentException($"Parsing failed:\nInvalid number of elements near '{line}'");
                }

                times.Add(vals);
            }

            // @cost
            line = reader.ReadLine()?.Trim();
            match = CostLine().Match(line ?? "");

            if (!match.Success) {
                throw new ArgumentException($"Parsing failed:\nMissing or invalid @cost line near '{line}'");
            }

            var cost = new List<List<int>>();
            for (int i = 0; i < nTasks; i++) {
                var vals = reader.ReadLine()?.Trim()?.Split(" ", StringSplitOptions.RemoveEmptyEntries)?.Select(v => int.Parse(v))?.ToList();

                if (vals is null || vals.Count != nProcs) {
                    throw new ArgumentException($"Parsing failed:\nInvalid number of elements near '{line}'");
                }

                cost.Add(vals);
            }

            // Przypisz następniki
            for (int i = 0; i < tasks.Count; i++) {
                tasks[i].Successors = taskSuccessors[i].Select(
                    s => (tasks[s.Item1], s.Item2)
                ).ToList();
            }

            return new Graph(tasks, procs, times, cost);
        }

        [GeneratedRegex("^@tasks (?<nTasks>\\d+)$")]
        private static partial Regex TasksLine();

        [GeneratedRegex("^(?<name>[a-zA-Z0-9]+) (?<successorCount>\\d)(?<successors>( \\d+\\(\\d+\\))*)$")]
        private static partial Regex TaskLine();

        [GeneratedRegex("^(?<index>\\d+)\\((?<commWeight>\\d+)\\)$")]
        private static partial Regex TaskSuccessor();

        [GeneratedRegex("^@proc (?<nProcs>\\d+)$")]
        private static partial Regex ProcLine();

        [GeneratedRegex("^(\\d+) (\\d+) (\\d+)$")]
        private static partial Regex ProcRegex();

        [GeneratedRegex("^@times$")]
        private static partial Regex TimesLine();

        [GeneratedRegex("^@cost$")]
        private static partial Regex CostLine();
    }

    public class Task {
        public string Name;
        public TaskType TaskType;
        public int SuccessorCount;
        public List<(Task Task, int CommWeight)> Successors;

        public Task(string name, TaskType taskType, int successorCount)
        {
            Name = name;
            TaskType = taskType;
            SuccessorCount = successorCount;
            Successors = [];
        }
    }

    public class Proc
    {
        public int Speed;
        public int Specialized;

        public Proc(int speed, int specialized)
        {
            Speed = speed;
            Specialized = specialized;
        }
    }
}