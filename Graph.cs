namespace etg {
	public class Graph(List<Task> tasks, List<Proc> procs, List<List<int>> times, List<List<int>> costs) {
		public List<Task> Tasks = tasks;
		public List<Proc> Procs = procs;
		public List<List<int>> Times = times;
		public List<List<int>> Costs = costs;

		public static Graph Parse(string str) {
			// TODO
			return new Graph([], [], [], []);
		}
	}

	public class Task(string name, int type, List<(Task, int)> requires) {
		public string Name = name;
		public int Type = type;
		public List<(Task, int)> Requires = requires;

		public Task(string name, int type): this(name, type, []) {}
	}

	public class Proc(string name, int zero, int type) {
		public string Name = name;
		public int Zero = zero;
		public int Type = type;
	}
}
