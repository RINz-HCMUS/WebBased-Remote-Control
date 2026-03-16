# Thiết Kế Cơ Sở Dữ Liệu (Database ERD)

Vì mục tiêu của đồ án ưu tiên tốc độ triển khai và quản lý thiết bị ngay trên RAM (In-Memory) qua danh sách SignalR tĩnh, ta có thể không cần thiết phải sử dụng SQL Server nặng nề ngay từ đầu đối với phiên bản MVP (Minimum Viable Product). 

*Tuy nhiên*, để hoàn thiện tài liệu phục vụ tính chuyên nghiệp hoặc scale tính năng sau này (Lưu nhật ký truy cập, Lưu tài khoản đăng nhập Admin, Báo cáo offline), chúng ta phác thảo một kiến trúc CSDL Quan hệ tiêu chuẩn (RDBMS) như sau:

## 1. Sơ đồ Thực thể Liên kết (Entity-Relationship Diagram)

```bash
erDiagram
    ADMINISTRATOR ||--o{ COMMAND_LOG : executes
    TARGET_AGENT ||--o{ COMMAND_LOG : receives
    TARGET_AGENT ||--o{ AGENT_STATUS_LOG : has_history
    TARGET_AGENT ||--o{ SAVED_FILES : owns

    ADMINISTRATOR {
        int AdminID PK
        string Username
        string PasswordHash
        datetime CreatedAt
        datetime LastLogin
        boolean IsActive
    }

    TARGET_AGENT {
        string HardwareID PK "UUID của máy trạm"
        string ComputerName
        string OSVersion
        string IPAddress
        string MACAddress
        boolean IsOnline
        datetime FirstSeen
        datetime LastSeen
    }

    COMMAND_LOG {
        int LogID PK
        string HardwareID FK "Thuộc Agent nào"
        int AdminID FK "Ai thực thi"
        string CommandType "VD: KILL_PROCESS, SCREENSHOT"
        string CommandArgs "Tham số truyền vào"
        string ResultStatus "SUCCESS, FAILED"
        string ResultMessage "Text độ dài lớn"
        datetime ExecutedAt
    }

    AGENT_STATUS_LOG {
        int StatusID PK
        string HardwareID FK
        string EventType "ONLINE, OFFLINE, ERROR"
        datetime EventTime
    }

    SAVED_FILES {
        int FileID PK
        string HardwareID FK
        string FileName
        string FileType "SCREENSHOT, DOWNLOADED_FILE, KEYLOG_TXT"
        string FilePathOnServer "Đường dẫn thư mục tĩnh"
        datetime UploadedAt
    }
```

## 2. Giải thích các Bảng CSDL (Tables Definition)

1. **Table `ADMINISTRATOR`**
   - Nơi lưu trữ tài khoản Đăng nhập vào Admin Web UI.
   - Mật khẩu phải được Hash theo tiêu chuẩn (bcrypt, PBKDF2).

2. **Table `TARGET_AGENT`**
   - Quản lý định danh các thiết bị bị điều khiển. 
   - `HardwareID` là khoá chính (Sử dụng chuỗi Hash của ID Mainboard + HDD Serial) để đảm bảo dù có đổi IP thì vẫn nhận diện được đúng máy đó.
   - Các trường `FirstSeen` (Lần đầu lây nhiễm) và `LastSeen` (Lần cuối hoạt động). Nếu Agent vào trạng thái Online sẽ đổi trường `IsOnline = true`.

3. **Table `COMMAND_LOG`**
   - Lưu lại dấu vết kiểm toán (Audit Log) về mọi hành động điều khiển mà Admin đã thao tác trên Agent để xem lại báo cáo.
   - Giúp cho người quản trị phân tích được số lần thực thi các lệnh có gặp lỗi hay không (`ResultStatus`).

4. **Table `AGENT_STATUS_LOG`**
   - Nhật ký (Timeline) bật / tắt kết nối mạng của Agent. Giúp Admin biết thiết bị nạn nhân (Target) thường được mở máy vào khung giờ nào để đưa rã kịch bản hành động.

5. **Table `SAVED_FILES`**
   - Nơi lưu đường dẫn của các tệp (Hình ảnh screenshot, File word tải lén, File log phím txt) từ Target được tải về và lưu trên Server. CSDL không lưu trực tiếp file dạng BLOB mà chỉ lưu đường dẫn thư mục vật lý `FilePathOnServer` (`/uploads/hwida1/screen1.jpg`) để tối ưu băng thông và tốc độ đọc ghi DB.
