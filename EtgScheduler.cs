using System.Diagnostics;

namespace etg
{
	
	// algorytm sortowania wybiera z najktrótszym czasem zakonczenia
	
	

	public class EtgScheduler : IScheduler
	{
		public string Name => "ETG Scheduling";

		public ScheduleResult Schedule(Graph graph)
		{
			var missingSpecializedTasks = new List<string>();
			var missingGeneralTasks = new List<string>();
			var result = new ScheduleResult();
			var taskCount = graph.Tasks.Count;
			var procCount = graph.Procs.Count;

			var predecessors = new List<List<int>>();
			for (int i = 0; i < taskCount; i++) {
				predecessors.Add([]);
			}

			for (int i = 0; i < taskCount; i++) {
				foreach (var successor in graph.Tasks[i].Successors)
				{
					var si = successor.Index;
					predecessors[si].Add(i);
				}
			}

			// Sortowanie topologiczne (Kahn's algorithm)
			var order = TopologicalSort(taskCount, predecessors, graph);

			// . Zachłanne przypisanie
			var procFreeAt = new int[procCount]; // kiedy każdy procesor będzie wolny
			var taskEndTime = new int[taskCount]; // kiedy każde zadanie się kończy

			foreach (var ti in order) {
				var task = graph.Tasks[ti];

				// Najwcześniejszy start = max(koniec wszystkich poprzedników)
				int earliestStart = 0;
				foreach (var pred in predecessors[ti])
				{
					earliestStart = Math.Max(earliestStart, taskEndTime[pred]);
				}

				List<int> candidateProcessors;

				switch (task.Type) {
					case TaskType.DT:
					case TaskType.CDT:
						candidateProcessors = GetSpecializedProcessors(graph);

						if (candidateProcessors.Count == 0) {
							candidateProcessors = Enumerable.Range(0, procCount).ToList();
							missingSpecializedTasks.Add(task.Name);
						}

						break;

					case TaskType.UT:
					case TaskType.CUT:
						candidateProcessors = GetGeneralProcessors(graph);

						if (candidateProcessors.Count == 0) {
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

				if (task.Type == TaskType.GT || task.Type == TaskType.DT || task.Type == TaskType.UT) {
					int bestProc = -1;
					int bestFinish = int.MaxValue;

					foreach (var p in candidateProcessors) {
						int start = Math.Max(
							earliestStart,
							procFreeAt[p]
						);

						int duration = graph.Times[ti][p];
						int finish = start + duration;

						if (finish < bestFinish) {
							bestFinish = finish;
							bestProc = p;
						}
					}

					if (bestProc == -1) {
						throw new InvalidOperationException($"Nie udało się przypisać procesora do zadania {task.Name}");
					}

					int actualStart = Math.Max(earliestStart, procFreeAt[bestProc]);
					int actualDuration = graph.Times[ti][bestProc];
					int actualCost = graph.Costs[ti][bestProc];
					procFreeAt[bestProc] = actualStart + actualDuration;
					taskEndTime[ti] = actualStart + actualDuration;

					result.ScheduledTasks.Add(
						new ScheduledTask {
							Task = task,
							TaskIndex = ti,
							ProcIndices = [bestProc],
							StartTime = actualStart,
							EndTime = actualStart + actualDuration,
							Duration = actualDuration,
							Cost = actualCost
						}
					);
				} else {
					int start = earliestStart;

					foreach (var p in candidateProcessors) {
						start = Math.Max(
							start,
							procFreeAt[p]
						);
					}

  

                    int duration = (int)MathF.Ceiling(1 / candidateProcessors.Sum( proc => 1 / (float)graph.Times[task.Index][proc] ));
					int cost = (int)MathF.Ceiling(candidateProcessors.Sum(proc => (float)graph.Costs[task.Index][proc]) / candidateProcessors.Count);

                    int finish = start + duration;

					foreach (var p in candidateProcessors) {
						procFreeAt[p] = finish;
					}

					taskEndTime[ti] = finish;


					result.ScheduledTasks.Add(
						new ScheduledTask {
							Task = task,
							TaskIndex = ti,
							ProcIndices = candidateProcessors,
							StartTime = start,
							EndTime = finish,
							Duration = duration,
							Cost = cost
						}
					);
				}
			}



			if (missingSpecializedTasks.Any()) {
				result.Warnings.Add($"Zadania {string.Join(", ", missingSpecializedTasks)} wymagają procesora specjalizowanego, ale żaden nie istnieje. Zostaną wykonane na procesorach ogólnych.");
			}

			if (missingGeneralTasks.Any()) {
				result.Warnings.Add($"Zadania {string.Join(", ", missingGeneralTasks)} wymagają procesora ogólnego, ale żaden nie istnieje. Zostaną wykonane na procesorach specjalizowanych.");
			}

			return result;
		}

		private static List<int> TopologicalSort(int taskCount, List<List<int>> predecessors, Graph graph) {
			// Oblicz liczbę poprzedników 
			var inDegree = new int[taskCount];
			for (int i = 0; i < taskCount; i++) {
				inDegree[i] = predecessors[i].Count;
			}

			// Kolejka priorytetowa — zadania z zerowym in-degree
			// Priorytet: mniejszy indeks = wcześniej (stabilna kolejność)
			var ready = new SortedSet<int>();
			for (int i = 0; i < taskCount; i++) {
				if (inDegree[i] == 0) {
					ready.Add(i);
				}
			}

			var order = new List<int>();

			while (ready.Count > 0) {
				var current = ready.Min;
				ready.Remove(current);
				order.Add(current);

				// Zmniejsz in-degree następników
				foreach (var successor in graph.Tasks[current].Successors) {
					var si = successor.Index;
					inDegree[si]--;
					if (inDegree[si] == 0) {
						ready.Add(si);
					}
				}
			}

			if (order.Count != taskCount) {
				throw new InvalidOperationException("Graf zawiera cykl — sortowanie topologiczne niemożliwe.");
			}

			return order;
		}

		private static List<int> GetSpecializedProcessors(Graph graph) {
			return graph.Procs
				.Select((p, i) => new { p, i })
				.Where(x => x.p.Specialized)
				.Select(x => x.i)
				.ToList();
		}

		private static List<int> GetGeneralProcessors(Graph graph) {
			return graph.Procs
				.Select((p, i) => new { p, i })
				.Where(x => !x.p.Specialized)
				.Select(x => x.i)
				.ToList();
		}
	}
}
