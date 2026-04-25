# 基础输入演示 — 如何导入

示例代码在包内的 **`Samples~/BasicDemo`** 中。带 **`~` 的文件夹在 Project 里**默认不展开显示**，所以会感觉「包里没有示例」——**需要从包管理器导入**。

## 在团结 / Unity 中导入

1. 菜单 **Window（窗口）→ Package Manager（包管理器）**  
2. 窗口左上角，包来源选 **In Project / 在项目中**（或仅显示本工程内包）  
3. 左侧列表中选中 **意识流魂核 (com.soulcore.unity)**  
4. 在右侧包详情**向下滚动到「Samples / 样本 / 示例」**区域  
5. 找到 **「基础输入演示」**，点击 **Import / 导入**  
6. 导入后，在 **Project** 里一般会出现：  
   `Assets/Samples/意识流魂核/1.3.1/基础输入演示（或类似路径）/BasicDemo/…`  
   （具体路径随 Unity 版本可能略有不同）

## 使用方式

- 在场景里给**任意物体**挂上 **`BasicDemoInput`**。  
- 在 Inspector 里把 **`Npcs` 列表**里拖入你挂了 **魂核 NPC** 的物体。  
- **Play** 后按键盘 **1 / 2 / 3** 分别模拟对话、赠礼、侮辱，**Console** 会打印决策与说明。

## 没有「Samples」区块时

- 确认选中的包是 **本工程** 的 `com.soulcore.unity`（`manifest` 的 `file:` 本地包也支持 Samples）。  
- 尝试在 Package Manager 里 **刷新 / 重开窗口**，或**重新聚焦**到该包。  
- 仍没有：可菜单 **魂核 → 帮助 → 如何导入基础输入示例** 查看与本文相同的提示。
