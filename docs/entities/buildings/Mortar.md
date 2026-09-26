# 投石台 · Mortar

- 定位：超远程地面范围伤害，有近距盲区，惩罚密集阵型。
- 轮廓：宽底炮臼；二级弹药架与备弹，三级测距环。
- 当前代码：`Rules.Buildings[Mortar]` 的 MinRange/SplashRadius、`Battle.StepDefenses`、`ModelIdentityArt.UpgradeMortar`、`DefenseIdentityEffects.PresentDefenseAttackEffect` 的高抛石块、落点冲击环与扬尘。
- 后续动作：落地扬尘与实际弹体到达时间应同步；当前出场反馈是即时生成。
- 验收：近身士兵不会被投石台错误命中，落点范围可读。
