# 项目架构与协作约定

本文件记录**当前工程事实**与今后新增文件的归属；不是一次性重排目录的计划。多人协作时先读根目录 `README.md`、本文件、`DESIGN.md` 和与本次改动有关的玩法契约。不要因为目录名里有 `Generated`、`Legacy` 或版本号就认定文件无用。

## 当前层级

```text
Hearthhold/
├─ UnityProject/
│  ├─ Assets/Hearthhold/
│  │  ├─ Core/          Hearthhold.Core：规则/数据/存档/战斗，不依赖 UnityEngine
│  │  ├─ Runtime/       Hearthhold.Runtime：Unity 输入、场景、UI、三维表现
│  │  ├─ Editor/        Hearthhold.Editor：仅编辑器运行的准备和构建入口
│  │  ├─ Resources/     运行时按名称加载的材质、模型、纹理及来源许可
│  │  ├─ Scenes/        当前主场景
│  │  ├─ Settings/      URP 渲染配置
│  │  └─ Shaders/       项目自有着色器
│  ├─ Packages/        Unity 包清单与锁文件
│  └─ ProjectSettings/ Unity 项目配置
├─ Tests/             Core 回归测试
├─ tools/             可重复执行的构建、测试、清理脚本
├─ docs/              架构、玩法契约、研究、平衡、美术决策
├─ release/           试玩包说明；不是构建输出
└─ artifacts/         本机生成产物，不入 Git
```

依赖方向：`Editor → Runtime → Core`，`Editor → Core` 也允许；`Core` 的 `.asmdef` 设置 `noEngineReferences`。核心规则不能在 UI 或模型脚本里再维护第二份数值。`Runtime/GameBootstrap.cs` 是启动/装配入口，功能由 `UnityModernHud.cs`、`UnityLayoutEditor.cs`、`UnityPresentation.cs` 等 `partial` 文件承担；`ModelViews.cs` 负责三维模型构造，`ModelPreviewService.cs` 独立拥有详情/名册预览模型、相机和 RenderTexture 生命周期。主聚落 UI 用 uGUI + TMP；战斗等面板仍有 IMGUI。这是过渡状态，不应误称为已经完成 UI 分层。

## 新增文件的归属

| 内容 | 放置位置 | 规则 |
|---|---|---|
| 存档格式、经济、兵种/建筑数据、战斗计算 | `Core/` | 不引用 Unity API；同时补 `Tests/` 回归 |
| 玩家输入、视图、动画、UI | `Runtime/` | 调用 Core；避免把规则写入 `GameBootstrap` 或某一张卡片 |
| Unity 导入/菜单/构建配置 | `Editor/` | 不进入玩家运行时程序集 |
| 在运行时通过 `Resources.Load` 加载的资产 | `Resources/` | 只放确需按路径加载的文件；避免将原始下载包整包导入 |
| 场景与渲染配置 | `Scenes/`、`Settings/`、`Shaders/` | 资产及 `.meta` 必须成对提交 |
| 可重复生成的构建、测试数据、截图、日志 | `artifacts/` | 不入 Git；由 `tools/` 脚本生成/清理 |
| 设计决策、外部来源、进度约定 | `docs/` | 修改规则前更新对应契约；美术新增前登记来源 |

之后若细分 `Runtime/UI`、`Runtime/World` 等子目录，应在一个独立变更里搬运源文件和相应 `.meta`，用 Unity 打开工程并跑测试确认 GUID、`Resources.Load` 路径及场景引用均未损坏。目前有进行中的改动，暂不进行大规模迁移。不要把业务代码继续堆进启动类；新增功能优先写专门组件或清晰命名的 `partial` 文件。

## 提交与验收约定

1. 小步提交：规则、界面、美术资源尽量分开；PR 写明影响范围、测试结果和截图（如涉及画面）。
2. 美术资源登记到 [美术资源清单](ART-ASSET-INVENTORY.md)；第三方资源补 [授权来源](../THIRD_PARTY_ASSETS.md) 与许可证副本。未知来源或许可不明的资产不能合入。
3. 保留 `Assets` 下的 `.meta`；文件重命名/移动时同步移动对应 `.meta`，不得重建 GUID。不要提交 `Library`、`Temp`、`UserSettings`、本地构建或测试存档。
4. 至少运行 `tools/test-core.ps1`；涉及 Unity 资产、UI、运行时脚本时再运行 `tools/build-unity.ps1 -Action Build` 和相关 `tools/test-unity-player.ps1` 用例。自动烟测不能替代实际鼠标试玩。
   `test-core.ps1` 会先执行 `tools/check-architecture.ps1`，防止引擎依赖渗入 Core、Runtime 反向依赖 Editor，或旧 IMGUI 聚落弹窗重新复制一套实现。
5. 改动数值、解锁、消耗前读 [进度与经济契约](PROGRESSION-AND-ECONOMY-CONTRACT.md)；改 UI 前读 [DESIGN.md](../DESIGN.md)。对外部作品只借鉴机制，不导入其原始美术或受限文件。

## 已知维护债务

- 本轮审计基线：`Core` 是 5 个无 Unity 引用的源文件；`GameBootstrap` 及其 7 个 `partial` 文件合计约 3,500 行。`partial` 只是拆文件，不是运行时解耦，因此不能宣称全项目已经达到低耦合。架构检查固定跨程序集边界，成品烟测覆盖迁移后的行为。
- `GameBootstrap` 的多个 `partial` 文件和 `ModelViews.cs` 仍偏大，不能因为它们分散在多个文件就认定已经低耦合。下一阶段应依次把战斗 HUD、阵型编辑交互和世界表现抽成独立组件，以明确输入/输出接口连接 `GameSession`；每次只迁移一条功能链并保留成品交互烟测。
- 聚落弹窗与初次远征教学已由 `Runtime/UnityModalHud.cs` 的 uGUI + TMP 承载；旧 IMGUI 弹窗和旧研究目录绘制已移除。战斗栏、阵型编辑工具栏、帮助/结算等仍使用 IMGUI，下一阶段应按功能拆成独立视图组件。
- `Resources/GeneratedArt` 里保留早期生成位图，仍被运行时代码作为回退路径引用。要移除需先替换调用、验证构建及低配/缺失模型路径，不能只删图片。
- 文档中的早期验证记录属于历史信息，当前功能以代码、测试和最新契约为准；修改时同步纠正过时状态。
