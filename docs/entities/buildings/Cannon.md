# 重弩炮 · Cannon

- 定位：仅对地面单体重击，针对高血量前排；不能对空。
- 轮廓：重炮机座与炮口；二级后膛和后坐轨，三级压力表。
- 当前代码：`Rules.Buildings[Cannon]`、`Battle.StepDefenses`、`ModelIdentityArt.UpgradeCannon`、`DefenseIdentityEffects.PresentDefenseAttackEffect` 的重弹道。
- 后续动作：炮口后坐与弹壳反馈应按射击节奏绑定；当前保留通用攻击动画兜底。
- 验收：炮弹、火花和目标受击能对应一次射击；与哨塔的箭矢不能共用同一 VFX。
