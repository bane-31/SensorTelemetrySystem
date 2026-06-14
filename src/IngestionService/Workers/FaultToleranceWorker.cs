using IngestionService.Persistence;
using Microsoft.EntityFrameworkCore;
using SharedContracts.Enums;

namespace IngestionService.Workers
{
    public class FaultToleranceWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FaultToleranceWorker> _logger;

        private const int RequiredActiveSensors = 5;
        private static readonly TimeSpan InactiveThreshold = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan CheckInterval = TimeSpan.FromSeconds(2);

        public FaultToleranceWorker(IServiceScopeFactory scopeFactory, ILogger<FaultToleranceWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("FaultToleranceWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckSensorsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while checking sensor fault tolerance.");
                }

                await Task.Delay(CheckInterval, stoppingToken);
            }
        }

        private async Task CheckSensorsAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var now = DateTime.UtcNow;

            var sensors = await context.Sensors.ToListAsync(cancellationToken);

            var changed = false;

            foreach (var sensor in sensors)
            {
                if (sensor.IsBlocked && sensor.BlockedUntil.HasValue && sensor.BlockedUntil.Value <= now)
                {
                    sensor.IsBlocked = false;
                    sensor.BlockedUntil = null;
                    changed = true;

                    _logger.LogInformation("Sensor {SensorId} unblocked.", sensor.Id);
                }

                if (sensor.IsBlocked && sensor.IsActive)
                {
                    sensor.IsActive = false;
                    changed = true;

                    _logger.LogWarning("Sensor {SensorId} deactivated because it is blocked.", sensor.Id);
                }

                if (sensor.Quality == SensorQuality.BAD && sensor.IsActive)
                {
                    sensor.IsActive = false;
                    changed = true;

                    _logger.LogWarning("Sensor {SensorId} deactivated because it is BAD.", sensor.Id);
                }

                if (sensor.IsActive && now - sensor.LastSeen > InactiveThreshold)
                {
                    sensor.IsActive = false;
                    changed = true;

                    _logger.LogWarning(
                        "Sensor {SensorId} marked inactive. LastSeen: {LastSeen}",
                        sensor.Id,
                        sensor.LastSeen);
                }
            }

            var activeSensors = sensors
                .Where(s =>
                    s.IsActive &&
                    !s.IsBlocked &&
                    s.Quality != SensorQuality.BAD)
                .OrderByDescending(s => s.LastSeen)
                .ToList();

            if (activeSensors.Count > RequiredActiveSensors)
            {
                var extraSensors = activeSensors
                    .Skip(RequiredActiveSensors)
                    .ToList();

                foreach (var sensor in extraSensors)
                {
                    sensor.IsActive = false;
                    changed = true;

                    _logger.LogWarning("Sensor {SensorId} deactivated because there are more than 5 active sensors.", sensor.Id);
                }
            }

            activeSensors = sensors
                .Where(s =>
                    s.IsActive &&
                    !s.IsBlocked &&
                    s.Quality != SensorQuality.BAD)
                .ToList();

            var missingCount = RequiredActiveSensors - activeSensors.Count;

            if (missingCount > 0)
            {
                var reserveSensors = sensors
                    .Where(s =>
                        !s.IsActive &&
                        !s.IsBlocked &&
                        s.Quality != SensorQuality.BAD &&
                        now - s.LastSeen <= InactiveThreshold)
                    .OrderByDescending(s => s.LastSeen)
                    .Take(missingCount)
                    .ToList();

                foreach (var sensor in reserveSensors)
                {
                    sensor.IsActive = true;
                    changed = true;

                    _logger.LogInformation("Reserve sensor {SensorId} promoted to ACTIVE.", sensor.Id);
                }

                if (reserveSensors.Count < missingCount)
                {
                    _logger.LogWarning(
                        "Not enough available sensors. Active: {ActiveCount}, Required: {RequiredCount}",
                        activeSensors.Count + reserveSensors.Count,
                        RequiredActiveSensors);
                }
            }

            if (changed)
            {
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}