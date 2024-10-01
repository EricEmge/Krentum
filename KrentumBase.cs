using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Krentum", menuName = "Krentum/Create new Krenum")]

public class KrentumBase : ScriptableObject
{
    [SerializeField] string name;

    [TextArea]
    [SerializeField] string description;

    [SerializeField] Sprite frontSprite;
    [SerializeField] Sprite backSprite;

    [SerializeField] KrentumAura type;

    //stats
    [SerializeField] int maxHp;
    [SerializeField] int physicalAttack;
    [SerializeField] int physicalDefense;
    [SerializeField] int rangeAttack;
    [SerializeField] int rangeDefense;
    [SerializeField] int speed;
    [SerializeField] int luck;

    [SerializeField] List<LearnableMove> learnableMoves;

    public string Name {  get { return name; } }
    public string Description { get { return description; } }

    public Sprite FrontSprite { get { return frontSprite; } }

    public Sprite BackSprite { get { return backSprite; } }

    public int MaxHp { get { return maxHp; } }

    public int PhysicalAttack { get { return physicalAttack; } }

    public int PhysicalDefense { get { return PhysicalDefense; } }

    public int RangeAttack { get { return RangeAttack; } }

    public int RangeDefense { get { return RangeDefense; } }

    public int Speed { get { return speed; } }

    public int Luck { get { return luck; } }

    public List<LearnableMove> LearnableMoves{
        get{return learnableMoves;}
    }
}

[System.Serializable]

public class LearnableMove{
    [SerializeField] MoveBase moveBase;
    [SerializeField] int level;

    public MoveBase Base{
        get {return moveBase;}
    }

    public int Level{
        get{return level;}
    }
}
public enum KrentumAura
{
    None,
    Husk,
    Lucid,
    Poison,
    Volatile,
    NonVolatile
}