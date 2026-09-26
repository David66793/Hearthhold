# 猎空弩 · AirDefense

- 定位：只锁定空中单位，高单次伤害；地面兵可绕过其火力。
- 轮廓：朝天瞄准架；二级追踪镜和叉架，三级高度风向叶。
- 当前代码：`Rules.Buildings[AirDefense]` 的 TargetsAir/TargetsGround、`ModelIdentityArt.UpgradeAirDefense`、`DefenseIdentityEffects.PresentDefenseAttackEffect` 的双弩轨迹。
- 后续动作：炮架俯仰追随空中目标，进一步让双弩箭尾轨迹区别于哨塔。
- 验收：没有空军时不射击；立体瞄准轮廓清楚表现“对空”。
