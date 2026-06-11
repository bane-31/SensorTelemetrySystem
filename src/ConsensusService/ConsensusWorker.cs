using IngestionService.Entities;
using IngestionService.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedContracts.Enums;

namespace ConsensusService
{
    public class ConsensusWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ConsensusWorker> _logger;

        private const int MinimumSensorsForConsensus = 3;
        private const int SuspiciousLimitBeforeBad = 3;

        private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

        public ConsensusWorker(IServiceScopeFactory scopeFactory, ILogger<ConsensusWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("ConsensusWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessPreviousMinuteAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while calculating consensus.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task ProcessPreviousMinuteAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;

            var currentMinuteStart = new DateTime(
                now.Year,
                now.Month,
                now.Day,
                now.Hour,
                now.Minute,
                0,
                DateTimeKind.Utc);

            var windowStart = currentMinuteStart.AddMinutes(-1);
            var windowEnd = currentMinuteStart;

            var alreadyProcessed = await context.ConsensusResults
                .AnyAsync(c =>
                    c.WindowStart == windowStart &&
                    c.WindowEnd == windowEnd,
                    cancellationToken);

            if (alreadyProcessed)
            {
                return;
            }

            var metrics = await context.Metrics
                .Include(m => m.Sensor)
                .Where(m =>
                    !m.IsConsensus &&
                    m.Timestamp >= windowStart &&
                    m.Timestamp < windowEnd &&
                    m.Sensor.Quality == SensorQuality.GOOD)
                .ToListAsync(cancellationToken);

            if (!metrics.Any())
            {
                _logger.LogInformation(
                    "No metrics found for consensus window {WindowStart} - {WindowEnd}.",
                    windowStart,
                    windowEnd);

                return;
            }

            var sensorValues = metrics
                .GroupBy(m => m.SensorId)
                .Select(g => new SensorMinuteValue
                {
                    SensorId = g.Key,
                    Value = g.Average(m => m.Value),
                    SamplesCount = g.Count()
                })
                .ToList();

            if (sensorValues.Count < MinimumSensorsForConsensus)
            {
                _logger.LogWarning(
                    "Not enough GOOD sensors for consensus. Found {Count}, required {Required}. Window: {WindowStart} - {WindowEnd}",
                    sensorValues.Count,
                    MinimumSensorsForConsensus,
                    windowStart,
                    windowEnd);

                return;
            }

            var median = CalculateMedian(sensorValues.Select(s => s.Value).ToList());

            var deviations = sensorValues
                .Select(s => Math.Abs(s.Value - median))
                .ToList();

            var medianDeviation = CalculateMedian(deviations);

            var allowedDeviation = Math.Max(20.0, medianDeviation * 3.0);

            var includedSensors = new List<SensorMinuteValue>();
            var excludedSensors = new List<SensorMinuteValue>();

            foreach (var sensorValue in sensorValues)
            {
                var deviation = Math.Abs(sensorValue.Value - median);

                if (deviation > allowedDeviation)
                {
                    excludedSensors.Add(sensorValue);
                }
                else
                {
                    includedSensors.Add(sensorValue);
                }
            }

            if (includedSensors.Count < MinimumSensorsForConsensus)
            {
                _logger.LogWarning(
                    "Consensus rejected because too many sensors were excluded. Included: {Included}, Excluded: {Excluded}.",
                    includedSensors.Count,
                    excludedSensors.Count);

                return;
            }

            var consensusValue = CalculateMedian(includedSensors.Select(s => s.Value).ToList());

            var consensusResult = new ConsensusResult
            {
                WindowStart = windowStart,
                WindowEnd = windowEnd,
                Value = Math.Round(consensusValue, 2),
                IncludedSensorsCount = includedSensors.Count,
                ExcludedSensorsCount = excludedSensors.Count,
                CreatedAt = DateTime.UtcNow
            };

            context.ConsensusResults.Add(consensusResult);

            await UpdateSensorQualityAsync(
                context,
                includedSensors,
                excludedSensors,
                allowedDeviation,
                median,
                cancellationToken);

            await context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Consensus calculated for {WindowStart} - {WindowEnd}. Value: {Value}. Included: {Included}, Excluded: {Excluded}.",
                windowStart,
                windowEnd,
                consensusResult.Value,
                consensusResult.IncludedSensorsCount,
                consensusResult.ExcludedSensorsCount);
        }

        private async Task UpdateSensorQualityAsync(
            AppDbContext context,
            List<SensorMinuteValue> includedSensors,
            List<SensorMinuteValue> excludedSensors,
            double allowedDeviation,
            double median,
            CancellationToken cancellationToken)
        {
            var includedIds = includedSensors.Select(s => s.SensorId).ToList();
            var excludedIds = excludedSensors.Select(s => s.SensorId).ToList();

            var includedSensorEntities = await context.Sensors
                .Where(s => includedIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            foreach (var sensor in includedSensorEntities)
            {
                if (sensor.SuspiciousCount > 0)
                {
                    sensor.SuspiciousCount--;
                }
            }

            var excludedSensorEntities = await context.Sensors
                .Where(s => excludedIds.Contains(s.Id))
                .ToListAsync(cancellationToken);

            foreach (var sensor in excludedSensorEntities)
            {
                var sensorValue = excludedSensors.First(s => s.SensorId == sensor.Id);
                var deviation = Math.Abs(sensorValue.Value - median);

                sensor.SuspiciousCount++;

                _logger.LogWarning(
                    "Sensor {SensorId} suspicious. Value: {Value}, Median: {Median}, Deviation: {Deviation}, AllowedDeviation: {AllowedDeviation}, SuspiciousCount: {SuspiciousCount}.",
                    sensor.Id,
                    Math.Round(sensorValue.Value, 2),
                    Math.Round(median, 2),
                    Math.Round(deviation, 2),
                    Math.Round(allowedDeviation, 2),
                    sensor.SuspiciousCount);

                if (sensor.SuspiciousCount >= SuspiciousLimitBeforeBad)
                {
                    sensor.Quality = SensorQuality.BAD;
                    sensor.IsActive = false;

                    _logger.LogWarning(
                        "Sensor {SensorId} marked as BAD because it repeatedly deviated from consensus.",
                        sensor.Id);
                }
            }
        }

        private static double CalculateMedian(List<double> values)
        {
            if (values.Count == 0)
            {
                throw new InvalidOperationException("Cannot calculate median for an empty list.");
            }

            var sorted = values.OrderBy(v => v).ToList();
            var middle = sorted.Count / 2;

            if (sorted.Count % 2 == 1)
            {
                return sorted[middle];
            }

            return (sorted[middle - 1] + sorted[middle]) / 2.0;
        }

        private class SensorMinuteValue
        {
            public Guid SensorId { get; set; }

            public double Value { get; set; }

            public int SamplesCount { get; set; }
        }
    }
}