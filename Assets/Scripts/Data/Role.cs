using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HSFrameWork;
using HSFrameWork.ConfigTable;

public class Role {

    public string RoleName { get; set; }
    public int MaxHp { get; set; }
    public int MaxMp { get; set; }
    public int HP { get; set; }
    public int MP { get; set; }
    public int Power { get; set; }
    public int Solid { get; set; }
    public int Quick { get; set; }

    public int Poison { get; set; }

    public List<ActionBall> BallPool
    {
        get
        {
            if (_ballPool == null)
                GenerateBallPool();
            return _ballPool;
        }
        set
        {
            _ballPool = value;
        }
    }
    public List<Skill> EquipedBaseSKills { get; set; }
    public List<Skill> EquipedSpecialSkills { get; set; }
    public Skill EquipedXinfaSkill { get; set; }

    private List<ActionBall> _ballPool;
    private Dictionary<string, List<Trigger>> _triggersMap = new Dictionary<string, List<Trigger>>();
    /// <summary>
    /// 根据规则生成行动池
    /// </summary>
    /// <returns></returns>
    public void GenerateBallPool()
    {
        // 设计：每一门装备的武学心法各自带不同数量的球，球池里就是把各个Trigger所带的球加起来
        // 另外一种设计是固定数量根据概率分配
        // Test 各放十个球
        List<ActionBall> tempPool = new List<ActionBall>();
        // Skill提供的球
        foreach (var skill in EquipedBaseSKills)
        {
            foreach (var balls in skill.AddingBalls)
            {
                for (int i = 0; i < balls.Value; i++)
                {
                    ActionBall ball = new ActionBall(balls.Key);
                    tempPool.Add(ball);
                }
            }
        }
        _ballPool = Tools.RandomSortList(tempPool);
    }

    public int GetAttack(Skill skill)
    {
        float attack = 0f;
        foreach (var ratio in skill.DamageRatio)
        {
            int index = ConfigTable.Get<ActionBallPojo>(ratio.Key).index;
            // 对应的球加成 * 角色对应属性
            attack += GetAttributeFromActionBallIndex((ActionBallType)index) * ratio.Value;
        }
        return Convert.ToInt32(Math.Ceiling(attack));
    }

    public int GetDefence()
    {
        int defence = 0;
        //defence += Solid * 1;  // 暂时所有人没有防御的概念
        return 0;
    }

    /// <summary>
    /// 获取闪避权重
    /// </summary>
    /// <returns></returns>
    public int GetShanbiWeight()
    {
        int shanbi = 0;
        // 闪避与Quick相关
        shanbi += Quick * 1;
        // 自身闪避还会与当前buff等状态相关，后续添加
        return shanbi;
    }

    public int GetMingzhongWeight()
    {
        int mingzhong = 0;
        // 暂时的
        mingzhong += Quick * 1;
        return mingzhong;
    }

    public int GetCriticalWeight()
    {
        int critical = 0;
        critical += Power * 1;
        return critical;
    }

    /// <summary>
    /// 刷新人物身上的trigger
    /// </summary>
    /// <returns></returns>
    public void RefreshTrigger()
    {
        _triggersMap.Clear();

    }

    private void AddTriggerMap (Trigger trigger)
    {
        if (_triggersMap.ContainsKey(trigger.Name))
        {
            _triggersMap[trigger.Name].Add(trigger);
        }
        else
        {
            List<Trigger> tmp = new List<Trigger>() { trigger };
            _triggersMap.Add(trigger.Name, tmp);
        }
    }

    private int GetAttributeFromActionBallIndex(ActionBallType index)
    {
        switch (index)
        {
            case ActionBallType.Power:
                return Power;
            case ActionBallType.Quick:
                return Power;
            case ActionBallType.Block:
                return Power;
            case ActionBallType.Posion:
                return Power;
            default:
                Debug.LogError("错误的ActionBallType！");
                return 0;
        }
    }

}
