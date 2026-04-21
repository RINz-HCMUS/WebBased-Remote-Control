# Web-Based Remote Control & Management System

This repository contains the source code for the "Web-based Remote Control Application" project. The goal of this system is to allow an administrator to manage, monitor, and execute commands on **multiple remote computers simultaneously** through a unified Web Browser dashboard interface—without requiring any standalone admin client software.

This system leverages **ASP.NET Core (Hub-Spoke architecture)** and **SignalR** for low-latency Real-time communication, bypassing traditional 1-to-1 socket limitations.

---

> **⚠️ EDUCATIONAL & LIABILITY DISCLAIMER:**
> This software is developed strictly for **educational purposes, academic research, and authorized internal IT administration**. Features such as Keylogger, Webcam Streaming, and Remote Shell Execution are designed to simulate administrative tools or security testing software. 
> 
> **Important:** Antivirus software may flag the `AgentApp` as suspicious or malware due to its remote-control nature and system-level API usage (e.g., P/Invoke `GetAsyncKeyState`). 
> The author(s) and contributors are not responsible for any illegal use, unauthorized data access, or privacy violations caused by users deploying this software.

---

## Architecture Overview
The system is divided into two main modules:
1. `WebApp` (Central Hub & Dashboard): An ASP.NET Core MVC application. It acts as the SignalR Hub routing commands and provides a sleek web-based dashboard for administration.
2. `AgentApp` (Target Client): A lightweight C# Console application that acts as a background service on target machines. It automatically connects to the Hub and listens for administrative commands to execute system actions.

*For deep technical analysis and architecture diagrams, check the `Bao_Cao_Tong_Quan.md` file.*

## Implemented Features
- [x] **Multi-Device Management:** Auto-detect online/offline devices with connection heartbeat.
- [x] **Process Management:** View running background processes, thread count, and remotely Kill them.
- [x] **Application Management:** Browse graphical applications currently open and start new applications remotely.
- [x] **Screen Capture:** Capture live screenshots of the target's primary display.
- [x] **Keylogger:** Real-time polling of keystrokes relayed instantly to the web console.
- [x] **File Explorer:** Browse remote drives and directories.
- [x] **File Transfer:** Download files from the Agent and Upload files from Admin to the Agent.
- [x] **Webcam Stream:** Access target webcams (multi-cam support) and stream via compressed Base64 images for smooth live view.
- [x] **Remote Terminal:** A hidden command-line shell to execute native bash/cmd/powershell commands remotely.
- [x] **Power Controls:** Force Restart or Shutdown the target computer.

---

## 🛠 Installation & Usage Guide

### Prerequisites
The application utilizes the latest .NET framework to optimize WebSocket transport:
1. **.NET 8.0 SDK**: Required to compile and run the application.
   - [Download .NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
2. **Visual Studio 2022**: Recommended for running multi-project solutions easily.

### Step 1: Open the Project
1. Open Visual Studio.
2. Select **Open a project or solution**, navigate to the source directory, and open `RemoteControl.sln`.

### Step 2: Restore Dependencies
The project uses several NuGet packages (`System.Drawing.Common`, `AForge.Video`, `Microsoft.AspNetCore.SignalR.Client`):
- In the **Solution Explorer**, right-click the top-level `Solution 'RemoteControl'` and select **Restore NuGet Packages**.

### Step 3: Configure Multi-Project Startup
To simulate the Server and Client locally at the same time:
1. Right-click the `Solution 'RemoteControl'` and select **Properties**.
2. Under **Startup Project**, select **Multiple startup projects**.
3. In the Action column, set both `AgentApp` and `WebApp` to **Start**.
4. Click **Apply** and **OK**.

### Step 4: Running the System
- Press **F5** or the **Start** button in Visual Studio.
- Two things will open:
  - A browser window pointing to your local network IP (e.g., `http://192.168.x.x:5000/`). This is the Admin Dashboard.
  - A black Terminal window (the Agent) indicating it successfully connected to the Hub.
  - *(Note: Ensure your firewall allows connections on port 5000 if connecting from a different device on the same LAN).*
- In the browser, look at the sidebar to find your PC listed as `Online`. Click it to load the control panel.

**Explore!** Try requesting processes, taking a screenshot, or browsing your C:\ drive directly from the web interface.
