# 灼光塔 · BeamTower

- 定位：持续锁定同一目标时伤害递增；换目标后重置积蓄。
- 轮廓：狭长聚焦核心；二级镜环和晶核，三级瞄准轨。
- 当前代码：`Battle.StepDefenses` 的 RampDamage/LockedUnitId、`ModelIdentityArt.UpgradeBeamTower`、`DefenseIdentityEffects.PresentDefenseAttackEffect` 的双层锁定光束。
- 后续动作：逐级亮度节奏应直接显示锁定增长；当前光束是每次攻击时短暂出现，尚非跨攻击间隔的持续束。
- 验收：换目标时数值重置，特效也必须重置，不能仅把普通弹体染亮。
