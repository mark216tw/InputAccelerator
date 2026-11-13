# 第三方程式元件授權

本專案自身以 [MIT License](../LICENSE) 授權。以下為主要相依元件的授權說明。實際授權條款以各套件隨附內容為準。

| 元件 | 版本 | 用途 | 授權 |
| --- | --- | --- | --- |
| .NET Runtime | 10 | 執行平台 | MIT License |
| Windows App SDK（Microsoft_WindowsAppSDK） | 2.6.5 | WinUI 3 與 Windows 平台功能 | Microsoft 授權條款 |
| CommunityToolkit.Mvvm | 8.4.2 | MVVM 基礎建設 | MIT License |
| Microsoft.Extensions.Hosting | 10.0.14 | 主機與相依注入 | MIT License |
| Microsoft.Windows.SDK.BuildTools | 10.0.26100.4654 | Windows SDK 建置工具 | Microsoft 授權條款 |
| xUnit | 2.9.3 | 單元測試 | Apache License 2.0 |
| Microsoft.NET.Test.Sdk | 17.14.1 | 測試執行 | MIT License |

## 注意事項

- 本專案的 MIT License 僅適用於本專案原始碼與自行產生之資產，不改變第三方程式元件的授權。
- 重新散布本專案發佈檔時，應同時遵守上述元件的授權條款。
- 套件版本集中於 `Directory.Packages.props`，升級前應確認對應授權與相容性。
