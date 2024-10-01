using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Krentum
{
    public KrentumBase Base {get; set;}

    public int Level {get; set;}

    public int HP{ get; set;}

    public List<Move> Moves{ get; set;}

    //gen moves
    public Krentum(KrentumBase kBase, int kLevel)
    {

        Base = kBase;
        Level = kLevel;
        HP = MaxHp;

        Moves = new List<Move>();
        foreach(var move in Base.LearnableMoves)
        {
            if(move.Level <= Level){
                Moves.Add(new Move(move.Base));
            }

            if(Moves.Count >=3){break;}
        }
    }

    public int MaxHp
    {
        get { return Mathf.FloorToInt((Base.MaxHp * Level) / 100f) + 10; }
    }
    public int PhysicalAttack
    {
        get { return Mathf.FloorToInt((Base.PhysicalAttack * Level) / 100f) + 5; }
    }

    public int PhysicalDefense
    {
        get { return Mathf.FloorToInt((Base.PhysicalDefense * Level) / 100f) + 5; }
    }

    public int RangeAttack
    {
        get { return Mathf.FloorToInt((Base.RangeAttack * Level) / 100f) + 5; }
    }

    public int RangeDefense
    {
        get { return Mathf.FloorToInt((Base.RangeDefense * Level) / 100f) + 5; }
    }

    public int Speed
    {
        get { return Mathf.FloorToInt((Base.Speed * Level) / 100f) + 5; }
    }

    public int Luck
    {
        get { return Mathf.FloorToInt((Base.Luck * Level) / 100f) + 5; }
    }
}