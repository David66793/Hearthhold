# 守炉盾 · HearthShield

- 玩法职责：被动降低英雄承受的防御建筑伤害；不改变普通兵种的护甲。等级提高减伤比例，以战斗快照为准。
- 三维识别：左臂随动作运动的圆角盾面、锻造外框和炉心盾脐；2 级增加竖向护脊，3 级增加底部火印。卸下时装备盾牌整体消失。
- 战斗反馈：佩戴盾牌的英雄受击时，盾脐位置出现短促的暖金格挡脉冲和金属火花；没有装备的英雄及战宠不触发这套反馈。
- 代码对应：`Battle.MitigateDefenseDamage` 读取 `EquippedLevel(HearthShield)`；`HeroEquipmentArt.BuildHearthShield` 把盾挂在左上臂；`UnityPresentation.PresentHearthShieldBlock` 只在防御伤害命中装备英雄时播放。
- 验收：`tools/test-unity-player.ps1 -Case Equipment` 检查装备切换；`-Case EquipmentCombat` 检查战斗格挡反馈。后续可继续增加专用盾面网格与挡格音效。
