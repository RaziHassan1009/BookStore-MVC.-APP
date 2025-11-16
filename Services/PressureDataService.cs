using GrapheneSensore.Data;
using GrapheneSensore.Models;
using GrapheneSensore.Configuration;
using GrapheneSensore.Logging;
using GrapheneSensore.Validation;
using GrapheneSensore.Constants;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace GrapheneSensore.Services
{
    /// <summary>
    /// Service for managing pressure map data operations
    /// </summary>
    public class PressureDataService
    {
        private readonly int _matrixSize;
        private readonly int _pressureThreshold;
        private readonly int _minAreaPixels;
        private readonly int _lowerThreshold;
        private readonly Logger _logger;

        public PressureDataService()
        {
            var config = AppConfiguration.Instance;
            _matrixSize = config.MatrixSize;
            _pressureThreshold = config.PressureThreshold;
            _minAreaPixels = config.MinAreaPixels;
            _lowerThreshold = config.LowerThreshold;
            _logger = Logger.Instance;
        }

        /// <summary>
        /// Retrieves pressure map data for a user within an optional date range
        /// </summary>
        public async Task<List<PressureMapData>> GetUserDataAsync(Guid userId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // Validate date range if provided
                if (startDate.HasValue && endDate.HasValue)
                {
                    var (isValid, message) = InputValidator.ValidateDateRange(startDate, endDate);
                    if (!isValid)
                    {
                        _logger.LogWarning($"Invalid date range: {message}", "PressureDataService");
                        throw new ArgumentException(message);
                    }
                }

                using var context = new SensoreDbContext();
                var query = context.PressureMapData
                    .Where(p => p.UserId == userId);

                if (startDate.HasValue)
                    query = query.Where(p => p.RecordedDateTime >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(p => p.RecordedDateTime <= endDate.Value);

                var data = await query.OrderByDescending(p => p.RecordedDateTime).ToListAsync();
                
                // Deserialize matrix data with error handling
                foreach (var item in data)
                {
                    item.Matrix = DeserializeMatrix(item.MatrixData);
                    if (item.Matrix == null)
                    {
                        _logger.LogWarning($"Failed to deserialize matrix for DataId: {item.DataId}", "PressureDataService");
                    }
                }

                _logger.LogInfo($"Retrieved {data.Count} frames for user {userId}", "PressureDataService");
                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving user data for {userId}", ex, "PressureDataService");
                throw;
            }
        }

        /// <summary>
        /// Retrieves a specific pressure map frame by ID
        /// </summary>
        public async Task<PressureMapData?> GetFrameByIdAsync(long dataId)
        {
            try
            {
                using var context = new SensoreDbContext();
                var data = await context.PressureMapData.FindAsync(dataId);
                
                if (data != null)
                {
                    data.Matrix = DeserializeMatrix(data.MatrixData);
                    if (data.Matrix == null)
                    {
                        _logger.LogWarning($"Matrix data is null for DataId: {dataId}", "PressureDataService");
                    }
                }

                return data;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving frame {dataId}", ex, "PressureDataService");
                throw;
            }
        }

        /// <summary>
        /// Imports pressure map data from CSV file
        /// </summary>
        public async Task<long> ImportCsvDataAsync(string filePath, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            }
            
            if (userId == Guid.Empty)
            {
                throw new ArgumentException("User ID cannot be empty", nameof(userId));
            }
            
            try
            {
                _logger.LogInfo($"Starting CSV import from {filePath} for user {userId}", "PressureDataService");

                // Validate file exists
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"CSV file not found: {filePath}");
                }

                // Validate file name format
                var fileName = Path.GetFileName(filePath);
                var (isValid, message) = InputValidator.ValidateCsvFileName(fileName);
                if (!isValid)
                {
                    throw new ArgumentException(message);
                }

                var lines = await File.ReadAllLinesAsync(filePath);
                if (lines.Length == 0)
                {
                    throw new InvalidDataException("CSV file is empty");
                }

                var frames = new List<PressureMapData>();
                
                // Extract date from filename (format: userid_YYYYMMDD.csv)
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                var datePart = fileNameWithoutExt.Split('_')[1];
                var baseDate = DateTime.ParseExact(datePart, "yyyyMMdd", null);

                int frameNumber = 0;
                int successfulFrames = 0;
                int failedFrames = 0;

                for (int i = 0; i < lines.Length; i += _matrixSize)
                {
                    if (i + _matrixSize > lines.Length)
                    {
                        _logger.LogWarning($"Incomplete frame at line {i}, skipping", "PressureDataService");
                        break;
                    }

                    try
                    {
                        var matrix = new int[_matrixSize, _matrixSize];
                        
                        for (int row = 0; row < _matrixSize; row++)
                        {
                            var values = lines[i + row].Split(',');
                            if (values.Length < _matrixSize)
                            {
                                throw new InvalidDataException($"Row {i + row} has insufficient columns: {values.Length} (expected {_matrixSize})");
                            }

                            for (int col = 0; col < _matrixSize; col++)
                            {
                                if (int.TryParse(values[col].Trim(), out int value))
                                {
                                    if (value < 0 || value > 255)
                                    {
                                        _logger.LogWarning($"Invalid pressure value {value} at row {row}, col {col}. Clamping to valid range.", "PressureDataService");
                                        value = Math.Clamp(value, 0, 255);
                                    }
                                    matrix[row, col] = value;
                                }
                                else
                                {
                                    _logger.LogWarning($"Failed to parse value '{values[col]}' at row {row}, col {col}. Using 0.", "PressureDataService");
                                    matrix[row, col] = 0;
                                }
                            }
                        }

                        // Validate matrix
                        var (matrixValid, matrixMessage) = InputValidator.ValidateMatrix(matrix);
                        if (!matrixValid)
                        {
                            _logger.LogWarning($"Frame {frameNumber} validation failed: {matrixMessage}", "PressureDataService");
                            failedFrames++;
                            frameNumber++;
                            continue;
                        }

                        var frameData = new PressureMapData
                        {
                            UserId = userId,
                            RecordedDateTime = baseDate.AddSeconds(frameNumber * 2), // Assuming 2 seconds per frame
                            FrameNumber = frameNumber,
                            MatrixData = SerializeMatrix(matrix),
                            Matrix = matrix
                        };

                        // Calculate metrics
                        CalculateMetrics(frameData);
                        
                        // Check for alerts
                        CheckForAlerts(frameData);

                        frames.Add(frameData);
                        successfulFrames++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"Error processing frame {frameNumber}", ex, "PressureDataService");
                        failedFrames++;
                    }
                    finally
                    {
                        frameNumber++;
                    }
                }

                if (frames.Count == 0)
                {
                    throw new InvalidDataException($"No valid frames found in CSV file. Failed frames: {failedFrames}");
                }

                // Save to database in batches
                var batchSize = AppConfiguration.Instance.DataImportBatchSize;
                using var context = new SensoreDbContext();
                
                for (int i = 0; i < frames.Count; i += batchSize)
                {
                    var batch = frames.Skip(i).Take(batchSize).ToList();
                    await context.PressureMapData.AddRangeAsync(batch);
                    await context.SaveChangesAsync();
                    _logger.LogInfo($"Saved batch {i / batchSize + 1} ({batch.Count} frames)", "PressureDataService");
                }

                // Create alerts if needed
                await CreateAlertsAsync(frames);

                _logger.LogInfo($"Import completed: {successfulFrames} successful, {failedFrames} failed", "PressureDataService");
                return frames.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError($"CSV import failed for {filePath}", ex, "PressureDataService");
                throw;
            }
        }

        public void CalculateMetrics(PressureMapData data)
        {
            if (data.Matrix == null)
                return;

            // Calculate Peak Pressure Index
            data.PeakPressure = CalculatePeakPressure(data.Matrix);

            // Calculate Contact Area %
            data.ContactAreaPercentage = CalculateContactArea(data.Matrix);
        }

        private int CalculatePeakPressure(int[,] matrix)
        {
            if (matrix == null)
            {
                _logger.LogWarning("Matrix is null in CalculatePeakPressure", "PressureDataService");
                return 0;
            }

            var highPressureRegions = new List<(int value, int count)>();

            // Find contiguous high-pressure regions
            bool[,] visited = new bool[_matrixSize, _matrixSize];
            
            for (int i = 0; i < _matrixSize; i++)
            {
                for (int j = 0; j < _matrixSize; j++)
                {
                    if (!visited[i, j] && matrix[i, j] > _lowerThreshold)
                    {
                        var (maxValue, pixelCount) = FloodFill(matrix, visited, i, j);
                        if (pixelCount >= _minAreaPixels)
                        {
                            highPressureRegions.Add((maxValue, pixelCount));
                        }
                    }
                }
            }

            return highPressureRegions.Any() ? highPressureRegions.Max(r => r.value) : 0;
        }

        private (int maxValue, int count) FloodFill(int[,] matrix, bool[,] visited, int row, int col)
        {
            if (row < 0 || row >= _matrixSize || col < 0 || col >= _matrixSize)
                return (0, 0);

            if (visited[row, col] || matrix[row, col] <= _lowerThreshold)
                return (0, 0);

            visited[row, col] = true;
            int maxValue = matrix[row, col];
            int count = 1;

            // Check 4 adjacent cells
            var directions = new[] { (-1, 0), (1, 0), (0, -1), (0, 1) };
            foreach (var (dr, dc) in directions)
            {
                var (val, cnt) = FloodFill(matrix, visited, row + dr, col + dc);
                maxValue = Math.Max(maxValue, val);
                count += cnt;
            }

            return (maxValue, count);
        }

        private decimal CalculateContactArea(int[,] matrix)
        {
            if (matrix == null)
            {
                _logger.LogWarning("Matrix is null in CalculateContactArea", "PressureDataService");
                return 0;
            }

            int totalPixels = _matrixSize * _matrixSize;
            int contactPixels = 0;

            for (int i = 0; i < _matrixSize; i++)
            {
            for (int j = 0; j < _matrixSize; j++)
                {
                    if (matrix[i, j] > _lowerThreshold)
                    {
                        contactPixels++;
                    }
                }
            }

            return (decimal)contactPixels / totalPixels * 100;
        }

        private void CheckForAlerts(PressureMapData data)
        {
            if (data.PeakPressure.HasValue && data.PeakPressure.Value > _pressureThreshold)
            {
                data.HasAlert = true;
                data.AlertMessage = $"High pressure detected: {data.PeakPressure.Value}";
            }
        }

        private async Task CreateAlertsAsync(List<PressureMapData> frames)
        {
            using var context = new SensoreDbContext();
            var alerts = new List<Alert>();

            foreach (var frame in frames.Where(f => f.HasAlert))
            {
                var alert = new Alert
                {
                    DataId = frame.DataId,
                    UserId = frame.UserId,
                    AlertType = AppConstants.ALERT_TYPE_HIGH_PRESSURE,
                    Severity = frame.PeakPressure > AppConstants.CRITICAL_PRESSURE_THRESHOLD 
                        ? AppConstants.SEVERITY_CRITICAL 
                        : AppConstants.SEVERITY_HIGH,
                    Message = frame.AlertMessage,
                    AlertDateTime = frame.RecordedDateTime
                };
                alerts.Add(alert);
            }

            if (alerts.Any())
            {
                await context.Alerts.AddRangeAsync(alerts);
                await context.SaveChangesAsync();
            }
        }

        private string SerializeMatrix(int[,] matrix)
        {
            return JsonConvert.SerializeObject(matrix);
        }

        private int[,]? DeserializeMatrix(string matrixData)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(matrixData))
                {
                    _logger.LogWarning("Matrix data string is null or empty", "PressureDataService");
                    return null;
                }

                var matrix = JsonConvert.DeserializeObject<int[,]>(matrixData);
                
                if (matrix == null)
                {
                    _logger.LogWarning("Deserialized matrix is null", "PressureDataService");
                    return null;
                }

                // Validate deserialized matrix
                var (isValid, message) = InputValidator.ValidateMatrix(matrix);
                if (!isValid)
                {
                    _logger.LogWarning($"Deserialized matrix validation failed: {message}", "PressureDataService");
                    return null;
                }

                return matrix;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to deserialize matrix data", ex, "PressureDataService");
                return null;
            }
        }

        public async Task<List<PressureMapData>> GetRecentFramesAsync(Guid userId, int hours)
        {
            var startDate = DateTime.Now.AddHours(-hours);
            return await GetUserDataAsync(userId, startDate);
        }
    }
}
