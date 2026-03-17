using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;

namespace WebApp.Hubs
{
    public class RemoteControlHub : Hub
    {
        // Thread-safe dictionary: ConnectionId -> MachineName
        public static readonly ConcurrentDictionary<string, string> ConnectedAgents = new();

        public override Task OnConnectedAsync()
        {
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (ConnectedAgents.TryRemove(Context.ConnectionId, out var machineName))
            {
                Clients.All.SendAsync("AgentDisconnected", Context.ConnectionId, machineName);
            }
            return base.OnDisconnectedAsync(exception);
        }

        // Agent calls this to register
        public async Task RegisterAgent(string machineName)
        {
            ConnectedAgents.TryAdd(Context.ConnectionId, machineName);
            await Clients.All.SendAsync("AgentConnected", Context.ConnectionId, machineName);
        }

        // Admin calls this on load to get currently connected agents
        public async Task GetConnectedAgents()
        {
            var agentsList = ConnectedAgents.Select(x => new { id = x.Key, name = x.Value }).ToList();
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

        // --- NEW: File Download ---
        public async Task RequestFileDownload(string targetConnectionId, string filePath)
        {
            await Clients.Client(targetConnectionId).SendAsync("DownloadFileCommand", filePath);
        }

        public async Task SendFileResult(string adminConnectionId, string fileName, string base64Data)
        {
            await Clients.All.SendAsync("ReceiveFile", Context.ConnectionId, fileName, base64Data);
        }

        // --- NEW: Webcam Stream ---
        public async Task RequestStartWebcam(string targetConnectionId)
        {
            await Clients.Client(targetConnectionId).SendAsync("StartWebcamCommand");
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
    }
}
