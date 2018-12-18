using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HSFrameWork;
using HSFrameWork.ConfigTable;

public enum ActionBallType
{
    Power = 1, // 力
    Quick, // 疾
    Block, // 御
    Posion, // 毒
}

public class ActionBall
{
    public string Key { get; set; }

    public ActionBallType Type { get; set; }

    public ActionBallPojo Pojo { get; set; }
    public ActionBall(string key)
    {
        var actionBallPojo = ConfigTable.Get<ActionBallPojo>(key);
        Pojo = actionBallPojo;
        Key = actionBallPojo.Key;
    }
    public ActionBall(ActionBallType ballType)
    {
        Type = ballType;
    }

}
