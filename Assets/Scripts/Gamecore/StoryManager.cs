using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StoryManager : IStoryManager
{
    public void RunCommand(string type, string value = "")
    {
        switch (type)
        {
            case "测试":
                Debug.Log("测试");
                break;
            case "RefreshBattlePanel":
                SceneManager.LoadScene("BattleTest");
                break;
            default:
                break;
        }
    }
}
