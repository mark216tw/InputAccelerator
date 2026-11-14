# 系統架構與技術文件

本文件描述「快速輸入」（InputAccelerator）的技術架構、執行流程、關鍵模組與 Windows 平台整合方式。

## 1. 技術基線

| 項目 | 內容 |
| --- | --- |
| 語言 | C#（nullable reference types、file-scoped namespace） |
| 執行環境 | .NET 10（`net10.0-windows10.0.19041.0`） |
| UI 框架 | WinUI 3（Windows App SDK 2.6.5） |
| MVVM | CommunityToolkit.Mvvm |
| 依賴注入 | Microsoft.Extensions.Hosting／DependencyInjection |
| 記錄 | Microsoft.Extensions.Logging + 自訂檔案 Provider |
| 測試 | xUnit |
| 支援平台 | Windows 10 1809（build 17763）以上、Windows 11 |
| 支援架構 | x64、arm64 |

## 2. 解決方案結構

```text
InputAccelerator.slnx
├─ src/
│  ├─ InputAccelerator/          WinUI 3 應用程式
│  │  ├─ Views/                  主視窗、浮動視窗、設定視窗、編輯對話框
│  │  ├─ ViewModels/             MVVM ViewModel
│  │  ├─ Services/               應用服務與協調器
│  │  ├─ Interop/                Win32 P/Invoke 與視窗訊息處理
│  │  ├─ Helpers/                路徑、字串、色彩、WrapPanel
│  │  ├─ Styles/                 共用樣式
│  │  ├─ Strings/zh-Hant/        繁體中文資源
│  │  └─ Assets/                 App 圖示
│  └─ InputAccelerator.Core/     與 UI 無關的類別庫
│     ├─ Models/                 資料模型與列舉
│     └─ Services/               JSON 儲存、文字組合、視窗位置驗證
├─ tests/
│  └─ InputAccelerator.Tests/    Core 邏輯單元測試
└─ tools/
   └─ GenerateIcon.ps1           圖示產生腳本
```

### 2.1 分層原則

```text
┌─────────────────────────────────────────────┐
│ Views（XAML + code-behind）                 │
│  MainWindow / FloatingWindow / Settings...  │
└──────────────────────┬──────────────────────┘
                       │ Binding / events
┌──────────────────────▼──────────────────────┐
│ ViewModels（CommunityToolkit.Mvvm）          │
│  MainViewModel / EntryListViewModel / ...    │
└──────────────────────┬──────────────────────┘
                       │ 呼叫服務
┌──────────────────────▼──────────────────────┐
│ Application Services（InputAccelerator）     │
│  AppCoordinator / InputInjection / Tray ...  │
└──────────────────────┬──────────────────────┘
                       │
┌──────────────────────▼──────────────────────┐
│ Core（無 UI 相依，可單元測試）               │
│  Models / EntryRepository / SettingsStore /  │
│  TextInputComposer / WindowPlacementValidator│
└─────────────────────────────────────────────┘
```

`InputAccelerator.Core` 不參考 WinUI 或 Windows App SDK，因此可在一般 .NET 測試流程中驗證。UI、Win32 interop 與平台事件留在 `InputAccelerator` 專案。

## 3. 啟動流程

程式進入點由 WinUI 產生，主要流程位於 `App.OnLaunched`：

1. 建立 `SingleInstanceService`。
   - 以具名 Mutex `Local\InputAccelerator.SingleInstance` 判斷是否已有執行個體。
   - 若已存在，廣播 `InputAccelerator.Activate` 視窗訊息，通知既有執行個體顯示主視窗，然後結束。
2. 建立 `AppPaths` 資料目錄（`%AppData%\InputAccelerator` 與 `logs`）。
3. 以 `Host.CreateApplicationBuilder()` 建立 DI 容器。
4. 註冊記錄 Provider、服務、ViewModel、視窗與 `AppCoordinator`。
5. 啟動 Host，取得 `AppCoordinator` 並執行 `InitializeAsync()`。
6. 發生未處理例外時寫入 `logs\app.log` 並結束。

### 3.1 相依注入註冊

`App.xaml.cs` 以 Singleton 註冊：

- 資料層：`EntryRepository`、`SettingsStore`、`SettingsService`
- 外觀：`AppearanceService`
- ViewModel：`EntryListViewModel`、`MainViewModel`、`FloatingBarViewModel`
- 平台服務：`ForegroundWindowTracker`、`InputInjectionService`、`TrayIconService`、`SingleInstanceService`
- 視窗：`MainWindow`、`FloatingWindow`、`SettingsWindow`
- 協調：`FloatingWindowPlacementManager`、`AppCoordinator`

單一實例與服務皆為長生命週期物件，與桌面常駐程式模型相符。

### 3.2 AppCoordinator 初始化

`InitializeAsync` 負責串接各模組：

1. 載入 `settings.json`。
2. 載入 `entries.json` 至 `EntryListViewModel`。
3. 將設定套用到 `MainViewModel` 與 `AppearanceService`。
4. 訂閱浮動視窗開關、單一實例啟用、輸入失敗、系統匣命令等事件。
5. 初始化系統匣圖示。
6. 附加浮動視窗位置管理與視窗關閉事件。
7. 顯示主視窗。
8. 若設定為啟用浮動視窗，以不啟用焦點的方式顯示。

`ExitAsync` 會依序儲存浮動視窗位置、項目清單與設定，然後釋放系統匣、單一實例與位置管理器，最後關閉視窗並結束應用程式。

## 4. 主要模組

### 4.1 Views

| 檔案 | 責任 |
| --- | --- |
| `MainWindow.xaml(.cs)` | 主視窗、項目清單、新增／編輯／刪除／排序、More 按鈕、縮到系統匣 |
| `FloatingWindow.xaml(.cs)` | 無邊框置頂浮動視窗、膠囊清單、拖曳、調整大小、收合／還原 |
| `SettingsWindow.xaml(.cs)` | 顯示模式與浮動視窗背景設定，非模態 singleton 視窗 |
| `EditEntryDialog.xaml(.cs)` | 新增／編輯文字或按鍵組合的 `ContentDialog` |
| `HotkeyRecorder.xaml(.cs)` | 多步驟按鍵序列錄製控制項，讀取修飾鍵狀態、累積步驟並在閒置後完成 |

### 4.2 ViewModels

| 檔案 | 責任 |
| --- | --- |
| `EntryListViewModel` | 項目清單集合、新增／更新／刪除／移動、儲存協調 |
| `MainViewModel` | 主視窗設定狀態、輸入模式、主題、背景選項、版本文字 |
| `FloatingBarViewModel` | 將項目清單轉為浮動視窗膠囊，依類型呼叫文字或按鍵組合輸入服務 |
| `FloatingEntryItemViewModel` | 單一膠囊項目的顯示文字、預覽與配色 |
| `TextEntryViewModel` | 單筆文字或按鍵組合的可觀察包裝 |
| `FloatingBackgroundOption` | 設定視窗背景色塊選項 |

### 4.3 Application Services

| 服務 | 責任 |
| --- | --- |
| `AppCoordinator` | 應用程式生命週期與模組協調 |
| `SettingsService` | 記憶體設定快照、序列化儲存、位置更新 |
| `AppearanceService` | 解析顯示模式、監聽系統色彩變化、產生浮動視窗配色 |
| `InputInjectionService` | 還原目標焦點並執行模擬輸入、剪貼簿貼上或按鍵組合送出 |
| `ForegroundWindowTracker` | 以 `SetWinEventHook` 追蹤最近一個外部前景視窗 |
| `FloatingWindowPlacementManager` | 還原、驗證、延遲儲存浮動視窗位置與大小 |
| `TrayIconService` | 原生 `Shell_NotifyIcon` 系統匣圖示與選單 |
| `SingleInstanceService` | Mutex 單一實例與跨程序啟用訊息 |
| `FileLoggerProvider` | 將記錄寫入 `%AppData%\InputAccelerator\logs\app.log` |

### 4.4 Core Services

| 服務 | 責任 |
| --- | --- |
| `EntryRepository` | 非同步載入／儲存項目清單，使用暫存檔原子替換，損壞檔備份 |
| `SettingsStore` | 非同步載入／儲存設定，列舉與背景索引正規化 |
| `TextInputComposer` | 將文字拆解為 Unicode 字元與 Enter／Tab 虛擬鍵單元 |
| `HotkeyParser` | 解析、正規化與格式化 `Ctrl+C`、`Alt+H, W` 等按鍵序列，維護支援的按鍵對照表 |
| `HotkeyInputComposer` | 將按鍵序列轉為批次送出單元，處理修飾鍵保持、釋放、Extended Key 與步驟停頓 |
| `TextPreview` | 產生清單與對話框使用的單行預覽文字 |
| `WindowPlacementValidator` | 依螢幕工作區、DPI 與最小尺寸驗證視窗位置 |

## 5. Windows 平台整合

### 5.1 輸入注入

`InputInjectionService.SendAsync` 的流程：

```text
使用者點擊膠囊
  → ForegroundWindowTracker.ResolveInjectionTarget()
  → 若前景不是目標視窗，SetForegroundWindow 並等待 70ms
  → 依項目類型選擇：
        文字項目：依 InputMode 選擇
                 模擬輸入：TextInputComposer.Compose()
                          Unicode 字元每批 32 個送出
                          Enter／Tab 以虛擬鍵送出，之後等待 50ms
                 剪貼簿：寫入 Clipboard 並 Flush()
                         等待 120ms 後送出 Ctrl+V
        按鍵組合：HotkeyParser.TryParse()
                  HotkeyInputComposer.ComposeSequence()
                  逐批 SendInput 送出各步驟；相鄰步驟共用修飾鍵時保持按住
                  步驟之間停頓 150ms；失敗或取消時釋放修飾鍵
  → 失敗時透過事件通知系統匣氣球提示（5 秒節流）
```

重點設計：

- **不搶焦點**：浮動視窗使用 `WS_EX_NOACTIVATE` 與 `WS_EX_TOOLWINDOW`，顯示時呼叫 `Show(activateWindow: false)` 與 `SetWindowPos(... SWP_NOACTIVATE)`。
- **前景鎖定**：主視窗必要時使用 `AttachThreadInput` 輔助 `SetForegroundWindow`。
- **輸入節奏**：Enter／Tab 後延遲 50ms，避免記事本等程式在連續輸入時字元亂序。
- **批次送出**：Unicode 字元以每批 32 個 `SendInput` 呼叫送出，降低輸入佇列壓力。

### 5.2 無邊框浮動視窗

- 使用 `OverlappedPresenter.SetBorderAndTitleBar(false, false)` 移除標題列與邊框。
- `FloatingWindowInterop` 處理：
  - `WM_NCCALCSIZE`：移除系統框架殘留。
  - `WM_GETMINMAXINFO`：套用最小尺寸，並在收合時切換追蹤尺寸。
  - DWM 圓角屬性（Windows 11）。
- 因 `WS_EX_NOACTIVATE` 會讓 `HTCAPTION` 系統拖曳失效，改由 XAML pointer 事件搭配 `GetCursorPos` 與 `SetWindowPos` 手動移動／調整大小。
- 收合時以 `AppWindow.MoveAndResize` 縮成 56 DIP 區塊，並暫停調整大小。
- 置頂採三層防護：`WS_EX_TOPMOST` 樣式、攔截 `WM_WINDOWPOSCHANGING` 強制 `HWND_TOPMOST`，以及可見期間每 2 秒與點擊時呼叫 `EnsureTopmost()`；避免 WinUI 重設視窗狀態或其他置頂視窗造成遮蓋。
- 已知限制：UAC 安全桌面與獨佔全螢幕程式仍可能暫時蓋住浮動視窗。

### 5.3 視窗位置記憶

`FloatingWindowPlacementManager`：

- 啟動時讀取 `AppSettings.FloatingWindowPlacement`，經 `WindowPlacementValidator.Normalize` 驗證後套用。
- 監聽 `AppWindow.Changed`，以 500ms 單次 `DispatcherQueueTimer` 延遲儲存，避免拖曳期間頻繁寫入磁碟。
- 收合期間保存「展開」位置，不會把 56×56 小區塊寫成下次啟動尺寸。
- 螢幕配置改變或儲存值無效時，選擇與儲存矩形交集最大的螢幕；若完全無交集，選擇最近螢幕。
- 尺寸與座標會被限制在工作區內，並套用最小／預設尺寸。

### 5.4 顯示模式與配色

`AppearanceService`：

- `AppTheme` 支援 `System`、`Light`、`Dark`。
- 跟隨系統時以 `UISettings.GetColorValue(UIColorType.Background)` 判斷明暗。
- 訂閱 `UISettings.ColorValuesChanged`，透過 `DispatcherQueue` 回到 UI 執行緒更新。
- `FloatingBackgroundPreset` 定義 12 種固定背景色，各自包含背景色、前景色與明暗標記。
- 依背景明暗計算膠囊背景與邊框透明度，確保對比。
- 主視窗、設定視窗與浮動視窗都透過 `ElementTheme` 與 `ThemeResource`／執行時筆刷套用。

### 5.5 系統匣與單一實例

- 系統匣以隱藏訊息視窗接收 `Shell_NotifyIcon` 回呼。
- 支援 `TaskbarCreated` 訊息，在 Explorer 重新啟動後重新加入圖示。
- 選單提供顯示主視窗、切換浮動視窗、結束。
- 單一實例以 Mutex 判斷，第二次啟動透過廣播視窗訊息觸發既有執行個體顯示主視窗。

## 6. 執行緒模型

- UI 與 ViewModel 操作在 WinUI UI 執行緒。
- 檔案 I/O 使用非同步 API；Core 服務使用 `ConfigureAwait(false)`。
- 模擬輸入與剪貼簿送出包在 `Task.Run`，避免長時間阻塞 UI。
- 系統色彩變化與視窗位置儲存經 `DispatcherQueue` 回到 UI 執行緒。
- 記錄 Provider 以執行緒安全方式寫入檔案。

## 7. 資料流

### 7.1 項目清單

```text
EditEntryDialog
  → MainViewModel.AddEntry / UpdateEntry / RemoveEntry
  → EntryListViewModel
  → EntryRepository.SaveAsync（背景非同步）
  → FloatingBarViewModel.RebuildItems
  → FloatingWindow 更新膠囊
```

### 7.2 設定

```text
SettingsWindow / 主視窗
  → MainViewModel 屬性變更
  → SettingsService.Current
  → AppearanceService.Apply 或 SettingsService.SaveAsync
  → 主視窗／設定視窗／浮動視窗套用主題與配色
```

### 7.3 輸入

```text
FloatingWindow.OnEntryClick
  → FloatingBarViewModel.SendAsync
      ├─ 文字項目 → InputInjectionService.SendAsync
      └─ 按鍵組合 → InputInjectionService.SendHotkeyAsync
  → ForegroundWindowTracker + NativeMethods.SendInput
  → 目標外部視窗
```

## 8. 檔案與資料格式

### 8.1 `entries.json`

```json
[
  {
    "id": "3f2504e0-4f89-11d3-9a0c-0305e82c3301",
    "kind": "text",
    "content": "常用文字內容"
  },
  {
    "id": "9c8b7a60-1f2e-4d3c-8b7a-605142536475",
    "kind": "hotkey",
    "label": "複製",
    "hotkey": "Ctrl+C"
  },
  {
    "id": "4b3a2918-0e7d-4c6f-9a51-2d8c7b6e5f40",
    "kind": "hotkey",
    "label": "功能區",
    "hotkey": "Alt+H, W"
  }
]
```

- 採 camelCase、縮排輸出；`kind` 以字串儲存，缺少時視為 `text`。
- 使用寬鬆 JSON 編碼，中文與 `+`、`,` 等字元保持可讀。
- 載入時忽略空白文字與無效按鍵組合，修正重複或空的 GUID。
- 按鍵組合會轉為正規化格式（例如 `control+escape` → `Ctrl+Esc`、`alt+h, w` → `Alt+H, W`）。
- 多步驟以逗號分隔，最多 8 步；單一組合鍵格式不變。
- 儲存時先寫入 `.tmp` 再以 `File.Move(..., overwrite: true)` 原子替換。

### 8.2 `settings.json`

```json
{
  "isOverlayEnabled": true,
  "inputMode": "simulatedInput",
  "theme": "system",
  "floatingBackgroundIndex": 0,
  "floatingWindowPlacement": {
    "x": 1200,
    "y": 200,
    "width": 220,
    "height": 340
  }
}
```

- 列舉以 camelCase 字串儲存。
- 載入時驗證列舉、背景索引與視窗尺寸；無效值回到預設。

## 9. 日誌與錯誤處理

- `FileLoggerProvider` 寫入 `%AppData%\InputAccelerator\logs\app.log`。
- 不記錄使用者文字內容。
- 未處理例外由 `App.UnhandledException` 與 `OnLaunched` 的 try/catch 捕捉。
- 輸入失敗透過 `InjectionFailed` 事件顯示系統匣氣球，並以 5 秒節流避免連續提示。
- 檔案損壞時備份為 `.bad-<時間戳>`，不直接覆寫。

## 10. 建置架構

- `Directory.Build.props` 管理版本、nullable、警告視為錯誤等共用設定。
- `Directory.Packages.props` 集中管理套件版本。
- `InputAccelerator.csproj` 支援 `x64` 與 `arm64`，`RuntimeIdentifiers` 為 `win-x64;win-arm64`。
- 發佈採 framework-dependent（`SelfContained=false`、`WindowsAppSDKSelfContained=false`），需安裝 Windows App SDK Runtime。
- `app.manifest` 使用 `processorArchitecture="*"`，同時適用 x64 與 arm64。

## 11. 測試策略

單元測試聚焦於與 UI 無關的 Core 邏輯：

- `EntryRepositoryTests`：載入、儲存、損壞檔備份、ID 正規化、按鍵組合與舊版資料相容。
- `SettingsStoreTests`：設定載入、列舉正規化、無效背景索引。
- `HotkeyParserTests`：解析、正規化、特殊鍵、無效組合、多步驟序列與步數上限。
- `HotkeyInputComposerTests`：修飾鍵順序、反向放開、Extended Key、序列批次與跨步驟修飾鍵保持。
- `TextInputComposerTests`：CRLF／LF、`\n` 逸出、Tab、Unicode 拆分。
- `TextEscapeTests`：`\n` 轉換與無效逸出保留。
- `TextPreviewTests`：預覽長度與換行處理。
- `WindowPlacementValidatorTests`：多螢幕、越界、尺寸限制、預設位置。
- `FloatingBackgroundPresetsTests`：索引範圍與色彩資料。

UI、Win32 interop、系統匣與輸入注入需以手動整合測試驗證，詳見 [建置與發佈指南](BUILD_AND_RELEASE.md)。

## 12. 擴充建議

- 新增輸入模式：擴充 `InputMode` 與 `InputInjectionService`，並在設定介面加入選項。
- 新增支援按鍵：在 `HotkeyParser.EnumerateKeys` 加入鍵名、虛擬鍵碼與 Extended Key 標記，並補充測試。
- 新增背景色：在 `FloatingBackgroundPresets.All` 加入項目，並補充資源字串。
- 新增資料欄位：修改 `AppSettings` 與 `SettingsStore.Normalize`，保持向前相容。
- 若要改採 SQLite：可替換 Core 的 Repository 實作，UI 與 ViewModel 不需大幅修改。
