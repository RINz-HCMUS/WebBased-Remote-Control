using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebApp.Hubs;
using System.Net;
using System.Net.NetworkInformation;

var builder = WebApplication.CreateBuilder(args);
var localIp = GetLocalIPv4();
if (localIp == null)
{
    localIp = "127.0.0.1";
}

// Bind directly to the LAN IP address to allow connections from other machines
builder.WebHost.UseUrls($"http://{localIp}:5000");
// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddSignalR(options =>
{
    // Increase max message size to handle large base64 image strings (10 MB)
    options.MaximumReceiveMessageSize = 10 * 1024 * 1024;
});

var app = builder.Build();

Console.WriteLine($"WebApp started: http://{localIp}:5000");
Console.WriteLine($"WebApp LAN address: http://{localIp}:5000");

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();
app.MapHub<RemoteControlHub>("/remoteHub");

app.Run();

static string GetLocalIPv4()
{
    var interfaces = NetworkInterface.GetAllNetworkInterfaces()
        .Where(i => i.OperationalStatus == OperationalStatus.Up &&
                    i.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    i.NetworkInterfaceType != NetworkInterfaceType.Tunnel);

    // Prioritize Wi-Fi adapters
    var wifiInterfaces = interfaces.Where(i => i.Name.ToLower().Contains("wi-fi") || i.Name.ToLower().Contains("wireless"));
    var preferredInterfaces = wifiInterfaces.Any() ? wifiInterfaces : interfaces;

    foreach (var netInterface in preferredInterfaces)
    {
        var properties = netInterface.GetIPProperties();
        if (properties == null)
            continue;

        foreach (var addr in properties.UnicastAddresses)
        {
            if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                !IPAddress.IsLoopback(addr.Address))
            {
                var ip = addr.Address.ToString();
                if (ip.StartsWith("192.168.") || ip.StartsWith("10.") || ip.StartsWith("172."))
                    return ip;
            }
        }
    }

    var hostEntry = Dns.GetHostEntry(Dns.GetHostName());
    var fallbackIp = hostEntry.AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
    return fallbackIp?.ToString() ?? "127.0.0.1";
}
