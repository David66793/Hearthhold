# 对象设计档案

此目录逐对象约定 Hearthhold 的玩法职责、三维识别轮廓、升级差异、动作/VFX 和验收标准。改动对象前先读对应档案，再改规则与美术代码；若实现与目标不同，必须在档案中标注“当前”与“待完成”，不要把计划写成已实现。

代码入口：`Core/Model.cs` 管数值和解锁，`Core/Battle.cs` 管确定性战斗，`Runtime/ModelViews.cs` 管基础 3D 模型，`Runtime/ModelIdentityArt.cs` 管逐对象的结构升级，`Runtime/SpellIdentityEffects.cs` 管四种法术各自的出场效果，`Runtime/DefenseIdentityEffects.cs` 管五类防御建筑各自的攻击效果，`Runtime/UnityPresentation.cs` 管共用战斗 VFX。详情预览由 `Runtime/ModelPreviewService.cs` 控制。所有类型均须保持三维轮廓差异，单纯换颜色不算升级。

验收：在 1440×900 与 1280×720 的正常相机距离检查正反视角；升级前后比较可见结构、动画和特效；规则变化补核心测试，视觉变化补 Windows 烟测。第三方资源必须先登记授权，不允许用 2D 看板冒充三维模型。

建筑：[议事堡](buildings/Keep.md) · [金矿](buildings/Mine.md) · [晶露池](buildings/Reservoir.md) · [兵营](buildings/Barracks.md) · [重弩炮](buildings/Cannon.md) · [哨塔](buildings/Watchtower.md) · [石墙](buildings/Wall.md) · [训练营](buildings/TrainingCamp.md) · [实验室](buildings/Laboratory.md) · [投石台](buildings/Mortar.md) · [猎空弩](buildings/AirDefense.md) · [风暴塔](buildings/ArcTower.md) · [灼光塔](buildings/BeamTower.md) · [英雄殿堂](buildings/HeroHall.md) · [战宠小屋](buildings/PetLodge.md)。

兵种：[先锋](troops/Vanguard.md) · [游侠](troops/Ranger.md) · [铁卫](troops/Guardian.md) · [破城手](troops/Sapper.md) · [翼骑](troops/SkyRider.md) · [炼金师](troops/Alchemist.md) · [医师](troops/Medic.md) · [唤灵师](troops/Summoner.md)。

法术：[疗愈](spells/Heal.md) · [战吼](spells/Fury.md) · [霜封](spells/Freeze.md) · [裂地](spells/Breach.md)。英雄：[烬卫](heroes/EmberWarden.md)。战宠：[燧爪](pets/CinderFox.md) · [苔背](pets/Mossback.md)。
