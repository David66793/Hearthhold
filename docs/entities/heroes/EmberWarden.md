# 烬卫 · EmberWarden

- 职责：独立英雄槽，不占营位；高生命重装突进，装备改变抗伤、破墙和主动技能。
- 三维识别：炉心胸甲、锻造披风与冠盔；二级指挥肩甲，三级誓约羽冠，不复用普通铁卫的升级饰件。
- 主动技能：炉心号令恢复自身生命并强化附近友军；金色扩散波、跟随英雄的持续光环和盟友启动脉冲与增益持续时间一致。
- 代码：`Battle.CastHeroSkill`、`ModelIdentityArt.AddEmberWardenIdentity`、`HeroEquipmentArt.AddHeroEquipmentArt`、`UnityPresentation.SpawnHeroCommand/HeroCommandVisual`。
- 当前：装备会按实际槽位增减不同三维构件，预览随槽位和装备等级即时重建；战斗读取出征时的装备快照。
- 当前缺口：英雄仍与铁卫共享一套导入骨骼底模，但不复用铁卫等级饰件；下一阶段应换独立盔甲/轮廓资产。装备件现在是程序化原型，尚需独立精模、绑定与材质打磨。
- 验收：技能成功才播放特效，死亡后光环停用；和铁卫从正反两面都能分辨。
