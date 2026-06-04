using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

// rysuje tylko graf


namespace etg
{
    public static class GraphRenderer
    {
        private const double NodeWidth = 70;
        private const double NodeHeight = 40;
        private const double LayerSpacingY = 80;
        private const double NodeSpacingX = 90;
        private const double PaddingTop = 30;
        private const double PaddingLeft = 30;

        private static readonly Dictionary<TaskType, Color> TaskColors = new() {
            { TaskType.GT,  Color.FromRgb(0x42, 0x9E, 0xF5) },  // niebieski
			{ TaskType.UT,  Color.FromRgb(0xAB, 0x47, 0xBC) },  // fioletowy
			{ TaskType.DT,  Color.FromRgb(0xFF, 0x98, 0x00) },  // pomarańczowy
			{ TaskType.CDT, Color.FromRgb(0xEF, 0x53, 0x50) },  // czerwony
			{ TaskType.CGT, Color.FromRgb(0x66, 0xBB, 0x6A) },  // zielony
		};

        public static void Draw(Canvas canvas, Graph graph)
        {
            canvas.Children.Clear();

            if (graph.Tasks.Count == 0) return;

            //  Wyznacz warstwy (najdłuższa ścieżka od korzenia)
            var layers = ComputeLayers(graph);

            // Oblicz pozycje węzłów
            var positions = ComputePositions(graph, layers);

            //  Rysuj krawędzie 
            DrawEdges(canvas, graph, positions);

            // Rysuj węzły
            DrawNodes(canvas, graph, positions);

            //  Ustaw rozmiar Canvas, żeby ScrollViewer działał
            double maxX = positions.Values.Max(p => p.X) + NodeWidth + PaddingLeft;
            double maxY = positions.Values.Max(p => p.Y) + NodeHeight + LayerSpacingY;

            canvas.Width = Math.Max(maxX, 400);
            canvas.Height = Math.Max(maxY, 200);
        }

        private static Dictionary<int, int> ComputeLayers(Graph graph)
        {
            var taskIndex = new Dictionary<Task, int>();
            for (int i = 0; i < graph.Tasks.Count; i++)
            {
                taskIndex[graph.Tasks[i]] = i;
            }

            // Zbuduj listę poprzedników
            var predecessors = new Dictionary<int, List<int>>();
            for (int i = 0; i < graph.Tasks.Count; i++)
            {
                predecessors[i] = [];
            }
            for (int i = 0; i < graph.Tasks.Count; i++)
            {
                foreach (var (successor, _) in graph.Tasks[i].Successors)
                {
                    var si = taskIndex[successor];
                    predecessors[si].Add(i);
                }
            }

            // Najdłuższa ścieżka od korzenia = warstwa
            var layer = new Dictionary<int, int>();
            var visited = new HashSet<int>();

            int ComputeLayer(int idx)
            {
                if (layer.ContainsKey(idx)) return layer[idx];
                if (predecessors[idx].Count == 0)
                {
                    layer[idx] = 0;
                    return 0;
                }
                var maxPred = predecessors[idx].Max(p => ComputeLayer(p));
                layer[idx] = maxPred + 1;
                return layer[idx];
            }

            for (int i = 0; i < graph.Tasks.Count; i++)
            {
                ComputeLayer(i);
            }

            return layer;
        }

        private static Dictionary<int, Point> ComputePositions(Graph graph, Dictionary<int, int> layers)
        {
            // Grupuj zadania wg warstw
            var layerGroups = new Dictionary<int, List<int>>();
            foreach (var (idx, lay) in layers)
            {
                if (!layerGroups.ContainsKey(lay))
                    layerGroups[lay] = [];
                layerGroups[lay].Add(idx);
            }

            // Sortuj zadania w każdej warstwie wg indeksu
            foreach (var group in layerGroups.Values)
            {
                group.Sort();
            }

            var positions = new Dictionary<int, Point>();

            foreach (var (lay, group) in layerGroups)
            {
                double totalWidth = group.Count * NodeWidth + (group.Count - 1) * (NodeSpacingX - NodeWidth);
                double startX = PaddingLeft + (group.Count > 1 ? 0 : totalWidth / 2);

                for (int i = 0; i < group.Count; i++)
                {
                    double x = PaddingLeft + i * NodeSpacingX;
                    double y = PaddingTop + lay * LayerSpacingY;
                    positions[group[i]] = new Point(x, y);
                }
            }

            // Wycentruj warstwy względem najszerszej
            int maxCount = layerGroups.Values.Max(g => g.Count);
            double maxWidth = maxCount * NodeSpacingX;

            foreach (var (lay, group) in layerGroups)
            {
                double groupWidth = group.Count * NodeSpacingX;
                double offset = (maxWidth - groupWidth) / 2;
                for (int i = 0; i < group.Count; i++)
                {
                    var idx = group[i];
                    var p = positions[idx];
                    positions[idx] = new Point(p.X + offset, p.Y);
                }
            }

            return positions;
        }

        private static void DrawEdges(Canvas canvas, Graph graph, Dictionary<int, Point> positions)
        {
            var taskIndex = new Dictionary<Task, int>();
            for (int i = 0; i < graph.Tasks.Count; i++)
            {
                taskIndex[graph.Tasks[i]] = i;
            }

            foreach (var (fromIdx, fromPos) in positions)
            {
                var task = graph.Tasks[fromIdx];
                foreach (var (successor, _) in task.Successors)
                {
                    var toIdx = taskIndex[successor];
                    var toPos = positions[toIdx];

                    // Linia od dolnej krawędzi węzła do górnej krawędzi następnika
                    double x1 = fromPos.X + NodeWidth / 2;
                    double y1 = fromPos.Y + NodeHeight;
                    double x2 = toPos.X + NodeWidth / 2;
                    double y2 = toPos.Y;

                    var line = new Line
                    {
                        X1 = x1,
                        Y1 = y1,
                        X2 = x2,
                        Y2 = y2,
                        Stroke = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                        StrokeThickness = 1.5,
                    };
                    canvas.Children.Add(line);

                    // Strzałka (trójkąt)
                    DrawArrowHead(canvas, x1, y1, x2, y2);
                }
            }
        }

        private static void DrawArrowHead(Canvas canvas, double x1, double y1, double x2, double y2)
        {
            double arrowSize = 8;
            double angle = Math.Atan2(y2 - y1, x2 - x1);

            var p1 = new Point(x2, y2);
            var p2 = new Point(
                x2 - arrowSize * Math.Cos(angle - Math.PI / 6),
                y2 - arrowSize * Math.Sin(angle - Math.PI / 6)
            );
            var p3 = new Point(
                x2 - arrowSize * Math.Cos(angle + Math.PI / 6),
                y2 - arrowSize * Math.Sin(angle + Math.PI / 6)
            );

            var arrow = new Polygon
            {
                Points = [p1, p2, p3],
                Fill = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
            };
            canvas.Children.Add(arrow);
        }

        private static void DrawNodes(Canvas canvas, Graph graph, Dictionary<int, Point> positions)
        {
            foreach (var (idx, pos) in positions)
            {
                var task = graph.Tasks[idx];
                var color = TaskColors.GetValueOrDefault(task.TaskType, Colors.Gray);

                // Prostokąt z zaokrąglonymi rogami
                var rect = new Rectangle
                {
                    Width = NodeWidth,
                    Height = NodeHeight,
                    RadiusX = 6,
                    RadiusY = 6,
                    Fill = new SolidColorBrush(color),
                    Stroke = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                    StrokeThickness = 1.5,
                };
                Canvas.SetLeft(rect, pos.X);
                Canvas.SetTop(rect, pos.Y);
                canvas.Children.Add(rect);

                // Nazwa zadania
                var label = new TextBlock
                {
                    Text = task.Name,
                    Foreground = Brushes.White,
                    FontWeight = FontWeights.Bold,
                    FontSize = 12,
                    TextAlignment = TextAlignment.Center,
                };

                // Wycentruj tekst w prostokącie
                label.Measure(new Size(NodeWidth, NodeHeight));
                double textX = pos.X + (NodeWidth - label.DesiredSize.Width) / 2;
                double textY = pos.Y + (NodeHeight - label.DesiredSize.Height) / 2;
                Canvas.SetLeft(label, textX);
                Canvas.SetTop(label, textY);
                canvas.Children.Add(label);
            }
        }
    }
}