using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Role {

    public string RoleName { get; set; }
    public int MaxHp { get; set; }
    public int MaxMp { get; set; }
    public int HP { get; set; }
    public int MP { get; set; }
    public int Attack { get; set; }
    public int Defence { get; set; }
    public int Dodge { get; set; }
    public List<ActionBall> BallPool { get; set; }
    public List<Skill> EquipedBaseSKills { get; set; }
    public List<Skill> EquipedSpecialSkills { get; set; }
    public Skill EquipedXinfaSkill { get; set; }

}
