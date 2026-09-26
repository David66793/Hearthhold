# 破城手 · Sapper

- 职责：优先城墙，墙体伤害倍率高；生命薄，需要掩护。
- 三维识别：火药桶与引线；二级爆破绑带，三级钻头，强调工具而非武器换色。
- 动作/VFX：导入匕首/近战动作作当前基础；专用引爆、墙体裂缝仍待制作。
- 当前代码：`Rules.Troops[Sapper]` 的 PreferWalls、`Battle.Attack` 的破墙倍率、`ModelIdentityArt.AddTroopIdentityUpgrade`。
- 验收：面对长墙排会就近破口，不会到另一端兜圈；与游侠武器/轮廓不同。
