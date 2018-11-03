using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Role {

    public string RoleName { get; set; }
    public int MaxHp { get; set; }
    public int MaxMp { get; set; }
    public int HP { get; set; }
    public int MP { get; set; }
    public int Attack { get; set; }
    public int Defence { get; set; }
    public int Dodge { get; set; }
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
        for (int i = 0; i < 10; i++)
        {
            tempPool.Add(new ActionBall(ActionBallType.Power));
        }
        for (int i = 0; i < 10; i++)
        {
            tempPool.Add(new ActionBall(ActionBallType.Block));
        }
        for (int i = 0; i < 10; i++)
        {
            tempPool.Add(new ActionBall(ActionBallType.Quick));
        }
        _ballPool = Tools.RandomSortList(tempPool);
        
    }

}
