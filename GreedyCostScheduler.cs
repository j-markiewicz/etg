using System.Windows;

namespace etg {
	/// <summary>
	/// A greedy scheduler, scheduling at each point in time the first task
	/// that can be immediately scheduled in its cheapest proc
	/// </summary>
	public class GreedyCostScheduler : IScheduler {
		public string Name => "Greedy (min. cost)";

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

				// Get the best task to schedule
				var task = schedulableNow.Find(t => busyProcs[graph.Procs.MinBy(p => graph.Costs[t.Index][p.Index])!.Index] <= time);
				if (task == null) {
					time += 1;
					continue;
				}

				// Get all the procs usable by the longest task
				var usableProcs = graph.Procs.FindAll(p =>
					task.Type == TaskType.GT ||
					task.Type == TaskType.CGT ||
					(task.Type == TaskType.UT && !p.Specialized) ||
					(task.Type == TaskType.CUT && !p.Specialized) ||
					(task.Type == TaskType.DT && p.Specialized) ||
					(task.Type == TaskType.CDT && p.Specialized)
				);

				if (usableProcs.Count == 0) {
					tasksMissingProc.Add(task.Name);
					usableProcs = graph.Procs;
				}

				usableProcs = usableProcs.FindAll(p => busyProcs[p.Index] <= time);
				if (usableProcs.Count == 0) {
					time += 1;
					continue;
				}

				var proc = usableProcs.MinBy(p => graph.Costs[task.Index][p.Index])!;
				var cost = graph.Costs[task.Index][proc.Index];
				var duration = graph.Times[task.Index][proc.Index];

				result.ScheduledTasks.Add(new ScheduledTask {
					Task = task,
					TaskIndex = task.Index,
					ProcIndices = [proc.Index],
					StartTime = time,
					EndTime = time + duration,
					Duration = duration,
					Cost = cost
				});

				busyProcs[proc.Index] = time + duration;
				taskCompletions[task.Index] = time + duration;
				tasksToSchedule = tasksToSchedule.FindAll(t => t != task);
			}

			if (tasksMissingProc.Count > 0) {
				tasksMissingProc.Sort();
				result.Warnings.Add($"Zadania {string.Join(", ", tasksMissingProc.Distinct())} nie mają wymaganego typu procesora. Zostaną wykonane na dowolnych procesorach.");
			}

			return result;
		}
	}
}
