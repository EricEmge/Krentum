using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BattleHud : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI nameText;

    [SerializeField] TextMeshProUGUI levelText;
    [SerializeField] HPBar hpBar;


    public void SetData(Krentum krentum){
        nameText.text = krentum.Base.Name;
        levelText.text = ""+krentum.Level;
        hpBar.SetHP((float) krentum.HP / krentum.MaxHp);
    }
}
