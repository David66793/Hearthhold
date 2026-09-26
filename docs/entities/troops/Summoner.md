# 唤灵师 · Summoner

- 职责：定期唤出分身制造数量压力，分身生命与伤害低于本体。
- 三维识别：暗袍、双角、仪式杖与紫色宝珠；二级环形符印，三级阴影冠。
- 动作/VFX：召唤时脚下短脉冲，分身独立索敌；当前召唤体仍借用先锋模型，专属影形待完成。
- 当前代码：`Rules.Troops[Summoner]` 的 SummonCooldown、`Battle.StepUnit` 召唤分支、`ModelIdentityArt.AddTroopIdentityUpgrade`。
- 验收：本体和医师/炼金师不能只差材质颜色；召唤体必须能从本体辨认。
