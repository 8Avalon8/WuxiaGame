using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HpBarItemUI : MonoBehaviour {

    public Image m_LifeBarImg;

    /// <summary>
    /// 改变HP槽的值
    /// </summary>
    /// <param name="percent"></param>
    public void DoChangeValue(float percent,int duration=1)
    {
        m_LifeBarImg.DOFillAmount(percent,duration);
    }
}
