using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HSFrameWork.ConfigTable;
using HSFrameWork.Common;
using HanSquirrel;
using HanSquirrel.ResourceManager;

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
    public Button m_StartActionBtn;
    public Button m_CancelActionsBtn;

    public HpBarItemUI m_LifeBar;
    public HpBarItemUI m_EnemyLifeBar;
    public BallBarItemUI m_BallBarItemUI;
    public BallBarItemUI m_EnemyBallBarItemUI;



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
        var skills = ConfigTable.GetAll<SkillPojo>();
        foreach (var skill in skills)
        {
            Debug.Log(skill.showname);
        }
    }
    // Use this for initialization
    void Start()
    {
        // 绑定Button
        m_StartActionBtn.onClick.AddListener(OnStartAction);
        m_CancelActionsBtn.onClick.AddListener(OnCancelAction);

        BattleManager.Instance.OnTestStartBattle();
        m_Player = BattleManager.Instance.Player;
        m_Enemy = BattleManager.Instance.Enemy;
        m_LifeBar.DoChangeValue(2);
        m_EnemyLifeBar.DoChangeValue(2);
        // 技能按钮
        BindSkillButtons();
        // 填充内力槽的球
        StartCoroutine(ShowBallsInBar(m_EnergyBarTrs, EnergyBallObj, m_Player.MP));
        StartCoroutine(ShowBallsInBar(m_EnemyEnergyBarTrs, EnergyBallObj, m_Enemy.MP));
        //StartCoroutine(ShowBallsInActionBar(m_EnemyBallBar, m_Player.BallPool, 9));
        // 填充行动槽的球
        StartCoroutine(ShowBallsInActionBar(m_BallBarItemUI, m_BallBar, m_Player.BallPool, 9));
        StartCoroutine(ShowBallsInActionBar(m_EnemyBallBarItemUI, m_EnemyBallBar, m_Enemy.BallPool, 9));
        Refresh();
        BattleManager.Instance.StartRound();
    }

    void Refresh()
    {

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
        foreach (KeyValuePair<string, int> kvp in skill.CostBalls)
        {
            if (kvp.Value > 0)
                Debug.Log(kvp.Key + kvp.Value.ToString());
        }
    }

    void OnSkillButtonExit(Skill skill)
    {
        m_BallBarItemUI.StopFadeInAndOutBalls();

    }

    void OnStartAction()
    {
        foreach (var rst in BattleManager.Instance.StartComputing())
        {
            
            if (rst.Target.Hp != 0)
            {
                m_EnemyLifeBar.DoChangeValue((m_Enemy.HP + rst.Target.Hp) * 1.0f / m_Enemy.MaxHp);
                m_Player.HP += rst.Source.Hp;
                m_Enemy.HP += rst.Target.Hp;
            }
            else
            {
                Debug.Log("Battle Finish");
            }
        }
        BattleManager.Instance.ClearCommand();
    }

    void OnCancelAction()
    {
        BattleManager.Instance.ClearCommand();
        m_BallBarItemUI.Clear();
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

    IEnumerator ShowBallsInActionBar(BallBarItemUI baritem, Transform balltrs, List<ActionBall> ballpool, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject ball = ResourceLoader.CreatePrefabInstance(ConStr.ActionBall);
            ball.GetComponent<ActionBallItemUI>().Bind(ballpool[i].Key);
            ball.transform.SetParent(balltrs, false);
            // 加入m_BallBarItemUI
            baritem.Add(ball.GetComponent<ActionBallItemUI>());
            yield return new WaitForSeconds(0.2f);
        }
    }

    /// <summary>
    /// 播放执行完操作后的动画和UI变化
    /// </summary>
    void PlayCommandAnimation()
    {
        // 1.播放skill对应的动画
        // 2.对应播放对手的动画
        // 3.关键帧对UI展示进行操作
    }
}
