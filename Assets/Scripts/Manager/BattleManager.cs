using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleManager {
    #region DEBUG
    const bool ISDEBUG = true;
    void Log(string info)
    {
        if (ISDEBUG) Debug.Log(info);
    }
    #endregion

    private static BattleManager _instance;
    private BattleManager() { }


    public static BattleManager Instance
    {
        get
        {
            if (null == _instance)
            {
                _instance = new BattleManager();
            }
            return _instance;
        }
    }

    enum Battle_States
    {
        INITIAL_BATTLE = 0,
        ROUND_START,
        WAITING_FOR_CHOOSE_COMMANDS,
        WAITING_FOR_AI_COMMANDS,
        RUNNING_COMMANDS,
        END_ROUND,
        END_BATTLE,
    }


    public Role Player { get; set; }
    public Role Enemy { get; set; }
    // 战斗回合数
    private int Round { get; set; }
    // 一个回合选择的操作序列
    private List<Skill> CommandSuequence { get; set; }

    private Battle_States CurrentBattleState
    {
        get { return _state; }
        set
        {
            _state = value;
            Log("Current Battle State : " + value);
        }
    }

    private Battle_States _state;

    public void StartBattle(Role player, Role enemy)
    {
        InitBattle();

    }

    public void OnTestStartBattle()
    {
        GenerateTestRoleData();
        InitBattle();
    }

    /// <summary>
    /// 初始化战斗
    /// </summary>
    private void InitBattle()
    {
        CurrentBattleState = Battle_States.INITIAL_BATTLE;
    }

    private void GenerateTestRoleData()
    {
        Player = new Role
        {
            RoleName = "小虾米",
            HP = 100,
            MaxHp = 100,
            MP = 5,
            MaxMp = 5,
            Attack = 1,
            Defence = 1,
            Dodge = 1,
            BallPool = new List<ActionBall>(),
            EquipedBaseSKills = new List<Skill>(),
            EquipedSpecialSkills = new List<Skill>(),
            EquipedXinfaSkill = new Skill(),
        };
        // 填充技能
        Player.EquipedBaseSKills.Add(new Skill
        {
            Key = "基础攻击",
            Name = "基础攻击",
            CD = 0,
            DamageWeight = 2,
            DefenceWeight = 1,
            HitWeight = 1,
            DodgeWeight = 1,
            CostBalls = new Dictionary<ActionBall, int>()
            {
                {new ActionBall(ActionBallType.Power),1 },
            }
        });
        Player.EquipedBaseSKills.Add(new Skill
        {
            Key = "基础防御",
            Name = "基础防御",
            CD = 0,
            DamageWeight = 1,
            DefenceWeight = 2,
            HitWeight = 1,
            DodgeWeight = 1,
            CostBalls = new Dictionary<ActionBall, int>()
            {
                {new ActionBall(ActionBallType.Block),1 },
            }
        });
        Player.EquipedBaseSKills.Add(new Skill
        {
            Key = "基础闪避",
            Name = "基础闪避",
            CD = 0,
            DamageWeight = 1,
            DefenceWeight = 1,
            HitWeight = 1,
            DodgeWeight = 2,
            CostBalls = new Dictionary<ActionBall, int>()
            {
                {new ActionBall(ActionBallType.Quick),1 },
            }
        });
        Player.EquipedSpecialSkills.Add(new Skill
        {
            Key = "太极拳法",
            Name = "太极拳法",
            CD = 1,
            DamageWeight = 5,
            DefenceWeight = 1,
            HitWeight = 1,
            DodgeWeight = 1,
            CostBalls = new Dictionary<ActionBall, int>()
            {
                {new ActionBall(ActionBallType.Quick),2 },
                {new ActionBall(ActionBallType.Power),1 },
            }
        });
        Player.EquipedSpecialSkills.Add(new Skill
        {
            Key = "凌波微步",
            Name = "凌波微步",
            CD = 2,
            DamageWeight = 1,
            DefenceWeight = 1,
            HitWeight = 1,
            DodgeWeight = 5,
            CostBalls = new Dictionary<ActionBall, int>()
            {
                {new ActionBall(ActionBallType.Quick),3 },
            }
        });
        Player.EquipedSpecialSkills.Add(new Skill
        {
            Key = "沾衣十八跌",
            Name = "沾衣十八跌",
            CD = 2,
            DamageWeight = 1,
            DefenceWeight = 5,
            HitWeight = 1,
            DodgeWeight = 5,
            CostBalls = new Dictionary<ActionBall, int>()
            {
                {new ActionBall(ActionBallType.Quick),1 },
                {new ActionBall(ActionBallType.Block),1 },
                {new ActionBall(ActionBallType.Power),1 },
            }
        });
        Player.EquipedXinfaSkill = new Skill
        {
            Key = "吸星大法",
            Name = "吸星大法",
            CD = 2,
            DamageWeight = 1,
            DefenceWeight = 5,
            HitWeight = 1,
            DodgeWeight = 5,
            CostBalls = new Dictionary<ActionBall, int>()
            {
                {new ActionBall(ActionBallType.Quick),1 },
                {new ActionBall(ActionBallType.Block),1 },
                {new ActionBall(ActionBallType.Power),1 },
            }
        };
        Player.GenerateBallPool();
        Enemy = new Role
        {
            RoleName = "半瓶神仙醋",
            HP = 100,
            MaxHp = 100,
            MP = 3,
            MaxMp = 3,
            Attack = 1,
            Defence = 1,
            Dodge = 1,
            BallPool = new List<ActionBall>(),
            EquipedBaseSKills = new List<Skill>(),
            EquipedSpecialSkills = new List<Skill>(),
            EquipedXinfaSkill = new Skill(),
        };
        // 填充技能
        Enemy.EquipedBaseSKills.Add(new Skill("基础攻击", "基础攻击", 0));
        Enemy.EquipedBaseSKills.Add(new Skill("基础防御", "基础防御", 0));
        Enemy.EquipedBaseSKills.Add(new Skill("基础闪避", "基础闪避", 0));
        Enemy.EquipedSpecialSkills.Add(new Skill("太极拳法", "太极拳法", 1));
        Enemy.EquipedSpecialSkills.Add(new Skill("凌波微步", "凌波微步", 2));
        Enemy.EquipedSpecialSkills.Add(new Skill("沾衣十八跌", "沾衣十八跌", 1));
        Enemy.EquipedXinfaSkill = new Skill("吸星大法", "吸星大法", 3);
        Enemy.GenerateBallPool();

    }
}
