# CoinCollector
# Coin Collector — 莫兰迪色系 3D 金币收集游戏

Unity 2021.3.45f2c1 · 3D · 课程作业交付版本

一个完整的第三人称 3D 金币收集游戏：**主菜单 → 关卡 → 收集金币 → 胜利面板 → 重开 / 返回菜单**。
打开项目即可直接运行，无需手动创建物体、挂载脚本或连接 Inspector 引用。

运行截图如下
![Uploading 01_MainMenu.png…]()


## 1. 如何打开与运行

| 步骤 | 操作 |
| --- | --- |
| 1 | 用 **Unity Hub** → `Open` → `Add project from disk`，选择本文件夹（`Collector2`） |
| 2 | Unity 版本选择 **2021.3.45f2c1**（项目已用该版本生成，升级版本可能导致材质/UI 表现差异） |
| 3 | 打开后，在 `Assets/Scenes/` 双击 **`MorandiMainMenu`**（首次打开若弹出场景丢失提示，选 `MorandiMainMenu` 即可） |
| 4 | 点击编辑器顶部 **▶ Play** |
| 5 | 主菜单点击 **START GAME** 进入关卡，**QUIT** 退出（在编辑器内点击 QUIT 只会在 Console 打印日志，这是 Unity 编辑器的正常行为，打包后才会真正退出） |

> **推荐做法**：直接打开 `MorandiMainMenu` 而不是 `SampleScene`。`SampleScene` 是新建项目时的原始场景，与本项目无关，未做任何修改。

---

## 2. 操作方式

| 按键 / 按钮 | 作用 |
| --- | --- |
| `W` / `A` / `S` / `D`（或方向键） | 移动椭圆玩家（方向以摄像机水平朝向为参考） |
| `START GAME` | 进入关卡 `MorandiGameScene` |
| `QUIT` | 退出程序（打包后生效） |
| `Restart` | 重新加载关卡，分数归零 |
| `Main Menu` | 返回主菜单 |

目标：收集全部 **10 枚金币**（HUD 显示 `Coins: 0 / 10`），达到 10 分即弹出胜利面板 `YOU WIN!`，并显示最终分数。胜利后玩家停止移动、不再计分。

---

## 3. 项目结构

```
Assets/
├── Scenes/
│   ├── MorandiMainMenu.unity        # 主菜单（Build Index 0）
│   ├── MorandiGameScene.unity       # 关卡（Build Index 1）
│   └── SampleScene.unity            # Unity 新建项目自带，未使用、未修改
├── Scripts/
│   ├── PlayerMovement.cs            # WASD 移动（Rigidbody 驱动）
│   ├── Coin.cs                      # 金币触发器与计分
│   ├── GameManager.cs               # 分数 / 目标 / 胜利状态
│   ├── UIManager.cs                 # HUD 与胜利面板
│   ├── MainMenu.cs                  # 主菜单按钮逻辑
│   ├── CameraFollow.cs              # 摄像机跟随 / 固定俯视
│   └── CoinCollector.asmdef         # 程序集定义
├── Materials/
│   ├── Mat_Ground                   # #A9B9A5 灰调鼠尾草绿
│   ├── Mat_Wall                     # #A9BBC5 灰调浅蓝
│   ├── Mat_Obstacle                 # #8FA99A 灰调豆沙绿
│   ├── Mat_Player                   # #C68E85 低饱和陶土粉
│   ├── Mat_Coin                     # #D8B15B 柔和金（Metallic 0.85 / Smoothness 0.62）
│   ├── Mat_CoinHighlight            # #F1D995 浅香槟金
│   └── Mat_UIPanel                  # #E8E1D5 暖灰米白
├── Prefabs/
│   ├── Player.prefab                # 椭圆玩家（Sphere 非等比缩放）
│   └── Coin.prefab                  # 扁平圆柱金币（含 Trigger）
├── Editor/
│   ├── ProjectBuilder.cs            # 一键重建全部场景 / 材质 / UI / 引用
│   ├── ProjectVerifier.cs           # 69 项静态验收
│   ├── PhysicsSimulationTest.cs     # 物理与碰撞仿真
│   ├── PlayModeSmokeTest.cs         # 冒烟测试
│   └── ScreenshotTool.cs            # 一键截取运行画面
└── Audio/ UI/                       # 占位目录（当前版本无外部音频依赖）
```

根目录另有 `VerificationReport.md`（静态验收报告）与 `PhysicsReport.md`（物理仿真报告）。

---

## 4. 场景配置清单

### MorandiMainMenu

| 对象 | 关键配置 |
| --- | --- |
| Canvas | `Scale With Screen Size`，参考分辨率 **1920 × 1080** |
| TitleText | `COIN COLLECTOR`，字体 Arial，颜色 #45564D |
| StartButton | OnClick → `MainMenuController.StartGame()`（加载 MorandiGameScene） |
| QuitButton | OnClick → `MainMenuController.QuitGame()`（`Application.Quit()`） |
| EventSystem | 已添加，按钮可点击 |
| Main Camera | 背景色 #B8C7CC |

### MorandiGameScene

| 对象 | 关键配置 |
| --- | --- |
| Ground | 立方体，尺寸 32 × 1 × 32，顶面 y = 0，材质 Mat_Ground，实体碰撞体 |
| Walls | 四面封闭，半场 16 米，墙高 3、厚 1，材质 Mat_Wall，实体碰撞体（非 Trigger） |
| Obstacles | 5 个（要求 ≥ 4），位置 (−6,−6)、(7,−4)、(−8,7)、(5,8)、(0.5,3)，尺寸高 1.5、厚 2，实体碰撞体 |
| Player | 球体非等比缩放 **(1, 1.3, 0.8)**（椭圆，非胶囊），Tag = `Player`，Rigidbody（重力开启、Continuous、Interpolate、FreezeRotation 全部旋转），起点 (0, 0.65, −12) |
| Coins | 10 枚，扁平圆柱（0.9 × 0.06 × 0.9），高度 y = 0.7，Trigger 碰撞体，材质 Mat_Coin |
| Main Camera | 固定俯视 (0, 26, −22)、旋转 (50, 0, 0)，FOV 60，挂 CameraFollow |
| Directional Light | 主光源 + 环境光（Sky #B8C7CC / Ground #8FA99A）+ Reflection Probe (0, 6, 0)，保证金属金币可见 |
| Canvas / ScoreText | HUD `Coins: 0 / 10` |
| VictoryPanel | 初始隐藏，含 `YOU WIN!`、最终分数、`Restart`、`Main Menu` |
| GameManager | targetScore = 10，已连接 UIManager 与 PlayerMovement，场景内唯一 |
| UIManager | ScoreText / VictoryPanel / VictoryScoreText / RestartButton / MainMenuButton 全部连接 |
| EventSystem | 已添加 |

---

## 5. 脚本职责

| 脚本 | 职责 |
| --- | --- |
| `PlayerMovement.cs` | `Input.GetAxisRaw` 读取 WASD；`FixedUpdate` 中用 Rigidbody 速度驱动（不直接改 Transform，故不穿墙）；方向取摄像机水平投影并归一化；`SetMovementEnabled(false)` 在胜利时禁用移动 |
| `Coin.cs` | `OnTriggerEnter` + `CompareTag("Player")`，每枚只触发一次（内部 `isCollected` 标志），调用 `GameManager.AddScore(value)` 后销毁自身 |
| `GameManager.cs` | 场景内单例（重复实例自毁）；`AddScore`；达到目标分数调用 `UIManager.ShowVictory` 并禁用移动；胜利后不再加分 |
| `UIManager.cs` | `UpdateScore(score, target)` 更新 HUD；`ShowVictory` 显示面板；`RestartGame()` / `GoToMainMenu()` 切换场景（切换前校验场景是否在 Build Settings 中） |
| `MainMenu.cs` | `StartGame()` 加载关卡；`QuitGame()` 退出并在编辑器中打印日志便于验证 |
| `CameraFollow.cs` | 支持跟随玩家或固定俯视（关卡中使用固定俯视，保证全场可见） |

---

## 6. 如何打包（Build）

1. `File → Build Settings`
2. 确认场景顺序：**Index 0 = `MorandiMainMenu`**，**Index 1 = `MorandiGameScene`**（已配置好，不要新增 `SampleScene`）
3. Platform 选 `PC, Mac & Linux Standalone`，Target Platform 选 `Windows`，Architecture `x86_64`
4. 点击 `Build`，选择输出文件夹（例如 `Builds/Windows`），等待编译完成
5. 运行生成的 `CoinCollector.exe` → 首先进入主菜单

> 若 Build Settings 中场景为空（例如误操作），请用菜单 **`CoinCollector → Build Complete Project`** 一键重建场景与 Build Settings，然后用 **`CoinCollector → Verify Project`** 复检。

---

## 7. 编辑器自动化菜单

打开 Unity 后，顶部菜单 **CoinCollector** 下提供：

| 菜单项 | 作用 |
| --- | --- |
| `Build Complete Project` | 从零重建目录、材质、预制体、两个场景、UI 绑定与 Build Settings |
| `Build And Verify` | 重建 + 立即执行静态验收 |
| `Verify Project` | 运行 69 项静态检查，输出 `VerificationReport.md` |
| `Run Physics Simulation Test` | 物理步进仿真，输出 `PhysicsReport.md` |
| `Run PlayMode Smoke Test` | 冒烟测试 |
| `Capture Screenshots` | 一键截取主菜单 / 关卡 / 胜利面板到 `Screenshots/` |

---

## 8. 运行截图

截图已生成，位于 `Screenshots/`（如需重新截取，在 Unity 中点击 `CoinCollector → Capture Screenshots`）：

| 画面 | 文件 |
| --- | --- |
| 主菜单 | `Screenshots/01_MainMenu.png` |
| 游戏关卡 + HUD | `Screenshots/02_GameScene.png` |
| 胜利面板 | `Screenshots/03_Victory.png` |

---

## 9. 已知限制

- 编辑器内点击 `QUIT` 不会关闭 Unity（Unity 设计如此），仅打印 Console 日志；打包后才真正退出。
- 当前版本未加入音效（`Assets/Audio/` 为占位目录）。
- 摄像机使用固定俯视，玩家可能移动到被障碍物遮挡的位置；如需更强跟随感，可在 `Main Camera` 的 `CameraFollow` 组件上勾选 `followTarget`。

