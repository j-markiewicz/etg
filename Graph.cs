using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace etg {
	public enum TaskType {
		/// <summary>
		/// General Task - dowolny zasób
		/// </summary>
		GT,
		/// <summary>
		/// Universal Task - tylko zasób uniwersalny
		/// </summary>
		UT,
		/// <summary>
		/// Dedicated Task - tylko zasób specjalistyczny
		/// </summary>
		DT,
		/// <summary>
		/// Common General Task - wiele zasobów dowolnego typu
		/// </summary>
		CGT,
		/// <summary>
		/// Common Universal Task - wiele zasobów uniwersalnych
		/// </summary>
		CUT,
		/// <summary>
		/// Common Dedicated Task - wiele zasobów specjalistycznych
		/// </summary>
		CDT
	}

	public partial class Graph(List<Task> tasks, List<Proc> procs, List<List<int>> times, List<List<int>> costs) {
		public List<Task> Tasks = tasks;
		public List<Proc> Procs = procs;
		public List<List<int>> Times = times;
		public List<List<int>> Costs = costs;

		private static TaskType ParseTaskType(string type) {
			return type switch {
				"CGT" => TaskType.CGT,
				"CUT" => TaskType.CUT,
				"CDT" => TaskType.CDT,
				"GT" => TaskType.GT,
				"UT" => TaskType.UT,
				"DT" => TaskType.DT,
				_ => throw new ArgumentException($"Unknown task type '{type}'"),
			};
		}

		public static Graph Parse(string str) {
			var reader = new StringReader(str.Trim());

			// @tasks N
			var lastLine = "[start of file]";
			var line = reader.ReadLine()?.Trim();
			var match = TasksLine().Match(line ?? "");

			if (!match.Success) {
				throw new ArgumentException($"Parsing failed:\nMissing or invalid @tasks line near '{lastLine}', '{line}'");
			}

			lastLine = line;
			var nTasks = int.Parse(match.Groups["nTasks"].Value);
			var tasks = new List<Task>();
			var successors = new List<List<int>>();

			// tasks
			for (int i = 0; i < nTasks; i++) {
				line = reader.ReadLine()?.Trim();
				match = TaskLine().Match(line ?? "");

				if (!match.Success) {
					throw new ArgumentException($"Parsing failed:\nMissing or invalid task near '{lastLine}', '{line}'");
				}

				lastLine = line;
				var type = ParseTaskType(match.Groups["type"].Value);
				var index = int.Parse(match.Groups["index"].Value);
				var nSuccessors = int.Parse(match.Groups["nSuccessors"].Value);
				var taskSuccessors = match.Groups["successors"]
						.Value
						.Trim()
						.Split(" ", StringSplitOptions.RemoveEmptyEntries)
						.Select(int.Parse)
						.ToList();

				if (index != tasks.Count) {
					throw new ArgumentException($"Parsing failed:\nIncorrect task index near '{lastLine}', '{line}'");
				} else if (nSuccessors != taskSuccessors.Count) {
					throw new ArgumentException($"Parsing failed:\nIncorrect number of successors near '{lastLine}', '{line}'");
				}

				tasks.Add(new Task(type, index));
				successors.Add(taskSuccessors);
			}

			// successors
			for (int i = 0; i < tasks.Count; i++) {
				tasks[i].Successors = successors[i].Select(s => tasks[s]).ToList();
			}

			// @proc M
			line = reader.ReadLine()?.Trim();
			match = ProcLine().Match(line ?? "");

			if (!match.Success) {
				throw new ArgumentException($"Parsing failed:\nMissing or invalid @proc line near '{lastLine}', '{line}'");
			}

			lastLine = line;
			var nProcs = int.Parse(match.Groups["nProcs"].Value);
			var procs = new List<Proc>();

			// procs
			for (int i = 0; i < nProcs; i++) {
				line = reader.ReadLine()?.Trim();
				match = ProcRegex().Match(line ?? "");

				if (!match.Success) {
					throw new ArgumentException($"Parsing failed:\nMissing or invalid proc near '{lastLine}', '{line}'");
				}

				lastLine = line;
				var speed = int.Parse(match.Groups["speed"].Value);
				var specialized = int.Parse(match.Groups["type"].Value) != 0;
				procs.Add(new Proc(speed, specialized));
			}

			// @times
			line = reader.ReadLine()?.Trim();
			match = TimesLine().Match(line ?? "");

			if (!match.Success) {
				throw new ArgumentException($"Parsing failed:\nMissing or invalid @times line near '{lastLine}', '{line}'");
			}

			lastLine = line;
			var times = new List<List<int>>();
			for (int i = 0; i < nTasks; i++) {
				var vals = reader.ReadLine()?.Trim()?.Split(" ", StringSplitOptions.RemoveEmptyEntries)?.Select(v => int.Parse(v))?.ToList();

				if (vals is null || vals.Count != nProcs) {
					throw new ArgumentException($"Parsing failed:\nInvalid number of elements near '{lastLine}', '{line}'");
				}

				lastLine = line;
				times.Add(vals);
			}

			// @cost
			line = reader.ReadLine()?.Trim();
			match = CostLine().Match(line ?? "");

			if (!match.Success) {
				throw new ArgumentException($"Parsing failed:\nMissing or invalid @cost line near '{lastLine}', '{line}'");
			}

			lastLine = line;
			var cost = new List<List<int>>();
			for (int i = 0; i < nTasks; i++) {
				var vals = reader.ReadLine()?.Trim()?.Split(" ", StringSplitOptions.RemoveEmptyEntries)?.Select(v => int.Parse(v))?.ToList();

				if (vals is null || vals.Count != nProcs) {
					throw new ArgumentException($"Parsing failed:\nInvalid number of elements near '{lastLine}', '{line}'");
				}

				lastLine = line;
				cost.Add(vals);
			}

			line = reader.ReadLine()?.Trim() ?? "";
			if (line != "") {
				throw new ArgumentException($"Parsing failed:\nUnexpected extra line near '{lastLine}', '{line}'");
			}

			return new Graph(tasks, procs, times, cost);
		}

		[GeneratedRegex("^@tasks\\s+(?<nTasks>\\d+)$")]
		private static partial Regex TasksLine();

		[GeneratedRegex("^(?<type>CGT|CUT|CDT|GT|UT|DT)(?<index>\\d+)\\s+(?<nSuccessors>\\d+)(?<successors>(\\s+\\d+)*)$")]
		private static partial Regex TaskLine();

		[GeneratedRegex("^@proc\\s+(?<nProcs>\\d+)$")]
		private static partial Regex ProcLine();

		[GeneratedRegex("^(?<speed>\\d+)\\s+(?<type>0|1)$")]
		private static partial Regex ProcRegex();

		[GeneratedRegex("^@times$")]
		private static partial Regex TimesLine();

		[GeneratedRegex("^@cost$")]
		private static partial Regex CostLine();
	}

	public class Task(TaskType type, int index) {
		public TaskType Type = type;
		public int Index = index;
		public List<Task> Successors = [];
		public string Name { get => $"{Type}{Index}"; }
	}

	public class Proc(int speed, bool specialized) {
		public int Speed = speed;
		public bool Specialized = specialized;
	}
}
