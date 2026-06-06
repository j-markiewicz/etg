using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace etg {
	public partial class MainWindow : Window {
		const string EXAMPLE_ETG = "@tasks 10\nGT0 2 1 2\nGT1 2 3 5\nUT2 3 9 4 6\nCDT3 2 7 9\nCGT4 1 8\nGT5 1 9\nCGT6 0\nCGT7 0\nDT8 0\nCGT9 0\n@proc 4\n100 1\n200 1\n500 0\n300 0\n@times\n30 10 3 4\n50 20 6 5\n20 10 3 5\n10 8  1 2\n30 15 4 10\n50 30 5 5\n40 15 10 12\n30 15 5 8\n20 5  2 4\n10 5  3 4\n@cost\n3 2 50 10\n5 4 80 20\n3 3 60 20\n3 1 20 5\n3 2 70 30\n5 3 80 15\n3 2 70 15\n3 2 50 18\n3 1 30 10\n3 1 40 12\n";
		Graph? Graph = null;
		ScheduleResult? ScheduleResult = null;

		public MainWindow() {
			InitializeComponent();
			PreviewText.Text = EXAMPLE_ETG;
		}

		private void LoadFromFile_Click(object sender, RoutedEventArgs e) {
			var dialog = new OpenFileDialog();
			if (dialog.ShowDialog() == true)
			{
				var file = dialog.OpenFile();
				Filename.Text = dialog.SafeFileName;
				PreviewText.Text = new StreamReader(file).ReadToEnd();
				GraphTab.IsEnabled = false;
				GanttTab.IsEnabled = false;
				ScheduleButton.IsEnabled = false;
				PreviewTab.Focus();
				Graph = null;
				ScheduleResult = null;
				ClearDiagnostics();
			}
		}

		private void Open_Click(object sender, RoutedEventArgs e) {
			try {
				Graph = Graph.Parse(PreviewText.Text);
				GraphTab.IsEnabled = true;
				GanttTab.IsEnabled = true;
				ScheduleButton.IsEnabled = true;
				RunDiagnostics(Graph);
				GraphRenderer.Draw(GraphCanvas, Graph);
				PreviewTab.Focus();
			} catch (Exception ex) {
				GraphTab.IsEnabled = false;
				GanttTab.IsEnabled = false;
				PreviewTab.Focus();
				Graph = null;
				ScheduleResult = null;
				ClearDiagnostics();
				AddDiagnostic("Błąd parsowania", ex.Message, DiagnosticLevel.Error);
			}
		}

		private enum DiagnosticLevel { Info, Warning, Error, Success }

		private void ClearDiagnostics() {
			DiagnosticsText.Text = "Wczytaj plik i kliknij 'Wczytaj', aby zobaczyć diagnostykę.";
			DiagnosticsText.Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));
			DiagnosticsList.Items.Clear();
		}

		private void AddDiagnostic(string title, string message, DiagnosticLevel level) {
			var color = level switch {
				DiagnosticLevel.Error => Colors.Red,
				DiagnosticLevel.Warning => Color.FromRgb(0xCC, 0x88, 0x00),
				DiagnosticLevel.Success => Color.FromRgb(0x22, 0x88, 0x22),
				_ => Color.FromRgb(0x33, 0x66, 0x99),
			};

			var icon = level switch {
				DiagnosticLevel.Error => "❌",
				DiagnosticLevel.Warning => "⚠️",
				DiagnosticLevel.Success => "✅",
				_ => "ℹ️",
			};

			var panel = new StackPanel { Margin = new Thickness(0, 4, 0, 4) };

			var header = new TextBlock {
				FontWeight = FontWeights.SemiBold,
				Foreground = new SolidColorBrush(color),
			};

			header.Inlines.Add(new Run($"{icon} {title}"));
			panel.Children.Add(header);

			var body = new TextBlock {
				Text = message,
				TextWrapping = TextWrapping.Wrap,
				Margin = new Thickness(20, 2, 0, 0),
				Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x44, 0x44)),
			};

			panel.Children.Add(body);
			DiagnosticsList.Items.Add(panel);
		}

		private void RunDiagnostics(Graph graph) {
			DiagnosticsList.Items.Clear();
			DiagnosticsText.Text = "";

			var specializedCount = graph.Procs.Count(p => p.Specialized);
			var generalCount = graph.Procs.Count(p => !p.Specialized);
			bool hasWarnings = false;

			// Sprawdzenie: zadania DT/CDT bez procesora specjalistycznego
			var dtTasks = graph.Tasks.Where(t => t.Type == TaskType.DT || t.Type == TaskType.CDT).ToList();
			if (dtTasks.Count > 0 && specializedCount == 0) {
				AddDiagnostic(
					"Brak procesora specjalistycznego",
					$"Zadania {string.Join(", ", dtTasks.Select(t => t.Name))} preferują zasób specjalistyczny, ale żaden procesor nie ma flagi specjalistyczny=1. " +
					$"Zostaną przypisane do ogólnego procesora z wyższym kosztem.",
					DiagnosticLevel.Warning
				);
				hasWarnings = true;
			}

			if (!hasWarnings) {
				AddDiagnostic(
					"Konfiguracja poprawna",
					$"Wczytano {graph.Tasks.Count} zadań i {graph.Procs.Count} procesorów ({specializedCount} specjalistycznych, {generalCount} ogólnych).",
					DiagnosticLevel.Success
				);
			}
		}

		private void Schedule_Click(object sender, RoutedEventArgs e) {
			if (Graph == null) return;

			try {
				IScheduler scheduler;

				switch (SchedulerComboBox.SelectedIndex) {
					case 0:
						scheduler = new EtgScheduler();
						break;

					case 1:
						scheduler = new HeftScheduler();
						break;

					case 2:
						scheduler = new CpopScheduler();
						break;

					case 3:
						scheduler = new GreedyTimeScheduler();
						break;

					case 4:
						scheduler = new GreedyCostScheduler();
						break;

					default:
						scheduler = new EtgScheduler();
						break;
				}
				var result = scheduler.Schedule(Graph);

				ScheduleResult = result;

				if (result.Warnings.Any()) {
					MessageBox.Show(
						string.Join("\n\n", result.Warnings),
						"Ostrzeżenia",
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
				}

				GanttTab.IsEnabled = true;
				GanttRenderer.Draw(GanttCanvas, Graph, result);
				GanttTab.Focus();
			} catch (Exception ex) {
				MessageBox.Show(ex.Message, "Błąd szeregowania", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}
