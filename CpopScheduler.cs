namespace etg {
	public class CpopScheduler : IScheduler {
		public string Name => "CPOP Scheduling";

		public ScheduleResult Schedule(Graph graph) {
			var result = new ScheduleResult();
			var taskCount = graph.Tasks.Count;
			var procCount = graph.Procs.Count;
			var missingSpecializedTasks = new List<string>();
			var missingGeneralTasks = new List<string>();

			var predecessors = BuildPredecessors(graph);
			var avgTimes = Enumerable.Range(0, taskCount)
				.Select(i => graph.Times[i].Average())
				.ToArray();
			var upwardRanks = ComputeUpwardRanks(graph, avgTimes);
			var downwardRanks = ComputeDownwardRanks(graph, predecessors, avgTimes);
			var priorities = Enumerable.Range(0, taskCount)
				.ToDictionary(i => i, i => upwardRanks[i] + downwardRanks[i]);
			var criticalTasks = FindCriticalTasks(priorities);
			var criticalProcessor = ChooseCriticalProcessor(graph, criticalTasks);
			var order = PriorityTopologicalSort(graph, predecessors, priorities);

			var procFreeAt = new int[procCount];
			var taskEndTime = new int[taskCount];

			foreach (var ti in order) {
				var task = graph.Tasks[ti];
				var earliestStart = predecessors[ti]
					.Select(pred => taskEndTime[pred])
					.DefaultIfEmpty(0)
					.Max();

				var candidateProcessors = GetCandidateProcessors(
					graph,
					task,
					missingSpecializedTasks,
					missingGeneralTasks
				);

				if (IsSingleProcessorTask(task)) {
					var bestProc = -1;
					var bestFinish = int.MaxValue;

					if (criticalTasks.Contains(ti) && criticalProcessor != -1 && candidateProcessors.Contains(criticalProcessor)) {
						bestProc = criticalProcessor;
					} else {
						foreach (var p in candidateProcessors) {
							var start = Math.Max(earliestStart, procFreeAt[p]);
							var finish = start + graph.Times[ti][p];

							if (finish < bestFinish) {
								bestFinish = finish;
								bestProc = p;
							}
						}
					}

					if (bestProc == -1) {
						throw new InvalidOperationException($"Nie udało się przypisać procesora do zadania {task.Name}");
					}

					var actualStart = Math.Max(earliestStart, procFreeAt[bestProc]);
					var actualDuration = graph.Times[ti][bestProc];
					var actualCost = graph.Costs[ti][bestProc];
					var actualFinish = actualStart + actualDuration;

					procFreeAt[bestProc] = actualFinish;
					taskEndTime[ti] = actualFinish;

					result.ScheduledTasks.Add(new ScheduledTask {
						Task = task,
						TaskIndex = ti,
						ProcIndices = [bestProc],
						StartTime = actualStart,
						EndTime = actualFinish,
						Duration = actualDuration,
						Cost = actualCost
					});
				} else {
					var start = earliestStart;

					foreach (var p in candidateProcessors) {
						start = Math.Max(start, procFreeAt[p]);
					}

					var duration = ComputeParallelDuration(graph, ti, candidateProcessors);
					var cost = ComputeParallelCost(graph, ti, candidateProcessors);
					var finish = start + duration;

					foreach (var p in candidateProcessors) {
						procFreeAt[p] = finish;
					}

					taskEndTime[ti] = finish;

					result.ScheduledTasks.Add(new ScheduledTask {
						Task = task,
						TaskIndex = ti,
						ProcIndices = candidateProcessors,
						StartTime = start,
						EndTime = finish,
						Duration = duration,
						Cost = cost
					});
				}
			}

			if (missingSpecializedTasks.Any()) {
				result.Warnings.Add($"Zadania {string.Join(", ", missingSpecializedTasks.Distinct())} wymagają procesora specjalizowanego, ale żaden nie istnieje. Zostaną wykonane na procesorach ogólnych.");
			}

			if (missingGeneralTasks.Any()) {
				result.Warnings.Add($"Zadania {string.Join(", ", missingGeneralTasks.Distinct())} wymagają procesora ogólnego, ale żaden nie istnieje. Zostaną wykonane na procesorach specjalizowanych.");
			}

			return result;
		}

		private static List<List<int>> BuildPredecessors(Graph graph) {
			var predecessors = Enumerable.Range(0, graph.Tasks.Count)
				.Select(_ => new List<int>())
				.ToList();

			foreach (var task in graph.Tasks) {
				foreach (var successor in task.Successors) {
					predecessors[successor.Index].Add(task.Index);
				}
			}

			return predecessors;
		}

		private static Dictionary<int, double> ComputeUpwardRanks(Graph graph, double[] avgTimes) {
			var ranks = new Dictionary<int, double>();

			double Rank(int ti) {
				if (ranks.TryGetValue(ti, out var rank)) {
					return rank;
				}

				var maxSuccessorRank = graph.Tasks[ti].Successors
					.Select(successor => Rank(successor.Index))
					.DefaultIfEmpty(0)
					.Max();

				ranks[ti] = avgTimes[ti] + maxSuccessorRank;
				return ranks[ti];
			}

			for (var i = 0; i < graph.Tasks.Count; i++) {
				Rank(i);
			}

			return ranks;
		}

		private static Dictionary<int, double> ComputeDownwardRanks(Graph graph, List<List<int>> predecessors, double[] avgTimes) {
			var ranks = new Dictionary<int, double>();

			double Rank(int ti) {
				if (ranks.TryGetValue(ti, out var rank)) {
					return rank;
				}

				var maxPredecessorRank = predecessors[ti]
					.Select(pred => Rank(pred) + avgTimes[pred])
					.DefaultIfEmpty(0)
					.Max();

				ranks[ti] = maxPredecessorRank;
				return ranks[ti];
			}

			for (var i = 0; i < graph.Tasks.Count; i++) {
				Rank(i);
			}

			return ranks;
		}

		private static HashSet<int> FindCriticalTasks(Dictionary<int, double> priorities) {
			var criticalPriority = priorities.Values.Max();

			return priorities
				.Where(priority => Math.Abs(priority.Value - criticalPriority) < 0.0001)
				.Select(priority => priority.Key)
				.ToHashSet();
		}

		private static int ChooseCriticalProcessor(Graph graph, HashSet<int> criticalTasks) {
			var singleCriticalTasks = criticalTasks
				.Where(ti => IsSingleProcessorTask(graph.Tasks[ti]))
				.ToList();

			if (singleCriticalTasks.Count == 0) {
				return -1;
			}

			var validProcessors = Enumerable.Range(0, graph.Procs.Count)
				.Where(p => singleCriticalTasks.All(ti => IsProcessorAllowed(graph.Tasks[ti], graph.Procs[p])))
				.ToList();

			if (validProcessors.Count == 0) {
				return -1;
			}

			return validProcessors.MinBy(p => singleCriticalTasks.Sum(ti => graph.Times[ti][p]));
		}

		private static List<int> PriorityTopologicalSort(Graph graph, List<List<int>> predecessors, Dictionary<int, double> priorities) {
			var inDegree = predecessors.Select(p => p.Count).ToArray();
			var ready = Enumerable.Range(0, graph.Tasks.Count)
				.Where(i => inDegree[i] == 0)
				.ToList();
			var order = new List<int>();

			while (ready.Count > 0) {
				var current = ready
					.OrderByDescending(i => priorities[i])
					.ThenBy(i => i)
					.First();

				ready.Remove(current);
				order.Add(current);

				foreach (var successor in graph.Tasks[current].Successors) {
					inDegree[successor.Index]--;

					if (inDegree[successor.Index] == 0) {
						ready.Add(successor.Index);
					}
				}
			}

			if (order.Count != graph.Tasks.Count) {
				throw new InvalidOperationException("Graf zawiera cykl — sortowanie topologiczne niemożliwe.");
			}

			return order;
		}

		private static List<int> GetCandidateProcessors(
			Graph graph,
			Task task,
			List<string> missingSpecializedTasks,
			List<string> missingGeneralTasks
		) {
			var processors = graph.Procs
				.Where(proc => IsProcessorAllowed(task, proc))
				.Select(proc => proc.Index)
				.ToList();

			if (processors.Count > 0) {
				return processors;
			}

			switch (task.Type) {
				case TaskType.DT:
				case TaskType.CDT:
					missingSpecializedTasks.Add(task.Name);
					break;

				case TaskType.UT:
				case TaskType.CUT:
					missingGeneralTasks.Add(task.Name);
					break;
			}

			return Enumerable.Range(0, graph.Procs.Count).ToList();
		}

		private static bool IsSingleProcessorTask(Task task) {
			return task.Type is TaskType.GT or TaskType.UT or TaskType.DT;
		}

		private static bool IsProcessorAllowed(Task task, Proc proc) {
			return task.Type switch {
				TaskType.GT or TaskType.CGT => true,
				TaskType.UT or TaskType.CUT => !proc.Specialized,
				TaskType.DT or TaskType.CDT => proc.Specialized,
				_ => false
			};
		}

		private static int ComputeParallelDuration(Graph graph, int taskIndex, List<int> procIndices) {
			return (int)MathF.Ceiling(
				1 / procIndices.Sum(procIndex => 1 / (float)graph.Times[taskIndex][procIndex])
			);
		}

		private static int ComputeParallelCost(Graph graph, int taskIndex, List<int> procIndices) {
			return (int)MathF.Ceiling(
				procIndices.Sum(procIndex => (float)graph.Costs[taskIndex][procIndex]) / procIndices.Count
			);
		}
	}
}
