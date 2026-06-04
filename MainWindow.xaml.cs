using Microsoft.Win32;
using System.IO;
using System.Windows;

namespace etg {
	public partial class MainWindow: Window {
		const string EXAMPLE_ETG = "@tasks 10\nGT0 2 1(0) 2(0)\nGT1 2 3(0) 5(0)\nUT2 3 9(0) 4(0) 6(0)\nCDT3 2 7(0) 9(0)\nCGT4 1 8(0)\nGT5 1 9(0)\nCGT6 0\nCGT7 0\nDT8 0\nCGT9 0\n@proc 4\n100 0 1\n200 0 1\n500 0 0\n300 0 0\n@times\n30 10 3 4\n50 20 6 5\n20 10 3 5\n10 8  1 2\n30 15 4 10\n50 30 5 5\n40 15 10 12\n30 15 5 8\n20 5  2 4\n10 5  3 4\n@cost\n3 2 50 10\n5 4 80 20\n3 3 60 20\n3 1 20 5\n3 2 70 30\n5 3 80 15\n3 2 70 15\n3 2 50 18\n3 1 30 10\n3 1 40 12\n";
		Graph? Graph = null;

		public MainWindow() {
			InitializeComponent();
			PreviewText.Text = EXAMPLE_ETG;

		}

		private void LoadFromFile_Click(object sender, RoutedEventArgs e) {
			var dialog = new OpenFileDialog();
			if (dialog.ShowDialog() == true) {
				var file = dialog.OpenFile();
				Filename.Text = dialog.SafeFileName;
				PreviewText.Text = new StreamReader(file).ReadToEnd();
				GraphTab.IsEnabled = false;
				GanttTab.IsEnabled = false;
				PreviewTab.Focus();
				Graph = null;
			}
		}

		private void Open_Click(object sender, RoutedEventArgs e) {
			try {
				Graph = Graph.Parse(PreviewText.Text);
				GraphTab.IsEnabled = true;
				GanttTab.IsEnabled = true;
				GraphTab.Focus();
			} catch (Exception ex) {
				GraphTab.IsEnabled = false;
				GanttTab.IsEnabled = false;
				PreviewTab.Focus();
				Graph = null;
				MessageBox.Show(ex.Message, "Błąd!", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}
	}
}
