# 美术资源清单与来源规则

结论：**不是全部生成的。** 当前游戏主体是 Unity 三维场景，主建筑与多数兵种优先加载第三方 CC0 FBX 模型；自有程序化网格承担城墙、补件、等级装饰、部分战宠与回退模型；另有生成位图用于旧回退显示及个别 UI 图案。不要把生成的 PNG 当作当前三维模型，也不要因它们看似旧版就直接删除。

| 类别 | 工程位置 / 实例 | 来源与当前用途 | 处理原则 |
|---|---|---|---|
| 第三方建筑模型 | `Resources/ThirdParty/KayKitMedieval/` | KayKit Medieval Hexagon Pack，CC0；当前建筑优先加载 | 保留用到的 FBX、贴图、许可证及 `.meta` |
| 第三方角色模型 | `Resources/ThirdParty/QuaterniusRPG/`、`KayKitAdventurers/` | Quaternius RPG 和 KayKit Adventurers，CC0；按兵种映射到三维骨骼角色/动画 | 保留已引用子集及许可证；未使用原始包不导入 |
| 项目程序化三维 | `Core/ModelGeometry.cs`、`Runtime/ModelViews.cs`、`Runtime/ModelIdentityArt.cs`、`Runtime/SpellIdentityEffects.cs`、`Runtime/DefenseIdentityEffects.cs` 等 | 项目代码生成墙体、逐对象等级饰件、战宠形体、法术及防御特效/回退模型；设计档案见 `docs/entities/` | 属于源码，不是可清理缓存 |
| 生成位图 | `Resources/GeneratedArt/` | 早期建筑图及兵种图集；`ModelViews.cs` 仍按名称加载作为回退显示 | 目前仍属运行时依赖；替换调用并验证后才能考虑移除 |
| UI 位图 | `Resources/UI/ExpeditionEmblemV1.png` | 项目内的远征徽章位图，聚落 HUD 使用 | 按 UI 资产维护，勿与 FBX 混淆；原始生成记录待补 |
| 材质、Shader、URP 设置 | `Resources/*.mat`、`Shaders/`、`Settings/` | 工程内创建/配置；支撑当前渲染、特效和显示 | 属于工程源资产，保留 `.meta` |
| 美术方向资料 | `docs/art/`、`docs/VISUAL-DIRECTION.md`、`docs/ART-VERTICAL-SLICE-3D.md` | 对照图、目标画风及 3D 验收说明 | 保留作为设计依据，不放入运行时资源 |

第三方作者、原始网址、许可和导入子集以 [THIRD_PARTY_ASSETS.md](../THIRD_PARTY_ASSETS.md) 为准；许可证原件放在相应 `ThirdParty` 目录。`ModelViews` 当前优先尝试外部三维模型，找不到才使用程序化几何；早期生成位图的加载入口仍存在，所以“清理无用素材”必须通过代码引用和实际构建双重验证。

现有生成位图的逐张提示词、模型版本和原始任务编号未在仓库中完整保存，不能反推出精确生产过程；这是一项待补的资产溯源缺口。今后新增素材按下述字段登记，不能把“文件名含 Generated”当作完整的来源证明。

## 新资源入库登记

每批新资源在本文件或独立资产登记表至少记录：资产名称/文件路径、作者与来源网址、许可及许可证副本、原始格式和导入改动、当前使用位置、能否再生成。AI 生成资源记录所用工具、生成日期、提示词或生成任务标识、人工修改和适用范围；来源不明者标为待核验，不进入正式版本。购买资源还要记录购买主体与授权范围，不能因为“仅自己玩”就省略许可核对。

新增图/模型应优先在 Unity 内按实际游戏视角验收：轮廓清晰、能旋转观察、与地面接触自然，动画和材质风格协调。单张精美平面图不能代替可用的三维建筑或角色。
