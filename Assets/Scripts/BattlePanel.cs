using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattlePanel : MonoBehaviour
{

    [Header("Text")]
    public Text m_NameText;
    public Text m_EnemyNameText;
    [Header("Transform")]
    public Transform m_EnergyBarTrs;
    public Transform m_EnemyEnergyBarTrs;
    public Transform m_BallBar;
    public Transform m_EnemyBallBar;
    [Header("Prefab")]
    public GameObject EnergyBallObj;
    public GameObject LiBallObj;
    public GameObject QiBallObj;
    public GameObject XunBallObj;
    [Header("Button")]
    public Button[] m_BaseSkillBtnArray;
    public Button[] m_SpecialSkillBtnArray;
    public Button m_XinfaBtn;
    public Button m_StartActionButton;

    public HpBarItemUI m_LifeBar;
    public HpBarItemUI m_EnemyLifeBar;
    public BallBarItemUI m_BallBarItemUI;



    private Role m_Player;
    private Role m_Enemy;

    private void Awake()
    {
        m_LifeBar.DoChangeValue(0, 0);
        m_EnemyLifeBar.DoChangeValue(0, 0);
        Clear(m_EnergyBarTrs);
        Clear(m_EnemyEnergyBarTrs);
        Clear(m_BallBar);
        Clear(m_EnemyBallBar);
    }
    // Use this for initialization
    void Start()
    {
        BattleManager.Instance.OnTestStartBattle();
        m_Player = BattleManager.Instance.Player;
        m_Enemy = BattleManager.Instance.Enemy;
        Refresh();
    }

    void Refresh()
    {
        m_LifeBar.DoChangeValue(2);
        m_EnemyLifeBar.DoChangeValue(2);
        BindSkillButtons();
        StartCoroutine(ShowBallsInBar(m_EnergyBarTrs, EnergyBallObj, m_Player.MP));
        StartCoroutine(ShowBallsInBar(m_EnemyEnergyBarTrs, EnergyBallObj, m_Enemy.MP));
        //StartCoroutine(ShowBallsInActionBar(m_EnemyBallBar, m_Player.BallPool, 9));
        StartCoroutine(ShowBallsInActionBar(m_BallBar, m_Enemy.BallPool, 9));
    }



    void Clear(Transform trs)
    {
        foreach (Transform child in trs)
        {
            Destroy(child.gameObject);
        }
    }
    // Update is called once per frame
    void Update()
    {

    }

    void BindSkillButtons()
    {
        // 基础技能
        for (int i = 0; i < m_BaseSkillBtnArray.Length; i++)
        {
            Button btn = m_BaseSkillBtnArray[i];
            Skill skill = m_Player.EquipedBaseSKills[i];
            btn.GetComponent<SkillButton>().BindSkill(skill, () =>
            {
                OnSKillButtonEnter(skill);
            }, ()=>
            {
                OnSkillButtonExit(skill);
            });
            btn.onClick.AddListener(() =>
            {
                OnSkillButtonClick(skill);
            });
        }
        // 特殊技能
        for (int i = 0; i < m_SpecialSkillBtnArray.Length; i++)
        {
            Button btn = m_SpecialSkillBtnArray[i];
            Skill skill = m_Player.EquipedSpecialSkills[i];
            btn.GetComponent<SkillButton>().BindSkill(skill, () =>
            {
                OnSKillButtonEnter(skill);
            }, () =>
            {
                OnSkillButtonExit(skill);
            });
            btn.onClick.AddListener(() =>
            {
                OnSkillButtonClick(skill);
            });
        }
        // 心法技能
        m_XinfaBtn.GetComponent<SkillButton>().BindSkill(m_Player.EquipedXinfaSkill, () =>
        {
            OnSKillButtonEnter(m_Player.EquipedXinfaSkill);
        }, () =>
        {
            OnSkillButtonExit(m_Player.EquipedXinfaSkill);
        });
        m_XinfaBtn.onClick.AddListener(() =>
        {
            OnSkillButtonClick(m_Player.EquipedXinfaSkill);
        });
    }

    void OnSkillButtonClick(Skill skill)
    {
        if (m_BallBarItemUI.PrepareCostBalls(skill.CostBalls))
        {
            m_BallBarItemUI.StopFadeInAndOutBalls();
            Debug.Log("PreUse Success");
            BattleManager.Instance.AddCommand(skill);
            OnSKillButtonEnter(skill);
        }
        else
            Debug.Log("Not enough balls in bar");
    }

    void OnSKillButtonEnter(Skill skill)
    {
        m_BallBarItemUI.StartFadeInAndOutBalls(skill.CostBalls);
        foreach (KeyValuePair<ActionBall, int> kvp in skill.CostBalls)
        {
            if (kvp.Value > 0)
                Debug.Log(kvp.Key.Type + kvp.Value.ToString());
        }
    }

    void OnSkillButtonExit(Skill skill)
    {
        m_BallBarItemUI.StopFadeInAndOutBalls();

    }

    IEnumerator ShowBallsInBar(Transform balltrs, GameObject ballprefab, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject ball = Instantiate(ballprefab);
            ball.transform.SetParent(balltrs, false);
            yield return new WaitForSeconds(0.2f);
        }
    }

    IEnumerator ShowBallsInActionBar(Transform balltrs, List<ActionBall> ballpool, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject ball;
            if (ballpool[i].Type == ActionBallType.Power)
            {
                ball = Instantiate(LiBallObj);
                ball.GetComponent<ActionBallItemUI>().m_ActionBall = new ActionBall(ActionBallType.Power);
            }
            else if (ballpool[i].Type == ActionBallType.Quick)
            {
                ball = Instantiate(XunBallObj);
                ball.GetComponent<ActionBallItemUI>().m_ActionBall = new ActionBall(ActionBallType.Quick);
            }
            else if (ballpool[i].Type == ActionBallType.Block)
            {
                ball = Instantiate(QiBallObj);
                ball.GetComponent<ActionBallItemUI>().m_ActionBall = new ActionBall(ActionBallType.Block);
            }
            else
            {
                ball = Instantiate(QiBallObj);
                ball.GetComponent<ActionBallItemUI>().m_ActionBall = new ActionBall(ActionBallType.Block);
            }
            ball.transform.SetParent(balltrs, false);
            // 加入m_BallBarItemUI
            m_BallBarItemUI.Add(ball.GetComponent<ActionBallItemUI>());
            yield return new WaitForSeconds(0.2f);
        }
    }
}
