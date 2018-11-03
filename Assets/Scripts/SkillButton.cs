using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SkillButton : MonoBehaviour, IPointerEnterHandler,IPointerExitHandler {
    public Text m_Text;

    private Action m_OnPointerEnterAction;
    private Action m_OnPointerExitAction;
    private Skill m_Skill;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void BindSkill(Skill skill, Action onenter, Action onexit)
    {
        m_Text.text = skill.Name;
        m_Skill = skill;
        m_OnPointerEnterAction = onenter;
        m_OnPointerExitAction = onexit;

    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_OnPointerEnterAction != null)
            m_OnPointerEnterAction();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (m_OnPointerExitAction != null)
            m_OnPointerExitAction();
    }
}
