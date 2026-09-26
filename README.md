# 篝火堡垒 / Hearthhold

Hearthhold 是一个面向 Windows PC 的原创 3D 聚落建造与异步攻城原型。当前唯一客户端是 **Unity 6000.6.0f1 + Universal Render Pipeline (URP) 17.6.0**，使用 C#；早期 WinForms/GDI+ 预览器已经退役，不再作为构建、测试或发布目标。

## 技术栈与边界

| 层 | 实际采用的技术 | 职责 |
|---|---|---|
| 游戏规则 | 纯 C# `Hearthhold.Core` 程序集，不引用 UnityEngine | 存档、经济、建造、寻路、战斗和数据规则 |
| Unity 客户端 | `Hearthhold.Runtime`、Unity 3D、URP | 场景、模型、动画、输入、特效和存档入口 |
| 界面 | 主聚落为 **uGUI + TextMesh Pro**；部分次级/旧面板仍用 Unity IMGUI | 当前并非 UI Toolkit，也不是网页前端 |
| 编辑器与构建 | `Hearthhold.Editor`、PowerShell 脚本 | 工程准备、Windows 构建、成品烟雾测试 |
| 自动测试 | `Tests/CoreTests.cs` + Windows .NET Framework C# 编译器 | 不启动 Unity 的规则回归；另有 Unity 成品测试 |

三个 Unity 程序集通过 `.asmdef` 分层：`Runtime → Core`，`Editor → Runtime/Core`，`Core` 不反向依赖 Unity。详细入口、目录职责、协作规则及技术债见 [项目架构与协作约定](docs/ARCHITECTURE.md)。

## 当前内容

- 40×40 聚落地图，建筑建造、移动、升级、拆除、数量上限与整排城墙操作。
- 金币、晶露、仓储、离线生产和自动存档。
- 兵营管理容量，训练营管理兵种解锁，实验室管理兵种与法术等级。
- 8 类兵种、4 类法术、英雄与协同战宠，以及 10 关 PvE 战役。
- 地面寻路、破墙、空军、守军、防御反击、集火令和战斗结算。
- Unity 三维建筑、骨骼角色、攻击动作、受击反馈和动画详情预览。

具体进度、经济和解锁规则以 [进度与经济约定](docs/PROGRESSION-AND-ECONOMY-CONTRACT.md) 为准；界面设计以 [DESIGN.md](DESIGN.md) 为准；每类建筑、兵种、法术、英雄和战宠的职责、形态、效果及待办见 [对象设计档案](docs/entities/README.md)。修改相关系统前先更新对应约定，避免规则漂移。

美术并非全部由生成工具制作：当前主要三维模型来自经许可导入的 CC0 资源，另有本项目程序化网格、材质/特效与少量生成位图。资产分类、运行时优先级和新增素材登记规则见 [美术资源清单](docs/ART-ASSET-INVENTORY.md)，第三方原始来源与许可见 [THIRD_PARTY_ASSETS.md](THIRD_PARTY_ASSETS.md)。项目不包含其他商业游戏的原始素材。

## 打开与运行

工程要求 Unity `6000.6.0f1` / URP `17.6.0`：

1. 在 Unity Hub 中添加 `UnityProject`。
2. 使用指定版本打开工程。
3. 选择 `Hearthhold > Open main scene`，然后进入 Play 模式。

Windows 构建：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\build-unity.ps1 -Action Build
```

构建输出位于 `artifacts/WindowsUnity/Hearthhold.exe`。发布包可通过追加 `-Package` 生成。

## 验证

运行不依赖旧 UI 的核心规则测试：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\test-core.ps1
```

该脚本也会先执行架构边界检查：`Core` 不得引用 Unity API，`Runtime` 不得反向依赖 `Editor`，已迁移的旧 IMGUI 聚落弹窗不得重新出现。可单独运行 `tools/check-architecture.ps1`。

运行 Unity 成品烟雾测试：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\test-unity-player.ps1 -Case All
```

烟雾测试会在 `artifacts/screenshots/` 生成画面对照 PNG；隔离的 `.village.xml` 测试存档及备份写入 `artifacts/TestData/`，日志写入 `artifacts/UnityLogs/`。它们都不是游戏美术资源，也不会提交到版本控制。部分截图文件名沿用早期测试编号（如 `v090`），文件内容以最后一次烟测重新生成的时间为准，并不表示客户端仍是那个版本。

## 常用操作

| 操作 | 按键 |
|---|---|
| 移动镜头 | WASD / 鼠标中键拖动 |
| 缩放 / 复位 | 滚轮 / Home |
| 旋转镜头 | R / Shift+R |
| 建筑目录 | B |
| 编队与训练 | T |
| 兵种图鉴 | I |
| 移动 / 升级 / 拆除 | M / U / Delete |
| 撤销 / 重做移动 | Ctrl+Z / Ctrl+Y |
| 投放兵种 | 1—8 后点击战线 |
| 英雄 / 英雄技能 | H / V |
| 法术 | Q / Z / X / C |
| 集火令 | F |
| 帮助 / 全屏 | F1 / F11 |

## 存档

Unity 存档位于 `Application.persistentDataPath/village.xml`，备份文件为同目录下的 `village.xml.bak`。读取失败时不会静默覆盖原文件。更新或替换构建前建议备份该目录。

## 目录

```text
Hearthhold/
  UnityProject/
    Assets/Hearthhold/
      Core/          游戏数据、经济、战斗、寻路和存档规则
      Runtime/       Unity 表现、输入和界面
      Editor/        工程准备与 Windows 构建
    Packages/        Unity 包依赖
    ProjectSettings/ Unity 工程配置
  Tests/             与表现层无关的核心规则测试
  tools/             构建、测试和本地清理脚本
  docs/              架构、资源清单、设计约定、平衡与开发资料
  release/           当前 Unity 试玩说明
  artifacts/         本机构建和测试产物，不作为源码
```

## 本地清理

下面的命令只删除 Unity 可再生成缓存，默认保留编辑器个人设置、截图、日志、测试临时数据和当前 Windows 构建。首次重新打开 Unity 会重新导入资源，可能耗时较长；使用 `-WhatIf` 可预览：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\clean-workspace.ps1
```

增加 `-IncludeTransientArtifacts` 可清理日志、测试临时数据和测试程序；`-PruneObsoleteScreenshots` 只保留当前烟测脚本列出的截图、清理旧版 PNG 与烟测存档；`-IncludeTestArtifacts` 会清空全部截图。只有明确不需要当前试玩包时才增加 `-IncludeCurrentBuild`。`artifacts/references` 是人工保存的参考索引，不在自动清理范围。不要手动清理 `Assets`、`.meta`、`Packages` 或 `ProjectSettings`。

## 项目范围

当前版本是单机可玩原型，尚未实现联网账号、匹配、服务端权威结算、完整音效、英雄装备、更多战宠与攻城器械。玩法参考与研究基线见 [家乡玩法研究](docs/COC_HOME_VILLAGE_BENCHMARK_2026.md)，实现时应继续使用原创名称、数值、代码和美术。
