using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExcuteCommandLogic {

    public ExcuteCommandLogic(Skill skill, Role source, Role target)
    {
        Rst = new SkillResult();

        int attack = DoComputeAttack(source,skill);
        if (IsShanbiSuccess())
        {
            return;
        }
        //AdjustDefence();
        //GetCriticalProperty();
        //DoComputeFinalDamage();
        Rst.Target.Hp -= attack;
    }

    public SkillResult Rst { get; set; }

    private int DoComputeAttack(Role source, Skill skill)
    {
        int attack = source.GetAttack(skill);
        return attack;
    }

    private bool IsShanbiSuccess()
    {
        // 命中权重和闪避权重间进行计算
        return false;
    }

}

/// <summary>
/// 技能造成的影响
/// </summary>
public class SkillResult
{
    public SkillResult()
    {
        Source = new SkillRoleResult();
        Target = new SkillRoleResult();
    }
    public SkillRoleResult Source { get; set; }
    public SkillRoleResult Target { get; set; }
}

/// <summary>
/// 技能对角色的影响
/// </summary>
public class SkillRoleResult
{
    public SkillRoleResult()
    {
        Hp = 0;
        Mp = 0;
    }
    public int Hp { get; set; }
    public int Mp { get; set; }
}