using HSFrameWork.ConfigTable;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skill
{
    /* 已过期
     * 一个技能应如下：
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

    public enum SkillType
    {
        Quanfa = 1,
        Tuifa,
        Xinfa,
    }

    // 标识符
    public string Key { get; set; }
    // 显示名
    public string Name { get; set; }
    // 开场冷却时间
    public int StartCD { get; set; }
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
    public Dictionary<string, int> CostBalls { get; set; }
    // 攻击对应的属性权值
    public Dictionary<string, float> DamageRatio { get; set; }
    // 需要消耗的生命值
    public int CostHp { get; set; }
    // 需要消耗的内力值
    public int CostMp { get; set; }

    public SkillPojo skillpojo;

    public Dictionary<string, int> AddingBalls { get; set; }

    public Skill()
    {

    }

    public Skill(string keyName)
    {
        var pojo = ConfigTable.Get<SkillPojo>(keyName);
        if (pojo == null)
            Debug.LogError("未找到SkillPjo：" + keyName);
        skillpojo = pojo;
        Key = pojo.Key;
        Name = pojo.showname;
        CD = pojo.coolDown;
        StartCD = pojo.startCoolDown;
        DamageRatio = GetDamageRatio(pojo.damageRatio);
        CostBalls = GetCostBalls(pojo.skillCost);
        AddingBalls = GetAddingBalls(pojo.ballAddingEffect);

    }

    private Dictionary<string,int> GetCostBalls(string skillCost)
    {
        Dictionary<string, int> ballDict = new Dictionary<string, int>();
        string[] ballArray = skillCost.Split(',');
        foreach (string ballcostinfo in ballArray)
        {
            string[] ballinfo = ballcostinfo.Split(':');
            if (ballinfo.Length != 2)
            {
                Debug.LogError(Name + "技能消耗格式错误！");
                continue;
            }
            string ballName = ballinfo[0];
            int ballCount = int.Parse(ballinfo[1]);
            ballDict.Add(ballName, ballCount);
        }
        return ballDict;
    }

    private Dictionary<string, int> GetAddingBalls(string ballAddingEffect)
    {
        Dictionary<string, int> ballDict = new Dictionary<string, int>();
        string[] ballArray = ballAddingEffect.Split(',');
        foreach (string ballcostinfo in ballArray)
        {
            string[] ballinfo = ballcostinfo.Split(':');
            if (ballinfo.Length != 2)
            {
                Debug.LogError(Name + "球加成格式错误！");
                continue;
            }
            string ballName = ballinfo[0];
            int ballCount = int.Parse(ballinfo[1]);
            ballDict.Add(ballName, ballCount);
        }
        return ballDict;
    }

    private Dictionary<string,float> GetDamageRatio(string damageRatio)
    {
        var ratioDict = new Dictionary<string, float>();
        string[] ratioArray = damageRatio.Split(',');
        foreach (string ballcostinfo in ratioArray)
        {
            string[] ratioinfo = ballcostinfo.Split(':');
            if (ratioinfo.Length != 2)
            {
                Debug.LogError(Name + "伤害加成格式错误");
                continue;
            }
            string ballName = ratioinfo[0];
            float ratio = float.Parse(ratioinfo[1]);
            ratioDict.Add(ballName, ratio);
        }
        return ratioDict;
    }
}
