using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Dialog : MonoBehaviour
{

    public Image m_RoleHeadImg;
    public Text m_ContentText;

    private string m_Content;
    // Use this for initialization
    void Start()
    {
        m_ContentText.text = "";
        m_Content = "sdfsdfsldkfjslkdfjlxsckvjlskdfjlsdcfkj";
    }

    // Update is called once per frame
    void Update()
    {
        StartCoroutine(TypeText());
    }

    IEnumerator TypeText()
    {
        int curTextLength = m_ContentText.text.Length;
        if (m_ContentText.text.Length < m_Content.Length)
        {
            m_ContentText.text += m_Content[curTextLength];
            yield return new WaitForSeconds(0.5f);
        }
    }
}
