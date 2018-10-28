using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattlePanel : MonoBehaviour {

    public Text m_NameText;
    public Text m_EnemyNameText;
    public Transform m_LifeBarTrs;
    public Transform m_EnemyLifeBarTrs;
    public Transform m_EnergyBarTrs;
    public Transform m_EnemyEnergyBarTrs;
    public Transform m_ActionBarTrs;
    public Transform m_EnemyActionBarTrs;
    public Button[] m_BaseSkillBtnArray;
    public Button[] m_SpecialSkillBtnArray;
    public Button m_XinfaBtn;
    public Button m_StartActionButton;
    public GameObject LifeBallObj;
    public GameObject EnergyBallObj;
    public GameObject AttackBallObj;
    public GameObject DefenceBallObj;
    public GameObject DodgeBallObj;

    private Role m_Player;
    private Role m_Enemy;

    private void Awake()
    {
        Clear(m_LifeBarTrs);
        Clear(m_EnemyLifeBarTrs);
        Clear(m_EnergyBarTrs);
        Clear(m_EnemyEnergyBarTrs);
    }
    // Use this for initialization
    void Start () {
        BattleManager.Instance.OnTestStartBattle();
        m_Player = BattleManager.Instance.Player;
        m_Enemy = BattleManager.Instance.Enemy;
        Refresh();
    }

    void Refresh()
    {
        StartCoroutine(ShowBallsInBar(m_LifeBarTrs, LifeBallObj, m_Player.HP / 10));
        StartCoroutine(ShowBallsInBar(m_EnemyLifeBarTrs, LifeBallObj, m_Enemy.HP / 10));
        StartCoroutine(ShowBallsInBar(m_EnergyBarTrs, EnergyBallObj, m_Player.MP));
        StartCoroutine(ShowBallsInBar(m_EnemyEnergyBarTrs, EnergyBallObj, m_Enemy.MP));
    }



    void Clear(Transform trs)
    {
        foreach (Transform child in trs)
        {
            Destroy(child.gameObject);
        }
    }
	// Update is called once per frame
	void Update () {
		
	}

    IEnumerator ShowBallsInBar(Transform balltrs, GameObject ballprefab, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject ball = Instantiate(ballprefab);
            ball.transform.SetParent(balltrs, false);
            yield return new WaitForSeconds(0.05f);
        }
    }
}
