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
        foreach (var item in m_ActionBallItemList)
        {
            item.SetPreUse(false);
        }
    }

    public void Add(ActionBallItemUI item)
    {
        if (m_ActionBallItemList == null)
            m_ActionBallItemList = new List<ActionBallItemUI>();
        m_ActionBallItemList.Add(item);
    }

    public void StartFadeInAndOutBalls(Dictionary<string, int> costballs)
    {
        List<ActionBallItemUI> costItemList = new List<ActionBallItemUI>();
        // 遍历字典判断是否包含可用球
        foreach (KeyValuePair<string, int> kvp in costballs)
        {
            List<ActionBallItemUI> itemList = GetBallsByName(kvp.Key);
            // 如果没有对应的则返回
            if (GetCount(kvp.Key) < kvp.Value)
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
        // 如果没有对应的则返回
        if (m_FadeInAndOutItemList == null) return;
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
    public bool PrepareCostBalls(Dictionary<string, int> costballs)
    {
        List<ActionBallItemUI> costItemList = new List<ActionBallItemUI>();
        // 遍历字典判断是否包含可用球
        foreach (KeyValuePair<string, int> kvp in costballs)
        {
            List<ActionBallItemUI> itemList = GetBallsByName(kvp.Key);
            if (GetCount(kvp.Key) < kvp.Value)
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

    private List<ActionBallItemUI> GetBallsByName(string key)
    {
        var rst_List = m_ActionBallItemList.FindAll((ActionBallItemUI) => ActionBallItemUI.Key == key && !ActionBallItemUI.IsPreUsed);
        rst_List.Reverse();
        return rst_List;
    }

    /// <summary>
    /// 获取未被使用的一种类型的球的数量
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    private int GetCount(string key)
    {
        return m_ActionBallItemList.FindAll((ActionBallItemUI) => ActionBallItemUI.Key == key && !ActionBallItemUI.IsPreUsed).Count;
    }
}
