using System.IO;
using System.Text.RegularExpressions;

namespace etg {
	public partial class Graph(List<Task> tasks, List<Proc> procs, List<List<int>> times, List<List<int>> costs) {
		public List<Task> Tasks = tasks;
		public List<Proc> Procs = procs;
		public List<List<int>> Times = times;
		public List<List<int>> Costs = costs;

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
			var taskRequirements = new List<List<(int, int)>>();

			// tasks
			for (int i = 0; i < nTasks; i++) {
				line = reader.ReadLine()?.Trim();
				match = TaskLine().Match(line ?? "");

				if (!match.Success) {
					throw new ArgumentException($"Parsing failed:\nMissing or invalid task near '{line}'");
				}

				var name = match.Groups["name"].Value;
				var type = int.Parse(match.Groups["type"].Value);
				var requiresStr = match.Groups["requires"].Value.Trim();
				var requires = requiresStr != "" ? requiresStr.Split(" ", StringSplitOptions.RemoveEmptyEntries).Select(req => {
					var groups = TaskRequirement().Match(req).Groups;
					return (
						int.Parse(groups["index"].Value),
						int.Parse(groups["number"].Value)
					);
				}).ToList() : [];

				tasks.Add(new Task(name, type));
				taskRequirements.Add(requires);
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
				match = Proc().Match(line ?? "");

				if (!match.Success) {
					throw new ArgumentException($"Parsing failed:\nMissing or invalid proc near '{line}'");
				}

				var a = int.Parse(match.Groups[1].Value);
				var b = int.Parse(match.Groups[2].Value);
				var c = int.Parse(match.Groups[3].Value);
				procs.Add(new Proc(a, b, c));
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

			return new Graph(
				tasks.Select((t, i) => {
					t.Requires = taskRequirements[i].Select(
						r => (tasks[r.Item1], r.Item2)
					).ToList();
					return t;
				}).ToList(),
				procs,
				times,
				cost
			);
		}

		[GeneratedRegex("^@tasks (?<nTasks>\\d+)$")]
		private static partial Regex TasksLine();

		[GeneratedRegex("^(?<name>[a-zA-Z0-9]+) (?<type>\\d)(?<requires>( \\d+\\(\\d+\\))*)$")]
		private static partial Regex TaskLine();

		[GeneratedRegex("^(?<index>\\d+)\\((?<number>\\d+)\\)$")]
		private static partial Regex TaskRequirement();

		[GeneratedRegex("^@proc (?<nProcs>\\d+)$")]
		private static partial Regex ProcLine();

		[GeneratedRegex("^(\\d+) (\\d+) (\\d+)$")]
		private static partial Regex Proc();

		[GeneratedRegex("^@times$")]
		private static partial Regex TimesLine();

		[GeneratedRegex("^@cost$")]
		private static partial Regex CostLine();
	}

	public class Task(string name, int type, List<(Task, int)> requires) {
		public string Name = name;
		public int Type = type;
		public List<(Task, int)> Requires = requires;

		public Task(string name, int type): this(name, type, []) {}
	}

	public class Proc(int name, int zero, int type) {
		public int Name = name;
		public int Zero = zero;
		public int Type = type;
	}
}
