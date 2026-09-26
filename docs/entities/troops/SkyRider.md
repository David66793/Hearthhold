# 翼骑 · SkyRider

- 职责：空军，直飞越墙并优先防御；受猎空弩克制。
- 三维识别：实体羽翼和悬浮晶核；二级翼梁，三级尾部稳定翼。
- 动作/VFX：翅膀关节拍动，移动不用地面 A*；死亡应坠落而非原地消失。
- 当前代码：`Rules.Troops[SkyRider]` 的 Flying/PreferDefenses、`ModelViews.AddSkyWing`、`ArticulatedModelAnimator`、`ModelIdentityArt.AddTroopIdentityUpgrade`。
- 验收：旋转预览任意角度都是真 3D 翅膀，不是面对镜头的平面贴图。
