# 裂地 · Breach

- 机制：一次性区域破墙，造成高额墙体伤害，可打开新的地面路线。
- 形态：由中心向外扩散的双层冲击环、飞散石块和地表火花；没有持续场。
- 代码：`Battle.CastBreach`、`SpellIdentityEffects.PresentBreachEffect`、`Pathfinder` 的地图修订重算。
- 当前缺口：地面裂纹实体网格和不同墙级的破碎层次尚未实现。
- 验收：破墙后目标选择立即重算；与持续治疗/战吼/霜封在时间结构上明显不同。
