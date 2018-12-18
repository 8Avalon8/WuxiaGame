using ProtoBuf;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuffInstance {

    public string Name
    {
        get { return "sdf"; }
    }

    [ProtoMember(1)]
    public int Level { get; set; }
    public int LeftRound { get; set; }

    private BuffPojo buff;

}
