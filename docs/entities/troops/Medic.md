# 医师 · Medic

- 职责：寻找受伤友军并治疗，不对建筑造成伤害。
- 三维识别：白色分摆、医疗包、十字和治疗杖；二级急救包框架，三级双叉杖头。
- 动作/VFX：使用导入施法动作；绿色治疗光点与友军回复同步，不复用炼金爆炸。
- 当前代码：`Rules.Troops[Medic]` 的 HealPower、`Battle.StepMedic`、`ModelViews.AddTroopRoleArt`、`ModelIdentityArt.AddTroopIdentityUpgrade`。
- 验收：若全队满血不应虚假施法；离开治疗范围后停止回复。
