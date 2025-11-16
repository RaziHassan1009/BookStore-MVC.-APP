using GrapheneSensore.Configuration;
using GrapheneSensore.Logging;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace GrapheneSensore.Controls
{
    /// <summary>
    /// Custom control for rendering pressure map heat maps
    /// </summary>
    public partial class HeatMapControl : UserControl
    {
        private readonly int _matrixSize;
        private readonly Logger _logger;
        private int[,]? _matrix;

        public static readonly DependencyProperty MatrixDataProperty =
            DependencyProperty.Register(
                nameof(MatrixData),
                typeof(int[,]),
                typeof(HeatMapControl),
                new PropertyMetadata(null, OnMatrixDataChanged));

        public int[,]? MatrixData
        {
            get => (int[,]?)GetValue(MatrixDataProperty);
            set => SetValue(MatrixDataProperty, value);
        }

        public HeatMapControl()
        {
            InitializeComponent();
            _matrixSize = AppConfiguration.Instance.MatrixSize;
            _logger = Logger.Instance;
        }

        private static void OnMatrixDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HeatMapControl control)
            {
                try
                {
                    control._matrix = e.NewValue as int[,];
                    
                    // Validate matrix dimensions if not null
                    if (control._matrix != null)
                    {
                        var rows = control._matrix.GetLength(0);
                        var cols = control._matrix.GetLength(1);
                        
                        if (rows != control._matrixSize || cols != control._matrixSize)
                        {
                            control._logger.LogWarning($"Invalid matrix dimensions: {rows}x{cols}, expected {control._matrixSize}x{control._matrixSize}", "HeatMapControl");
                            control._matrix = null;
                        }
                    }
                    
                    control.RenderHeatMap();
                }
                catch (Exception ex)
                {
                    control._logger.LogError("Error updating heat map data", ex, "HeatMapControl");
                    control._matrix = null;
                    control.RenderHeatMap();
                }
            }
        }

        private void HeatMapCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RenderHeatMap();
        }

        private void RenderHeatMap()
        {
            try
            {
                HeatMapCanvas.Children.Clear();

                if (_matrix == null)
                {
                    // Display "No Data" message
                    DisplayNoDataMessage();
                    return;
                }

                if (HeatMapCanvas.ActualWidth == 0 || HeatMapCanvas.ActualHeight == 0)
                    return;

                double cellWidth = HeatMapCanvas.ActualWidth / _matrixSize;
                double cellHeight = HeatMapCanvas.ActualHeight / _matrixSize;

                for (int row = 0; row < _matrixSize; row++)
                {
                    for (int col = 0; col < _matrixSize; col++)
                    {
                        int value = 0;
                        try
                        {
                            value = _matrix[row, col];
                        }
                        catch (IndexOutOfRangeException)
                        {
                            _logger.LogWarning($"Index out of range at [{row},{col}]", "HeatMapControl");
                            value = 0;
                        }
                        
                        var rect = new Rectangle
                        {
                            Width = cellWidth,
                            Height = cellHeight,
                            Fill = new SolidColorBrush(GetColorForValue(value)),
                            Stroke = Brushes.Transparent,
                            StrokeThickness = 0
                        };

                        Canvas.SetLeft(rect, col * cellWidth);
                        Canvas.SetTop(rect, row * cellHeight);

                        // Add tooltip with pressure value
                        rect.ToolTip = $"Row: {row}, Col: {col}\nPressure: {value}";

                        HeatMapCanvas.Children.Add(rect);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error rendering heat map", ex, "HeatMapControl");
                DisplayErrorMessage();
            }
        }

        private void DisplayNoDataMessage()
        {
            var textBlock = new TextBlock
            {
                Text = "No Data Available",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Canvas.SetLeft(textBlock, (HeatMapCanvas.ActualWidth - 200) / 2);
            Canvas.SetTop(textBlock, (HeatMapCanvas.ActualHeight - 30) / 2);
            HeatMapCanvas.Children.Add(textBlock);
        }

        private void DisplayErrorMessage()
        {
            var textBlock = new TextBlock
            {
                Text = "Error Loading Data",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Red,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            Canvas.SetLeft(textBlock, (HeatMapCanvas.ActualWidth - 200) / 2);
            Canvas.SetTop(textBlock, (HeatMapCanvas.ActualHeight - 30) / 2);
            HeatMapCanvas.Children.Add(textBlock);
        }

        private Color GetColorForValue(int value)
        {
            if (value <= 0)
                return Colors.White;

            // Normalize value to 0-1 range (assuming max pressure is 255)
            double normalized = Math.Min(value / 255.0, 1.0);

            // Color gradient: Blue -> Cyan -> Green -> Yellow -> Red
            if (normalized < 0.25)
            {
                // Blue to Cyan
                double t = normalized / 0.25;
                return Color.FromRgb(0, (byte)(255 * t), 255);
            }
            else if (normalized < 0.5)
            {
                // Cyan to Green
                double t = (normalized - 0.25) / 0.25;
                return Color.FromRgb(0, 255, (byte)(255 * (1 - t)));
            }
            else if (normalized < 0.75)
            {
                // Green to Yellow
                double t = (normalized - 0.5) / 0.25;
                return Color.FromRgb((byte)(255 * t), 255, 0);
            }
            else
            {
                // Yellow to Red
                double t = (normalized - 0.75) / 0.25;
                return Color.FromRgb(255, (byte)(255 * (1 - t)), 0);
            }
        }
    }
}
