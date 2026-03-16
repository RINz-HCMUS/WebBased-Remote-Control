# Đồ án: Ứng dụng điều khiển từ xa qua Web App

Đây là mã nguồn cho đồ án môn học (Topic 04), với mục tiêu xây dựng một ứng dụng cho phép admin có thể điều khiển và quản lý **cùng lúc nhiều máy tính** thông qua giao diện Web Browser thống nhất.

Hệ thống được thiết kế theo kiến trúc **ASP.NET Core (Hub-Spoke)** kết hợp **SignalR** cho kết nối Real-time, loại bỏ giới hạn của việc sử dụng Socket 1-1 truyền thống.

## Tóm tắt các Module
1. `WebApp` (Server Trung tâm & Client Browser): Chạy ASP.NET Core MVC. Đóng vai trò là SignalR Hub trung chuyển lệnh và cung cấp giao diện Dashboard quản lý trực tiếp trên Web.
2. `AgentApp` (Client PC bị điều khiển): Ứng dụng C# Console chạy ngầm trên thiết bị đích. Tự động kết nối lên Hub chờ chỉ thị và thực thi các khối lệnh can thiệp vào máy tính.

## Các chức năng hỗ trợ
- [x] Nhận diện đa thiết bị online/offline (Multi-Device Management)
- [x] Lấy Danh sách và Hiển thị số lượng luồng (Threads) của Tiến trình (Processes)
- [x] Tắt (Kill) Tiến trình từ xa qua Web
- [ ] List/Start/Stop Applications (Đang phát triển)
- [ ] Chụp màn hình - Screenshot (Đang phát triển)
- [ ] Bắt phím - Keylogger (Đang phát triển)
- [ ] Tải tệp tin - Download file (Đang phát triển)
- [ ] Xem luồng Webcam trực tiếp (Đang phát triển)
- [ ] Tắt / Khởi động lại hệ thống (Shutdown/Restart) (Đang phát triển)

---

## 🛠 Hướng dẫn Cài đặt & Chạy ứng dụng

### Yêu cầu tiên quyết
Ứng dụng sử dụng phiên bản mới nhất của nền tảng .NET để tối ưu truyền tải WebSockets. Bạn cần chuẩn bị:
1. **.NET 8.0 SDK**: Cần có để biên dịch và chạy được ứng dụng (Bắt buộc).
   - Tải xuống tại: [Download .NET 8.0 SDK x64](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
2. **Visual Studio 2022** (Phiên bản Community/Professional đều được): Phục vụ mở mã nguồn, quản lý đa cấu hình (Multi-project run) và tự động tải thư viện.

### Bước 1: Mở Project
1. Mở phần mềm Visual Studio. 
2. Chọn **Open a project or solution**, duyệt đến thư mục mã nguồn và chọn thiết lập mở file `RemoteControl.sln`.

### Bước 2: Tải các thư viện mạng & xử lý hình ảnh
Project sử dụng một số NuGet packages như `System.Drawing`, `AForge.Video` và `SignalR.Client`:
1. Trong Visual Studio, nhìn sang cột **Solution Explorer** ở bên phải màn hình.
2. Click **chuột phải** vào chữ `Solution 'RemoteControl'` ở dòng trên cùng nhất.
3. Chọn tính năng **Restore NuGet Packages**. Khi góc màn hình báo "Trạng thái tải hoàn tất", các công cụ cần thiết đã được thêm.

### Bước 3: Cấu hình chạy giả lập Server & Agent cùng lúc
Vì ứng dụng có 2 nửa riêng biệt, bạn cần thiết lập để Start cả 2 luồng:
1. Click **chuột phải** vào chữ `Solution 'RemoteControl'` và chọn **Properties** (hoặc Configure Startup Projects).
2. Tại màn hình Property Pages, mục **Startup Project**: Tick vào Option thứ 3 có tên **Multiple startup projects**.
3. Sẽ có danh sách 2 tên Project hiện ra, ở cột **Action**, bạn xổ menu drop-down và chuyển chữ `None` sang chữ **Start** đối với cả 2 dòng `AgentApp` và `WebApp`.
4. Bấm **Apply** và **OK** để lưu cấu hình.

### Bước 4: Chạy hệ thống
- Click phím **Start (hình tam giác xoay sang phải, màu xanh lá)** tại thanh menu trên cùng của Visual Studio (Hoặc nhấn phím `F5`).
- Sẽ có 2 cửa sổ đồng thời được mở:
  - Một trình duyệt sẽ bật lên địa chỉ (Ví dụ: `https://localhost:5001/`). Đây là bảng điều khiển cho Quản trị viên.
  - Một ô cửa sổ đen Terminal hiện chữ báo hiệu trạng thái *Connected*. Đây chính là máy con (PC) đang ngầm kết nối.
- Ở phía trình duyệt, tên thiết bị PC con sẽ sáng lên phía góc Trái biểu tượng `Online`. Bạn click vào tên thiết bị này để gọi Panel điều khiển ra.

**Trải nghiệm sơ khai**: Ấn nút "Refresh Processes" và hệ thống lập tức List các Process của chính máy tính bạn lên giao diện web.
