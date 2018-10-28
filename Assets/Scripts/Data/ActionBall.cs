using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum ActionBallType
{
    Power = 1, // 力
    Quick, // 迅
    Block, // 闪
    Posion, // 毒
}

public class ActionBall {

    public ActionBallType Type { get; set; }

    public ActionBall(ActionBallType ballType)
    {
        Type = ballType;
    }

}
