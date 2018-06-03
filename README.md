# Agugu

Agugu 是一個基於 [Ntreev Photoshop Parser](https://github.com/NtreevSoft/psd-parser) 的 Photoshop PSD to Unity uGUI 匯入工具。除了單次匯入的功能之外，也可以追蹤 PSD 檔案更新，並將使用在舊的 UI 結構上新增的 GameObject 與 Component 移植到新產生的 UI 結構。希望能達到修改 UI 排版只需要美術修改 PSD 而不需程式輔助。

### 注意事項

目前還在開發中，並非 Production Ready，幾個比較大的問題：

- Text 圖層轉換成 uGUI Text 字型大小還沒有辦法做到一致，只能近似
- 只有在 Photoshop CC 2018 最大相容性格式測試，有遇過 CS6 檔案無法讀取要用 CC 2018 轉存後才能使用的情況
- 可能有 UX 上不佳的地方
- 可能有 bug

### 使用方法

先在 Assets/Create 下創建 Agugu Config

<p align="center">
  <img src="https://github.com/FrankNine/Agugu/blob/develop/Documents/Images/CreateAguguConfig.png?raw=true" alt="Create Config"/>
</p>

然後將 PSD 檔放到 Assets 下，選擇 PSD 後按上方選單的 Agugu/Import

<p align="center">
  <img src="https://github.com/FrankNine/Agugu/blob/develop/Documents/Images/MenuImport.png?raw=true" alt="Import Menu"/>
</p>

Import Selection With Canvas 會在做出 Prefab 後幫你創建跟文件一樣大的 Canvas，然後把匯入好的 uGUI Prefab Instance 放在下面。如果沒有既有的 Canvas 則建議使用 Import With Canvas。

或者是在 Agugu Config 下把 PSD 加到 Tracked PSD 清單，當 PSD 有改動時會觸發 PostProcessor 重新生成 Prefab。*Aagugu 會試圖將所有手動 Apply 到 Prefab 的修改移動到重新生成的 Prefab，如果有修改被遺漏請回報。*

<p align="center">
  <img src="https://github.com/FrankNine/Agugu/blob/develop/Documents/Images/AguguConfig.png?raw=true" alt="Agugu Config"/>
</p>

如果你有使用 Photoshop 的 Text 圖層應該要把字型檔也匯入 Unity，然後將字型名稱跟字型加到 Font Lookup 裡。當 Agugu 處理 Text 沒有從 Font Lookup 找到字型時會提醒你：

<p align="center">
  <img src="https://github.com/FrankNine/Agugu/blob/develop/Documents/Images/FontNotFound.png?raw=true" alt="Font Not Found"/>
</p>

### 圖層標記

Agugu 可以從 PSD 讀取各圖層的標記設定。要編輯圖層的標記，請在 Photoshop File/Scripts/Browse... 讀取 Agugu.jsx 開啟圖層標記工具（這個工具預計之後會使用 Adobe CEP 重寫 UI）

<p align="center">
  <img src="https://github.com/FrankNine/Agugu/blob/develop/Documents/Images/AguguPhotoshop.PNG?raw=true" alt="Agugu Photoshop UI"/>
</p>

- 左邊是圖層清單，前綴是 Photoshop 內建 Layer ID。選擇圖層後可以編輯標記
- 上方是現有標記清單，也可以按 Clear 清除這個圖層所有標記
- 如果圖層標記為 Skip 就會被忽略，如果 Group 被標為 Skip 則以下的圖層都會被忽略
- 如果在一般圖層打上 empty 標記，則該圖層貼圖不會被匯入，但是一樣位置大小會加上一個 EmptyGraphic，這是用來做透明的觸控範圍
- Anchor 跟 Pivot 同 uGUI，設定後圖層視覺位置不變，但是 Anchor Pivot 會照設定改變
- 任何修改後都要按 Serialize 按鈕把設定存回 PSD 的 XMP Block，然後還要儲存 PSD 本體
- ESC 關閉這個視窗
