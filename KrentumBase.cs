using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Krenum", menuName = "Krenum/Create new Krenum")]

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