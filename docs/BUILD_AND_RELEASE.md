# 建置與發佈指南

本文件說明開發環境、建置、測試、格式檢查、x64／arm64 免安裝發佈與 GitHub Release 流程。

## 1. 開發環境

| 項目 | 需求 |
| --- | --- |
| 作業系統 | Windows 10 1809 以上或 Windows 11 |
| .NET SDK | .NET 10 SDK |
| 工作負載 | .NET 桌面開發（WinUI 3 透過 Windows App SDK NuGet 套件） |
| Git | 用於版本控制與發佈 |

確認環境：

```powershell
dotnet --version
```

## 2. 還原與建置

```powershell
dotnet restore InputAccelerator.slnx
dotnet build InputAccelerator.slnx -c Debug
```

建置輸出：

```text
src/InputAccelerator/bin/x64/Debug/net10.0-windows10.0.19041.0/InputAccelerator.exe
```

專案支援 `x64` 與 `arm64`。`Directory.Build.props` 啟用 `Nullable` 與 `TreatWarningsAsErrors`，建置時不應忽略警告。

## 3. 測試與格式檢查

```powershell
dotnet test InputAccelerator.slnx -c Debug
dotnet format InputAccelerator.slnx --verify-no-changes
```

單元測試涵蓋 Core 的：

- JSON 儲存與損壞檔處理
- 設定正規化
- 文字輸入組合
- 文字預覽
- 視窗位置驗證
- 浮動視窗背景色定義

UI、Win32 interop、系統匣與輸入注入仍需手動驗收，清單見 [系統設計文件](SYSTEM_DESIGN.md#122-手動驗收清單)。

## 4. 版本管理

版本集中於 `Directory.Build.props`：

```xml
<Version>1.3.10</Version>
<AssemblyVersion>1.3.0.0</AssemblyVersion>
<FileVersion>1.3.0.0</FileVersion>
```

規則：

- `Version` 採 `主.次.修`。
- 每次功能變更遞增版本。
- `AssemblyVersion` 僅在主次版變更時調整。
- 版本異動需同步更新 `CHANGELOG.md`、`RELEASE_NOTES.md` 與 `docs/DEV_LOG.md`。

## 5. 發佈免安裝版本

本專案提供 **framework-dependent（非 Self-Contained）** 版本，不提供安裝檔。

### 5.1 x64

```powershell
dotnet publish src/InputAccelerator/InputAccelerator.csproj `
  -c Release -r win-x64 `
  --self-contained false `
  -p:WindowsAppSDKSelfContained=false `
  -p:WindowsPackageType=None `
  -p:Platform=x64 `
  -o artifacts/publish/win-x64
```

### 5.2 arm64

```powershell
dotnet publish src/InputAccelerator/InputAccelerator.csproj `
  -c Release -r win-arm64 `
  --self-contained false `
  -p:WindowsAppSDKSelfContained=false `
  -p:WindowsPackageType=None `
  -p:Platform=arm64 `
  -o artifacts/publish/win-arm64
```

### 5.3 驗證輸出

確認輸出資料夾包含：

```text
InputAccelerator.exe
InputAccelerator.dll
InputAccelerator.Core.dll
Assets/AppIcon.ico
*.dll（Windows App SDK 與 .NET 相依）
```

> framework-dependent 版本不包含 .NET Runtime 與 Windows App SDK Runtime。使用者需自行安裝對應架構的 **Windows App SDK 2.6.5 Runtime**。
>
> `InputAccelerator.csproj` 會在 `dotnet publish` 時排除未使用的 Windows ML 原生元件（`onnxruntime.dll`、`DirectML.dll`、`Microsoft.Windows.AI.MachineLearning.dll`），這些元件由系統的 Windows App SDK Runtime 提供。

## 6. 打包 zip

建議讓 zip 解壓縮後包含單一資料夾，避免檔案散落。

```powershell
$version = "1.3.10"

New-Item -ItemType Directory -Force -Path "artifacts/stage/win-x64" | Out-Null
New-Item -ItemType Directory -Force -Path "artifacts/stage/win-arm64" | Out-Null

Copy-Item "artifacts/publish/win-x64/*" `
  -Destination "artifacts/stage/win-x64/InputAccelerator" `
  -Recurse -Force

Copy-Item "artifacts/publish/win-arm64/*" `
  -Destination "artifacts/stage/win-arm64/InputAccelerator" `
  -Recurse -Force

Compress-Archive `
  -Path "artifacts/stage/win-x64/InputAccelerator" `
  -DestinationPath "artifacts/InputAccelerator-$version-win-x64.zip" `
  -Force

Compress-Archive `
  -Path "artifacts/stage/win-arm64/InputAccelerator" `
  -DestinationPath "artifacts/InputAccelerator-$version-win-arm64.zip" `
  -Force
```

輸出：

```text
artifacts/InputAccelerator-1.3.10-win-x64.zip
artifacts/InputAccelerator-1.3.10-win-arm64.zip
```

`artifacts/` 與 `publish/` 已在 `.gitignore` 中排除，不會進入版本控制。

## 7. 建立 GitHub Release

### 7.1 前置檢查

```powershell
git status
git log --oneline -5
```

確認：

- 工作目錄乾淨。
- 版本號與文件一致。
- `LICENSE` 為 MIT License。
- 兩個 zip 已產生且可解壓縮執行。

### 7.2 建立 Release

```powershell
gh release create v1.3.10 `
  "artifacts/InputAccelerator-1.3.10-win-x64.zip" `
  "artifacts/InputAccelerator-1.3.10-win-arm64.zip" `
  --title "快速輸入 1.3.10" `
  --notes-file RELEASE_NOTES.md
```

若要標記為最新版本，`gh` 預設會將最新建立的 Release 設為 latest。

### 7.3 Release 內容建議

- 標題：`快速輸入 <版本>`
- 說明：引用 `RELEASE_NOTES.md`
- 附件：
  - `InputAccelerator-<版本>-win-x64.zip`
  - `InputAccelerator-<版本>-win-arm64.zip`
- 明確標示：
  - 為 framework-dependent 版本。
  - 需安裝 Windows App SDK 2.6.5 Runtime。
  - 不提供安裝檔。

## 8. 發佈檢查清單

- [ ] `dotnet build` 成功且 0 警告。
- [ ] `dotnet test` 全部通過。
- [ ] `dotnet format --verify-no-changes` 通過。
- [ ] 版本已更新於 `Directory.Build.props`。
- [ ] `CHANGELOG.md`、`RELEASE_NOTES.md`、`docs/DEV_LOG.md` 已更新。
- [ ] x64 publish 成功。
- [ ] arm64 publish 成功。
- [ ] 兩個 zip 解壓縮後可啟動。
- [ ] 在 x64 與 arm64 裝置（或模擬環境）完成手動驗收。
- [ ] Git 提交與推送完成。
- [ ] GitHub Release 附件齊全。
- [ ] Release 頁面標示 Runtime 需求與非安裝檔性質。

## 9. 常見問題

### 建置時找不到 Windows App SDK

確認已還原 NuGet 套件，且 `Directory.Packages.props` 中的 `Microsoft.WindowsAppSDK` 版本正確。

### arm64 publish 失敗

確認：

- 使用 .NET 10 SDK。
- `InputAccelerator.csproj` 的 `Platforms` 包含 `arm64`。
- `RuntimeIdentifiers` 包含 `win-arm64`。
- 命令帶有 `-p:Platform=arm64` 與 `-r win-arm64`。

### 執行時缺少 Runtime

安裝對應架構的 Windows App SDK 2.6.5 Runtime。framework-dependent 版本不會自帶 Runtime。

### zip 太大

本專案發佈為 framework-dependent，若仍偏大，請確認沒有誤帶 `--self-contained true` 或 `WindowsAppSDKSelfContained=true`。
