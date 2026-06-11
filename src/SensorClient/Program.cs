using System.Net.Http.Json;
using SharedContracts.DTOs;
using SharedContracts.Enums;

Console.Title = "Sensor Client - Dynamic Node";
var random = new Random();

//for testing purposes
var mode = args.Length > 0 ? args[0].ToLower() : "normal";
bool spamMode = mode == "spam";
bool outlierMode = mode == "outlier";
Console.WriteLine($"[MODE] Sensor running in {mode.ToUpper()} mode.");

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

httpClient.DefaultRequestHeaders.Add("X-Sensor-Id", sensorId.ToString());

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
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine($"[ERROR] Failed registration: {error}");
    Console.ResetColor();
    return;
}

Console.WriteLine("[SUCCESS] Sensor is successfuly registered! Initiating telemetry sending...\n");

while (true)
{
    double currentTemp;

    if (outlierMode)
    {
        currentTemp = minValue + (random.NextDouble() * range) + 100;   // Extreme outlier value
    }
    else
    {
        currentTemp = minValue + (random.NextDouble() * range);
    }

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
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [LIMIT] Too many request! Pausing telemetry for 5 seconds...");
            Console.ResetColor();

            await Task.Delay(5000);
            continue;
        }

        if ((int)response.StatusCode == 403)
        {
            Console.ForegroundColor = ConsoleColor.Magenta;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [BLOCKED] Server has denied access (Quarantine).");
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [HIBERNATION] Pausing telemetry for 30 seconds...");
            Console.ResetColor();

            await Task.Delay(30000);
            continue;
        }

        if (!response.IsSuccessStatusCode)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [Error] Denied from server, status: {response.StatusCode}");
            Console.ResetColor();

            await Task.Delay(3000);
            continue;
        }

        Console.ForegroundColor = priority switch
        {
            3 => ConsoleColor.Red,
            2 => ConsoleColor.DarkYellow,
            1 => ConsoleColor.Yellow,
            _ => ConsoleColor.Green
        };

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] MSG: {metricDto.MessageId} | TEMP: {metricDto.Value}°C | PRIORITY: {priority}");
        Console.ResetColor();
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"Error while sending: {ex.Message}");
        await Task.Delay(5000);
        continue;
    }

    Console.ResetColor();

    int delay = spamMode ? 50 : random.Next(1000, 10001);   //50ms in spam mode
    await Task.Delay(delay);
}