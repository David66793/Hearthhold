# 战吼 · Fury

- 机制：区域内友军获得短时伤害与速度强化，离开区域后增益逐步消失。
- 形态：琥珀心核、四瓣立体火舌与上升粒子；不能只是把疗愈喷泉换成橙色。
- 代码：`Battle.CastFury/ApplySpellZone`、`SpellIdentityEffects.PresentFuryEffect`、`SpawnSpellField` style 1。
- 当前缺口：每名受益友军的长期状态标记尚未单独实现，现以区域场和出场脉冲传达。
- 验收：强化伤害与模型特效同一时窗；移出区域后不保留完整持续场增益。
