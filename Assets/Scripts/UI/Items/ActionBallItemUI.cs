using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using HSFrameWork;
using HSFrameWork.ConfigTable;
using HanSquirrel.ResourceManager;

public class ActionBallItemUI : MonoBehaviour {

    private const string IMAGE_PATH = "Assets/Resources/Icon/{0}.png";

    public Image m_Image;
    public string Key { get { return m_ActionBall.Key; } set { } }
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

    public void Bind(string key)
    {
        m_ActionBall = new ActionBall(key);
        string iconstr = m_ActionBall.Pojo.iconstr;
        m_Image.sprite = ResourceLoader.LoadAsset<Sprite>(string.Format(IMAGE_PATH, iconstr));
    }

}
