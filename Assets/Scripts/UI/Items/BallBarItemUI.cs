using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BallBarItemUI : MonoBehaviour {

    public List<ActionBallItemUI> m_ActionBallItemList;

    private List<ActionBallItemUI> m_FadeInAndOutItemList;

    /// <summary>
    /// 清除行动槽中的所有球
    /// </summary>
    public void Clear()
    {

    }

    public void Add(ActionBallItemUI item)
    {
        if (m_ActionBallItemList == null)
            m_ActionBallItemList = new List<ActionBallItemUI>();
        m_ActionBallItemList.Add(item);
    }

    public void StartFadeInAndOutBalls(Dictionary<ActionBall, int> costballs)
    {
        List<ActionBallItemUI> costItemList = new List<ActionBallItemUI>();
        // 遍历字典判断是否包含可用球
        foreach (KeyValuePair<ActionBall, int> kvp in costballs)
        {
            List<ActionBallItemUI> itemList = GetBallsByType(kvp.Key.Type);
            if (GetCount(kvp.Key.Type) < kvp.Value)
                return;
            costItemList.AddRange(itemList.Take(kvp.Value));
        }
        // 一切就绪准备置为闪烁
        m_FadeInAndOutItemList = costItemList;
        foreach (var item in m_FadeInAndOutItemList)
        {
                item.SetFadeInAndOut(true);
        }
    }

    public void StopFadeInAndOutBalls()
    {
        foreach (var item in m_FadeInAndOutItemList)
        {
            item.SetFadeInAndOut(false);
        }
    }

    /// <summary>
    /// 判断槽里面是否有足够的球能被消耗
    /// </summary>
    /// <param name="costballs">需要消耗的球的类型和数量</param>
    /// <returns></returns>
    public bool PrepareCostBalls(Dictionary<ActionBall, int> costballs)
    {
        List<ActionBallItemUI> costItemList = new List<ActionBallItemUI>();
        // 遍历字典判断是否包含可用球
        foreach (KeyValuePair<ActionBall, int> kvp in costballs)
        {
            List<ActionBallItemUI> itemList = GetBallsByType(kvp.Key.Type);
            if (GetCount(kvp.Key.Type) < kvp.Value)
                return false;
            costItemList.AddRange(itemList.Take(kvp.Value));
        }
        // 一切就绪准备置为Disable
        foreach (var item in costItemList)
        {
            item.SetPreUse(true);
        }
        return true;
    }

    private List<ActionBallItemUI> GetBallsByType(ActionBallType type)
    {
        var rst_List = m_ActionBallItemList.FindAll((ActionBallItemUI) => ActionBallItemUI.Type == type && !ActionBallItemUI.IsPreUsed);
        rst_List.Reverse();
        return rst_List;
    }

    /// <summary>
    /// 获取未被使用的一种类型的球的数量
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private int GetCount(ActionBallType type)
    {
        return m_ActionBallItemList.FindAll((ActionBallItemUI) => ActionBallItemUI.Type == type && !ActionBallItemUI.IsPreUsed).Count;
    }
}
