using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

// to tylko rysuje wykres


namespace etg
{
    public static class GanttRenderer
    {
        private const double RowHeight = 50;
        private const double RowSpacing = 10;
        private const double LabelWidth = 60;
        private const double PaddingTop = 40;
        private const double PaddingLeft = 10;
        private const double PaddingBottom = 40;
        private const double MinPixelsPerUnit = 8;

        private static readonly Dictionary<TaskType, Color> TaskColors = new() {
            { TaskType.GT,  Color.FromRgb(0x42, 0x9E, 0xF5) },
            { TaskType.UT,  Color.FromRgb(0xAB, 0x47, 0xBC) },
            { TaskType.DT,  Color.FromRgb(0xFF, 0x98, 0x00) },
            { TaskType.CDT, Color.FromRgb(0xEF, 0x53, 0x50) },
            { TaskType.CGT, Color.FromRgb(0x66, 0xBB, 0x6A) },
        };

        public static void Draw(Canvas canvas, Graph graph, ScheduleResult result)
        {
            canvas.Children.Clear();

            if (result.ScheduledTasks.Count == 0) return;

            int procCount = graph.Procs.Count;
            int makespan = result.Makespan;

            
            double chartWidth = Math.Max(makespan * MinPixelsPerUnit, 600);
            double scale = chartWidth / makespan;

            double chartStartX = PaddingLeft + LabelWidth;
            double totalWidth = chartStartX + chartWidth + PaddingLeft;
            double totalHeight = PaddingTop + procCount * (RowHeight + RowSpacing) + PaddingBottom;

            canvas.Width = totalWidth;
            canvas.Height = totalHeight;

            DrawTimeAxis(canvas, makespan, scale, chartStartX, procCount);

            //  Wiersze procesorów
            for (int p = 0; p < procCount; p++)
            {
                double rowY = PaddingTop + p * (RowHeight + RowSpacing);

                // Etykieta procesora
                var label = new TextBlock
                {
                    Text = $"P{p}",
                    FontWeight = FontWeights.Bold,
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                };
                label.Measure(new Size(LabelWidth, RowHeight));
                Canvas.SetLeft(label, PaddingLeft + (LabelWidth - label.DesiredSize.Width) / 2);
                Canvas.SetTop(label, rowY + (RowHeight - label.DesiredSize.Height) / 2);
                canvas.Children.Add(label);

                // Typ procesora
                var typeLabel = new TextBlock
                {
                    Text = graph.Procs[p].Specialized == 1 ? "(spec.)" : "(ogólny)",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),
                };
                typeLabel.Measure(new Size(LabelWidth, RowHeight));
                Canvas.SetLeft(typeLabel, PaddingLeft + (LabelWidth - typeLabel.DesiredSize.Width) / 2);
                Canvas.SetTop(typeLabel, rowY + (RowHeight - typeLabel.DesiredSize.Height) / 2 + 14);
                canvas.Children.Add(typeLabel);

                
                var bg = new Rectangle
                {
                    Width = chartWidth,
                    Height = RowHeight,
                    Fill = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)),
                    RadiusX = 3,
                    RadiusY = 3,
                };
                Canvas.SetLeft(bg, chartStartX);
                Canvas.SetTop(bg, rowY);
                canvas.Children.Add(bg);
            }

            // Bloki zadań
            foreach (var st in result.ScheduledTasks)
            {
                double x = chartStartX + st.StartTime * scale;
                double width = st.Duration * scale;

                var color =
                    TaskColors.GetValueOrDefault(
                        st.Task.TaskType,
                        Colors.Gray);

                foreach (var proc in st.ProcIndices)
                {
                    double rowY =
                        PaddingTop +
                        proc * (RowHeight + RowSpacing);

                    DrawTaskBlock(
                        canvas,
                        st,
                        x,
                        width,
                        rowY,
                        color);
                }
            }


            DrawSummary(canvas, result, PaddingTop + procCount * (RowHeight + RowSpacing) + 10, chartStartX);
        }

        private static void DrawTimeAxis(Canvas canvas, int makespan, double scale, double chartStartX, int procCount)
        {
            //  interwał znaczników
            int interval = makespan <= 20 ? 1
                : makespan <= 50 ? 5
                : makespan <= 200 ? 10
                : makespan <= 500 ? 25
                : 50;

            double axisEndY = PaddingTop + procCount * (RowHeight + RowSpacing);

            for (int t = 0; t <= makespan; t += interval)
            {
                double x = chartStartX + t * scale;

                // Znacznik na górze
                var tick = new TextBlock
                {
                    Text = t.ToString(),
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)),
                };

                tick.Measure(new Size(50, 20));
                Canvas.SetLeft(tick, x - tick.DesiredSize.Width / 2);
                Canvas.SetTop(tick, PaddingTop - 20);
                canvas.Children.Add(tick);

                // Linia pionowa (siatka)
                var gridLine = new Line
                {
                    X1 = x,
                    Y1 = PaddingTop,
                    X2 = x,
                    Y2 = axisEndY,
                    Stroke = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD)),
                    StrokeThickness = 0.5,
                };
                canvas.Children.Add(gridLine);
            }
        }

        private static void DrawSummary(Canvas canvas, ScheduleResult result, double y, double x)
        {
            var summary = new TextBlock
            {
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
            };
            summary.Inlines.Add(new System.Windows.Documents.Run("Makespan: ") { FontWeight = FontWeights.Bold });
            summary.Inlines.Add(new System.Windows.Documents.Run($"{result.Makespan}    "));
            summary.Inlines.Add(new System.Windows.Documents.Run("Koszt całkowity: ") { FontWeight = FontWeights.Bold });
            summary.Inlines.Add(new System.Windows.Documents.Run($"{result.TotalCost}"));
            Canvas.SetLeft(summary, x);
            Canvas.SetTop(summary, y);
            canvas.Children.Add(summary);
        }


        private static void DrawTaskBlock(Canvas canvas, ScheduledTask st, double x, double width, double rowY, Color color)
        {
            var rect = new Rectangle
            {
                Width = Math.Max(width - 2, 1),
                Height = RowHeight - 6,

                RadiusX = 4,
                RadiusY = 4,

                Fill = new SolidColorBrush(color),

                Stroke =
                    new SolidColorBrush(
                        DarkenColor(color, 0.3)),

                StrokeThickness = 1
            };

            Canvas.SetLeft(rect, x + 1);
            Canvas.SetTop(rect, rowY + 3);

            canvas.Children.Add(rect);

            var taskLabel = new TextBlock
            {
                Text = st.Task.Name,

                Foreground = Brushes.White,

                FontWeight = FontWeights.Bold,

                FontSize = 11,

                TextAlignment = TextAlignment.Center
            };

            taskLabel.Measure(new Size(width, RowHeight));

            if (taskLabel.DesiredSize.Width < width - 4)
            {
                Canvas.SetLeft(
                    taskLabel,
                    x + (width - taskLabel.DesiredSize.Width) / 2);

                Canvas.SetTop(
                    taskLabel,
                    rowY +
                    (RowHeight - taskLabel.DesiredSize.Height) / 2 - 6);

                canvas.Children.Add(taskLabel);
            }

            var timeLabel = new TextBlock
            {
                Text = $"{st.StartTime}-{st.EndTime}",

                Foreground =
                    new SolidColorBrush(
                        Color.FromRgb(255, 255, 220)),

                FontSize = 9,

                TextAlignment = TextAlignment.Center
            };

            timeLabel.Measure(
                new Size(width, RowHeight));

            if (timeLabel.DesiredSize.Width < width - 4)
            {
                Canvas.SetLeft(
                    timeLabel,
                    x + (width - timeLabel.DesiredSize.Width) / 2);

                Canvas.SetTop(
                    timeLabel,
                    rowY +
                    (RowHeight - timeLabel.DesiredSize.Height) / 2 + 8);

                canvas.Children.Add(timeLabel);
            }
        }




        private static Color DarkenColor(Color color, double factor)
        {
            return Color.FromRgb(
                (byte)(color.R * (1 - factor)),
                (byte)(color.G * (1 - factor)),
                (byte)(color.B * (1 - factor))
            );
        }
    }
}