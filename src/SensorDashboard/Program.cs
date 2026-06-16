using Microsoft.AspNetCore.SignalR.Client;
using SharedContracts.DTOs;
using System;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace SensorDashboard
{
    internal class Program
    {
        private static HubConnection? _hubConnection;
        private static readonly HttpClient _httpClient = new HttpClient();

        private static readonly string IngestionApiUrl =
    (Environment.GetEnvironmentVariable("INGESTION_URL") ?? "http://localhost:5128") + "/api";

        private static readonly string NotificationHubUrl =
            Environment.GetEnvironmentVariable("NOTIFICATION_URL") ?? "http://localhost:5112/alarmHub";

        static async Task Main(string[] args)
        {
            Console.Title = "Operator Control Station - UI Dashboard";
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("====================================================");
            Console.WriteLine("    STARTING OPERATOR CONTROL STATION DASHBOARD     ");
            Console.WriteLine("====================================================");
            Console.ResetColor();

            _hubConnection = new HubConnectionBuilder()
                .WithUrl(NotificationHubUrl)
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.On<MetricSubmissionDTO>("ReceiveAlarm", (alarm) =>
            {
                int currentLeft = Console.CursorLeft;
                int currentTop = Console.CursorTop;

                Console.ForegroundColor = alarm.Priority switch
                {
                    3 => ConsoleColor.Red,          
                    2 => ConsoleColor.DarkYellow,   
                    1 => ConsoleColor.Yellow,      
                    _ => ConsoleColor.White
                };

                Console.WriteLine($"\n\a[ALERT RECEIVED] [{DateTime.Now:HH:mm:ss}] Telemetry boundary breach detected!");
                Console.WriteLine($"-> Sensor Node ID: {alarm.SensorId}");
                Console.WriteLine($"-> Triggered Value: {alarm.Value}°C");
                Console.WriteLine($"-> Assigned Priority: P{alarm.Priority}");
                Console.ResetColor();
                Console.Write("\nSelect option: ");

                if (currentLeft >= 0 && currentTop >= 0 && currentTop < Console.BufferHeight)
                {
                    Console.SetCursorPosition(currentLeft, currentTop);
                }
            });

            try
            {
                await _hubConnection.StartAsync();
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("[SUCCESS] Real-time monitoring linked via SignalR web-socket.");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[CRITICAL] Failed to establish real-time socket connection: {ex.Message}");
                Console.ResetColor();
            }

            while (true)
            {
                PrintMenu();
                string? choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await FetchActiveSensorsOverviewAsync();
                        break;
                    case "2":
                        await FetchHistoricalConsensusReportAsync();
                        break;
                    case "3":
                        Console.Clear();
                        Console.WriteLine("Console stream cleared and logs reset.");
                        break;
                    case "4":
                        Console.WriteLine("Shutting down Dashboard. Disconnecting socket...");
                        await _hubConnection.DisposeAsync();
                        return;
                    default:
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("Invalid command sequence. Try again.");
                        Console.ResetColor();
                        break;
                }
            }
        }

        private static void PrintMenu()
        {
            Console.WriteLine("\n====================================================");
            Console.WriteLine("              DASHBOARD MANAGEMENT MENU             ");
            Console.WriteLine("====================================================");
            Console.WriteLine("1. View Active Infrastructure Sensors Overview");
            Console.WriteLine("2. Fetch Historical System Consensus Logs (CQRS)");
            Console.WriteLine("3. Clear Screen & Refresh Logs");
            Console.WriteLine("4. Exit Station");
            Console.Write("Select option: ");
        }

        private static async Task FetchActiveSensorsOverviewAsync()
        {
            try
            {
                Console.Clear();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("=== INFRASTRUCTURE NODE MONITORING OVERVIEW ===");
                Console.ResetColor();

                Console.WriteLine("--------------------------------------------------------------------------------------------------");
                Console.WriteLine($"{"Sensor UUID",-38} | {"State",-10} | {"Quality",-10} | {"Blocked",-10}");
                Console.WriteLine("--------------------------------------------------------------------------------------------------");

                var sensors =
                    await _httpClient.GetFromJsonAsync<List<dynamic>>
                    (
                        $"{IngestionApiUrl}/sensors"
                    );

                if (sensors == null || sensors.Count == 0)
                {
                    Console.WriteLine("No sensors found.");
                    return;
                }

                foreach (var sensor in sensors)
                {
                    Console.WriteLine(sensor);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Failed to load sensors: {ex.Message}");
                Console.ResetColor();
            }
        }
        private static async Task FetchHistoricalConsensusReportAsync()
        {
            try
            {
                Console.Clear();

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("=== HISTORICAL BFT SYSTEM CONSENSUS WINDOW LOGS ===");
                Console.ResetColor();

                var results =
                    await _httpClient.GetFromJsonAsync<List<ConsensusResultDto>>
                    (
                        $"{IngestionApiUrl}/consensus"
                    );

                if (results == null || results.Count == 0)
                {
                    Console.WriteLine("No consensus results found.");
                    return;
                }

                Console.WriteLine("-------------------------------------------------------------------------------------");
                Console.WriteLine($"{"Window",-25} | {"Consensus",-12} | {"Included",-10} | {"Excluded",-10}");
                Console.WriteLine("-------------------------------------------------------------------------------------");

                foreach (var item in results)
                {
                    Console.WriteLine(
                        $"{item.WindowStart:HH:mm:ss}-{item.WindowEnd:HH:mm:ss} | " +
                        $"{item.Value,-12:F2} | " +
                        $"{item.IncludedSensorsCount,-10} | " +
                        $"{item.ExcludedSensorsCount,-10}");
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] Failed to load consensus results: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
}