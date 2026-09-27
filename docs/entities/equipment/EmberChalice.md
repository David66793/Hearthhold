# 余烬圣盏 · EmberChalice

- 玩法职责：英雄号令时为范围内友军追加即时治疗；与火炬的范围/持续加成属于不同构筑路径。
- 三维识别：腰侧金属盏、细脚和缓慢悬浮的青绿余烬；二级加侧把手，三级加上浮光点。与火炬的长杆和火焰轮廓明显不同。
- 当前代码：`Battle.CastHeroSkill` 读取 `EquippedLevel(EmberChalice)` 结算治疗；`HeroEquipmentArt.BuildEmberChalice` 与 `HeroEquipmentMotion` 管盏体/浮烬；`UnityPresentation.SpawnChaliceRestoration` 在受益友军脚下播放短时恢复印记。
- 验收：未锻造或未装配时不可出现在英雄身上；升级的视觉饰件和治疗等级一致。后续应增加单位身体上的轻量受益动画，不能与普通疗愈法术共用完整特效。
