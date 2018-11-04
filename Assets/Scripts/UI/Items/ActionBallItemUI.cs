using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class ActionBallItemUI : MonoBehaviour {

    public Image m_Image;
    public ActionBallType Type { get { return m_ActionBall.Type; } set { } }
    public bool IsPreUsed { get; set; }
    public ActionBall m_ActionBall;

    private Tweener m_LoopFade;

    public void SetPreUse(bool istrue)
    {
        if (istrue)
        {
            IsPreUsed = true;
            m_Image.DOFade(0.5f, 0);
        }
        else
        {
            IsPreUsed = false;
            m_Image.DOFade(1f, 0);
        }
    }

    public void SetFadeInAndOut(bool active = true)
    {
        if (active)
            m_LoopFade = m_Image.DOFade(0f, 0.5f).SetLoops(-1, LoopType.Yoyo);
        else
        {
            m_LoopFade.OnKill(()=> 
            {
                if (IsPreUsed)
                    m_Image.DOFade(0.5f, 0.1f);
                else
                    m_Image.DOFade(1, 0.1f);
            });
            m_LoopFade.Kill(true);
        }
    }

}
