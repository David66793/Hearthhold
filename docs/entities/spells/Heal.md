# 疗愈之雨 · Heal

- 机制：持续地面区域，定时恢复区域内存活友军；离开区域停止受益。
- 形态：低亮度圆形边界仅说明半径，核心为立体喷泉、五瓣浮叶和上升光点；不得只换成绿色地贴。
- 代码：`Battle.CastHeal/ApplyHealPulse`、`SpellIdentityEffects.PresentHealEffect`、`SpawnSpellField` style 0。
- 当前缺口：每次治疗脉冲与单位身上的可见受益反馈仍可进一步同步。
- 验收：持续时长等于规则数据，进入/离开区域的治疗测试通过；镜头反转后仍有高度与体积。
