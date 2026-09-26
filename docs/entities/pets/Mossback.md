# 苔背 · Mossback

- 职责：规划中的守护型战宠，当前等级与预览存在，实际解锁仍由战宠小屋/规则门槛控制。
- 三维识别：宽壳、苔藓背层、方嘴、柱腿；二级壳脊，三级枝角；绝不与狐狸共用尖耳和长尾。
- 动作：壳体起伏、短步重踏、头部顶撞、受击缩颈和伏地死亡。
- 代码：`ModelIdentityArt.CreateMossbackSculpt/MossbackArtAnimator/AddPetIdentityUpgrade`、`Battle.StepUnit` 的战宠协同规则。
- 当前缺口：独特守护技能和实际解锁体验尚未完整实现；不能把通用宠物规则描述成已实现的专属机制。
- 验收：未解锁预览仍能旋转查看壳体，战斗时攻击/死亡不复用狐狸的前扑与侧倒。
