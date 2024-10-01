using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleUnit : MonoBehaviour
{
    [SerializeField] KrentumBase _base;
    [SerializeField] int level;
    [SerializeField] bool isPlayerUnit;

    public Krentum Krentum{get; set;}

    public void Setup(){
        Krentum = new Krentum(_base, level);
        if(isPlayerUnit){
            GetComponent<Image>().sprite = Krentum.Base.BackSprite;
        }
        else{
            GetComponent<Image>().sprite = Krentum.Base.FrontSprite;
        }
    }
}
