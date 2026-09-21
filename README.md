# 篝火堡垒 / Hearthhold

Hearthhold 是一个面向 Windows PC 的原创 3D 聚落建造与异步攻城原型。当前只维护 Unity 客户端；早期 WinForms/GDI+ 预览器已经退役，不再作为构建、测试或发布目标。

## 当前内容

- 40×40 聚落地图，建筑建造、移动、升级、拆除、数量上限与整排城墙操作。
- 金币、晶露、仓储、离线生产和自动存档。
- 兵营管理容量，训练营管理兵种解锁，实验室管理兵种与法术等级。
- 8 类兵种、4 类法术、英雄与协同战宠，以及 10 关 PvE 战役。
- 地面寻路、破墙、空军、守军、防御反击、集火令和战斗结算。
- Unity 三维建筑、骨骼角色、攻击动作、受击反馈和动画详情预览。

具体进度、经济和解锁规则以 [进度与经济约定](docs/PROGRESSION-AND-ECONOMY-CONTRACT.md) 为准；界面设计以 [DESIGN.md](DESIGN.md) 为准。修改相关系统前先更新对应约定，避免规则漂移。

第三方 CC0 模型的来源和许可证记录见 [THIRD_PARTY_ASSETS.md](THIRD_PARTY_ASSETS.md)。项目不包含其他商业游戏的原始素材。

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

运行 Unity 成品烟雾测试：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\test-unity-player.ps1 -Case All
```

烟雾测试会生成本地截图与日志，但这些产物不会提交到版本控制。

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
  docs/              设计约定、平衡与开发资料
  release/           当前 Unity 试玩说明
  artifacts/         本机构建和测试产物，不作为源码
```

## 本地清理

下面的命令只删除 Unity 可再生成缓存，默认保留测试截图和当前 Windows 构建：

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\tools\clean-workspace.ps1
```

增加 `-IncludeTestArtifacts` 可清理测试截图、日志与临时数据；需要同时删除当前构建时，再显式增加 `-IncludeCurrentBuild`。

## 项目范围

当前版本是单机可玩原型，尚未实现联网账号、匹配、服务端权威结算、完整音效、英雄装备、更多战宠与攻城器械。玩法参考与研究基线见 [家乡玩法研究](docs/COC_HOME_VILLAGE_BENCHMARK_2026.md)，实现时应继续使用原创名称、数值、代码和美术。
