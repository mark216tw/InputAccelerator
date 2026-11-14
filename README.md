# 快速輸入（InputAccelerator）

快速輸入是一款 Windows 桌面小工具，用來集中管理常用文字與按鍵組合，並透過可置頂、可拖曳、可調整大小的無邊框浮動視窗，把內容快速送到目前游標位置。適合需要反覆貼上固定內容或執行固定快捷鍵的情境，例如客服回覆、表單填寫、程式片段、常用地址與簽名檔。

本專案以 C#、.NET 10 與 WinUI 3（Windows App SDK）開發，提供 x64 與 arm64 的免安裝版本，採 MIT License 授權。

## 功能特色

- 新增、編輯、刪除多行文字或按鍵組合，並以上移／下移按鈕手動排序。
- 支援「按鍵組合」項目，可直接錄製 `Ctrl+C`、`Alt+F4` 等組合鍵，點擊後送出到目標視窗。
- 支援多步驟按鍵序列，例如 `Alt+H, W`、`Ctrl+K, Ctrl+C`；錄製停止約 1 秒後自動完成，最多 8 步。
- 浮動視窗永遠置頂、無標題列，點擊項目即可將文字或按鍵組合送到前一個使用中的視窗。
- 浮動視窗不搶焦點，點擊後目標程式仍能收到內容。
- 項目以緊湊膠囊排列，依視窗寬度自動換行，內容過多時可垂直捲動。
- 浮動視窗可拖曳移動、從右下角調整大小，並記住位置與大小。
- 浮動視窗可縮小為小區塊，點擊小區塊即可還原原來的位置與大小。
- 浮動視窗可設定閒置自動縮小，預設不自動縮小。
- 主視窗右上角的 More 按鈕可開啟獨立「設定」視窗。
- 設定視窗提供顯示模式、浮動視窗背景、自動縮小時間與文字輸入方式，按下「儲存」後生效。
- 顯示模式支援跟隨系統、淺色、深色。
- 浮動視窗背景支援跟隨系統與 12 種固定色，文字與膠囊會自動使用對比色。
- 支援「模擬鍵盤輸入」與「剪貼簿貼上」兩種輸入方式。
- 可選擇在文字送出後自動補送 Enter 鍵，預設關閉；按鍵組合不套用。
- 系統匣常駐，關閉主視窗後仍可從系統匣重新開啟。
- 單一實例：重複啟動時會帶回既有主視窗。
- 內建「字」App 圖示，支援淺色、深色與高對比背景。

## 系統需求

- Windows 10 1809（build 17763）以上或 Windows 11。
- 免安裝版需要先安裝 **Windows App SDK 2.6.5 Runtime**（x64 或 arm64 對應版本）。
- 開發需要 .NET 10 SDK。

> 本專案不提供安裝檔（MSI／MSIX）。請下載對應架構的 zip、解壓縮後直接執行 `InputAccelerator.exe`。

## 下載與執行

請至 [GitHub Releases](../../releases) 下載最新版本：

| 檔案 | 架構 | 說明 |
| --- | --- | --- |
| `InputAccelerator-<版本>-win-x64.zip` | x64 | 適用多數 Intel／AMD Windows 電腦 |
| `InputAccelerator-<版本>-win-arm64.zip` | arm64 | 適用 Windows on Arm 裝置 |

兩者皆為 framework-dependent（非 Self-Contained）版本，解壓縮後直接執行 `InputAccelerator.exe`。若缺少 Windows App SDK Runtime，程式會無法啟動，請先安裝對應架構的 Runtime。

## 快速開始

1. 解壓縮下載的 zip。
2. 啟動 `InputAccelerator.exe`，會開啟主視窗與浮動視窗。
3. 在主視窗按「新增」，選擇「文字」並輸入常用文字（可含換行，或以 `\n` 代表換行），或選擇「按鍵組合」錄製快捷鍵；多步驟可依序按下，例如 `Alt+H` 後放開 Alt 再按 `W`，停止約 1 秒會自動完成並顯示 `Alt+H, W`。
4. 將游標放到目標程式（例如記事本、瀏覽器、Word），再點擊浮動視窗中的項目。
5. 需要調整輸入方式、主題或背景色時，點擊主視窗右上角的 More 圖示，調整後按「儲存」。

完整操作說明請見 [使用指南](docs/USER_GUIDE.md)。

## 資料位置

```text
%AppData%\InputAccelerator\
  entries.json      # 文字與按鍵組合項目清單
  settings.json     # 輸入方式、顯示模式、浮動視窗背景、自動縮小時間、開關與位置大小
  logs\app.log      # 執行記錄（不記錄文字內容）
```

移除程式不會自動刪除上述資料；如需完全清除，請手動刪除 `%AppData%\InputAccelerator` 資料夾。

## 建置與測試

```powershell
# 建置 Debug
dotnet build InputAccelerator.slnx -c Debug

# 執行單元測試
dotnet test InputAccelerator.slnx -c Debug

# 檢查程式碼格式
dotnet format InputAccelerator.slnx --verify-no-changes
```

建置輸出位於：

```text
src/InputAccelerator/bin/x64/Debug/net10.0-windows10.0.19041.0/InputAccelerator.exe
```

發佈 x64 與 arm64 免安裝 zip 的完整步驟請見 [建置與發佈指南](docs/BUILD_AND_RELEASE.md)。

## 專案結構

```text
src/InputAccelerator/          WinUI 3 應用程式（UI、服務、Win32 interop）
src/InputAccelerator.Core/     與 UI 無關的模型、儲存與文字處理邏輯
tests/InputAccelerator.Tests/  xUnit 單元測試
tools/GenerateIcon.ps1         圖示產生工具
docs/                          使用、架構、設計與發佈文件
```

## 文件索引

- [使用指南](docs/USER_GUIDE.md)
- [系統架構與技術文件](docs/ARCHITECTURE.md)
- [系統設計文件](docs/SYSTEM_DESIGN.md)
- [建置與發佈指南](docs/BUILD_AND_RELEASE.md)
- [第三方程式元件授權](docs/THIRD_PARTY_NOTICES.md)
- [變更記錄](CHANGELOG.md)
- [版本說明](RELEASE_NOTES.md)
- [開發紀錄](docs/DEV_LOG.md)

## 已知限制

- 若目標程式以系統管理員權限執行，本程式需同樣以管理員權限執行，Windows 才會允許模擬輸入。
- 終端機、部分遊戲或不接受 Unicode 模擬輸入的程式，可能需切換成「剪貼簿貼上」。
- 按鍵組合固定以模擬按鍵送出，不受「文字輸入方式」與「自動輸出 Enter」影響。
- 多步驟序列以逗號分隔，例如 `Alt+H, W`；`Alt+H+W` 不是有效格式。
- 序列最多 8 步，步驟之間固定停頓約 150ms；目標程式若需要更長的反應時間，可能需調整錄製方式或等待後續版本。
- `Win+L`、`Ctrl+Alt+Del` 等系統保留組合鍵可能由 Windows 直接處理，無法保證送到目標程式。
- 剪貼簿模式會覆蓋目前剪貼簿內容，且不會自動還原。
- Windows 10 沒有 DWM 圓角視窗效果，外框會是直角。
- 浮動視窗的收合狀態不會保留，重新啟動時一律以展開狀態開啟。
- 尚未在 Windows 10 1809 實機驗證。

## 授權

本專案採用 [MIT License](LICENSE)。
