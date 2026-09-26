# 霜封 · Freeze

- 机制：区域内防御建筑停止攻击，解除后继续按原规则攻击。
- 形态：冰棱核心、斜置晶柱和低亮度蓝色边界；受控塔脚下另有冻结封印。
- 代码：`Battle.CastFreeze/ApplySpellZone`、`SpellIdentityEffects.PresentFreezeEffect`、`FrozenDefenseVisual`。
- 当前缺口：建筑材质表面结霜和解冻碎裂仍待制作，不能用全塔变白代替。
- 验收：被冻结建筑不出弹，不在范围内的塔照常攻击；封印随状态结束消失。
