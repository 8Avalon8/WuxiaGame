using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class MpBallItemUI : MonoBehaviour {

    public GameObject m_FullObj;

    private Tweener m_LoopFade;

    public void SetEmpty()
    {
        m_FullObj.SetActive(false);
    }

    public void SetFull()
    {
        m_FullObj.SetActive(true);
    }

    /// <summary>
    /// 显示即将使用的效果
    /// </summary>
    public void SetPreUse()
    {
        m_LoopFade = m_FullObj.GetComponent<Image>().DOFade(0.5f, 0.1f).SetLoops(-1,LoopType.Yoyo);
    }

    public void StopPreUse()
    {
        m_LoopFade.Complete();
    }
}
