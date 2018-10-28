using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skill
{
    /*一个技能应如下：
     * 
     * Key ： 太极拳_基础拳法
     * Name： 太极崩拳
     * CD  ： 0（基础技能无CD，特殊技能有CD）
     * HitWeight：50 命中附加权重 在判定命中时增加命中率
     * DodgeWeight： 20 闪避附加权重 在判定中时增加闪避率
     * DamageRate: 1.2 伤害倍率 基础攻击值的倍率加成
     * DefenceWeight： 20 格挡附加权值 在收到攻击时提供格挡加成
     *
     */

    // 标识符
    public string Key { get; set; }
    // 显示名
    public string Name { get; set; }
    // 冷却时间
    public int CD { get; set; }
    // 伤害加成
    public int DamageWeight { get; set; }
    // 格挡权值
    public int DefenceWeight { get; set; }
    // 命中权值
    public int HitWeight { get; set; }
    // 闪避权值
    public int DodgeWeight { get; set; }
    // 需要消耗的行动球
    public List<ActionBall> CostBalls { get; set; }

    public Skill()
    {

    }

    public Skill(string key, string name, int cd)
    {
        Key = key;
        Name = name;
        CD = cd;
        DamageWeight = 1;
        DefenceWeight = 1;
        HitWeight = 1;
        DodgeWeight = 1;
        CostBalls = new List<ActionBall>();
    }

}
