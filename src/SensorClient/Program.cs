using System.Net.Http.Json;
using SharedContracts.DTOs;
using SharedContracts.Enums;

Console.Title = "Sensor Client - Dynamic Node";
var random = new Random();

var sensorId = Guid.NewGuid();
double minValue = random.Next(-20, 0);
double maxValue = random.Next(50, 100);

double range = maxValue - minValue;
double yellowThresholdHigh = minValue + (range * 0.70);
double yellowThresholdLow = maxValue - (range * 0.70);
double orangeThresholdHigh = minValue + (range * 0.85);
double orangeThresholdLow = maxValue - (range * 0.85);
double redThresholdHigh = minValue + (range * 0.95);
double redThresholdLow = maxValue - (range * 0.95);

Console.WriteLine($"=== Sensor {sensorId} initialized ===");
Console.WriteLine($"Value range: {minValue}°C to {maxValue}°C");
Console.WriteLine($"Threshold: Yellow(<{Math.Round(yellowThresholdLow, 1)} or >{Math.Round(yellowThresholdHigh, 1)})," +
    $" Orange(<{Math.Round(orangeThresholdLow, 1)} or >{Math.Round(orangeThresholdHigh, 1)})," +
    $" Red(<{Math.Round(redThresholdLow, 1)} or >{Math.Round(redThresholdHigh, 1)})");

using var httpClient = new HttpClient();
httpClient.BaseAddress = new Uri("http://localhost:5128");

long messageCounter = 1;

var registerDto = new SensorRegisterDTO
{
    Id = sensorId,
    MinValue = minValue,
    MaxValue = maxValue,
    Quality = SensorQuality.GOOD
};

Console.WriteLine($"[REGISTRATION] Attempting sensor registration: {sensorId}");
var regResponse = await httpClient.PostAsJsonAsync("/api/sensors", registerDto);

if (!regResponse.IsSuccessStatusCode)
{
    var error = await regResponse.Content.ReadAsStringAsync();
    Console.WriteLine($"[ERROR] Failed registration: {error}");
}

Console.WriteLine("[SUCCESS] Sensor is successfuly registered! Initiating telemetry sending...\n");

while (true)
{
    double currentTemp = minValue + (random.NextDouble() * range);

    int priority = 0;
    if (currentTemp < redThresholdLow || currentTemp > redThresholdHigh) priority = 3;
    else if (currentTemp < orangeThresholdLow || currentTemp > orangeThresholdHigh) priority = 2;
    else if (currentTemp < yellowThresholdLow || currentTemp > yellowThresholdHigh) priority = 1;

    var metricDto = new MetricSubmissionDTO
    {
        MessageId = messageCounter++,
        SensorId = sensorId,
        Value = Math.Round(currentTemp, 2),
        Timestamp = DateTime.UtcNow,
        Priority = priority
    };

    try
    {
        var response = await httpClient.PostAsJsonAsync("/api/metrics", metricDto);

        if ((int)response.StatusCode == 429)
        {
            Console.WriteLine("[LIMIT] Limit reached! Pausing...");
            await Task.Delay(5000);
        }

        Console.ForegroundColor = priority switch
        {
            3 => ConsoleColor.Red,
            2 => ConsoleColor.DarkYellow,
            1 => ConsoleColor.Yellow,
            _ => ConsoleColor.Green
        };

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] MSG: {metricDto.MessageId} | TEMP: {metricDto.Value}°C | PRIORITY: {priority}");
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"Error while sending: {ex.Message}");
    }

    Console.ResetColor();

    int delay = random.Next(1000, 10001);
    await Task.Delay(delay);
}