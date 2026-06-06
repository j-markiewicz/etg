using System.Windows;

namespace etg {
	/// <summary>
	/// A greedy scheduler, selecting at each point in time the longest task
	/// (including successors) and scheduling it on the fastest available proc
	/// </summary>
	public class GreedyTimeScheduler : IScheduler {
		public string Name => "Greedy (min. time)";

		public ScheduleResult Schedule(Graph graph) {
			var tasksMissingProc = new List<string>();
			var result = new ScheduleResult();
			var tasksToSchedule = graph.Tasks;
			var taskCompletions = graph.Tasks.Select(_ => int.MaxValue).ToList();
			var noncompletedTasks = graph.Tasks;
			var busyProcs = graph.Procs.Select(_ => 0).ToList();
			var time = 0;

			while (tasksToSchedule.Count > 0) {
				// Mark completed tasks as such
				noncompletedTasks = noncompletedTasks.FindAll(t => taskCompletions[t.Index] > time);

				// Get all yet-unscheduled tasks without unfulfilled requirements
				var schedulableNow = tasksToSchedule.FindAll(
					t => noncompletedTasks.All(
						ts => !ts.Successors.Contains(t)
					)
				);

				if (schedulableNow.Count == 0) {
					time += 1;
					continue;
				}

				// Get the schedulable task with the longest minimum time (including successors)
				var longestTask = schedulableNow.MaxBy(t => MinTaskTime(graph, t))!;

				// Get all the procs usable by the longest task
				var usableProcs = graph.Procs.FindAll(p =>
					longestTask.Type == TaskType.GT ||
					longestTask.Type == TaskType.CGT ||
					(longestTask.Type == TaskType.UT && !p.Specialized) ||
					(longestTask.Type == TaskType.CUT && !p.Specialized) ||
					(longestTask.Type == TaskType.DT && p.Specialized) ||
					(longestTask.Type == TaskType.CDT && p.Specialized)
				);

				if (usableProcs.Count == 0) {
					tasksMissingProc.Add(longestTask.Name);
					usableProcs = graph.Procs;
				}

				usableProcs = usableProcs.FindAll(p => busyProcs[p.Index] <= time);
				if (usableProcs.Count == 0) {
					time += 1;
					continue;
				}

				// Schedule the task
				var multipleProcs = longestTask.Type switch {
					TaskType.CGT or TaskType.CUT or TaskType.CDT => true,
					TaskType.GT or TaskType.UT or TaskType.DT => false,
					var x => throw new OverflowException($"Invalid TaskType value {x}"),
				};

				if (multipleProcs) {
					var cost = (int)MathF.Ceiling(
						usableProcs.Sum(
							proc => (float)graph.Costs[longestTask.Index][proc.Index]
						) / usableProcs.Count
					);

					var duration = (int)MathF.Ceiling(
						1 / usableProcs.Sum(
							proc => 1 / (float)graph.Times[longestTask.Index][proc.Index]
						)
					);

					result.ScheduledTasks.Add(new ScheduledTask {
						Task = longestTask,
						TaskIndex = longestTask.Index,
						ProcIndices = usableProcs.Select(p => p.Index).ToList(),
						StartTime = time,
						EndTime = time + duration,
						Duration = duration,
						Cost = cost
					});

					foreach (var proc in usableProcs) {
						busyProcs[proc.Index] = time + duration;
					}

					taskCompletions[longestTask.Index] = time + duration;
					tasksToSchedule = tasksToSchedule.FindAll(t => t != longestTask);
				} else {
					var proc = usableProcs.MinBy(p => graph.Times[longestTask.Index][p.Index])!;
					var cost = graph.Costs[longestTask.Index][proc.Index];
					var duration = graph.Times[longestTask.Index][proc.Index];

					result.ScheduledTasks.Add(new ScheduledTask {
						Task = longestTask,
						TaskIndex = longestTask.Index,
						ProcIndices = [proc.Index],
						StartTime = time,
						EndTime = time + duration,
						Duration = duration,
						Cost = cost
					});

					busyProcs[proc.Index] = time + duration;
					taskCompletions[longestTask.Index] = time + duration;
					tasksToSchedule = tasksToSchedule.FindAll(t => t != longestTask);
				}
			}

			if (tasksMissingProc.Count > 0) {
				tasksMissingProc.Sort();
				result.Warnings.Add($"Zadania {string.Join(", ", tasksMissingProc.Distinct())} nie mają wymaganego typu procesora. Zostaną wykonane na dowolnych procesorach.");
			}

			return result;
		}

		/// <summary>
		/// Return the approximate minimum total time (i.e. assuming no
		/// concurrency) to complete this task and all its successors.
		/// This function ignores the processor/task type.
		/// </summary>
		/// <param name="graph">The graph the task belongs to</param>
		/// <param name="task">The task</param>
		/// <returns>The minimum total completion time</returns>
		private static long MinTaskTime(Graph graph, Task task) {
			return graph.Times[task.Index].Min() + task.Successors.Sum(t => MinTaskTime(graph, t));
		}
	}
}
