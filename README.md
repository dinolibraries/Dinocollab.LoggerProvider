# Dinocollab.LoggerProvider

[![NuGet](https://img.shields.io/nuget/v/Dinocollab.LoggerProvider.svg)](https://www.nuget.org/packages/Dinocollab.LoggerProvider)
[Dinocollab.LoggerProvider![Nuget](https://img.shields.io/nuget/dt/Dinocollab.LoggerProvider)](https://www.nuget.org/packages/Dinocollab.LoggerProvider) 
Dinocollab.LoggerProvider là một thư viện provider logging cho .NET, dùng để gửi và lưu trữ log từ ứng dụng sang backend (ví dụ QuestDB). README này trình bày cách cài đặt, cấu hình liên kết dự án (để nút **Get** trên NuGet dẫn tới đúng trang) và ví dụ sử dụng cơ bản.

Repository: https://github.com/dinolibraries/Dinocollab.LoggerProvider

## Nút "Get" trên NuGet
Trên trang gói NuGet, nút **Get** và phần liên kết sẽ dựa trên metadata của package (các thuộc tính trong file `.csproj`). Để đảm bảo nút dẫn tới đúng trang dự án, thêm các thuộc tính sau vào `PropertyGroup` trong `*.csproj`:

```xml
<PropertyGroup>
  <PackageProjectUrl>https://github.com/dinolibraries/Dinocollab.LoggerProvider</PackageProjectUrl>
  <RepositoryUrl>https://github.com/dinolibraries/Dinocollab.LoggerProvider</RepositoryUrl>
  <RepositoryType>git</RepositoryType>
</PropertyGroup>
```

Lưu ý: tôi đã cập nhật các file project trong repository để bao gồm các thuộc tính này.

## Cài đặt
Sử dụng `dotnet` CLI:

```bash
dotnet add package Dinocollab.LoggerProvider
```

Hoặc thêm `PackageReference` vào `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Dinocollab.LoggerProvider" Version="*" />
</ItemGroup>
```

## Ví dụ sử dụng nhanh
Thêm provider vào pipeline logging (ví dụ trong `Program.cs`):

```csharp
using Dinocollab.LoggerProvider;

var builder = WebApplication.CreateBuilder(args);

// Ví dụ khởi tạo (tham số tùy theo implementation thực tế)
builder.Logging.AddProvider(new QuestDbLogWorker(new QuestDbOptions {
    Host = "127.0.0.1",
    Port = 9000
}));

var app = builder.Build();
app.Run();
```

Xem thư mục `Dinocollab.LoggerProvider/QuestDB` để biết các lớp helper và tùy chọn cấu hình có sẵn.

## Cấu hình liên kết (homeUrl / projectUrl)
Nếu bạn muốn thay đổi đường dẫn hiển thị trên NuGet (ví dụ dùng trang docs khác), cập nhật giá trị `PackageProjectUrl` và `RepositoryUrl` trong `*.csproj` tương ứng.

## Đóng góp
Mọi đóng góp đều hoan nghênh: mở issue hoặc gửi pull request trên GitHub.

## License
Kiểm tra file `LICENSE` trong repository để biết giấy phép dự án. Nếu chưa có và bạn muốn, tôi có thể thêm file license phù hợp.

---

Nếu bạn muốn, tôi sẽ:

- Tạo commit cho các thay đổi (`git add` + `git commit`).
- Thêm badge CI hoặc link docs nếu cần.

Bạn muốn tôi tạo commit bây giờ không?

# Dinocollab.LoggerProvider

[![NuGet](https://img.shields.io/nuget/v/Dinocollab.LoggerProvider.svg)](https://www.nuget.org/packages/Dinocollab.LoggerProvider)

Thư viện `Dinocollab.LoggerProvider` — logger provider nhẹ cho các dự án .NET.

## Nút "Get" (NuGet)
Nút **Get** trên NuGet dẫn đến trang gói. Để nút và trang gói hiển thị chính xác liên kết tới trang chủ/ dự án, hãy đảm bảo cấu hình các thuộc tính sau trong file `.csproj` của bạn:

```xml
<PropertyGroup>
  <!-- Home / Project URL: hiển thị trên NuGet và trong nút Get -->
  <PackageProjectUrl>https://github.com/dinolibraries/Dinocollab.LoggerProvider</PackageProjectUrl>
  <RepositoryUrl>https://github.com/dinolibraries/Dinocollab.LoggerProvider</RepositoryUrl>
</PropertyGroup>
```

## Cài đặt
Sử dụng `dotnet` CLI:

```bash
dotnet add package Dinocollab.LoggerProvider
```

Hoặc dùng `PackageReference` trong `.csproj`:

```xml
<ItemGroup>
  <PackageReference Include="Dinocollab.LoggerProvider" Version="*" />
</ItemGroup>
```

## Sử dụng nhanh
1. Thêm provider vào cấu hình logging của bạn (ví dụ trong `Program.cs`):

```csharp
// ví dụ khởi tạo logger (tùy theo implementation của package)
builder.Logging.AddProvider(new Dinocollab.LoggerProvider.QuestDbLogWorker(/* options */));
```

2. Cấu hình `homeUrl`/`projectUrl` nếu cần hiển thị trong metadata hoặc docs:

- HomeUrl: https://github.com/dinolibraries/Dinocollab.LoggerProvider
- ProjectUrl: https://github.com/dinolibraries/Dinocollab.LoggerProvider

---

Nếu bạn muốn, tôi có thể cập nhật file `.csproj` mẫu trong repo để chèn tự động các thẻ `PackageProjectUrl`/`RepositoryUrl` và commit thay đổi. Bạn có muốn tôi làm điều đó không?
