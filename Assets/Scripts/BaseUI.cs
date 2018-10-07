using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class BaseUI : MonoBehaviour
{

    private Transform m_Transform;

    // UI是否已经显示
    [HideInInspector]
    public bool m_SelfShowed = false;
    // UI是否被初始化
    private bool m_Inited = false;
    private Vector3 m_DefaultPos = Vector3.zero;
    private CanvasGroup m_CanvasGroup;

    void Awake()
    {
        m_Transform = transform;
        m_DefaultPos = m_Transform.position;
    }

    void Start()
    {
        InitSuper();
    }

    public void InitSuper()
    {
        if (m_Inited)
            return;
        m_Inited = true;
        m_CanvasGroup = GetComponent<CanvasGroup>();
        if (m_CanvasGroup == null)
        {
            gameObject.AddComponent<CanvasGroup>();
            m_CanvasGroup = GetComponent<CanvasGroup>();
            m_CanvasGroup.alpha = 1;
            m_CanvasGroup.ignoreParentGroups = false;
            m_CanvasGroup.interactable = false;
            m_CanvasGroup.blocksRaycasts = false;
        }
        gameObject.SetActive(true);
        Button[] buttons = GetComponentsInChildren<Button>();
        foreach (var item in buttons)
        {
            Button btn = item as Button;
            btn.onClick.AddListener(delegate
            {
                this.DidOnClick(btn.gameObject);
            });
        }
        DidInitUI();
    }
    public void ShowUI()
    {
        if (!m_SelfShowed)
        {
            InitSuper();
            DidShowUI();
        }
    }
    public void HideUI()
    {
        if (m_SelfShowed)
        {
            InitSuper();
            DidHideUI();
        }
    }
    public void OpenActivity()
    {
        m_SelfShowed = true;
        gameObject.SetActive(true);
        m_Transform.position = m_DefaultPos + new Vector3(Screen.width, 0, 0);
        m_Transform.DOMoveX(m_DefaultPos.x, 0.2f);
    }
    public void CloseActivity()
    {
        m_SelfShowed = false;
        float outPos = m_Transform.position.x - Screen.width;
        m_Transform.DOMoveX(outPos, 0.2f).OnComplete(delegate
        {
            gameObject.SetActive(false);
        });
    }
    /// <summary>
    /// 在Start中初始化UI的操作
    /// </summary>
    public abstract void DidInitUI();
    /// <summary>
    /// 执行显示UI的操作
    /// </summary>
    public abstract void DidShowUI();
    /// <summary>
    /// 执行关闭UI的操作
    /// </summary>
    public abstract void DidHideUI();
    /// <summary>
    /// 注册按钮点击事件
    /// </summary>
    /// <param name="sender">Sender.</param>
    public abstract void DidOnClick(GameObject sender);
}