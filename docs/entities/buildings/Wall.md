# 石墙 · Wall

- 定位：改写地面可达图；空军与跨墙远程攻击不受同样阻拦。
- 轮廓：可连续拼接的石段；二级垛口，三级信标，两个地图轴向保持一致的接地与朝向。
- 当前代码：`ModelViews.AddBuildingLevelArt` 的 Wall 分支、`Pathfinder.StepCost/CanStepDiagonal`、`Battle.StepUnit` 的阻路墙段攻击。
- 后续动作：受击碎石应与损伤阈值关联；当前只在摧毁时有通用瓦砾。
- 验收：不能斜穿墙角，破墙后重新索敌；四个斜视方向的占地框都对齐。
