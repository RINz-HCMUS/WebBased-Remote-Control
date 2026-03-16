# Sơ đồ biểu diễn luồng dữ liệu (Sequence Diagrams)

Tài liệu này trình bày cách các thành phần trong hệ thống tương tác với nhau đối với các Request cơ bản và một số Request chuyên sâu.

## 1. Chu trình kết nối ban đầu và Cập nhật trạng thái (Connection & Registration)

Khi mục tiêu khởi động phần mềm Agent (ẩn danh).

```mermaid
sequenceDiagram
    participant OS as Target OS (Windows)
    participant Agent as Target Agent (C#)
    participant Hub as SignalR Hub (Backend)
    participant Admin as Admin Web UI (Browser)

    OS->>Agent: User runs App (Hidden)
    Agent->>Agent: Lấy thông tin HardwareID, MachineName
    Agent->>Hub: SignalR Connect (URL)
    activate Hub
    Hub-->>Agent: Connection Accepted
    Agent->>Hub: Invoke "RegisterAgent(HWID, MachineName)"
    Hub->>Hub: Lưu thông tin vào Danh sách Online
    Hub->>Admin: Broadcast "NewDeviceOnline(AgentData)"
    Admin->>Admin: Cập nhật UI bảng danh sách thiết bị
    deactivate Hub
```

## 2. Chu trình Gửi lệnh Yêu cầu dữ liệu (Ví dụ: Danh sách Tiến trình Process)

Admin muốn xem danh sách các ứng dụng/tiến trình đang chạy bên phía Agent.

```mermaid
sequenceDiagram
    participant Admin as Admin Web UI
    participant Backend as Web Server / Hub
    participant Agent as Target Agent
    participant TargetOS as OS (Target)

    Admin->>Backend: Click "List Processes" / Gửi Yêu cầu API (TargetID)
    activate Backend
    Backend->>Backend: Lookup ConnectionID của TargetID
    Backend->>Agent: Hub.Send(ConnectionID, "GetProcessesCommand")
    deactivate Backend
    
    activate Agent
    Agent->>TargetOS: System.Diagnostics.Process.GetProcesses()
    TargetOS-->>Agent: Return List of processes
    Agent->>Agent: Đóng gói JSON
    Agent->>Backend: Hub.Invoke("ReturnProcessesList", TargetID, JSON_Data)
    deactivate Agent
    
    activate Backend
    Backend->>Admin: Trả JSON về cho Web Client
    Admin->>Admin: Hiển thị lên Table (Datagrid)
    deactivate Backend
```

## 3. Chu trình Nhận Tín hiệu Khối lớn (Ví dụ: Chụp Màn Hình Real-time)

Truyền dữ liệu dạng hình ảnh (Image Stream) khá nặng nền phải bóc tách bằng Base64 chunked data qua websockets.

```mermaid
sequenceDiagram
    participant Admin as Admin Web UI
    participant Backend as SignalR Hub
    participant Agent as Target Agent

    Admin->>Backend: Request "Start Screen Stream" (TargetID)
    Backend->>Agent: Command "CaptureScreenLoop" (TargetID, FPS=5)
    
    loop Every 200ms
        Agent->>Agent: Capture Graphics.CopyFromScreen -> JPEG
        Agent->>Agent: Convert to Base64 String
        Agent-xBackend: Invoke "ReceiveScreenFrame(TargetID, Base64_String)"
        Backend-xAdmin: Push "UpdateScreenFrame(TargetID, Base64_String)"
        Admin->>Admin: Update <img src="data:image/jpeg;base64,...">
    end

    Admin->>Backend: Click "Stop Stream"
    Backend->>Agent: Command "StopCaptureScreen"
```

## 4. Chu trình Keylogger (Hook Event-driven)

Thay vì yêu cầu liên tục, Keylogger là kiến trúc hoạt động ngầm (Push từ dưới lên mỗi khi có sự kiện).

```mermaid
sequenceDiagram
    participant TargetOS as OS (Target)
    participant Agent as Target Agent
    participant Backend as SignalR Hub
    participant Admin as Admin Web UI

    Admin->>Backend: Command "Enable/Hook Keylogger" (TargetID)
    Backend->>Agent: Command "StartKeylogger"
    Agent->>TargetOS: SetWindowsHookEx (Keyboard Hook)
    TargetOS-->>Agent: Hook Setup Success
    
    TargetOS->>Agent: On KeyPress ('H')
    Agent-xBackend: Send(TargetID, "[H]")
    Backend-xAdmin: Push UI "[H]"

    TargetOS->>Agent: On KeyPress ('e', 'l', 'l', 'o')
    Agent-xBackend: Send(TargetID, "ello")
    Backend-xAdmin: Push UI "ello"
    
    Admin->>Admin: View Log Textbox shows "Hello"
```
