using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit1;
    [SerializeField] BattleHud playerHud1;
    [SerializeField] BattleUnit playerUnit2;
    [SerializeField] BattleHud playerHud2;
    [SerializeField] BattleUnit playerUnit3;
    [SerializeField] BattleHud playerHud3;

    [SerializeField] BattleUnit EnemyUnit1;
    [SerializeField] BattleHud EnemyHud1;
    [SerializeField] BattleUnit EnemyUnit2;
    [SerializeField] BattleHud EnemyHud2;
    [SerializeField] BattleUnit EnemyUnit3;
    [SerializeField] BattleHud EnemyHud3;

    [SerializeField] BattleDialogBox dialogBox;

    private void Start(){
        SetupBattle();
    }

    public void SetupBattle(){
        playerUnit1.Setup();
        playerHud1.SetData(playerUnit1.Krentum);

        playerUnit2.Setup();
        playerHud2.SetData(playerUnit2.Krentum);

        playerUnit3.Setup();
        playerHud3.SetData(playerUnit3.Krentum);

        EnemyUnit1.Setup();
        EnemyHud1.SetData(EnemyUnit1.Krentum);

        EnemyUnit2.Setup();
        EnemyHud2.SetData(EnemyUnit2.Krentum);

        EnemyUnit3.Setup();
        EnemyHud3.SetData(EnemyUnit3.Krentum);

        StartCoroutine(dialogBox.TypeDialog("A Batch of Krentum's Attacked!!!"));
    }
}
