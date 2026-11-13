# 開發紀錄

最新紀錄放最上面。每次開發工作都要更新，不要事後補寫。

---

## 2026-11-13 - v1.3.6

### 目標
修正浮動視窗有時被一般視窗蓋住、導致無法操作的問題；確保浮動視窗即使面對其他置頂視窗也能維持最上層，並縮小發佈 zip 體積。

### 變更
- `NativeMethods`：新增 `WS_EX_TOPMOST`、`WM_WINDOWPOSCHANGING` 常數與 `WINDOWPOS` 結構。
- `FloatingWindowInterop`：視窗建立時加入 `WS_EX_TOPMOST` 並立即呼叫 `SetWindowPos(HWND_TOPMOST)`；新增 `EnsureTopmost()`；攔截 `WM_WINDOWPOSCHANGING`，把非 topmost 的 z-order 變更強制改為 `HWND_TOPMOST`。
- `FloatingWindow`：顯示、點擊、收合／還原時補強置頂；新增 2 秒 `DispatcherQueueTimer` 於可見期間定期補強；關閉時停止並釋放。
- `InputAccelerator.csproj`：`dotnet publish` 時排除未使用的 Windows ML 原生元件（`onnxruntime.dll`、`DirectML.dll`、`Microsoft.Windows.AI.MachineLearning.dll`），縮小 zip 體積。
- 版本提升至 1.3.6，同步更新 `CHANGELOG.md`、`RELEASE_NOTES.md`、`ARCHITECTURE.md`、`USER_GUIDE.md`、`BUILD_AND_RELEASE.md`。

### 問題與解法
- 根因：原本只在顯示時設定一次 `IsAlwaysOnTop`／`HWND_TOPMOST`，WinUI 在隱藏／顯示或切換視窗狀態時可能重設 topmost；又因 `WS_EX_NOACTIVATE`，點擊無法把視窗抬回前景，因此一旦被一般視窗蓋住就無法操作。
- 解法：以 `WS_EX_TOPMOST` 與 `WM_WINDOWPOSCHANGING` 在 Win32 層硬釘住，再用定期計時器與點擊事件補強，處理其他置頂視窗造成的排序變化。

### 待辦／已知限制
- UAC 安全桌面與獨佔全螢幕程式仍可能暫時蓋住浮動視窗，程式無法突破。
- 尚未在 Windows 10 1809 與 arm64 實機驗證。

## 2026-11-13 - v1.3.5

### 目標
將應用程式名稱由「輸入加速」改為「快速輸入」，並同步更新文件；準備提交並發佈此版本。

### 變更
- `Resources.resw`、`Directory.Build.props`、視窗標題與系統匣：顯示名稱改為「快速輸入」。
- `README.md`、`ARCHITECTURE.md`、`SYSTEM_DESIGN.md`、`USER_GUIDE.md`、`BUILD_AND_RELEASE.md`：同步更新名稱。
- 版本提升至 1.3.5，更新 `CHANGELOG.md`、`RELEASE_NOTES.md`。

### 問題與解法
- 無。

### 待辦／已知限制
- 尚未在 Windows 10 1809 與 arm64 實機驗證。

## 2026-11-13 - v1.3.4

### 目標
將「選字後自動輸出 Enter 鍵」開關改放在「啟用浮動視窗」旁邊。

### 變更
- `MainWindow.xaml`：設定卡欄位順序改為「啟用浮動視窗、自動 Enter、文字輸入方式」。
- 版本提升至 1.3.4，同步更新 `CHANGELOG.md`、`RELEASE_NOTES.md`、`USER_GUIDE.md`。

### 問題與解法
- 無。

### 待辦／已知限制
- 尚未在 Windows 10 1809 與 arm64 實機驗證。

## 2026-11-13 - v1.3.3

### 目標
讓「選字後自動輸出 Enter 鍵」開關緊鄰「文字輸入方式」右側，不再被推到視窗最右緣；文字清單項目支援連按兩下開啟編輯視窗。

### 變更
- `MainWindow.xaml`：設定卡改為 `Auto/Auto/Auto/*` 欄位，自動 Enter 開關緊接文字輸入方式；清單加入 `DoubleTapped` 事件。
- `MainWindow.xaml.cs`：抽出 `EditSelectedEntryAsync`，編輯按鈕與雙擊共用；雙擊需命中 `ListViewItem` 才開啟對話框。
- 版本提升至 1.3.3，同步更新 `CHANGELOG.md`、`RELEASE_NOTES.md`、`USER_GUIDE.md`。

### 問題與解法
- 無。

### 待辦／已知限制
- 尚未在 Windows 10 1809 與 arm64 實機驗證。

## 2026-11-13 - v1.3.2

### 目標
調整主畫面設定卡版面，將「選字後自動輸出 Enter 鍵」開關移到「文字輸入方式」右側；文字內容支援 `\n` 符號代表換行，並在新增文字欄位下方加註說明。

### 變更
- `MainWindow.xaml`：設定卡改為三欄，自動 Enter 開關移至文字輸入方式右側。
- Core：新增 `TextEscape.Unescape`，將 `\n` 轉為換行；`TextInputComposer.Compose` 與剪貼簿貼上皆先轉換，儲存內容與清單預覽仍保留 `\n` 字面。
- `EditEntryDialog.xaml`、`Resources.resw`：欄位下方新增「可使用「\n」代表換行。」提示。
- 新增 `TextEscapeTests` 與 `\n` 對應 Return 的 `TextInputComposer` 測試。
- 版本提升至 1.3.2，同步更新 `CHANGELOG.md`、`RELEASE_NOTES.md`、`USER_GUIDE.md`。

### 問題與解法
- `C:\temp\name` 這類文字中的 `\n` 也會被視為換行，屬於目前簡化規則的已知限制。

### 待辦／已知限制
- 尚未在 Windows 10 1809 與 arm64 實機驗證。

## 2026-11-13 - v1.3.1

### 目標
在主畫面新增「選字後自動輸出 Enter 鍵」開關，預設關閉；開啟後點擊浮動視窗文字並送出成功時，自動補送一次 Enter。

### 變更
- Core：`AppSettings` 新增 `AutoEnterAfterSelection`（預設 `false`），`SettingsService` 儲存快照同步複製。
- `MainViewModel`：新增可雙向繫結的屬性，載入與變更時立即保存設定。
- `FloatingBarViewModel`：送出文字時帶入自動 Enter 設定。
- `InputInjectionService`：新增自動 Enter 參數，兩種輸入模式成功後以 `VK_RETURN` 補送 Enter；失敗時不送出。
- `MainWindow.xaml`：設定卡新增「選字後自動輸出 Enter 鍵」開關與資源字串。
- 新增設定持久化測試，版本提升至 1.3.1 並更新文件。

### 問題與解法
- 無。

### 待辦／已知限制
- 自動 Enter 只在文字送出成功後執行，若目標程式封鎖模擬按鍵仍可能無效。
- 尚未在 Windows 10 1809 與 arm64 實機驗證。

## 2026-11-13 - v1.3.0

### 目標
補齊專案文件與 MIT License，新增 arm64 建置支援，並以 x64、arm64 非 Self-Contained zip 建立 GitHub 公開 Release。

### 變更
- 新增 `LICENSE`（MIT License，著作權人 mark216tw）。
- 改寫 `README.md`：補充專案說明、下載方式、Runtime 需求、文件索引與授權。
- 新增 `docs/USER_GUIDE.md`、`docs/ARCHITECTURE.md`、`docs/SYSTEM_DESIGN.md`、`docs/BUILD_AND_RELEASE.md`、`docs/THIRD_PARTY_NOTICES.md`。
- `InputAccelerator.csproj`：新增 arm64 平台與 `win-arm64` Runtime Identifier，`PlatformTarget` 依平台切換。
- `app.manifest`：`processorArchitecture` 改為 `*`，同時支援 x64 與 arm64。
- 版本提升至 1.3.0，同步更新 `CHANGELOG.md`、`RELEASE_NOTES.md`。
- 建立 x64 與 arm64 framework-dependent 發佈，打包為 zip 並建立 GitHub Release。

### 問題與解法
- 專案原本僅設定 x64，arm64 發佈會被 MSBuild 拒絕 → 擴充 `Platforms`、`RuntimeIdentifiers`，並以條件式 `PlatformTarget` 區分架構。
- 應用程式資訊清單固定 `processorArchitecture="amd64"`，不適合 arm64 → 改為 `*`，由編譯器依實際架構輸出。

### 待辦／已知限制
- 尚未在 Windows 10 1809 實機驗證。
- arm64 版本尚未於實機完整手動驗收。
- 免安裝版仍需安裝 Windows App SDK 2.6.5 Runtime。

## 2026-11-13 - v1.2.0

### 目標
在主畫面右上角加入 More 圖示，將顯示模式與浮動視窗背景移到獨立「設定」視窗，並修正背景色塊沒有顯示顏色的問題。

### 變更
- 新增 `Views/SettingsWindow`：顯示模式 ComboBox、13 個浮動視窗背景色塊、背景說明；重用 `MainViewModel` 與 `AppearanceService`。
- 主畫面標題列新增 More 圖示按鈕，設定卡只保留浮動視窗開關與文字輸入方式。
- 設定視窗為 singleton，關閉時隱藏，下次由 More 重新開啟；主視窗縮到系統匣時一併隱藏。
- `AppearanceService` 新增 `CurrentElementTheme`，讓延遲開啟的設定視窗可立即套用目前主題。
- 修正色塊：項目樣板改為固定 24×24 `Border`，`GridViewItem` 設定內容置中。
- 新增設定視窗與 More 按鈕的資源字串；版本提升至 1.2.0。

### 問題與解法
- 背景色塊只看到空白／小點：`GridViewItem` 未設定內容對齊，樣板中的 `Border` 也沒有固定尺寸，量測後幾乎為 0 → 改為固定 24×24 並置中，實測色塊中心像素為設定的背景色。
- 設定視窗若在初始外觀套用後才建立會套用到舊主題 → 新增 `CurrentElementTheme`，建構與開啟時主動套用。

### 待辦／已知限制
- 設定視窗為非模態視窗，關閉後只隱藏，不會銷毀。
- 尚未在 Windows 10 1809 實機驗證。

## 2026-11-13 - v1.1.0

### 目標
讓浮動視窗更省空間：文字改為膠囊自動換行、可收合成小區塊並還原；新增浮動視窗背景色（跟隨系統 + 12 色）與主視窗顯示模式；主視窗與編輯介面改為緊湊排版。

### 變更
- Core：新增 `AppTheme`、`FloatingBackgroundPreset` 與 12 色清單；`AppSettings` 新增 `Theme`、`FloatingBackgroundIndex`；`SettingsStore` 加入正規化。
- 新增 `AppearanceService`：統一計算顯示模式與浮動視窗配色，跟隨系統時以 `UISettings.ColorValuesChanged` 即時更新。
- 浮動視窗：`ListView` 改為 `ScrollViewer` + `ItemsControl` + 自訂 `WrapPanel`，項目為膠囊按鈕；右上角新增收合按鈕，小區塊點擊可還原。
- `FloatingWindowPlacementManager`：收合期間不寫入小區塊尺寸，記住並保存展開位置；重新顯示時不強制放大收合區塊。
- 主視窗：設定卡改為三欄，新增顯示模式與 13 個背景色塊（跟隨系統 + 12 色）；文字清單與按鈕間距縮小。
- `EditEntryDialog`：輸入框最小高度與間距縮小。
- 新增資源字串、背景色盤測試與設定正規化測試。

### 問題與解法
- `ColorUtilities` 使用 `Colors.Transparent` 在 WinUI 3 無法解析 → 改用 `Color.FromArgb(0,0,0,0)`。
- 自訂膠囊按鈕若使用預設樣式，hover 會被主題色覆蓋 → 改用自訂 `ControlTemplate`，只調整透明度。
- 收合區塊尺寸小於拖曳調整最小值 → 收合改用程式碼 `AppWindow.MoveAndResize`；另發現 `WM_GETMINMAXINFO` 的 140×80 最小限制仍會把 56×56 拉回，因此依收合狀態切換最小追蹤尺寸，並在收合時暫停調整大小。

### 待辦／已知限制
- 收合狀態不跨程式重啟保存，重新啟動一律展開。
- 收合狀態下不提供拖曳移動。
- 尚未在 Windows 10 1809 實機驗證。

## 2026-11-13 - v1.0.0

### 目標
建立 Windows「輸入加速」桌面工具：管理多組可編輯／刪除／排序的文字，啟用後顯示無邊框置頂浮動視窗，點擊文字輸入到游標所在處，並記住視窗位置與大小；App 圖示中央為「字」。

### 變更
- 建立 WinUI 3（.NET 10、Windows App SDK 2.6.5）專案、Core 類別庫與 xUnit 測試專案。
- 新增主視窗：文字清單、新增／編輯／刪除對話框、上移／下移排序、輸入方式切換、浮動視窗開關。
- 新增浮動視窗：無邊框、置頂、不搶焦點、頂端拖曳、右下角調整大小、清單項目點擊輸入。
- 新增 Win32 interop：`WS_EX_NOACTIVATE`、`WS_EX_TOOLWINDOW`、`WM_NCCALCSIZE`、`WM_GETMINMAXINFO`、DWM 圓角。
- 新增 `SendInput` Unicode 模擬輸入與剪貼簿貼上模式。
- 新增前景視窗追蹤，必要時還原目標視窗焦點後再輸入。
- 新增 JSON 儲存（`%AppData%\InputAccelerator`）：文字清單、設定、浮動視窗位置大小。
- 新增系統匣服務（原生 `Shell_NotifyIcon`）、單一實例、關閉主視窗縮到系統匣。
- 新增圖示產生腳本與多尺寸 `AppIcon.ico`。

### 問題與解法
- CommunityToolkit.Mvvm 在 WinRT 專案要求 partial property → 將 `[ObservableProperty]` 改為 `public partial` 屬性。
- `ContentDialog` 的 `x:Uid` 會嘗試解析 `.AddTitle`／`.EditTitle` 等非屬性資源而拋 `XamlParseException` → 移除對話框根節點的 `x:Uid`，按鈕文字改由程式碼設定。
- `DisplayArea.FindAll()` 在列舉時發生 `InvalidCastException` → 改用 `EnumDisplayMonitors` / `GetMonitorInfo` 取得螢幕工作區。
- 浮動視窗不呼叫 `Activate()`，導致 `x:Bind` 的 `ItemsSource` 未更新 → 直接指定 `ListView.ItemsSource`，項目樣板改用一般 `Binding`。
- 使用 `HTCAPTION` 讓系統拖曳在 `WS_EX_NOACTIVATE` 視窗上無效 → 改由 XAML pointer 事件手動移動視窗。
- 無邊框搭配系統 resize frame 會留下約 9px 可見外框 → 使用 `WM_NCCALCSIZE` 移除框架，並在右下角加入 XAML 調整大小把手。
- 在記事本連續輸入多行中文時，Enter 後字元偶爾亂序或遺失 → 在 Enter／Tab 後延遲 50ms，Unicode 字元分批送出。
- 主視窗的強制前景在 Windows 前景鎖定下可能失敗 → 以 `AttachThreadInput` 輔助 `SetForegroundWindow`。

### 待辦／已知限制
- 尚未在 Windows 10 1809 實機驗證。
- 系統管理員權限的目標程式需以相同權限啟動本程式才能模擬輸入。
- 終端機／部分遊戲可能需使用剪貼簿模式；剪貼簿模式會覆蓋目前剪貼簿。
- Windows 10 不支援 DWM 圓角。
- Release 免安裝發佈尚未執行。
