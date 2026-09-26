# 游侠 · Ranger

- 职责：低生命远程输出，可隔墙射击；需要前排保护。
- 三维识别：弓、箭筒；二级备用箭筒，三级弓上测距点，不加通用肩甲。
- 动作/VFX：导入拉弓和放箭两段动作，随后发射细箭轨迹。
- 当前代码：`Rules.Troops[Ranger]`、`ImportedClipAnimator` 的 Bow_Attack_Draw/Shoot、`ModelIdentityArt.AddTroopIdentityUpgrade`。
- 验收：隔墙输出时箭从弓向目标发出；不能看起来像施法者或火枪。
