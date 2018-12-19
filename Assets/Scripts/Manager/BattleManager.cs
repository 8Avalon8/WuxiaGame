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

    public void StartRound()
    {
        CurrentBattleState = Battle_States.ROUND_START;
        CommandSuequence = new List<Skill>();

    }

    public IEnumerable<SkillResult> StartComputing()
    {
        foreach (var skill in CommandSuequence)
        {
            Debug.Log(skill.Name);
            Player.BallPoolManager.CostBalls(skill.CostBalls);
            ExcuteCommandLogic commandLogic = new ExcuteCommandLogic(skill,Player,Enemy);

            yield return commandLogic.Rst;
            //SetRoleStatus(commandLogic.Rst);
        }
    }

    private void SetRoleStatus(SkillResult rst)
    {
        Player.HP += rst.Source.Hp;
        Player.MP += rst.Source.Mp;
        Enemy.HP += rst.Target.Hp;
        Enemy.MP += rst.Target.Mp;
    }

    public void AddCommand(Skill skill)
    {
        CommandSuequence.Add(skill);
    }

    public void ClearCommand()
    {
        CommandSuequence.Clear();
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
            Power = 1,
            Solid = 1,
            Quick = 1,
            MaxBallSlotCount = 5,
            EquipedBaseSKills = new List<Skill>(),
            EquipedSpecialSkills = new List<Skill>(),
            EquipedXinfaSkill = new Skill(),
        };
        // 填充技能
        Player.EquipedBaseSKills.Add(new Skill("基础拳法1"));
        Player.EquipedBaseSKills.Add(new Skill("基础掌法2"));
        Player.EquipedBaseSKills.Add(new Skill("鞭腿"));
        Player.EquipedSpecialSkills.Add(new Skill("基础腿法2"));
        Player.EquipedSpecialSkills.Add(new Skill("寸拳"));
        Player.EquipedSpecialSkills.Add(new Skill("化骨绵掌"));
        Player.EquipedXinfaSkill = new Skill("化骨绵掌");
        Player.BallPoolManager.ResetBallPool();
        Enemy = new Role
        {
            RoleName = "半瓶神仙醋",
            HP = 20,
            MaxHp = 20,
            MP = 3,
            MaxMp = 3,
            Power = 1,
            Solid = 1,
            Quick = 1,
            MaxBallSlotCount = 5,
            EquipedBaseSKills = new List<Skill>(),
            EquipedSpecialSkills = new List<Skill>(),
            EquipedXinfaSkill = new Skill(),
        };
        // 填充技能
        Enemy.EquipedBaseSKills.Add(new Skill("基础拳法1"));
        Enemy.EquipedBaseSKills.Add(new Skill("基础掌法2"));
        Enemy.EquipedBaseSKills.Add(new Skill("鞭腿"));
        Enemy.EquipedSpecialSkills.Add(new Skill("基础腿法2"));
        Enemy.EquipedSpecialSkills.Add(new Skill("寸拳"));
        Enemy.EquipedSpecialSkills.Add(new Skill("化骨绵掌"));
        Enemy.EquipedXinfaSkill = new Skill("化骨绵掌");
        Enemy.BallPoolManager.ResetBallPool();

    }

    #region 战斗逻辑
    /** 战斗逻辑应该有ExcuteCommandLogic
     * DoComputeAttack
     * |
     * GetShanbiProbability
     * |
     * DoComputePureAttack
     * |
     * AdjustDefence
     * |
     * GetCriticalProperty
     * |
     * SkillEffectModel
     * |
     * return Attack Result
     **/




    /// <summary>
    /// 是否战斗结束
    /// </summary>
    /// <returns></returns>
    public bool IsBattleFinish()
    {
        if (Player.HP > 0 && Enemy.HP > 0)
            return true;
        return false;
    }

    public bool IsRoleNeedAI(Role role)
    {
        return false;
    }




    #endregion
}
