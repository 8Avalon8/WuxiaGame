using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ActionBallPoolManager {

    public List<ActionBall> BallSlotList
    {
        get
        {
            return _BallSlotList;
        }
    }

    public List<ActionBall> Pool
    {
        get
        {
            if (_BallPoolList == null)
                GeneratePool();
            return _BallPoolList;
        }
    }

    private List<ActionBall> _BallSlotList = new List<ActionBall>();
    private List<ActionBall> _BallPoolList = new List<ActionBall>();
    private Role _Owner;
    public ActionBallPoolManager(Role role)
    {
        _Owner = role;
        ResetBallPool();
    }

    /// <summary>
    /// 重置球池和球槽
    /// </summary>
    public void ResetBallPool()
    {
        _BallPoolList.Clear();
        GeneratePool();
    }
    /// <summary>
    /// 从球池中push出球到ballslot
    /// </summary>
    public void Push()
    {
        int pushCount = _Owner.MaxBallSlotCount - _BallSlotList.Count;
        for (int i = 0; i < pushCount; i++)
        {
            if (_BallPoolList.Count <= 0) return;
            // 从前往后把ballpoollist中的球放进slot
            _BallSlotList.Add(_BallPoolList[0]);
            _BallPoolList.RemoveAt(0);
        }
    }

    public void CostBalls(Dictionary<string,int> skillCostBalls)
    {
        foreach (var costtype in skillCostBalls)
        {
            for (int i = 0; i < costtype.Value; i++)
            {
                try
                {
                    Debug.Log(_BallSlotList.Count);
                    var tball = _BallSlotList.Find(ball => ball.Key == costtype.Key);
                    _BallSlotList.Remove(tball);
                    Debug.Log(_BallSlotList.Count);
                }
                catch (System.Exception ex)
                {
                    throw ex;
                }
            }
        }
    }
    /// <summary>
    /// 生成球池
    /// </summary>
    void GeneratePool()
    {
        // 设计：每一门装备的武学心法各自带不同数量的球，球池里就是把各个Trigger所带的球加起来
        // 另外一种设计是固定数量根据概率分配
        List<ActionBall> tempPool = new List<ActionBall>();
        // Skill提供的球
        foreach (var skill in _Owner.EquipedBaseSKills)
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
        _BallPoolList = Tools.RandomSortList(tempPool);
    }

}
