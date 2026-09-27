# 裂隙锤 · RiftHammer

- 玩法职责：提高英雄对城墙的伤害倍率，让破墙路线成为明确的配装选择；不强化对普通建筑的攻击。
- 三维识别：右手持握的长柄重锤、非对称锤头和破岩喙；2 级增加锤头护条，3 级增加发光锤核。它不是背部贴图，必须跟随右臂攻击动作。
- 战斗反馈：装备该锤的英雄打墙时，右臂先蓄力、再下砸、最后回收；裂光、金属火花、放射状地裂、碎石和金属/石块撞击声在约 0.3 秒的下砸节点出现。致命一击的墙体模型也保留到命中后再消失；规则伤害与寻路仍立即结算。攻击其他建筑仍使用常规近战反馈。裂光由受光照的三维几何构成，不是贴图。
- 代码对应：`Battle.Attack` 根据 `EquippedLevel(RiftHammer)` 计算破墙倍率；`HeroEquipmentArt.BuildRiftHammer` 挂在右手骨骼（无右手时回退至右前臂/上臂）；`ArticulatedModelAnimator` 提供专属动作；`UnityPresentation.FindRiftHammerWallAttacker` 判断攻击者与城墙目标并延迟呈现命中；`RiftHammerAudio` 合成原创短音效，无外部音频许可依赖。
- 验收：`tools/test-unity-player.ps1 -Case EquipmentAlt` 检查殿堂轮廓；`-Case EquipmentCombat` 在隔离战斗场景检查右臂动作、延迟命中、音频源和盾牌受击反馈。后续仍可加入真正录制/设计的高品质音效和更细的锤头网格。
