# Kiến trúc Hệ thống (System Architecture)

## 1. Mô hình tổng quan (High-Level Architecture)
Hệ thống **Remote Control WebApp** sử dụng mô hình kiến trúc **Client-Server-Agent** hay còn gọi là **C&C (Command & Control) Hub-Spoke**, bao gồm 3 lớp nhân tố chính:

1. **Web Board (Client / Frontend):** Giao diện quản trị viên chạy trên trình duyệt web, sử dụng HTML/CSS/JS (ASP.NET Core MVC Views).
2. **C&C Hub (Backend Server):** Máy chủ trung gian xử lý tín hiệu và điều phối đường truyền, được xây dựng bằng **ASP.NET Core** tích hợp **SignalR WebSockets / Hub**.
3. **Target Agent (Endpoint):** Ứng dụng chạy nền trên máy Client, nhận lệnh từ Hub và thao tác với hệ điều hành (Windows OS API), được viết bằng **C# Console Application**.

## 2. Sơ đồ Kiến trúc (Architecture Diagram)

```mermaid
graph TD
    subgraph WEB_FRONTEND[Web UI / Admin Dashboard]
        Admin((Admin)) -->|1. View/Click Control| WebBrowser(Web Browser - MVC Views)
        WebBrowser <-->|2. HTTP HTTPS / SignalR| WebServer
    end

    subgraph BACKEND_SERVER[ASP.NET Core Hub Server]
        WebServer[Web MVC API Controllers] <-->|3. Logic Processing| SignalRHub[SignalR Hub]
        SignalRHub -->|Store/Retrieve| MemoryCache[(In-Memory Cache / Database)]
    end

    subgraph TARGET_AGENTS[Target Devices]
        SignalRHub <-->|4. WebSocket Tunnel (Persistent)| Agent1(AgentApp 1 - PC A)
        SignalRHub <-->|WebSocket Tunnel| Agent2(AgentApp 2 - PC B)
        SignalRHub <-->|WebSocket Tunnel| AgentN(AgentApp N - PC N)
        
        Agent1 <-->|5. Interacts via C# APIs| OS1[Windows OS API]
    end

    classDef web fill:#3b82f6,stroke:#1d4ed8,stroke-width:2px,color:#fff;
    classDef server fill:#10b981,stroke:#047857,stroke-width:2px,color:#fff;
    classDef agent fill:#ef4444,stroke:#b91c1c,stroke-width:2px,color:#fff;
    
    class WebBrowser,WebServer web;
    class SignalRHub,MemoryCache server;
    class Agent1,Agent2,AgentN agent;
```

## 3. Chức năng từng Layer
### 3.1 Web Frontend (Admin View)
- Cung cấp giao diện trực quan cho Admin.
- Hiển thị danh sách thiết bị Online, lịch sử hoạt động, luồng màn hình/webcam live và danh sách files tiến trình.
- Phát đi tín hiệu yêu cầu API (vd: POST `/api/process/kill`) hoặc thông qua SignalR trực tiếp gọi hub backend.

### 3.2 Backend Server (SignalR Hub)
- **SignalR Hub:** Là trái tim của hệ thống lưới. Nó quản lý *Connection IDs* của tất cả Agent đang kết nối.
- **Connection Management:** Map `ConnectionId` với thông tin phần cứng `HardwareID` cụ thể để biết máy nào là máy nào. Đẩy trạng thái (Online/Offline) nếu 1 Agent ngắt kết nối đột ngột.
- **Relay System:** Nhận yêu cầu từ Web Controller (Ví dụ: "Chụp màn hình máy PC-A"), dò tìm Connection ID của `PC-A` và broadcast lệnh `TakeScreenshot` xuống Agent đó.
- Cân bằng và quản lý traffic dữ liệu khi truyền file ảnh kích thước lớn bằng Base64 array chunks (nếu tải trọng lớn).

### 3.3 Target Agent 
- Tạo socket mở theo chiều Connect-back (Hành động vượt qua Firewall/NAT tại mạng của Nạn nhân).
- Chờ lắng nghe event `OnCommandReceived` từ WebSockets:
    - Nếu là `GetProcesses`: Gọi `Process.GetProcesses()`.
    - Nếu là `Screenshot`: Gọi `Graphics.CopyFromScreen()` và nén JPEG -> Convert Base64.
    - Nếu là `Shutdown`: Gọi `Process.Start("shutdown.exe", "/s /t 0")`.
    - Nếu là `Keylogger`: Cài hook API Windows cấp thấp (Low-level User32.dll hook).
- Phản hồi kết quả lên ngược lại cho Backend Hub. Ngay lập tức Hub sẽ forward cho Web Frontend.
