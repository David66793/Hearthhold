# 行军火炬 · MarchTorch

- 玩法职责：增加英雄号令的影响半径与持续时间，强化前压型队伍的集结能力。
- 三维识别：背部铁柄、敞口火盆、持续跳动的立体火舌与小范围暖光；二级出现燃烧核心，三级分出侧焰。不能只改变英雄技能光环的颜色。
- 当前代码：`Rules.HeroCommandRadius/HeroCommandTicks` 同时供 `Battle.CastHeroSkill` 和 `HeroCommandVisual` 使用；`HeroEquipmentArt.BuildMarchTorch` 与 `HeroEquipmentMotion` 管局部火焰动画。
- 验收：装备、卸下、升级均改变预览模型；号令边界按实际半径显示，结束时间与规则一致。
