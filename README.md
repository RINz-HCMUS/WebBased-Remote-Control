<h1 align="center">Web-Based Remote Control & Management System 💻🚀</h1>

<p align="center">
  <strong>Topic 04: Building a Web Application for Remote Computer Control</strong><br>
  <em>Computer Networks - Graduation Project (22MMT)</em><br>
  <em>Major: Computer Networks and Telecommunications (Information Security Specialization)</em><br>
  <em>Faculty of Information Technology | University of Science, VNU-HCM</em><br>
  👤 <strong>Authors:</strong> Trần Lê Bảo Duy (22127089) & Võ Hữu Tuấn (22127439) | 👨‍🏫 <strong>Instructor:</strong> Mr. Đỗ Hoàng Cường
</p>

<p align="center">
  <strong>A modern, cross-platform remote administration tool built with ASP.NET Core and SignalR.</strong><br>
  Manage, monitor, and execute commands on multiple remote computers simultaneously directly through a Web Browser without requiring a standalone admin client desktop app.
</p>


<p align="center">
  <!-- Technology Badges -->
  <img src="https://img.shields.io/badge/.NET_8-512BD4?style=for-the-badge&logo=dotnet&logoColor=white" alt=".NET 8"/>
  <img src="https://img.shields.io/badge/C%23-239120?style=for-the-badge&logo=c-sharp&logoColor=white" alt="C#"/>
  <img src="https://img.shields.io/badge/SignalR-0078D4?style=for-the-badge&logo=microsoft&logoColor=white" alt="SignalR"/>
</p>

---

> **⚠️ EDUCATIONAL & LIABILITY DISCLAIMER:**
> This software is strictly developed for **educational purposes, academic research, and authorized internal IT administration**. Features such as *Keylogger*, *Webcam Streaming*, and *Remote Shell Execution* mimic features often found in remote access tools. Antiviruses may flag the agent as a false-positive due to its Windows API calls.
> **The creators and contributors are NOT responsible for any unauthorized or illegal usage of this software.**

---

## ✨ Features Highlights
* **Multi-Device Realtime Connection:** Powered by SignalR WebSockets for sub-second latency.
* **Process & Application Management:** Force-kill apps, run new instances, and check system threads.
* **Live Webcam & Screenshot:** Stream full desktop screens or live camera feeds encoded efficiently.
* **Live Keylogger:** Read and capture keystrokes natively from the background.
* **Terminal Shell:** Execute native cmd/powershell lines secretly in the background.
* **File Explorer:** Upload to or Download from the target's entire file system structure.

---

## 📖 Complete Documentation & Technical Reports

We have structured the documentation to keep this repository neat! For the installation guide, deep technical diving, and academic reports, please refer to the `Documents` directory:

| 📂 File / Folder | 📝 Description |
|---|---|
| 📄 [`Documents/README.md`](Documents/README.md) | **Getting Started:** Installation requirements, VS2022 setup, compile instructions, and how to use the dashboard. |
| 📄 [`Documents/Bao_Cao_Tong_Quan.md`](Documents/Bao_Cao_Tong_Quan.md) | **Technical Report (Vietnamese):** Detailed flowcharts, sequence diagrams, and technology stack breakdown for each major feature. |
| 🖼️ [`Documents/Flow Diagrams`](Documents/Flow%20Diagrams) | Includes original visual diagrams (architecture, file transfer sequences, heartbeat loops, etc). |

---

## 🛠 Project Structure
- **`RemoteControlSystem/WebApp`**: The central application. Serves the web-based ASP.NET Core control panel and acts as the SignalR Hub.
- **`RemoteControlSystem/AgentApp`**: The client payload. A lightweight C# app running on the background of remote machines, receiving orders natively via Windows APIs.

***
<p align="center">Made with ❤️ for the Computer Networks Graduation Project at the University of Science, VNU-HCM.</p>
