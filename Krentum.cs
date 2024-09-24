using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Krentum
{
    KrentumBase _base;

    int level;

    public Krentum(KrentumBase kBase, int kLevel)
    {
        _base = kBase;
        level = kLevel;
    }

    public int MaxHp
    {
        get { return Mathf.FloorToInt((_base.MaxHp * level) / 100f) + 10; }
    }
    public int PhysicalAttack
    {
        get { return Mathf.FloorToInt((_base.PhysicalAttack * level) / 100f) + 5; }
    }

    public int PhysicalDefense
    {
        get { return Mathf.FloorToInt((_base.PhysicalDefense * level) / 100f) + 5; }
    }

    public int RangeAttack
    {
        get { return Mathf.FloorToInt((_base.RangeAttack * level) / 100f) + 5; }
    }

    public int RangeDefense
    {
        get { return Mathf.FloorToInt((_base.RangeDefense * level) / 100f) + 5; }
    }

    public int Speed
    {
        get { return Mathf.FloorToInt((_base.Speed * level) / 100f) + 5; }
    }

    public int Luck
    {
        get { return Mathf.FloorToInt((_base.Luck * level) / 100f) + 5; }
    }
}
