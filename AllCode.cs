using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [SerializeField] List<AudioData> sfxList;

    [SerializeField] AudioSource musicPlayer;
    [SerializeField] AudioSource sfxPlayer;

    [SerializeField] float fadeDuration = 0.75f;

    AudioClip currMusic;
    float originalMusicVol;
    Dictionary<AudioId, AudioData> sfxLookup;


    public static AudioManager i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    private void Start()
    {
        originalMusicVol = musicPlayer.volume;

        sfxLookup = sfxList.ToDictionary(x => x.id);
    }

    public void PlaySfx(AudioClip clip, bool pauseMusic = false)
    {
        if (clip == null) return;

        if (pauseMusic)
        {
            musicPlayer.Pause();
            StartCoroutine(UnPauseMusic(clip.length));
        }

        sfxPlayer.PlayOneShot(clip);
    }

    public void PlaySfx(AudioId audioId, bool pauseMusic = false)
    {
        if (!sfxLookup.ContainsKey(audioId)) return;

        var audioData = sfxLookup[audioId];
        PlaySfx(audioData.clip, pauseMusic);
    }

    public void PlayMusic(AudioClip clip, bool loop = true, bool fade = false)
    {
        if (clip == null || clip == currMusic) return;

        currMusic = clip;
        StartCoroutine(PlayMusicAsync(clip, loop, fade));
    }

    IEnumerator PlayMusicAsync(AudioClip clip, bool loop, bool fade)
    {
        if (fade)
            yield return musicPlayer.DOFade(0, fadeDuration).WaitForCompletion();

        musicPlayer.clip = clip;
        musicPlayer.loop = loop;
        musicPlayer.Play();

        if (fade)
            yield return musicPlayer.DOFade(originalMusicVol, fadeDuration).WaitForCompletion();
    }

    IEnumerator UnPauseMusic(float delay)
    {
        yield return new WaitForSeconds(delay);

        musicPlayer.volume = 0;
        musicPlayer.UnPause();
        musicPlayer.DOFade(originalMusicVol, fadeDuration);
    }
}

public enum AudioId { UISelect, Hit, Faint, ExpGain, ItemObtained, CambroxObtained }

[System.Serializable]
public class AudioData
{
    public AudioId id;
    public AudioClip clip;
}
using GDE.GenericSelectionUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PartyScreen : SelectionUI<TextSlot>
{
    [SerializeField] Text messageText;

    PartyMemberUI[] memberSlots;
    List<Cambrox> Cambroxs;
    CambroxParty party;

    public Cambrox SelectedMember => Cambroxs[selectedItem];

    public void Init()
    {
        memberSlots = GetComponentsInChildren<PartyMemberUI>(true);
        SetSelectionSettings(SelectionType.Grid, 2);

        party = CambroxParty.GetPlayerParty();
        SetPartyData();

        party.OnUpdated += SetPartyData;
    }

    public void SetPartyData()
    {
        Cambroxs = party.Cambroxs;

        ClearItems();

        for (int i = 0; i < memberSlots.Length; i++)
        {
            if (i < Cambroxs.Count)
            {
                memberSlots[i].gameObject.SetActive(true);
                memberSlots[i].Init(Cambroxs[i]);
            }
            else
                memberSlots[i].gameObject.SetActive(false);
        }

        var textSlots = memberSlots.Select(m => m.GetComponent<TextSlot>());
        SetItems(textSlots.Take(Cambroxs.Count).ToList());

        messageText.text = "Choose a Cambrox";
    }

    public void ShowIfTmIsUsable(TmItem tmItem)
    {
        for (int i = 0; i < Cambroxs.Count; i++)
        {
            string message = tmItem.CanBeTaught(Cambroxs[i]) ? "ABLE!" : "NOT ABLE!";
            memberSlots[i].SetMessage(message);
        }
    }

    public void ClearMemberSlotMessages()
    {
        for (int i = 0; i < Cambroxs.Count; i++)
        {
            memberSlots[i].SetMessage("");
        }
    }

    public void SetMessageText(string message)
    {
        messageText.text = message;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PartyMemberUI : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] HPBar hpBar;
    [SerializeField] Text messageText;

    Cambrox _Cambrox;

    public void Init(Cambrox Cambrox)
    {
        _Cambrox = Cambrox;
        UpdateData();
        SetMessage("");

        _Cambrox.OnHPChanged += UpdateData;
    }

    void UpdateData()
    {
        nameText.text = _Cambrox.Base.Name;
        levelText.text = "Lvl " + _Cambrox.Level;
        hpBar.SetHP((float)_Cambrox.HP / _Cambrox.MaxHp);
    }

    public void SetSelected(bool selected)
    {
        if (selected)
            nameText.color = GlobalSettings.i.HighlightedColor;
        else
            nameText.color = Color.black;
    }

    public void SetMessage(string message)
    {
        messageText.text = message;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HPBar : MonoBehaviour
{
    [SerializeField] GameObject health;

    public bool IsUpdating { get; private set; }

    public void SetHP(float hpNormalized)
    {
        health.transform.localScale = new Vector3(hpNormalized, 1f);
    }

    public IEnumerator SetHPSmooth(float newHp)
    {
        IsUpdating = true;

        float curHp = health.transform.localScale.x;
        float changeAmt = curHp - newHp;

        while (curHp - newHp > Mathf.Epsilon)
        {
            curHp -= changeAmt * Time.deltaTime;
            health.transform.localScale = new Vector3(curHp, 1f);
            yield return null;
        }
        health.transform.localScale = new Vector3(newHp, 1f);

        IsUpdating = false;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BattleUnit : MonoBehaviour
{
    [SerializeField] bool isPlayerUnit;
    [SerializeField] BattleHud hud;

    public bool IsPlayerUnit {
        get { return isPlayerUnit; }
    }

    public BattleHud Hud {
        get { return hud;  }
    }

    public Cambrox Cambrox { get; set; }

    Image image;
    Vector3 orginalPos;
    Color originalColor;
    private void Awake()
    {
        image = GetComponent<Image>();
        orginalPos = image.transform.localPosition;
        originalColor = image.color;
    }

    public void Setup(Cambrox Cambrox)
    {
        Cambrox = Cambrox;
        if (isPlayerUnit)
            image.sprite = Cambrox.Base.BackSprite;
        else
            image.sprite = Cambrox.Base.FrontSprite;

        hud.gameObject.SetActive(true);
        hud.SetData(Cambrox);

        transform.localScale = new Vector3(1, 1, 1);
        image.color = originalColor;
        PlayEnterAnimation();
    }

    public void Clear()
    {
        hud.gameObject.SetActive(false);
    }

    public void PlayEnterAnimation()
    {
        if (isPlayerUnit)
            image.transform.localPosition = new Vector3(-500f, orginalPos.y);
        else
            image.transform.localPosition = new Vector3(500f, orginalPos.y);

        image.transform.DOLocalMoveX(orginalPos.x, 1f);
    }

    public void PlayAttackAnimation()
    {
        var sequence = DOTween.Sequence();
        if (isPlayerUnit)
            sequence.Append(image.transform.DOLocalMoveX(orginalPos.x + 50f, 0.25f));
        else
            sequence.Append(image.transform.DOLocalMoveX(orginalPos.x - 50f, 0.25f));

        sequence.Append(image.transform.DOLocalMoveX(orginalPos.x, 0.25f));
    }

    public void PlayHitAnimation()
    {
        var sequence = DOTween.Sequence();
        sequence.Append(image.DOColor(Color.gray, 0.1f));
        sequence.Append(image.DOColor(originalColor, 0.1f));
    }

    public void PlayFaintAnimation()
    {
        var sequence = DOTween.Sequence();
        sequence.Append(image.transform.DOLocalMoveY(orginalPos.y - 150f, 0.5f));
        sequence.Join(image.DOFade(0f, 0.5f));
    }

    public IEnumerator PlayCaptureAnimation()
    {
        var sequence = DOTween.Sequence();
        sequence.Append(image.DOFade(0, 0.5f));
        sequence.Join(transform.DOLocalMoveY(orginalPos.y + 50f, 0.5f));
        sequence.Join(transform.DOScale(new Vector3(0.3f, 0.3f, 1f), 0.5f));
        yield return sequence.WaitForCompletion();
    }

    public IEnumerator PlayBreakOutAnimation()
    {
        var sequence = DOTween.Sequence();
        sequence.Append(image.DOFade(1, 0.5f));
        sequence.Join(transform.DOLocalMoveY(orginalPos.y, 0.5f));
        sequence.Join(transform.DOScale(new Vector3(1f, 1f, 1f), 0.5f));
        yield return sequence.WaitForCompletion();
    }
}
using DG.Tweening;
using GDEUtils.StateMachine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public enum BattleAction { Move, SwitchCambrox, UseItem, Run }

public enum BattleTrigger { LongGrass, Water }

public class BattleSystem : MonoBehaviour
{
    [SerializeField] BattleUnit playerUnit;
    [SerializeField] BattleUnit enemyUnit;
    [SerializeField] BattleDialogBox dialogBox;
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] Image playerImage;
    [SerializeField] Image trainerImage;
    [SerializeField] GameObject cambroxballSprite;
    [SerializeField] MoveToForgetSelectionUI moveSelectionUI;
    [SerializeField] InventoryUI inventoryUI;

    [Header("Audio")]
    [SerializeField] AudioClip wildBattleMusic;
    [SerializeField] AudioClip trainerBattleMusic;
    [SerializeField] AudioClip battleVictoryMusic;

    [Header("Background Images")]
    [SerializeField] Image backgroundImage;
    [SerializeField] Sprite grassBackground;
    [SerializeField] Sprite waterBackground;

    public StateMachine<BattleSystem> StateMachine { get; private set; }

    public event Action<bool> OnBattleOver;

    public int SelectedMove { get; set; }
    public BattleAction SelectedAction { get; set; }
    public Cambrox SelectedCambrox { get; set; }
    public ItemBase SelectedItem { get; set; }

    public bool IsBattleOver { get; private set; }

    public CambroxParty PlayerParty { get; private set; }
    public CambroxParty TrainerParty { get; private set; }
    public Cambrox WildCambrox { get; private set; }

    public bool IsTrainerBattle { get; private set; } = false;
    PlayerController player;
    public TrainerController Trainer { get; private set; }

    public int EscapeAttempts { get; set; }

    BattleTrigger battleTrigger;

    public void StartBattle(CambroxParty playerParty, Cambrox wildCambrox,
        BattleTrigger trigger = BattleTrigger.LongGrass)
    {
        this.PlayerParty = playerParty;
        this.WildCambrox = wildCambrox;
        player = playerParty.GetComponent<PlayerController>();
        IsTrainerBattle = false;

        battleTrigger = trigger;

        AudioManager.i.PlayMusic(wildBattleMusic);

        StartCoroutine(SetupBattle());
    }

    public void StartTrainerBattle(CambroxParty playerParty, CambroxParty trainerParty,
        BattleTrigger trigger = BattleTrigger.LongGrass)
    {
        this.PlayerParty = playerParty;
        this.TrainerParty = trainerParty;

        IsTrainerBattle = true;
        player = playerParty.GetComponent<PlayerController>();
        Trainer = trainerParty.GetComponent<TrainerController>();

        battleTrigger = trigger;

        AudioManager.i.PlayMusic(trainerBattleMusic);

        StartCoroutine(SetupBattle());
    }

    public IEnumerator SetupBattle()
    {
        StateMachine = new StateMachine<BattleSystem>(this);

        playerUnit.Clear();
        enemyUnit.Clear();

        backgroundImage.sprite = (battleTrigger == BattleTrigger.LongGrass) ? grassBackground : waterBackground;

        if (!IsTrainerBattle)
        {
            // Wild Cambrox Battle
            playerUnit.Setup(PlayerParty.GetHealthyCambrox());
            enemyUnit.Setup(WildCambrox);

            dialogBox.SetMoveNames(playerUnit.Cambrox.Moves);
            yield return dialogBox.TypeDialog($"A wild {enemyUnit.Cambrox.Base.Name} appeared.");
        }
        else
        {
            // Trianer Battle

            // Show trainer and player sprites
            playerUnit.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(false);

            playerImage.gameObject.SetActive(true);
            trainerImage.gameObject.SetActive(true);
            playerImage.sprite = player.Sprite;
            trainerImage.sprite = Trainer.Sprite;

            yield return dialogBox.TypeDialog($"{Trainer.Name} wants to battle");

            // Send out first Cambrox of the trainer
            trainerImage.gameObject.SetActive(false);
            enemyUnit.gameObject.SetActive(true);
            var enemyCambrox = TrainerParty.GetHealthyCambrox();
            enemyUnit.Setup(enemyCambrox);
            yield return dialogBox.TypeDialog($"{Trainer.Name} send out {enemyCambrox.Base.Name}");

            // Send out first Cambrox of the player
            playerImage.gameObject.SetActive(false);
            playerUnit.gameObject.SetActive(true);
            var playerCambrox = PlayerParty.GetHealthyCambrox();
            playerUnit.Setup(playerCambrox);
            yield return dialogBox.TypeDialog($"Go {playerCambrox.Base.Name}!");
            dialogBox.SetMoveNames(playerUnit.Cambrox.Moves);
        }

        IsBattleOver = false;
        EscapeAttempts = 0;
        partyScreen.Init();

        StateMachine.ChangeState(ActionSelectionState.i);
    }

    public void BattleOver(bool won)
    {
        IsBattleOver = true;
        PlayerParty.Cambroxs.ForEach(p => p.OnBattleOver());
        playerUnit.Hud.ClearData();
        enemyUnit.Hud.ClearData();
        OnBattleOver(won);
    }

    public void HandleUpdate()
    {
        StateMachine.Execute();
    }

    public IEnumerator SwitchCambrox(Cambrox newCambrox)
    {
        if (playerUnit.Cambrox.HP > 0)
        {
            yield return dialogBox.TypeDialog($"Come back {playerUnit.Cambrox.Base.Name}");
            playerUnit.PlayFaintAnimation();
            yield return new WaitForSeconds(2f);
        }

        playerUnit.Setup(newCambrox);
        dialogBox.SetMoveNames(newCambrox.Moves);
        yield return dialogBox.TypeDialog($"Go {newCambrox.Base.Name}!");
    }

    public IEnumerator SendNextTrainerCambrox()
    {
        var nextCambrox = TrainerParty.GetHealthyCambrox();
        enemyUnit.Setup(nextCambrox);
        yield return dialogBox.TypeDialog($"{Trainer.Name} send out {nextCambrox.Base.Name}!");
    }

    public IEnumerator Throwcambroxball(cambroxballItem cambroxballItem)
    {

        if (IsTrainerBattle)
        {
            yield return dialogBox.TypeDialog($"You can't steal the trainers Cambrox!");
            yield break;
        }

        yield return dialogBox.TypeDialog($"{player.Name} used {cambroxballItem.Name.ToUpper()}!");

        var cambroxballObj = Instantiate(cambroxballSprite, playerUnit.transform.position - new Vector3(2, 0), Quaternion.identity);
        var cambroxball = cambroxballObj.GetComponent<SpriteRenderer>();
        cambroxball.sprite = cambroxballItem.Icon;

        // Animations
        yield return cambroxball.transform.DOJump(enemyUnit.transform.position + new Vector3(0, 2), 2f, 1, 1f).WaitForCompletion();
        yield return enemyUnit.PlayCaptureAnimation();
        yield return cambroxball.transform.DOMoveY(enemyUnit.transform.position.y - 1.3f, 0.5f).WaitForCompletion();

        int shakeCount = TryToCatchCambrox(enemyUnit.Cambrox, cambroxballItem);

        for (int i = 0; i < Mathf.Min(shakeCount, 3); ++i)
        {
            yield return new WaitForSeconds(0.5f);
            yield return cambroxball.transform.DOPunchRotation(new Vector3(0, 0, 10f), 0.8f).WaitForCompletion();
        }

        if (shakeCount == 4)
        {
            // Cambrox is caught
            yield return dialogBox.TypeDialog($"{enemyUnit.Cambrox.Base.Name} was caught");
            yield return cambroxball.DOFade(0, 1.5f).WaitForCompletion();

            PlayerParty.AddCambrox(enemyUnit.Cambrox);
            yield return dialogBox.TypeDialog($"{enemyUnit.Cambrox.Base.Name} has been added to your party");

            Destroy(cambroxball);
            BattleOver(true);
        }
        else
        {
            // Cambrox broke out
            yield return new WaitForSeconds(1f);
            cambroxball.DOFade(0, 0.2f);
            yield return enemyUnit.PlayBreakOutAnimation();

            if (shakeCount < 2)
                yield return dialogBox.TypeDialog($"{enemyUnit.Cambrox.Base.Name} broke free");
            else
                yield return dialogBox.TypeDialog($"Almost caught it");

            Destroy(cambroxball);
        }
    }

    int TryToCatchCambrox(Cambrox Cambrox, cambroxballItem cambroxballItem)
    {
        float a = (3 * Cambrox.MaxHp - 2 * Cambrox.HP) * Cambrox.Base.CatchRate * cambroxballItem.CatchRateModifier * ConditionsDB.GetStatusBonus(Cambrox.Status) / (3 * Cambrox.MaxHp);

        if (a >= 255)
            return 4;

        float b = 1048560 / Mathf.Sqrt(Mathf.Sqrt(16711680 / a));

        int shakeCount = 0;
        while (shakeCount < 4)
        {
            if (UnityEngine.Random.Range(0, 65535) >= b)
                break;

            ++shakeCount;
        }

        return shakeCount;
    }

    public BattleDialogBox DialogBox => dialogBox;

    public BattleUnit PlayerUnit => playerUnit;
    public BattleUnit EnemyUnit => enemyUnit;

    public PartyScreen PartyScreen => partyScreen;

    public AudioClip BattleVictoryMusic => battleVictoryMusic;
}
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleHud : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] Text statusText;
    [SerializeField] HPBar hpBar;
    [SerializeField] GameObject expBar;

    [SerializeField] Color psnColor;
    [SerializeField] Color brnColor;
    [SerializeField] Color slpColor;
    [SerializeField] Color parColor;
    [SerializeField] Color frzColor;

    Cambrox _Cambrox;
    Dictionary<ConditionID, Color> statusColors;

    public void SetData(Cambrox Cambrox)
    {
        if (_Cambrox != null)
        {
            _Cambrox.OnHPChanged -= UpdateHP;
            _Cambrox.OnStatusChanged -= SetStatusText;
        }

        _Cambrox = Cambrox;

        nameText.text = Cambrox.Base.Name;
        SetLevel();
        hpBar.SetHP((float) Cambrox.HP / Cambrox.MaxHp);
        SetExp();

        statusColors = new Dictionary<ConditionID, Color>()
        {
            {ConditionID.psn, psnColor },
            {ConditionID.brn, brnColor },
            {ConditionID.slp, slpColor },
            {ConditionID.par, parColor },
            {ConditionID.frz, frzColor },
        };

        SetStatusText();
        _Cambrox.OnStatusChanged += SetStatusText;
        _Cambrox.OnHPChanged += UpdateHP;
    }

    void SetStatusText()
    {
        if (_Cambrox.Status == null)
        {
            statusText.text = "";
        }
        else
        {
            statusText.text = _Cambrox.Status.Id.ToString().ToUpper();
            statusText.color = statusColors[_Cambrox.Status.Id];
        }
    }

    public void SetLevel()
    {
        levelText.text = "Lvl " + _Cambrox.Level;
    }

    public void SetExp()
    {
        if (expBar == null) return;

        float normalizedExp = _Cambrox.GetNormalizedExp();
        expBar.transform.localScale = new Vector3(normalizedExp, 1, 1);
    }

    public IEnumerator SetExpSmooth(bool reset = false)
    {
        if (expBar == null) yield break;

        if (reset)
            expBar.transform.localScale = new Vector3(0, 1, 1);

        float normalizedExp = _Cambrox.GetNormalizedExp();
        yield return expBar.transform.DOScaleX(normalizedExp, 1.5f).WaitForCompletion();
    }

    public void UpdateHP()
    {
        StartCoroutine(UpdateHPAsync());
    }

    public IEnumerator UpdateHPAsync()
    {
        yield return hpBar.SetHPSmooth((float)_Cambrox.HP / _Cambrox.MaxHp);
    }

    public IEnumerator WaitForHPUpdate()
    {
        yield return new WaitUntil(() => hpBar.IsUpdating == false);
    }

    public void ClearData()
    {
        if (_Cambrox != null)
        {
            _Cambrox.OnHPChanged -= UpdateHP;
            _Cambrox.OnStatusChanged -= SetStatusText;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BattleDialogBox : MonoBehaviour
{
    [SerializeField] int lettersPerSecond;

    [SerializeField] Text dialogText;
    [SerializeField] GameObject actionSelector;
    [SerializeField] GameObject moveSelector;
    [SerializeField] GameObject moveDetails;
    [SerializeField] GameObject choiceBox;

    [SerializeField] List<Text> actionTexts;
    [SerializeField] List<Text> moveTexts;

    [SerializeField] Text ppText;
    [SerializeField] Text typeText;

    [SerializeField] Text yesText;
    [SerializeField] Text noText;

    Color highlightedColor;
    private void Start()
    {
        highlightedColor = GlobalSettings.i.HighlightedColor;
    }

    public void SetDialog(string dialog)
    {
        dialogText.text = dialog;
    }

    public IEnumerator TypeDialog(string dialog)
    {
        dialogText.text = "";
        foreach (var letter in dialog.ToCharArray())
        {
            dialogText.text += letter;
            yield return new WaitForSeconds(1f / lettersPerSecond);
        }

        yield return new WaitForSeconds(1f);
    }

    public void EnableDialogText(bool enabled)
    {
        dialogText.enabled = enabled;
    }

    public void EnableActionSelector(bool enabled)
    {
        actionSelector.SetActive(enabled);
    }

    public void EnableMoveSelector(bool enabled)
    {
        moveSelector.SetActive(enabled);
        moveDetails.SetActive(enabled);
    }

    public void EnableChoiceBox(bool enabled)
    {
        choiceBox.SetActive(enabled);
    }

    public bool IsChoiceBoxEnabled => choiceBox.activeSelf;

    public void UpdateActionSelection(int selectedAction)
    {
        for (int i = 0; i < actionTexts.Count; ++i)
        {
            if (i == selectedAction)
                actionTexts[i].color = highlightedColor;
            else
                actionTexts[i].color = Color.black;
        }
    }

    public void UpdateMoveSelection(int selectedMove, Move move)
    {
        for (int i = 0; i < moveTexts.Count; ++i)
        {
            if (i == selectedMove)
                moveTexts[i].color = highlightedColor;
            else
                moveTexts[i].color = Color.black;
        }

        ppText.text = $"PP {move.PP}/{move.Base.PP}";
        typeText.text = move.Base.Type.ToString();

        if (move.PP == 0)
            ppText.color = Color.red;
        else
            ppText.color = Color.black;
    }

    public void SetMoveNames(List<Move> moves)
    {
        for (int i = 0; i < moveTexts.Count; ++i)
        {
            if (i < moves.Count)
                moveTexts[i].text = moves[i].Base.Name;
            else
                moveTexts[i].text = "-";
        }
    }

    public void UpdateChoiceBox(bool yesSelected)
    {
        if (yesSelected)
        {
            yesText.color = highlightedColor;
            noText.color = Color.black;
        }
        else
        {
            yesText.color = Color.black;
            noText.color = highlightedColor;
        }
    }
}
using GDE.GenericSelectionUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MoveToForgetSelectionUI : SelectionUI<TextSlot>
{
    [SerializeField] List<Text> moveTexts;

    public void SetMoveData(List<MoveBase> currentMoves, MoveBase newMove)
    {
        for (int i=0; i<currentMoves.Count; ++i)
        {
            moveTexts[i].text = currentMoves[i].Name;
        }

        moveTexts[currentMoves.Count].text = newMove.Name;

        SetItems(moveTexts.Select(m => m.GetComponent<TextSlot>()).ToList());
    }
}
using GDE.GenericSelectionUI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MoveSelectionUI : SelectionUI<TextSlot>
{
    [SerializeField] List<TextSlot> moveTexts;

    [SerializeField] Text ppText;
    [SerializeField] Text typeText;

    private void Start()
    {
        SetSelectionSettings(SelectionType.Grid, 2);
    }

    List<Move> _moves;
    public void SetMoves(List<Move> moves)
    {
        _moves = moves;

        selectedItem = 0;

        SetItems(moveTexts.Take(moves.Count).ToList());

        for (int i = 0; i < moveTexts.Count; ++i)
        {
            if (i < moves.Count)
                moveTexts[i].SetText(moves[i].Base.Name);
            else
                moveTexts[i].SetText("-");
        }
    }

    public override void UpdateSelectionInUI()
    {
        base.UpdateSelectionInUI();

        var move = _moves[selectedItem];

        ppText.text = $"PP {move.PP}/{move.Base.PP}";
        typeText.text = move.Base.Type.ToString();

        if (move.PP == 0)
            ppText.color = Color.red;
        else
            ppText.color = Color.black;
    }
}
using GDE.GenericSelectionUI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ActionSelectionUI : SelectionUI<TextSlot>
{
    private void Start()
    {
        SetSelectionSettings(SelectionType.Grid, 2);
        SetItems(GetComponentsInChildren<TextSlot>().ToList());
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AboutToUseState : State<BattleSystem>
{
    // Input
    public Cambrox NewCambrox { get; set; }

    bool aboutToUseChoice;

    public static AboutToUseState i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    BattleSystem bs;
    public override void Enter(BattleSystem owner)
    {
        bs = owner;
        StartCoroutine(StartState());
    }

    IEnumerator StartState()
    {
        yield return bs.DialogBox.TypeDialog($"{bs.Trainer.Name} is about to use {NewCambrox.Base.Name}. Do you want to change Cambrox?");
        bs.DialogBox.EnableChoiceBox(true);
    }

    public override void Execute()
    {
        if (!bs.DialogBox.IsChoiceBoxEnabled)
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow))
            aboutToUseChoice = !aboutToUseChoice;

        bs.DialogBox.UpdateChoiceBox(aboutToUseChoice);

        if (Input.GetKeyDown(KeyCode.Z))
        {
            bs.DialogBox.EnableChoiceBox(false);
            if (aboutToUseChoice == true)
            {
                // Yes Option
                StartCoroutine(SwitchAndContinueBattle());
            }
            else
            {
                // No Option
                StartCoroutine(ContinueBattle());
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            bs.DialogBox.EnableChoiceBox(false);
            StartCoroutine(ContinueBattle());
        }
    }

    IEnumerator SwitchAndContinueBattle()
    {
        yield return GameController.Instance.StateMachine.PushAndWait(PartyState.i);
        var selectedCambrox = PartyState.i.SelectedCambrox;
        if (selectedCambrox != null)
        {
            yield return bs.SwitchCambrox(selectedCambrox);
        }

        yield return ContinueBattle();
    }

    IEnumerator ContinueBattle()
    {
        yield return bs.SendNextTrainerCambrox();
        bs.StateMachine.Pop();
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionSelectionState : State<BattleSystem>
{
    [SerializeField] ActionSelectionUI selectionUI;

    public static ActionSelectionState i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    BattleSystem bs;
    public override void Enter(BattleSystem owner)
    {
        bs = owner;

        selectionUI.gameObject.SetActive(true);
        selectionUI.OnSelected += OnActionSelected;

        bs.DialogBox.SetDialog("Choose an action");
    }

    public override void Execute()
    {
        selectionUI.HandleUpdate();
    }

    public override void Exit()
    {
        selectionUI.gameObject.SetActive(false);
        selectionUI.OnSelected -= OnActionSelected;
    }

    void OnActionSelected(int selection)
    {
        if (selection == 0)
        {
            // Fight
            bs.SelectedAction = BattleAction.Move;
            MoveSelectionState.i.Moves = bs.PlayerUnit.Cambrox.Moves;
            bs.StateMachine.ChangeState(MoveSelectionState.i);
        }
        else if (selection == 1)
        {
            // Bag
            StartCoroutine(GoToInventoryState());
        }
        else if (selection == 2)
        {
            // Cambrox
            StartCoroutine(GoToPartyState());
        }
        else if (selection == 3)
        {
            // Run
            bs.SelectedAction = BattleAction.Run;
            bs.StateMachine.ChangeState(RunTurnState.i);
        }
    }

    IEnumerator GoToPartyState()
    {
        yield return GameController.Instance.StateMachine.PushAndWait(PartyState.i);
        var selectedCambrox = PartyState.i.SelectedCambrox;
        if (selectedCambrox != null)
        {
            bs.SelectedAction = BattleAction.SwitchCambrox;
            bs.SelectedCambrox = selectedCambrox;
            bs.StateMachine.ChangeState(RunTurnState.i);
        }
    }

    IEnumerator GoToInventoryState()
    {
        yield return GameController.Instance.StateMachine.PushAndWait(InventoryState.i);
        var selectedItem = InventoryState.i.SelectedItem;
        if (selectedItem != null)
        {
            bs.SelectedAction = BattleAction.UseItem;
            bs.SelectedItem = selectedItem;
            bs.StateMachine.ChangeState(RunTurnState.i);
        }
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSelectionState : State<BattleSystem>
{
    [SerializeField] MoveSelectionUI selectionUI;
    [SerializeField] GameObject moveDetailsUI;

    // Input
    public List<Move> Moves { get; set; }

    public static MoveSelectionState i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    BattleSystem bs;
    public override void Enter(BattleSystem owner)
    {
        bs = owner;

        selectionUI.SetMoves(Moves);

        selectionUI.gameObject.SetActive(true);
        selectionUI.OnSelected += OnMoveSelected;
        selectionUI.OnBack += OnBack;

        moveDetailsUI.SetActive(true);
        bs.DialogBox.EnableDialogText(false);
    }

    public override void Execute()
    {
        selectionUI.HandleUpdate();
    }

    public override void Exit()
    {
        selectionUI.gameObject.SetActive(false);
        selectionUI.OnSelected -= OnMoveSelected;
        selectionUI.OnBack -= OnBack;

        selectionUI.ClearItems();

        moveDetailsUI.SetActive(false);
        bs.DialogBox.EnableDialogText(true);
    }

    void OnMoveSelected(int selection)
    {
        bs.SelectedMove = selection;
        bs.StateMachine.ChangeState(RunTurnState.i);
    }

    void OnBack()
    {
        bs.StateMachine.ChangeState(ActionSelectionState.i);
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveSelectionState : State<BattleSystem>
{
    [SerializeField] MoveSelectionUI selectionUI;
    [SerializeField] GameObject moveDetailsUI;

    // Input
    public List<Move> Moves { get; set; }

    public static MoveSelectionState i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    BattleSystem bs;
    public override void Enter(BattleSystem owner)
    {
        bs = owner;

        selectionUI.SetMoves(Moves);

        selectionUI.gameObject.SetActive(true);
        selectionUI.OnSelected += OnMoveSelected;
        selectionUI.OnBack += OnBack;

        moveDetailsUI.SetActive(true);
        bs.DialogBox.EnableDialogText(false);
    }

    public override void Execute()
    {
        selectionUI.HandleUpdate();
    }

    public override void Exit()
    {
        selectionUI.gameObject.SetActive(false);
        selectionUI.OnSelected -= OnMoveSelected;
        selectionUI.OnBack -= OnBack;

        selectionUI.ClearItems();

        moveDetailsUI.SetActive(false);
        bs.DialogBox.EnableDialogText(true);
    }

    void OnMoveSelected(int selection)
    {
        bs.SelectedMove = selection;
        bs.StateMachine.ChangeState(RunTurnState.i);
    }

    void OnBack()
    {
        bs.StateMachine.ChangeState(ActionSelectionState.i);
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RunTurnState : State<BattleSystem>
{
    public static RunTurnState i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    BattleUnit playerUnit;
    BattleUnit enemyUnit;
    BattleDialogBox dialogBox;
    PartyScreen partyScreen;
    bool isTrainerBattle;
    CambroxParty playerParty;
    CambroxParty trainerParty;

    BattleSystem bs;
    public override void Enter(BattleSystem owner)
    {
        bs = owner;

        playerUnit = bs.PlayerUnit;
        enemyUnit = bs.EnemyUnit;
        dialogBox = bs.DialogBox;
        partyScreen = bs.PartyScreen;
        isTrainerBattle = bs.IsTrainerBattle;
        playerParty = bs.PlayerParty;
        trainerParty = bs.TrainerParty;

        StartCoroutine(RunTurns(bs.SelectedAction));
    }

    IEnumerator RunTurns(BattleAction playerAction)
    {

        if (playerAction == BattleAction.Move)
        {
            playerUnit.Cambrox.CurrentMove = playerUnit.Cambrox.Moves[bs.SelectedMove];
            enemyUnit.Cambrox.CurrentMove = enemyUnit.Cambrox.GetRandomMove();

            int playerMovePriority = playerUnit.Cambrox.CurrentMove.Base.Priority;
            int enemyMovePriority = enemyUnit.Cambrox.CurrentMove.Base.Priority;

            // Check who goes first
            bool playerGoesFirst = true;
            if (enemyMovePriority > playerMovePriority)
                playerGoesFirst = false;
            else if (enemyMovePriority == playerMovePriority)
                playerGoesFirst = playerUnit.Cambrox.Speed >= enemyUnit.Cambrox.Speed;

            var firstUnit = (playerGoesFirst) ? playerUnit : enemyUnit;
            var secondUnit = (playerGoesFirst) ? enemyUnit : playerUnit;

            var secondCambrox = secondUnit.Cambrox;

            // First Turn
            yield return RunMove(firstUnit, secondUnit, firstUnit.Cambrox.CurrentMove);
            yield return RunAfterTurn(firstUnit);
            if (bs.IsBattleOver) yield break;

            if (secondCambrox.HP > 0)
            {
                // Second Turn
                yield return RunMove(secondUnit, firstUnit, secondUnit.Cambrox.CurrentMove);
                yield return RunAfterTurn(secondUnit);
                if (bs.IsBattleOver) yield break;
            }
        }
        else
        {
            if (playerAction == BattleAction.SwitchCambrox)
            {
                yield return bs.SwitchCambrox(bs.SelectedCambrox);
            }
            else if (playerAction == BattleAction.UseItem)
            {
                if (bs.SelectedItem is cambroxballItem)
                {
                    yield return bs.Throwcambroxball(bs.SelectedItem as cambroxballItem);
                    if (bs.IsBattleOver) yield break;
                }
                else
                {
                    // This is handled from item screen, so do nothing and skip to enemy move
                }
            }
            else if (playerAction == BattleAction.Run)
            {
                yield return TryToEscape();
            }

            // Enemy Turn
            var enemyMove = enemyUnit.Cambrox.GetRandomMove();
            yield return RunMove(enemyUnit, playerUnit, enemyMove);
            yield return RunAfterTurn(enemyUnit);
            if (bs.IsBattleOver) yield break;
        }

        if (!bs.IsBattleOver)
            bs.StateMachine.ChangeState(ActionSelectionState.i);
    }

    IEnumerator RunMove(BattleUnit sourceUnit, BattleUnit targetUnit, Move move)
    {
        bool canRunMove = sourceUnit.Cambrox.OnBeforeMove();
        if (!canRunMove)
        {
            yield return ShowStatusChanges(sourceUnit.Cambrox);
            yield return sourceUnit.Hud.WaitForHPUpdate();
            yield break;
        }
        yield return ShowStatusChanges(sourceUnit.Cambrox);

        move.PP--;
        yield return dialogBox.TypeDialog($"{sourceUnit.Cambrox.Base.Name} used {move.Base.Name}");

        if (CheckIfMoveHits(move, sourceUnit.Cambrox, targetUnit.Cambrox))
        {

            sourceUnit.PlayAttackAnimation();
            AudioManager.i.PlaySfx(move.Base.Sound);

            yield return new WaitForSeconds(1f);

            targetUnit.PlayHitAnimation();
            AudioManager.i.PlaySfx(AudioId.Hit);

            if (move.Base.Category == MoveCategory.Status)
            {
                yield return RunMoveEffects(move.Base.Effects, sourceUnit.Cambrox, targetUnit.Cambrox, move.Base.Target);
            }
            else
            {
                var damageDetails = targetUnit.Cambrox.TakeDamage(move, sourceUnit.Cambrox);
                yield return targetUnit.Hud.WaitForHPUpdate();
                yield return ShowDamageDetails(damageDetails);
            }

            if (move.Base.Secondaries != null && move.Base.Secondaries.Count > 0 && targetUnit.Cambrox.HP > 0)
            {
                foreach (var secondary in move.Base.Secondaries)
                {
                    var rnd = UnityEngine.Random.Range(1, 101);
                    if (rnd <= secondary.Chance)
                        yield return RunMoveEffects(secondary, sourceUnit.Cambrox, targetUnit.Cambrox, secondary.Target);
                }
            }

            if (targetUnit.Cambrox.HP <= 0)
            {
                yield return HandleCambroxFainted(targetUnit);
            }

        }
        else
        {
            yield return dialogBox.TypeDialog($"{sourceUnit.Cambrox.Base.Name}'s attack missed");
        }
    }

    IEnumerator RunMoveEffects(MoveEffects effects, Cambrox source, Cambrox target, MoveTarget moveTarget)
    {
        // Stat Boosting
        if (effects.Boosts != null)
        {
            if (moveTarget == MoveTarget.Self)
                source.ApplyBoosts(effects.Boosts);
            else
                target.ApplyBoosts(effects.Boosts);
        }

        // Status Condition
        if (effects.Status != ConditionID.none)
        {
            target.SetStatus(effects.Status);
        }

        // Volatile Status Condition
        if (effects.VolatileStatus != ConditionID.none)
        {
            target.SetVolatileStatus(effects.VolatileStatus);
        }

        yield return ShowStatusChanges(source);
        yield return ShowStatusChanges(target);
    }

    IEnumerator RunAfterTurn(BattleUnit sourceUnit)
    {
        if (bs.IsBattleOver) yield break;

        // Statuses like burn or psn will hurt the Cambrox after the turn
        sourceUnit.Cambrox.OnAfterTurn();
        yield return ShowStatusChanges(sourceUnit.Cambrox);
        yield return sourceUnit.Hud.WaitForHPUpdate();
        if (sourceUnit.Cambrox.HP <= 0)
        {
            yield return HandleCambroxFainted(sourceUnit);
        }
    }

    bool CheckIfMoveHits(Move move, Cambrox source, Cambrox target)
    {
        if (move.Base.AlwaysHits)
            return true;

        float moveAccuracy = move.Base.Accuracy;

        int accuracy = source.StatBoosts[Stat.Accuracy];
        int evasion = target.StatBoosts[Stat.Evasion];

        var boostValues = new float[] { 1f, 4f / 3f, 5f / 3f, 2f, 7f / 3f, 8f / 3f, 3f };

        if (accuracy > 0)
            moveAccuracy *= boostValues[accuracy];
        else
            moveAccuracy /= boostValues[-accuracy];

        if (evasion > 0)
            moveAccuracy /= boostValues[evasion];
        else
            moveAccuracy *= boostValues[-evasion];

        return UnityEngine.Random.Range(1, 101) <= moveAccuracy;
    }

    IEnumerator ShowStatusChanges(Cambrox Cambrox)
    {
        while (Cambrox.StatusChanges.Count > 0)
        {
            var message = Cambrox.StatusChanges.Dequeue();
            yield return dialogBox.TypeDialog(message);
        }
    }

    IEnumerator HandleCambroxFainted(BattleUnit faintedUnit)
    {
        yield return dialogBox.TypeDialog($"{faintedUnit.Cambrox.Base.Name} Fainted");
        faintedUnit.PlayFaintAnimation();
        yield return new WaitForSeconds(2f);

        if (!faintedUnit.IsPlayerUnit)
        {
            bool battlWon = true;
            if (isTrainerBattle)
                battlWon = trainerParty.GetHealthyCambrox() == null;

            if (battlWon)
                AudioManager.i.PlayMusic(bs.BattleVictoryMusic);

            // Exp Gain
            int expYield = faintedUnit.Cambrox.Base.ExpYield;
            int enemyLevel = faintedUnit.Cambrox.Level;
            float trainerBonus = (isTrainerBattle) ? 1.5f : 1f;

            int expGain = Mathf.FloorToInt((expYield * enemyLevel * trainerBonus) / 7);
            playerUnit.Cambrox.Exp += expGain;
            yield return dialogBox.TypeDialog($"{playerUnit.Cambrox.Base.Name} gained {expGain} exp");
            yield return playerUnit.Hud.SetExpSmooth();

            // Check Level Up
            while (playerUnit.Cambrox.CheckForLevelUp())
            {
                playerUnit.Hud.SetLevel();
                yield return dialogBox.TypeDialog($"{playerUnit.Cambrox.Base.Name} grew to level {playerUnit.Cambrox.Level}");

                // Try to learn a new Move
                var newMove = playerUnit.Cambrox.GetLearnableMoveAtCurrLevel();
                if (newMove != null)
                {
                    if (playerUnit.Cambrox.Moves.Count < CambroxBase.MaxNumOfMoves)
                    {
                        playerUnit.Cambrox.LearnMove(newMove.Base);
                        yield return dialogBox.TypeDialog($"{playerUnit.Cambrox.Base.Name} learned {newMove.Base.Name}");
                        dialogBox.SetMoveNames(playerUnit.Cambrox.Moves);
                    }
                    else
                    {
                        yield return dialogBox.TypeDialog($"{playerUnit.Cambrox.Base.Name} trying to learn {newMove.Base.Name}");
                        yield return dialogBox.TypeDialog($"But it cannot learn more than {CambroxBase.MaxNumOfMoves} moves");
                        yield return dialogBox.TypeDialog($"Choose a move a move to forget");

                        MoveToForgetState.i.CurrentMoves = playerUnit.Cambrox.Moves.Select(x => x.Base).ToList();
                        MoveToForgetState.i.NewMove = newMove.Base;
                        yield return GameController.Instance.StateMachine.PushAndWait(MoveToForgetState.i);

                        var moveIndex = MoveToForgetState.i.Selection;
                        if (moveIndex == CambroxBase.MaxNumOfMoves || moveIndex == -1)
                        {
                            // Don't learn the new move
                            yield return dialogBox.TypeDialog($"{playerUnit.Cambrox.Base.Name} did not learn {newMove.Base.Name}");
                        }
                        else
                        {
                            // Forget the selected move and learn new move
                            var selectedMove = playerUnit.Cambrox.Moves[moveIndex].Base;
                            yield return dialogBox.TypeDialog($"{playerUnit.Cambrox.Base.Name} forgot {selectedMove.Name} and learned {newMove.Base.Name}");

                            playerUnit.Cambrox.Moves[moveIndex] = new Move(newMove.Base);
                        }
                    }
                }

                yield return playerUnit.Hud.SetExpSmooth(true);
            }


            yield return new WaitForSeconds(1f);
        }

        yield return CheckForBattleOver(faintedUnit);
    }

    IEnumerator CheckForBattleOver(BattleUnit faintedUnit)
    {
        if (faintedUnit.IsPlayerUnit)
        {
            var nextCambrox = playerParty.GetHealthyCambrox();
            if (nextCambrox != null)
            {
                yield return GameController.Instance.StateMachine.PushAndWait(PartyState.i);
                yield return bs.SwitchCambrox(PartyState.i.SelectedCambrox);
            }
            else
                bs.BattleOver(false);
        }
        else
        {
            if (!isTrainerBattle)
            {
                bs.BattleOver(true);
            }
            else
            {
                var nextCambrox = trainerParty.GetHealthyCambrox();
                if (nextCambrox != null) 
                {
                    AboutToUseState.i.NewCambrox = nextCambrox;
                    yield return bs.StateMachine.PushAndWait(AboutToUseState.i);
                }
                else
                    bs.BattleOver(true);
            }
        }
    }

    IEnumerator ShowDamageDetails(DamageDetails damageDetails)
    {
        if (damageDetails.Critical > 1f)
            yield return dialogBox.TypeDialog("A critical hit!");

        if (damageDetails.TypeEffectiveness > 1f)
            yield return dialogBox.TypeDialog("It's super effective!");
        else if (damageDetails.TypeEffectiveness < 1f)
            yield return dialogBox.TypeDialog("It's not very effective!");
    }

    IEnumerator TryToEscape()
    {

        if (isTrainerBattle)
        {
            yield return dialogBox.TypeDialog($"You can't run from trainer battles!");
            yield break;
        }

        ++bs.EscapeAttempts;

        int playerSpeed = playerUnit.Cambrox.Speed;
        int enemySpeed = enemyUnit.Cambrox.Speed;

        if (enemySpeed < playerSpeed)
        {
            yield return dialogBox.TypeDialog($"Ran away safely!");
            bs.BattleOver(true);
        }
        else
        {
            float f = (playerSpeed * 128) / enemySpeed + 30 * bs.EscapeAttempts;
            f = f % 256;

            if (UnityEngine.Random.Range(0, 256) < f)
            {
                yield return dialogBox.TypeDialog($"Ran away safely!");
                bs.BattleOver(true);
            }
            else
            {
                yield return dialogBox.TypeDialog($"Can't escape!");
            }
        }
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DexManager : MonoBehaviour
{
    [SerializeField] private List<Cambrox> cambroxList;
    [SerializeField] private GameObject dexEntryPrefab;
    [SerializeField] private Transform dexContentParent;

    // Dictionary to track seen Cambrox
    private Dictionary<Cambrox, bool> seenCambrox = new Dictionary<Cambrox, bool>();

    // Dictionary for the manual "where to catch" info
    private Dictionary<Cambrox, string> catchLocations = new Dictionary<Cambrox, string>();

    private void Start()
    {
        InitializeDex();
    }

    public void InitializeDex()
    {
        // Initialize the seen dictionary with all Cambrox set to false
        foreach (Cambrox cambrox in cambroxList)
        {
            seenCambrox[cambrox] = false;
            catchLocations[cambrox] = "Unknown"; // Default location
        }

        // Create UI entries for each Cambrox
        foreach (Cambrox cambrox in cambroxList)
        {
            GameObject dexEntry = Instantiate(dexEntryPrefab, dexContentParent);
            UpdateDexEntry(dexEntry, cambrox);
        }
    }

        public Cambrox GetCambroxAtIndex(int index)
    {
        if (index >= 0 && index < cambroxList.Count)
        {
            return cambroxList[index];
        }
        return null;
    }
    
    // Mark a Cambrox as seen and update the UI
    public void MarkAsSeen(Cambrox cambrox, string location)
    {
        if (seenCambrox.ContainsKey(cambrox))
        {
            seenCambrox[cambrox] = true;
            catchLocations[cambrox] = location;
            UpdateDex();
        }
    }

    private void UpdateDex()
    {
        // Clear current entries
        foreach (Transform child in dexContentParent)
        {
            Destroy(child.gameObject);
        }

        // Recreate UI entries with updated data
        foreach (Cambrox cambrox in cambroxList)
        {
            GameObject dexEntry = Instantiate(dexEntryPrefab, dexContentParent);
            UpdateDexEntry(dexEntry, cambrox);
        }
    }

    private void UpdateDexEntry(GameObject dexEntry, Cambrox cambrox)
    {
        // Find components within the instantiated prefab
        Text nameText = dexEntry.transform.Find("DexName").GetComponent<Text>();
        Image pictureImage = dexEntry.transform.Find("DexImage").GetComponent<Image>();
        Text locationText = dexEntry.transform.Find("DexLocation").GetComponent<Text>();

        // Check if Cambrox has been seen
        if (seenCambrox[cambrox])
        {
            nameText.text = cambrox.Base.Name;
            pictureImage.sprite = cambrox.Base.FrontSprite;
            locationText.text = catchLocations[cambrox];
        }
        else
        {
            nameText.text = "???";
            pictureImage.sprite = null; // Or a placeholder sprite
            locationText.text = "???";
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public float moveSpeed;

    public bool IsMoving { get; private set; }

    public float OffsetY { get; private set; } = 0.3f;

    CharacterAnimator animator;
    private void Awake()
    {
        animator = GetComponent<CharacterAnimator>();
        SetPositionAndSnapToTile(transform.position);
    }

    public void SetPositionAndSnapToTile(Vector2 pos)
    {
        pos.x = Mathf.Floor(pos.x) + 0.5f;
        pos.y = Mathf.Floor(pos.y) + 0.5f + OffsetY;

        transform.position = pos;
    }

    public IEnumerator Move(Vector2 moveVec, Action OnMoveOver = null, bool checkCollisions = true)
    {
        animator.MoveX = Mathf.Clamp(moveVec.x, -1f, 1f);
        animator.MoveY = Mathf.Clamp(moveVec.y, -1f, 1f);

        var targetPos = transform.position;
        targetPos.x += moveVec.x;
        targetPos.y += moveVec.y;

        var ledge = CheckForLedge(targetPos);
        if (ledge != null)
        {
            if (ledge.TryToJump(this, moveVec))
                yield break;
        }

        if (checkCollisions && !IsPathClear(targetPos))
            yield break;

        if (animator.IsSurfing && Physics2D.OverlapCircle(targetPos, 0.3f, GameLayers.i.WaterLayer) == null)
            animator.IsSurfing = false;

        IsMoving = true;

        while ((targetPos - transform.position).sqrMagnitude > Mathf.Epsilon)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
        transform.position = targetPos;

        IsMoving = false;

        OnMoveOver?.Invoke();
    }

    public void HandleUpdate()
    {
        animator.IsMoving = IsMoving;
    }

    private bool IsPathClear(Vector3 targetPos)
    {
        var diff = targetPos - transform.position;
        var dir = diff.normalized;

        var collisionLayer = GameLayers.i.SolidLayer | GameLayers.i.InteractableLayer | GameLayers.i.PlayerLayer;
        if (!animator.IsSurfing)
            collisionLayer = collisionLayer | GameLayers.i.WaterLayer;

        if (Physics2D.BoxCast(transform.position + dir, new Vector2(0.2f, 0.2f), 0f, dir, diff.magnitude - 1, collisionLayer) == true)
            return false;

        return true;
    }

    private bool IsWalkable(Vector3 targetPos)
    {
        if (Physics2D.OverlapCircle(targetPos, 0.2f, GameLayers.i.SolidLayer | GameLayers.i.InteractableLayer) != null)
        {
            return false;
        }

        return true;
    }

    Ledge CheckForLedge(Vector3 targetPos)
    {
        var collider = Physics2D.OverlapCircle(targetPos, 0.15f, GameLayers.i.LedgeLayer);
        return collider?.GetComponent<Ledge>();
    }

    public void LookTowards(Vector3 targetPos)
    {
        var xdiff = Mathf.Floor(targetPos.x) - Mathf.Floor(transform.position.x);
        var ydiff = Mathf.Floor(targetPos.y) - Mathf.Floor(transform.position.y);

        if (xdiff == 0 || ydiff == 0)
        {
            animator.MoveX = Mathf.Clamp(xdiff, -1f, 1f);
            animator.MoveY = Mathf.Clamp(ydiff, -1f, 1f);
        }
        else
            Debug.LogError("Error in Look Towards: You can't ask the character to look diagonally");
    }

    public CharacterAnimator Animator {
        get => animator;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterAnimator : MonoBehaviour
{
    [SerializeField] List<Sprite> walkDownSprites;
    [SerializeField] List<Sprite> walkUpSprites;
    [SerializeField] List<Sprite> walkRightSprites;
    [SerializeField] List<Sprite> walkLeftSprites;
    [SerializeField] List<Sprite> surfSprites;
    [SerializeField] FacingDirection defaultDirection = FacingDirection.Down;

    // Parameters
    public float MoveX { get; set; }
    public float MoveY { get; set; }
    public bool IsMoving { get; set; }
    public bool IsJumping { get; set; }
    public bool IsSurfing { get; set; }

    // States
    SpriteAnimator walkDownAnim;
    SpriteAnimator walkUpAnim;
    SpriteAnimator walkRightAnim;
    SpriteAnimator walkLeftAnim;

    SpriteAnimator currentAnim;
    bool wasPreviouslyMoving;

    // Refrences
    SpriteRenderer spriteRenderer;

    private void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        walkDownAnim = new SpriteAnimator(walkDownSprites, spriteRenderer);
        walkUpAnim = new SpriteAnimator(walkUpSprites, spriteRenderer);
        walkRightAnim = new SpriteAnimator(walkRightSprites, spriteRenderer);
        walkLeftAnim = new SpriteAnimator(walkLeftSprites, spriteRenderer);
        SetFacingDirection(defaultDirection);

        currentAnim = walkDownAnim;
    }

    private void Update()
    {
        var prevAnim = currentAnim;

        if (!IsSurfing)
        {
            if (MoveX == 1)
                currentAnim = walkRightAnim;
            else if (MoveX == -1)
                currentAnim = walkLeftAnim;
            else if (MoveY == 1)
                currentAnim = walkUpAnim;
            else if (MoveY == -1)
                currentAnim = walkDownAnim;

            if (currentAnim != prevAnim || IsMoving != wasPreviouslyMoving)
                currentAnim.Start();

            if (IsJumping)
                spriteRenderer.sprite = currentAnim.Frames[currentAnim.Frames.Count - 1];
            else if (IsMoving)
                currentAnim.HandleUpdate();
            else
                spriteRenderer.sprite = currentAnim.Frames[0];
        }
        else
        {
            if (MoveX == 1)
                spriteRenderer.sprite = surfSprites[2];
            else if (MoveX == -1)
                spriteRenderer.sprite = surfSprites[3];
            else if (MoveY == 1)
                spriteRenderer.sprite = surfSprites[1];
            else if (MoveY == -1)
                spriteRenderer.sprite = surfSprites[0];
        }


        wasPreviouslyMoving = IsMoving;
    }

    public void SetFacingDirection(FacingDirection dir)
    {
        if (dir == FacingDirection.Right)
            MoveX = 1;
        else if (dir == FacingDirection.Left)
            MoveX = -1;
        else if (dir == FacingDirection.Down)
            MoveY = -1;
        else if (dir == FacingDirection.Up)
            MoveY = 1;
    }

    public FacingDirection DefaultDirection {
        get => defaultDirection;
    }
}

public enum FacingDirection { Up, Down, Left, Right }
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Healer : MonoBehaviour
{
    public IEnumerator Heal(Transform player, Dialog dialog)
    {
        int selectedChoice = 0;

        yield return DialogManager.Instance.ShowDialog(dialog,
            new List<string>() { "Yes please", "No thanks" },
            (choiceIndex) => selectedChoice = choiceIndex);

        if (selectedChoice == 0)
        {
            // Yes
            yield return Fader.i.FadeIn(0.5f);

            var playerParty = player.GetComponent<CambroxParty>();
            playerParty.Cambroxs.ForEach(p => p.Heal());
            playerParty.PartyUpdated();

            yield return Fader.i.FadeOut(0.5f);

            yield return DialogManager.Instance.ShowDialogText($"Your Cambrox should be fully healed now");
        }
        else if (selectedChoice == 1)
        {
            // No
            yield return DialogManager.Instance.ShowDialogText($"Okay! Come back if you change your mind");
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Merchant : MonoBehaviour
{
    [SerializeField] List<ItemBase> availableItems;

    public IEnumerator Trade()
    {
        ShopMenuState.i.AvailableItems = availableItems;
        yield return GameController.Instance.StateMachine.PushAndWait(ShopMenuState.i);
    }

    public List<ItemBase> AvailableItems => availableItems;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCController : MonoBehaviour, Interactable, ISavable
{
    [SerializeField] Dialog dialog;

    [Header("Quests")]
    [SerializeField] QuestBase questToStart;
    [SerializeField] QuestBase questToComplete;

    [Header("Movement")]
    [SerializeField] List<Vector2> movementPattern;
    [SerializeField] float timeBetweenPattern;

    NPCState state;
    float idleTimer = 0f;
    int currentPattern = 0;
    Quest activeQuest;

    Character character;
    ItemGiver itemGiver;
    CambroxGiver CambroxGiver;
    Healer healer;
    Merchant merchant;
    private void Awake()
    {
        character = GetComponent<Character>();
        itemGiver = GetComponent<ItemGiver>();
        CambroxGiver = GetComponent<CambroxGiver>();
        healer = GetComponent<Healer>();
        merchant = GetComponent<Merchant>();
    }

    public IEnumerator Interact(Transform initiator)
    {
        if (state == NPCState.Idle)
        {
            state = NPCState.Dialog;
            character.LookTowards(initiator.position);

            if (questToComplete != null)
            {
                var quest = new Quest(questToComplete);
                yield return quest.CompleteQuest(initiator);
                questToComplete = null;

                Debug.Log($"{quest.Base.Name} completed");
            }

            if (itemGiver != null && itemGiver.CanBeGiven())
            {
                yield return itemGiver.GiveItem(initiator.GetComponent<PlayerController>());
            }
            else if (CambroxGiver != null && CambroxGiver.CanBeGiven())
            {
                yield return CambroxGiver.GiveCambrox(initiator.GetComponent<PlayerController>());
            }
            else if (questToStart != null)
            {
                activeQuest = new Quest(questToStart);
                yield return activeQuest.StartQuest();
                questToStart = null;

                if (activeQuest.CanBeCompleted())
                {
                    yield return activeQuest.CompleteQuest(initiator);
                    activeQuest = null;
                }
            }
            else if (activeQuest != null)
            {
                if (activeQuest.CanBeCompleted())
                {
                    yield return activeQuest.CompleteQuest(initiator);
                    activeQuest = null;
                }
                else
                {
                    yield return DialogManager.Instance.ShowDialog(activeQuest.Base.InProgressDialogue);
                }
            }
            else if (healer != null)
            {
                yield return healer.Heal(initiator, dialog);
            }
            else if (merchant != null)
            {
                yield return merchant.Trade();
            }
            else
            {
                yield return DialogManager.Instance.ShowDialog(dialog);
            }

            idleTimer = 0f;
            state = NPCState.Idle;
        }
    }

    private void Update()
    {
        if (state == NPCState.Idle)
        {
            idleTimer += Time.deltaTime;
            if (idleTimer > timeBetweenPattern)
            {
                idleTimer = 0f;
                if (movementPattern.Count > 0)
                    StartCoroutine(Walk());
            }
        }

        character.HandleUpdate();
    }

    IEnumerator Walk()
    {
        state = NPCState.Walking;

        var oldPos = transform.position;

        yield return character.Move(movementPattern[currentPattern]);

        if (transform.position != oldPos)
            currentPattern = (currentPattern + 1) % movementPattern.Count;

        state = NPCState.Idle;
    }

    public object CaptureState()
    {
        var saveData = new NPCQuestSaveData();
        saveData.activeQuest = activeQuest?.GetSaveData();

        if (questToStart != null)
            saveData.questToStart = (new Quest(questToStart)).GetSaveData();

        if (questToComplete != null)
            saveData.questToComplete = (new Quest(questToComplete)).GetSaveData();

        return saveData;
    }

    public void RestoreState(object state)
    {
        var saveData = state as NPCQuestSaveData;
        if (saveData != null)
        {
            activeQuest = (saveData.activeQuest != null) ? new Quest(saveData.activeQuest) : null;

            questToStart = (saveData.questToStart != null) ? new Quest(saveData.questToStart).Base : null;
            questToComplete = (saveData.questToComplete != null) ? new Quest(saveData.questToComplete).Base : null;
        }
    }
}

[System.Serializable]
public class NPCQuestSaveData
{
    public QuestSaveData activeQuest;
    public QuestSaveData questToStart;
    public QuestSaveData questToComplete;
}

public enum NPCState { Idle, Walking, Dialog }
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerController : MonoBehaviour, ISavable
{
    [SerializeField] string name;
    [SerializeField] Sprite sprite;

    private Vector2 input;

    public static PlayerController i { get; private set; }
    private Character character;
    private void Awake()
    {
        i = this;
        character = GetComponent<Character>();
    }

    public void HandleUpdate()
    {
        if (!character.IsMoving)
        {
            input.x = Input.GetAxisRaw("Horizontal");
            input.y = Input.GetAxisRaw("Vertical");

            // remove diagonal movement
            if (input.x != 0) input.y = 0;

            if (input != Vector2.zero)
            {
                StartCoroutine(character.Move(input, OnMoveOver));
            }
        }

        character.HandleUpdate();

        if (Input.GetKeyDown(KeyCode.Z))
            StartCoroutine(Interact());
    }

    IPlayerTriggerable currentlyInTrigger;
    private void OnMoveOver()
    {
        var colliders = Physics2D.OverlapCircleAll(transform.position - new Vector3(0, character.OffsetY), 0.2f, GameLayers.i.TriggerableLayers);

        IPlayerTriggerable triggerable = null;
        foreach (var collider in colliders)
        {
            triggerable = collider.GetComponent<IPlayerTriggerable>();
            if (triggerable != null)
            {
                if (triggerable == currentlyInTrigger && !triggerable.TriggerRepeatedly)
                    break;

                triggerable.OnPlayerTriggered(this);
                currentlyInTrigger = triggerable;
                break;
            }
        }

        if (colliders.Count() == 0 || triggerable != currentlyInTrigger)
            currentlyInTrigger = null;
    }

    IEnumerator Interact()
    {
        var facingDir = new Vector3(character.Animator.MoveX, character.Animator.MoveY);
        var interactPos = transform.position + facingDir;

        // Debug.DrawLine(transform.position, interactPos, Color.green, 0.5f);

        var collider = Physics2D.OverlapCircle(interactPos, 0.3f, GameLayers.i.InteractableLayer | GameLayers.i.WaterLayer);
        if (collider != null)
        {
            yield return collider.GetComponent<Interactable>()?.Interact(transform);
        }
    }

    public object CaptureState()
    {
        var saveData = new PlayerSaveData()
        {
            position = new float[] { transform.position.x, transform.position.y },
            Cambroxs = GetComponent<CambroxParty>().Cambroxs.Select(p => p.GetSaveData()).ToList()
        };

        return saveData;
    }

    public void RestoreState(object state)
    {
        var saveData = (PlayerSaveData)state;

        // Restore Position
        var pos = saveData.position;
        transform.position = new Vector3(pos[0], pos[1]);

        // Restore Party
        GetComponent<CambroxParty>().Cambroxs = saveData.Cambroxs.Select(s => new Cambrox(s)).ToList();
    }

    public string Name {
        get => name;
    }

    public Sprite Sprite {
        get => sprite;
    }

    public Character Character => character;
}

[Serializable]
public class PlayerSaveData
{
    public float[] position;
    public List<CambroxSaveData> Cambroxs;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainerController : MonoBehaviour, Interactable, ISavable
{
    [SerializeField] string name;
    [SerializeField] Sprite sprite;
    [SerializeField] Dialog dialog;
    [SerializeField] Dialog dialogAfterBattle;
    [SerializeField] GameObject exclamation;
    [SerializeField] GameObject fov;

    [SerializeField] AudioClip trainerAppearsClip;

    // State
    bool battleLost = false;

    Character character;
    private void Awake()
    {
        character = GetComponent<Character>();
    }

    private void Start()
    {
        SetFovRotation(character.Animator.DefaultDirection);
    }

    private void Update()
    {
        character.HandleUpdate();
    }

    public IEnumerator Interact(Transform initiator)
    {
        character.LookTowards(initiator.position);

        if (!battleLost)
        {
            AudioManager.i.PlayMusic(trainerAppearsClip);

            yield return DialogManager.Instance.ShowDialog(dialog);
            GameController.Instance.StartTrainerBattle(this);
        }
        else
        {
            yield return DialogManager.Instance.ShowDialog(dialogAfterBattle);
        }
    }

    public IEnumerator TriggerTrainerBattle(PlayerController player)
    {
        GameController.Instance.StateMachine.Push(CutsceneState.i);

        AudioManager.i.PlayMusic(trainerAppearsClip);

        // Show Exclamation
        exclamation.SetActive(true);
        yield return new WaitForSeconds(0.8f);
        exclamation.SetActive(false);

        // Walk towards the player
        var diff = player.transform.position - transform.position;
        var moveVec = diff - diff.normalized;
        moveVec = new Vector2(Mathf.Round(moveVec.x), Mathf.Round(moveVec.y));

        yield return character.Move(moveVec);

        // Show dialog
        yield return DialogManager.Instance.ShowDialog(dialog);

        GameController.Instance.StateMachine.Pop();

        GameController.Instance.StartTrainerBattle(this);
    }

    public void BattleLost()
    {
        battleLost = true;
        fov.gameObject.SetActive(false);
    }

    public void SetFovRotation(FacingDirection dir)
    {
        float angle = 0f;
        if (dir == FacingDirection.Right)
            angle = 90f;
        else if (dir == FacingDirection.Up)
            angle = 180f;
        else if (dir == FacingDirection.Left)
            angle = 270;

        fov.transform.eulerAngles = new Vector3(0f, 0f, angle);
    }

    public object CaptureState()
    {
        return battleLost;
    }

    public void RestoreState(object state)
    {
        battleLost = (bool)state;

        if (battleLost)
            fov.gameObject.SetActive(false);
    }

    public string Name {
        get => name;
    }

    public Sprite Sprite {
        get => sprite;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainerFov : MonoBehaviour, IPlayerTriggerable
{
    public void OnPlayerTriggered(PlayerController player)
    {
        player.Character.Animator.IsMoving = false;
        GameController.Instance.OnEnterTrainersView(GetComponentInParent<TrainerController>());
    }

    public bool TriggerRepeatedly => false;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EssentialObjects : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EssentialObjectsSpawner : MonoBehaviour
{
    [SerializeField] GameObject essentialObjectsPrefab;

    private void Awake()
    {
        var existingObjects = FindObjectsOfType<EssentialObjects>();
        if (existingObjects.Length == 0)
        {
            // If there is a grid, then spawn at it's center
            var spawnPos = new Vector3(0, 0, 0);

            var grid = FindObjectOfType<Grid>();
            if (grid != null)
                spawnPos = grid.transform.position;

            Instantiate(essentialObjectsPrefab, spawnPos, Quaternion.identity);
        }
    }
}
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Fader : MonoBehaviour
{
    public static Fader i { get; private set; }
    Image image;
    private void Awake()
    {
        i = this;
        image = GetComponent<Image>();
    }

    public IEnumerator FadeIn(float time)
    {
        yield return image.DOFade(1f, time).WaitForCompletion();
    }

    public IEnumerator FadeOut(float time)
    {
        yield return image.DOFade(0f, time).WaitForCompletion();
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class InvisibleTilemap : MonoBehaviour
{
    private void Start()
    {
        GetComponent<TilemapRenderer>().enabled = false;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnActorAction : CutsceneAction
{
    [SerializeField] CutsceneActor actor;
    [SerializeField] FacingDirection direction;

    public override IEnumerator Play()
    {
        actor.GetCharacter().Animator.SetFacingDirection(direction);
        yield break;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportObjectAction : CutsceneAction
{
    [SerializeField] GameObject go;
    [SerializeField] Vector2 position;

    public override IEnumerator Play()
    {
        go.transform.position = position;
        yield break;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCInteractAction : CutsceneAction
{
    [SerializeField] NPCController npc;

    public override IEnumerator Play()
    {
        yield return npc.Interact(PlayerController.i.transform);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MoveActorAction : CutsceneAction
{
    [SerializeField] CutsceneActor actor;
    [SerializeField] List<Vector2> movePatterns;

    public override IEnumerator Play()
    {
        var character = actor.GetCharacter();

        foreach (var moveVec in movePatterns)
        {
            yield return character.Move(moveVec, checkCollisions: false);
        }
    }
}

[System.Serializable]
public class CutsceneActor
{
    [SerializeField] bool isPlayer;
    [SerializeField] Character character;

    public Character GetCharacter() => (isPlayer) ? PlayerController.i.Character : character;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeOutAction : CutsceneAction
{
    [SerializeField] float duration = 0.5f;

    public override IEnumerator Play()
    {
        yield return Fader.i.FadeOut(duration);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeInAction : CutsceneAction
{
    [SerializeField] float duration = 0.5f;

    public override IEnumerator Play()
    {
        yield return Fader.i.FadeIn(duration);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnableObjectAction : CutsceneAction
{
    [SerializeField] GameObject go;

    public override IEnumerator Play()
    {
        go.SetActive(true);
        yield break;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DisableObjectAction : CutsceneAction
{
    [SerializeField] GameObject go;

    public override IEnumerator Play()
    {
        go.SetActive(false);


        yield break;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueAction : CutsceneAction
{
    [SerializeField] Dialog dialog;

    public override IEnumerator Play()
    {
        yield return DialogManager.Instance.ShowDialog(dialog);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CutsceneAction
{
    [SerializeField] string name;
    [SerializeField] bool waitForCompletion = true;

    public virtual IEnumerator Play()
    {
        yield break;
    }

    public string Name {
        get => name;
        set => name = value;
    }

    public bool WaitForCompletion => waitForCompletion;
}
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class Cutscene : MonoBehaviour, IPlayerTriggerable
{
    [SerializeReference]
    [SerializeField] List<CutsceneAction> actions = new List<CutsceneAction>();

    public bool TriggerRepeatedly => false;

    public IEnumerator Play()
    {
        GameController.Instance.StateMachine.Push(CutsceneState.i);

        foreach (var action in actions)
        {
            if (action.WaitForCompletion)
                yield return action.Play();
            else
                StartCoroutine(action.Play());
        }

        GameController.Instance.StateMachine.Pop();
    }

    public void AddAction(CutsceneAction action)
    {
#if UNITY_EDITOR
        Undo.RegisterCompleteObjectUndo(this, "Add action to cutscene.");
#endif

        action.Name = action.GetType().ToString();
        actions.Add(action);
    }

    public void OnPlayerTriggered(PlayerController player)
    {
        player.Character.Animator.IsMoving = false;
        StartCoroutine(Play());
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Cutscene))]
public class CutsceneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        var cutscene = target as Cutscene;

        using (var scope = new GUILayout.HorizontalScope()) 
        {
            if (GUILayout.Button("Dialogue"))
                cutscene.AddAction(new DialogueAction());
            else if (GUILayout.Button("Move Actor"))
                cutscene.AddAction(new MoveActorAction());
            else if (GUILayout.Button("Turn Actor"))
                cutscene.AddAction(new TurnActorAction());
        }


        using (var scope = new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Teleport Object"))
                cutscene.AddAction(new TeleportObjectAction());
            else if (GUILayout.Button("Enable Object"))
                cutscene.AddAction(new EnableObjectAction());
            else if (GUILayout.Button("Disable Object"))
                cutscene.AddAction(new DisableObjectAction());
        }

        using (var scope = new GUILayout.HorizontalScope())
        {
            if (GUILayout.Button("NPC Interact"))
                cutscene.AddAction(new NPCInteractAction());
            else if (GUILayout.Button("Fade In"))
                cutscene.AddAction(new FadeInAction());
            else if (GUILayout.Button("Fade Out"))
                cutscene.AddAction(new FadeOutAction());
        }

        base.OnInspectorGUI();
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(CutsceneActor))]
public class CutsceneActorDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        position = EditorGUI.PrefixLabel(position, label);

        var togglePos = new Rect(position.x, position.y, 70, position.height);
        var fieldPos = new Rect(position.x + 70, position.y, position.width - 70, position.height);

        var isPlayerProp = property.FindPropertyRelative("isPlayer");

        isPlayerProp.boolValue = GUI.Toggle(togglePos, isPlayerProp.boolValue, "Is Player");
        isPlayerProp.serializedObject.ApplyModifiedProperties();

        if (!isPlayerProp.boolValue)
            EditorGUI.PropertyField(fieldPos, property.FindPropertyRelative("character"), GUIContent.none);

        EditorGUI.EndProperty();
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConditionsDB
{
    public static void Init()
    {
        foreach (var kvp in Conditions)
        {
            var conditionId = kvp.Key;
            var condition = kvp.Value;

            condition.Id = conditionId;
        }
    }

    public static Dictionary<ConditionID, Condition> Conditions { get; set; } = new Dictionary<ConditionID, Condition>()
    {
        {
            ConditionID.psn,
            new Condition()
            {
                Name = "Poison",
                StartMessage = "has been poisoned",
                OnAfterTurn = (Cambrox Cambrox) =>
                {
                    Cambrox.DecreaseHP(Cambrox.MaxHp / 8);
                    Cambrox.StatusChanges.Enqueue($"{Cambrox.Base.Name} hurt itself due to poison");
                }
            }
        },
        {
            ConditionID.brn,
            new Condition()
            {
                Name = "Burn",
                StartMessage = "has been burned",
                OnAfterTurn = (Cambrox Cambrox) =>
                {
                    Cambrox.DecreaseHP(Cambrox.MaxHp / 16);
                    Cambrox.StatusChanges.Enqueue($"{Cambrox.Base.Name} hurt itself due to burn");
                }
            }
        },
        {
            ConditionID.par,
            new Condition()
            {
                Name = "Paralyzed",
                StartMessage = "has been paralyzed",
                OnBeforeMove = (Cambrox Cambrox) =>
                {
                    if  (Random.Range(1, 5) == 1)
                    {
                        Cambrox.StatusChanges.Enqueue($"{Cambrox.Base.Name}'s paralyzed and can't move");
                        return false;
                    }

                    return true;
                }
            }
        },
        {
            ConditionID.frz,
            new Condition()
            {
                Name = "Freeze",
                StartMessage = "has been frozen",
                OnBeforeMove = (Cambrox Cambrox) =>
                {
                    if  (Random.Range(1, 5) == 1)
                    {
                        Cambrox.CureStatus();
                        Cambrox.StatusChanges.Enqueue($"{Cambrox.Base.Name}'s is not frozen anymore");
                        return true;
                    }

                    return false;
                }
            }
        },
        {
            ConditionID.slp,
            new Condition()
            {
                Name = "Sleep",
                StartMessage = "has fallen asleep",
                OnStart = (Cambrox Cambrox) =>
                {
                    // Sleep for 1-3 turns
                    Cambrox.StatusTime = Random.Range(1, 4);
                    Debug.Log($"Will be asleep for {Cambrox.StatusTime} moves");
                },
                OnBeforeMove = (Cambrox Cambrox) =>
                {
                    if (Cambrox.StatusTime <= 0)
                    {
                        Cambrox.CureStatus();
                        Cambrox.StatusChanges.Enqueue($"{Cambrox.Base.Name} woke up!");
                        return true;
                    }

                    Cambrox.StatusTime--;
                    Cambrox.StatusChanges.Enqueue($"{Cambrox.Base.Name} is sleeping");
                    return false;
                }
            }
        },

        // Volatile Status Conditions
        {
            ConditionID.confusion,
            new Condition()
            {
                Name = "Confusion",
                StartMessage = "has been confused",
                OnStart = (Cambrox Cambrox) =>
                {
                    // Confused for 1 - 4 turns
                    Cambrox.VolatileStatusTime = Random.Range(1, 5);
                    Debug.Log($"Will be confused for {Cambrox.VolatileStatusTime} moves");
                },
                OnBeforeMove = (Cambrox Cambrox) =>
                {
                    if (Cambrox.VolatileStatusTime <= 0)
                    {
                        Cambrox.CureVolatileStatus();
                        Cambrox.StatusChanges.Enqueue($"{Cambrox.Base.Name} kicked out of confusion!");
                        return true;
                    }
                    Cambrox.VolatileStatusTime--;

                    // 50% chance to do a move
                    if (Random.Range(1, 3) == 1)
                        return true;

                    // Hurt by confusion
                    Cambrox.StatusChanges.Enqueue($"{Cambrox.Base.Name} is confused");
                    Cambrox.DecreaseHP(Cambrox.MaxHp / 8);
                    Cambrox.StatusChanges.Enqueue($"It hurt itself due to confusion");
                    return false;
                }
            }
        }
    };

    public static float GetStatusBonus(Condition condition)
    {
        if (condition == null)
            return 1f;
        else if (condition.Id == ConditionID.slp || condition.Id == ConditionID.frz)
            return 2f;
        else if (condition.Id == ConditionID.par || condition.Id == ConditionID.psn || condition.Id == ConditionID.brn)
            return 1.5f;

        return 1f;
    }
}

public enum ConditionID
{
    none, psn, brn, slp, par, frz,
    confusion
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDB : ScriptableObjectDB<ItemBase>
{
    
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveDB : ScriptableObjectDB<MoveBase>
{

}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CambroxDB : ScriptableObjectDB<CambroxBase>
{
    
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestDB : ScriptableObjectDB<QuestBase>
{
    
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChoiceBox : MonoBehaviour
{
    [SerializeField] ChoiceText choiceTextPrefab;

    bool choiceSelected = false;

    List<ChoiceText> choiceTexts;
    int currentChoice;

    public IEnumerator ShowChoices(List<string> choices, Action<int> onChoiceSelected)
    {
        choiceSelected = false;
        currentChoice = 0;

        gameObject.SetActive(true);

        // Delete existing choices
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        choiceTexts = new List<ChoiceText>();
        foreach (var choice in choices)
        {
            var choiceTextObj = Instantiate(choiceTextPrefab, transform);
            choiceTextObj.TextField.text = choice;
            choiceTexts.Add(choiceTextObj);
        }

        yield return new WaitUntil(() => choiceSelected == true);

        onChoiceSelected?.Invoke(currentChoice);
        gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.DownArrow))
            ++currentChoice;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            --currentChoice;

        currentChoice = Mathf.Clamp(currentChoice, 0, choiceTexts.Count - 1);

        for (int i = 0; i < choiceTexts.Count; i++)
        {
            choiceTexts[i].SetSelected(i == currentChoice);
        }

        if (Input.GetKeyDown(KeyCode.Z))
            choiceSelected = true;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChoiceText : MonoBehaviour
{
    Text text;
    private void Awake()
    {
        text = GetComponent<Text>();
    }

    public void SetSelected(bool selected)
    {
        text.color = (selected) ? GlobalSettings.i.HighlightedColor : Color.black;
    }

    public Text TextField => text;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Dialog
{
    [SerializeField] List<string> lines;

    public List<string> Lines {
        get { return lines; }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogManager : MonoBehaviour
{
    [SerializeField] GameObject dialogBox;
    [SerializeField] ChoiceBox choiceBox;
    [SerializeField] Text dialogText;
    [SerializeField] int lettersPerSecond;

    public event Action OnShowDialog;
    public event Action OnDialogFinished;

    public static DialogManager Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }

    public bool IsShowing { get; private set; }

    public IEnumerator ShowDialogText(string text, bool waitForInput = true, bool autoClose = true,
        List<string> choices = null, Action<int> onChoiceSelected = null)
    {
        OnShowDialog?.Invoke();
        IsShowing = true;
        dialogBox.SetActive(true);

        AudioManager.i.PlaySfx(AudioId.UISelect);
        yield return TypeDialog(text);
        if (waitForInput)
        {
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Z));
        }

        if (choices != null && choices.Count > 1)
        {
            yield return choiceBox.ShowChoices(choices, onChoiceSelected);
        }

        if (autoClose)
        {
            CloseDialog();
        }
        OnDialogFinished?.Invoke();
    }

    public void CloseDialog()
    {
        dialogBox.SetActive(false);
        IsShowing = false;
    }

    public IEnumerator ShowDialog(Dialog dialog, List<string> choices = null,
        Action<int> onChoiceSelected = null)
    {
        yield return new WaitForEndOfFrame();

        OnShowDialog?.Invoke();
        IsShowing = true;
        dialogBox.SetActive(true);

        foreach (var line in dialog.Lines)
        {
            AudioManager.i.PlaySfx(AudioId.UISelect);
            yield return TypeDialog(line);
            yield return new WaitUntil(() => Input.GetKeyDown(KeyCode.Z));
        }

        if (choices != null && choices.Count > 1)
        {
            yield return choiceBox.ShowChoices(choices, onChoiceSelected);
        }

        dialogBox.SetActive(false);
        IsShowing = false;
        OnDialogFinished?.Invoke();
    }

    public void HandleUpdate()
    {

    }

    public IEnumerator TypeDialog(string line)
    {
        dialogText.text = "";
        foreach (var letter in line.ToCharArray())
        {
            dialogText.text += letter;
            yield return new WaitForSeconds(1f / lettersPerSecond);
        }
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UseItemState : State<GameController>
{
    [SerializeField] PartyScreen partyScreen;
    [SerializeField] InventoryUI inventoryUI;

    // Output
    public bool ItemUsed { get; private set; }

    public static UseItemState i { get; private set; }
    Inventory inventory;
    private void Awake()
    {
        i = this;
        inventory = Inventory.GetInventory();
    }

    GameController gc;
    public override void Enter(GameController owner)
    {
        gc = owner;

        ItemUsed = false;

        StartCoroutine(UseItem());
    }

    IEnumerator UseItem()
    {
        var item = inventoryUI.SelectedItem;
        var Cambrox = partyScreen.SelectedMember;

        if (item is TmItem)
        {
            yield return HandleTmItems();
        }
        else
        {
            // Handle Evolution Items
            if (item is EvolutionItem)
            {
                var evolution = Cambrox.CheckForEvolution(item);
                if (evolution != null)
                {
                    yield return EvolutionState.i.Evolve(Cambrox, evolution);
                }
                else
                {
                    yield return DialogManager.Instance.ShowDialogText($"It won't have any affect!");
                    gc.StateMachine.Pop();
                    yield break;
                }
            }

            var usedItem = inventory.UseItem(item, partyScreen.SelectedMember);
            if (usedItem != null)
            {
                ItemUsed = true;

                if (usedItem is RecoveryItem)
                    yield return DialogManager.Instance.ShowDialogText($"The player used {usedItem.Name}");
            }
            else
            {
                if (inventoryUI.SelectedCategory == (int)ItemCategory.Items)
                    yield return DialogManager.Instance.ShowDialogText($"It won't have any affect!");
            }
        }

        gc.StateMachine.Pop();
    }

    IEnumerator HandleTmItems()
    {
        var tmItem = inventoryUI.SelectedItem as TmItem;
        if (tmItem == null)
            yield break;

        var Cambrox = partyScreen.SelectedMember;

        if (Cambrox.HasMove(tmItem.Move))
        {
            yield return DialogManager.Instance.ShowDialogText($"{Cambrox.Base.Name} already know {tmItem.Move.Name}");
            yield break;
        }

        if (!tmItem.CanBeTaught(Cambrox))
        {
            yield return DialogManager.Instance.ShowDialogText($"{Cambrox.Base.Name} can't learn {tmItem.Move.Name}");
            yield break;
        }

        if (Cambrox.Moves.Count < CambroxBase.MaxNumOfMoves)
        {
            Cambrox.LearnMove(tmItem.Move);
            yield return DialogManager.Instance.ShowDialogText($"{Cambrox.Base.Name} learned {tmItem.Move.Name}");
        }
        else
        {
            yield return DialogManager.Instance.ShowDialogText($"{Cambrox.Base.Name} is trying to learn {tmItem.Move.Name}");
            yield return DialogManager.Instance.ShowDialogText($"But it cannot learn more than {CambroxBase.MaxNumOfMoves} moves");

            yield return DialogManager.Instance.ShowDialogText($"Choose a move you wan't to forget", true, false);

            MoveToForgetState.i.CurrentMoves = Cambrox.Moves.Select(x => x.Base).ToList();
            MoveToForgetState.i.NewMove = tmItem.Move;
            yield return gc.StateMachine.PushAndWait(MoveToForgetState.i);

            var moveIndex = MoveToForgetState.i.Selection;
            if (moveIndex == CambroxBase.MaxNumOfMoves || moveIndex == -1)
            {
                // Don't learn the new move
                yield return DialogManager.Instance.ShowDialogText($"{Cambrox.Base.Name} did not learn {tmItem.Move.Name}");
            }
            else
            {
                // Forget the selected move and learn new move
                var selectedMove = Cambrox.Moves[moveIndex].Base;
                yield return DialogManager.Instance.ShowDialogText($"{Cambrox.Base.Name} forgot {selectedMove.Name} and learned {tmItem.Move.Name}");

                Cambrox.Moves[moveIndex] = new Move(tmItem.Move);
            }
        }
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SummaryState : State<GameController>
{
    [SerializeField] SummaryScreenUI summaryScreen;

    int selectedPage = 0;

    // Input
    public int SelectedCambroxIndex { get; set; }

    public static SummaryState i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    List<Cambrox> playerParty;
    private void Start()
    {
        playerParty = PlayerController.i.GetComponent<CambroxParty>().Cambroxs;
    }

    GameController gc;
    public override void Enter(GameController owner)
    {
        gc = owner;

        summaryScreen.gameObject.SetActive(true);
        summaryScreen.SetBasicDetails(playerParty[SelectedCambroxIndex]);
        summaryScreen.ShowPage(selectedPage);
    }

    public override void Execute()
    {

        if (!summaryScreen.InMoveSelection)
        {
            // Page Selection
            int prevPage = selectedPage;
            if (Input.GetKeyDown(KeyCode.LeftArrow))
            {
                selectedPage = Mathf.Abs((selectedPage - 1) % 2);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow))
            {
                selectedPage = (selectedPage + 1) % 2;
            }

            if (selectedPage != prevPage)
            {
                summaryScreen.ShowPage(selectedPage);
            }

            // Cambrox Selection
            int prevSelection = SelectedCambroxIndex;
            if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                SelectedCambroxIndex += 1;

                if (SelectedCambroxIndex >= playerParty.Count)
                    SelectedCambroxIndex = 0;
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                SelectedCambroxIndex -= 1;

                if (SelectedCambroxIndex < 0)
                    SelectedCambroxIndex = playerParty.Count - 1;
            }

            if (SelectedCambroxIndex != prevSelection)
            {
                summaryScreen.SetBasicDetails(playerParty[SelectedCambroxIndex]);
                summaryScreen.ShowPage(selectedPage);
            }
        }

        if (Input.GetKeyDown(KeyCode.Z))
        {
            if (selectedPage == 1 && !summaryScreen.InMoveSelection)
            {
                summaryScreen.InMoveSelection = true;
            }
        }
        else if (Input.GetKeyDown(KeyCode.X))
        {
            if (summaryScreen.InMoveSelection)
            {
                summaryScreen.InMoveSelection = false;
            }
            else
            {
                gc.StateMachine.Pop();
                return;
            }
        }

        summaryScreen.HandleUpdate();
    }

    public override void Exit()
    {
        summaryScreen.gameObject.SetActive(false);
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StorageState : State<GameController>
{
    [SerializeField] CambroxStorageUI storageUI;

    bool isMovingCambrox = false;
    int selectedSlotToMove = 0;
    Cambrox selectedCambroxToMove = null;

    CambroxParty party;

    public static StorageState i { get; private set; }
    private void Awake()
    {
        i = this;
        party = CambroxParty.GetPlayerParty();
    }

    GameController gc;
    public override void Enter(GameController owner)
    {
        gc = owner;

        storageUI.gameObject.SetActive(true);
        storageUI.SetDataInPartySlots();
        storageUI.SetDataInStorageSlots();

        storageUI.OnSelected += OnSlotSelected;
        storageUI.OnBack += OnBack;
    }

    public override void Execute()
    {
        storageUI.HandleUpdate();
    }

    public override void Exit()
    {
        storageUI.gameObject.SetActive(false);
        storageUI.OnSelected -= OnSlotSelected;
        storageUI.OnBack -= OnBack;
    }

    void OnSlotSelected(int slotIndex)
    {
        if (!isMovingCambrox)
        {
            var Cambrox = storageUI.TakeCambroxFromSlot(slotIndex);
            if (Cambrox != null)
            {
                isMovingCambrox = true;
                selectedSlotToMove = slotIndex;
                selectedCambroxToMove = Cambrox;
            }
        }
        else
        {
            isMovingCambrox = false;

            int firstSlotIndex = selectedSlotToMove;
            int secondSlotIndex = slotIndex;

            var secondCambrox = storageUI.TakeCambroxFromSlot(slotIndex);

            if (secondCambrox == null && storageUI.IsPartySlot(firstSlotIndex) && storageUI.IsPartySlot(secondSlotIndex))
            {
                storageUI.PutCambroxIntoSlot(selectedCambroxToMove, selectedSlotToMove);
                return;
            }

            storageUI.PutCambroxIntoSlot(selectedCambroxToMove, secondSlotIndex);

            if (secondCambrox != null)
                storageUI.PutCambroxIntoSlot(secondCambrox, firstSlotIndex);

            party.Cambroxs.RemoveAll(p => p == null);
            party.PartyUpdated();

            storageUI.SetDataInStorageSlots();
            storageUI.SetDataInPartySlots();
        }
    }

    void OnBack()
    {
        if (isMovingCambrox)
        {
            isMovingCambrox = false;
            storageUI.PutCambroxIntoSlot(selectedCambroxToMove, selectedSlotToMove);
        }
        else
        {
            gc.StateMachine.Pop();
        }
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseState : State<GameController>
{
    public static PauseState i { get; private set; }
    private void Awake()
    {
        i = this;
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartyState : State<GameController>
{
    [SerializeField] PartyScreen partyScreen;

    public Cambrox SelectedCambrox { get; private set; }

    bool isSwitchingPosition;
    int selectedIndexForSwitching = 0;

    public static PartyState i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    CambroxParty playerParty;
    private void Start()
    {
        playerParty = PlayerController.i.GetComponent<CambroxParty>();
    }

    GameController gc;
    public override void Enter(GameController owner)
    {
        gc = owner;

        SelectedCambrox = null;
        partyScreen.gameObject.SetActive(true);
        partyScreen.OnSelected += OnCambroxSelected;
        partyScreen.OnBack += OnBack;
    }

    public override void Execute()
    {
        partyScreen.HandleUpdate();
    }

    public override void Exit()
    {
        partyScreen.gameObject.SetActive(false);
        partyScreen.OnSelected -= OnCambroxSelected;
        partyScreen.OnBack -= OnBack;
    }

    void OnCambroxSelected(int selection)
    {
        SelectedCambrox = partyScreen.SelectedMember;
        StartCoroutine(CambroxSelectedAction(selection));
    }

    IEnumerator CambroxSelectedAction(int selectedCambroxIndex)
    {
        var prevState = gc.StateMachine.GetPrevState();
        if (prevState == InventoryState.i)
        {
            StartCoroutine(GoToUseItemState());
        }
        else if (prevState == BattleState.i)
        {
            var battleState = prevState as BattleState;

            DynamicMenuState.i.MenuItems = new List<string>() { "Shift", "Summary", "Cancel" };
            yield return gc.StateMachine.PushAndWait(DynamicMenuState.i);
            if (DynamicMenuState.i.SelectedItem == 0)
            {
                if (SelectedCambrox.HP <= 0)
                {
                    partyScreen.SetMessageText("You can't send out a fainted Cambrox");
                    yield break;
                }
                if (SelectedCambrox == battleState.BattleSystem.PlayerUnit.Cambrox)
                {
                    partyScreen.SetMessageText("You can't switch with the same Cambrox");
                    yield break;
                }

                gc.StateMachine.Pop();
            }
            else if (DynamicMenuState.i.SelectedItem == 1)
            {
                SummaryState.i.SelectedCambroxIndex = selectedCambroxIndex;
                yield return gc.StateMachine.PushAndWait(SummaryState.i);
            }
            else
            {
                yield break;
            }
        }
        else
        {
            if (isSwitchingPosition)
            {
                if (selectedIndexForSwitching == selectedCambroxIndex)
                {
                    partyScreen.SetMessageText("You can't switch with the same Cambrox");
                    yield break;
                }

                isSwitchingPosition = false;

                var tmpCambrox = playerParty.Cambroxs[selectedIndexForSwitching];
                playerParty.Cambroxs[selectedIndexForSwitching] = playerParty.Cambroxs[selectedCambroxIndex];
                playerParty.Cambroxs[selectedCambroxIndex] = tmpCambrox;
                playerParty.PartyUpdated();

                yield break;
            }

            DynamicMenuState.i.MenuItems = new List<string>() { "Summary", "Switch Position", "Cancel" };
            yield return gc.StateMachine.PushAndWait(DynamicMenuState.i);
            if (DynamicMenuState.i.SelectedItem == 0)
            {
                SummaryState.i.SelectedCambroxIndex = selectedCambroxIndex;
                yield return gc.StateMachine.PushAndWait(SummaryState.i);
            }
            else if (DynamicMenuState.i.SelectedItem == 1)
            {
                // Switch Position
                isSwitchingPosition = true;
                selectedIndexForSwitching = selectedCambroxIndex;
                partyScreen.SetMessageText("Choose a Cambrox to switch position with.");
            }
            else
            {
                yield break;
            }
        }
    }

    IEnumerator GoToUseItemState()
    {
        yield return gc.StateMachine.PushAndWait(UseItemState.i);
        gc.StateMachine.Pop();
    }

    void OnBack()
    {
        SelectedCambrox = null;

        var prevState = gc.StateMachine.GetPrevState();
        if (prevState == BattleState.i)
        {
            var battleState = prevState as BattleState;
            if (battleState.BattleSystem.PlayerUnit.Cambrox.HP <= 0)
            {
                partyScreen.SetMessageText("You have to choose a Cambrox to continue");
                return;
            }
        }

        gc.StateMachine.Pop();
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveToForgetState : State<GameController>
{
    [SerializeField] MoveToForgetSelectionUI moveSelectionUI;

    // Inputs
    public List<MoveBase> CurrentMoves { get; set; }
    public MoveBase NewMove { get; set; }

    // Output
    public int Selection { get; private set; }

    public static MoveToForgetState i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    GameController gc;
    public override void Enter(GameController owner)
    {
        gc = owner;

        Selection = 0;

        moveSelectionUI.gameObject.SetActive(true);
        moveSelectionUI.SetMoveData(CurrentMoves, NewMove);

        moveSelectionUI.OnSelected += OnMoveSelected;
        moveSelectionUI.OnBack += OnBack;
    }

    public override void Execute()
    {
        moveSelectionUI.HandleUpdate();
    }

    public override void Exit()
    {
        moveSelectionUI.gameObject.SetActive(false);
        moveSelectionUI.OnSelected -= OnMoveSelected;
        moveSelectionUI.OnBack -= OnBack;
    }

    void OnMoveSelected(int selection)
    {
        Selection = selection;
        gc.StateMachine.Pop();
    }

    void OnBack()
    {
        Selection = -1;
        gc.StateMachine.Pop();
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryState : State<GameController>
{
    [SerializeField] InventoryUI inventoryUI;

    // Output
    public ItemBase SelectedItem { get; private set; }

    public static InventoryState i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    Inventory inventory;
    private void Start()
    {
        inventory = Inventory.GetInventory();
    }

    GameController gc;
    public override void Enter(GameController owner)
    {
        gc = owner;

        SelectedItem = null;

        inventoryUI.gameObject.SetActive(true);
        inventoryUI.OnSelected += OnItemSelected;
        inventoryUI.OnBack += OnBack;
    }

    public override void Execute()
    {
        inventoryUI.HandleUpdate();
    }

    public override void Exit()
    {
        inventoryUI.gameObject.SetActive(false);
        inventoryUI.OnSelected -= OnItemSelected;
        inventoryUI.OnBack -= OnBack;
    }

    void OnItemSelected(int selection)
    {
        SelectedItem = inventoryUI.SelectedItem;

        if (gc.StateMachine.GetPrevState() != ShopSellingState.i)
            StartCoroutine(SelectCambroxAndUseItem());
        else
            gc.StateMachine.Pop();
    }

    void OnBack()
    {
        SelectedItem = null;
        gc.StateMachine.Pop();
    }

    IEnumerator SelectCambroxAndUseItem()
    {
        var prevState = gc.StateMachine.GetPrevState();
        if (prevState == BattleState.i)
        {
            // In Battle
            if (!SelectedItem.CanUseInBattle)
            {
                yield return DialogManager.Instance.ShowDialogText("This item cannot be used in battle");
                yield break;
            }
        }
        else
        {
            // Outside Battle
            if (!SelectedItem.CanUseOutsideBattle)
            {
                yield return DialogManager.Instance.ShowDialogText("This item cannot be used outside battle");
                yield break;
            }
        }

        if (SelectedItem is cambroxballItem)
        {
            inventory.UseItem(SelectedItem, null);
            gc.StateMachine.Pop();
            yield break;
        }

        yield return gc.StateMachine.PushAndWait(PartyState.i);

        if (prevState == BattleState.i)
        {
            if (UseItemState.i.ItemUsed)
                gc.StateMachine.Pop();
        }
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameMenuState : State<GameController>
{
    [SerializeField] MenuController menuController;

    public static GameMenuState i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    GameController gc;
    public override void Enter(GameController owner)
    {
        gc = owner;
        menuController.gameObject.SetActive(true);
        menuController.OnSelected += OnMenuItemSelected;
        menuController.OnBack += OnBack;
    }

    public override void Execute()
    {
        menuController.HandleUpdate();
    }

    public override void Exit()
    {
        menuController.gameObject.SetActive(false);
        menuController.OnSelected -= OnMenuItemSelected;
        menuController.OnBack -= OnBack;
    }

    void OnMenuItemSelected(int selection)
    {
        if(selection == 0)
            gc.StateMachine.Push(PartyState.i);
        else if (selection == 1) // Cambrox
            gc.StateMachine.Push(PartyState.i);
        else if (selection == 2) // Bag
            gc.StateMachine.Push(InventoryState.i);
        else if (selection == 6) // Boxes
            gc.StateMachine.Push(StorageState.i);
    }

    void OnBack()
    {
        gc.StateMachine.Pop();
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FreeRoamState : State<GameController>
{

    public static FreeRoamState i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    GameController gc;
    public override void Enter(GameController owner)
    {
        gc = owner;
    }

    public override void Execute()
    {
        PlayerController.i.HandleUpdate();

        if (Input.GetKeyDown(KeyCode.Return))
            gc.StateMachine.Push(GameMenuState.i);

    }
}
using GDEUtils.StateMachine;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EvolutionState : State<GameController>
{
    [SerializeField] GameObject evolutionUI;
    [SerializeField] Image CambroxImage;

    [SerializeField] AudioClip evolutionMusic;

    public static EvolutionState i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    public IEnumerator Evolve(Cambrox Cambrox, Evolution evolution)
    {
        var gc = GameController.Instance;
        gc.StateMachine.Push(this);

        evolutionUI.SetActive(true);

        AudioManager.i.PlayMusic(evolutionMusic);

        CambroxImage.sprite = Cambrox.Base.FrontSprite;
        yield return DialogManager.Instance.ShowDialogText($"{Cambrox.Base.Name} is evolving");

        var oldCambrox = Cambrox.Base;
        Cambrox.Evolve(evolution);

        CambroxImage.sprite = Cambrox.Base.FrontSprite;
        yield return DialogManager.Instance.ShowDialogText($"{oldCambrox.Name} evolved into {Cambrox.Base.Name}");

        evolutionUI.SetActive(false);

        gc.PartyScreen.SetPartyData();
        AudioManager.i.PlayMusic(gc.CurrentScene.SceneMusic, fade: true);

        gc.StateMachine.Pop();
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicMenuState : State<GameController>
{
    [SerializeField] DynamicMenuUI dynamicMenuUI;
    [SerializeField] TextSlot itemTextPrefab;

    // Input
    public List<string> MenuItems { get; set; }

    // Output
    public int? SelectedItem { get; private set; }

    public static DynamicMenuState i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    GameController gc;
    public override void Enter(GameController owner)
    {
        gc = owner;

        foreach (Transform child in dynamicMenuUI.transform)
            Destroy(child.gameObject);

        var itemTextSlots = new List<TextSlot>();
        foreach (var menuItem in MenuItems)
        {
            var itemTextSlot = Instantiate(itemTextPrefab, dynamicMenuUI.transform);
            itemTextSlot.SetText(menuItem);
            itemTextSlots.Add(itemTextSlot);
        }

        dynamicMenuUI.SetItems(itemTextSlots);

        dynamicMenuUI.gameObject.SetActive(true);
        dynamicMenuUI.OnSelected += OnItemSelected;
        dynamicMenuUI.OnBack += OnBack;
    }

    public override void Execute()
    {
        dynamicMenuUI.HandleUpdate();
    }

    public override void Exit()
    {
        dynamicMenuUI.ClearItems();

        dynamicMenuUI.gameObject.SetActive(false);
        dynamicMenuUI.OnSelected -= OnItemSelected;
        dynamicMenuUI.OnBack -= OnBack;
    }

    void OnItemSelected(int selectedItem)
    {
        SelectedItem = selectedItem;
        gc.StateMachine.Pop();
    }

    void OnBack()
    {
        SelectedItem = null;
        gc.StateMachine.Pop();
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueState : State<GameController>
{
    public static DialogueState i { get; private set; }
    private void Awake()
    {
        i = this;
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CutsceneState : State<GameController>
{
    public static CutsceneState i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    public override void Execute()
    {
        PlayerController.i.Character.HandleUpdate();
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleState : State<GameController>
{
    [SerializeField] BattleSystem battleSystem;

    // Input
    public BattleTrigger trigger { get; set; }
    public TrainerController trainer { get; set; }

    public static BattleState i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    GameController gc;
    public override void Enter(GameController owner)
    {
        gc = owner;

        battleSystem.gameObject.SetActive(true);
        gc.WorldCamera.gameObject.SetActive(false);

        var playerParty = gc.PlayerController.GetComponent<CambroxParty>();

        if (trainer == null)
        {
            var wildCambrox = gc.CurrentScene.GetComponent<MapArea>().GetRandomWildCambrox(trigger);
            var wildCambroxCopy = new Cambrox(wildCambrox.Base, wildCambrox.Level);
            battleSystem.StartBattle(playerParty, wildCambroxCopy, trigger);
        }
        else
        {
            var trainerParty = trainer.GetComponent<CambroxParty>();
            battleSystem.StartTrainerBattle(playerParty, trainerParty);
        }

        battleSystem.OnBattleOver += EndBattle;
    }

    public override void Execute()
    {
        battleSystem.HandleUpdate();
    }

    public override void Exit()
    {
        battleSystem.gameObject.SetActive(false);
        gc.WorldCamera.gameObject.SetActive(true);

        battleSystem.OnBattleOver -= EndBattle;
    }

    void EndBattle(bool won)
    {
        if (trainer != null && won == true)
        {
            trainer.BattleLost();
            trainer = null;
        }

        gc.StateMachine.Pop();
    }

    public BattleSystem BattleSystem => battleSystem;
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopSellingState : State<GameController>
{
    [SerializeField] InventoryUI inventoryUI;
    [SerializeField] WalletUI walletUI;
    [SerializeField] CountSelectorUI countSelectorUI;

    public static ShopSellingState i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    Inventory inventory;
    private void Start()
    {
        inventory = Inventory.GetInventory();
    }

    // Input
    public List<ItemBase> AvailableItems { get; set; }

    GameController gc;
    public override void Enter(GameController owner)
    {
        gc = owner;

        StartCoroutine(StartSellingState());
    }

    IEnumerator StartSellingState()
    {
        yield return gc.StateMachine.PushAndWait(InventoryState.i);

        var selectedItem = InventoryState.i.SelectedItem;
        if (selectedItem != null)
        {
            yield return SellItem(selectedItem);
            StartCoroutine(StartSellingState());
        }
        else
            gc.StateMachine.Pop();
    }

    IEnumerator SellItem(ItemBase item)
    {

        if (!item.IsSellable)
        {
            yield return DialogManager.Instance.ShowDialogText("You can't sell that!");
            yield break;
        }

        walletUI.Show();

        float sellingPrice = Mathf.Round(item.Price / 2);
        int countToSell = 1;

        int itemCount = inventory.GetItemCount(item);
        if (itemCount > 1)
        {
            yield return DialogManager.Instance.ShowDialogText($"How many would you like to sell?",
                waitForInput: false, autoClose: false);

            yield return countSelectorUI.ShowSelector(itemCount, sellingPrice,
                (selectedCount) => countToSell = selectedCount);

            DialogManager.Instance.CloseDialog();
        }

        sellingPrice = sellingPrice * countToSell;

        int selectedChoice = 0;
        yield return DialogManager.Instance.ShowDialogText($"I can give {sellingPrice} for that! Would you like to sell?",
            waitForInput: false,
            choices: new List<string>() { "Yes", "No" },
            onChoiceSelected: choiceIndex => selectedChoice = choiceIndex);

        if (selectedChoice == 0)
        {
            // Yes
            inventory.RemoveItem(item, countToSell);
            Wallet.i.AddMoney(sellingPrice);
            yield return DialogManager.Instance.ShowDialogText($"Turned over {item.Name} and received {sellingPrice}!");
        }

        walletUI.Close();
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopMenuState : State<GameController>
{

    public static ShopMenuState i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    // Input
    public List<ItemBase> AvailableItems { get; set; }

    GameController gc;
    public override void Enter(GameController owner)
    {
        gc = owner;

        StartCoroutine(StartMenuState());
    }

    IEnumerator StartMenuState()
    {

        int selectedChoice = 0;
        yield return DialogManager.Instance.ShowDialogText("How may I serve you",
            waitForInput: false,
            choices: new List<string>() { "Buy", "Sell", "Quit" },
            onChoiceSelected: choiceIndex => selectedChoice = choiceIndex);

        if (selectedChoice == 0)
        {
            // Buy
            ShopBuyingState.i.AvailableItems = AvailableItems;
            yield return gc.StateMachine.PushAndWait(ShopBuyingState.i);
        }
        else if (selectedChoice == 1)
        {
            // Sell
            yield return gc.StateMachine.PushAndWait(ShopSellingState.i);
        }
        else if (selectedChoice == 2)
        {
            // Quit
        }

        gc.StateMachine.Pop();
    }
}
using GDEUtils.StateMachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopBuyingState : State<GameController>
{
    [SerializeField] Vector2 shopCameraOffset;
    [SerializeField] ShopUI shopUI;
    [SerializeField] WalletUI walletUI;
    [SerializeField] CountSelectorUI countSelectorUI;

    public static ShopBuyingState i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    Inventory inventory;
    private void Start()
    {
        inventory = Inventory.GetInventory();
    }

    // Input
    public List<ItemBase> AvailableItems { get; set; }

    bool browseItems = false;

    GameController gc;
    public override void Enter(GameController owner)
    {
        gc = owner;

        browseItems = false;
        StartCoroutine(StartBuyingState());
    }

    public override void Execute()
    {
        if (browseItems)
            shopUI.HandleUpdate();
    }

    IEnumerator StartBuyingState()
    {
        yield return GameController.Instance.MoveCamera(shopCameraOffset);
        walletUI.Show();
        shopUI.Show(AvailableItems, (item) => StartCoroutine(BuyItem(item)),
            () => StartCoroutine(OnBackFromBuying()));

        browseItems = true;
    }

    IEnumerator BuyItem(ItemBase item)
    {
        browseItems = false;

        yield return DialogManager.Instance.ShowDialogText("How many would you like to buy?",
            waitForInput: false, autoClose: false);

        int countToBuy = 1;
        yield return countSelectorUI.ShowSelector(100, item.Price,
            (selectedCount) => countToBuy = selectedCount);

        DialogManager.Instance.CloseDialog();

        float totalPrice = item.Price * countToBuy;

        if (Wallet.i.HasMoney(totalPrice))
        {
            int selectedChoice = 0;
            yield return DialogManager.Instance.ShowDialogText($"That will be {totalPrice}",
                waitForInput: false,
                choices: new List<string>() { "Yes", "No" },
                onChoiceSelected: choiceIndex => selectedChoice = choiceIndex);

            if (selectedChoice == 0)
            {
                // Selected Yes
                inventory.AddItem(item, countToBuy);
                Wallet.i.TakeMoney(totalPrice);
                yield return DialogManager.Instance.ShowDialogText("Thank you for shopping with us!");
            }
        }
        else
        {
            yield return DialogManager.Instance.ShowDialogText("Not enough money for that!");
        }

        browseItems = true;
    }

    IEnumerator OnBackFromBuying()
    {
        yield return GameController.Instance.MoveCamera(-shopCameraOffset);
        shopUI.Close();
        walletUI.Close();
        gc.StateMachine.Pop();
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wallet : MonoBehaviour, ISavable
{
    [SerializeField] float money;


    public event Action OnMoneyChanged;

    public static Wallet i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    public void AddMoney(float amount)
    {
        money += amount;
        OnMoneyChanged?.Invoke();
    }

    public void TakeMoney(float amount)
    {
        money -= amount;
        OnMoneyChanged?.Invoke();
    }

    public bool HasMoney(float amount)
    {
        return amount <= money;
    }

    public object CaptureState()
    {
        return money;
    }

    public void RestoreState(object state)
    {
        money = (float)state;
    }

    public float Money => money;
}
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SurfableWater : MonoBehaviour, Interactable, IPlayerTriggerable
{
    bool isJumpingToWater = false;

    public bool TriggerRepeatedly => true;

    public IEnumerator Interact(Transform initiator)
    {
        var animator = initiator.GetComponent<CharacterAnimator>();
        if (animator.IsSurfing || isJumpingToWater)
            yield break;

        yield return DialogManager.Instance.ShowDialogText("The water is deep blue!");

        var CambroxWithSurf = initiator.GetComponent<CambroxParty>().Cambroxs.FirstOrDefault(p => p.Moves.Any(m => m.Base.Name == "Surf"));

        if (CambroxWithSurf != null)
        {
            int selectedChoice = 0;
            yield return DialogManager.Instance.ShowDialogText($"Should {CambroxWithSurf.Base.Name} use surf?",
                choices: new List<string>() { "Yes", "No" },
                onChoiceSelected: (selection) => selectedChoice = selection);

            if (selectedChoice == 0)
            {
                // Yes
                yield return DialogManager.Instance.ShowDialogText($"{CambroxWithSurf.Base.Name} used surf!");

                var dir = new Vector3(animator.MoveX, animator.MoveY);
                var targetPos = initiator.position + dir;

                isJumpingToWater = true;
                yield return initiator.DOJump(targetPos, 0.3f, 1, 0.5f).WaitForCompletion();
                isJumpingToWater = false;

                animator.IsSurfing = true;
            }
        }
    }

    public void OnPlayerTriggered(PlayerController player)
    {
        if (UnityEngine.Random.Range(1, 101) <= 10)
        {
            GameController.Instance.StartBattle(BattleTrigger.Water);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StoryItem : MonoBehaviour, IPlayerTriggerable
{
    [SerializeField] Dialog dialog;

    public void OnPlayerTriggered(PlayerController player)
    {
        player.Character.Animator.IsMoving = false;
        StartCoroutine(DialogManager.Instance.ShowDialog(dialog));
    }

    public bool TriggerRepeatedly => false;
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MapArea : MonoBehaviour
{
    [SerializeField] List<CambroxEncounterRecord> wildCambroxs;
    [SerializeField] List<CambroxEncounterRecord> wildCambroxsInWater;

    [HideInInspector]
    [SerializeField] int totalChance = 0;

    [HideInInspector]
    [SerializeField] int totalChanceWater = 0;

    private void OnValidate()
    {
        CalculateChancePercentage();
    }

    private void Start()
    {
        CalculateChancePercentage();
    }

    void CalculateChancePercentage()
    {
        totalChance = -1;
        totalChanceWater = -1;

        if (wildCambroxs.Count > 0)
        {
            totalChance = 0;
            foreach (var record in wildCambroxs)
            {
                record.chanceLower = totalChance;
                record.chanceUpper = totalChance + record.chancePercentage;

                totalChance = totalChance + record.chancePercentage;
            }
        }

        if (wildCambroxsInWater.Count > 0)
        {
            totalChanceWater = 0;
            foreach (var record in wildCambroxsInWater)
            {
                record.chanceLower = totalChanceWater;
                record.chanceUpper = totalChanceWater + record.chancePercentage;

                totalChanceWater = totalChanceWater + record.chancePercentage;
            }
        }
    }

    public Cambrox GetRandomWildCambrox(BattleTrigger trigger)
    {
        var CambroxList = (trigger == BattleTrigger.LongGrass) ? wildCambroxs : wildCambroxsInWater;

        int randVal = Random.Range(1, 101);
        var CambroxRecord = CambroxList.First(p => randVal >= p.chanceLower && randVal <= p.chanceUpper);

        var levelRange = CambroxRecord.levelRange;
        int level = levelRange.y == 0 ? levelRange.x : Random.Range(levelRange.x, levelRange.y + 1);

        var wildCambrox = new Cambrox(CambroxRecord.Cambrox, level);
        wildCambrox.Init();
        return wildCambrox;
    }
}

[System.Serializable]
public class CambroxEncounterRecord
{
    public CambroxBase Cambrox;
    public Vector2Int levelRange;
    public int chancePercentage;

    public int chanceLower { get; set; }
    public int chanceUpper { get; set; }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LongGrass : MonoBehaviour, IPlayerTriggerable
{
    public void OnPlayerTriggered(PlayerController player)
    {
        if (UnityEngine.Random.Range(1, 101) <= 10)
        {
            player.Character.Animator.IsMoving = false;
            GameController.Instance.StartBattle(BattleTrigger.LongGrass);
        }
    }

    public bool TriggerRepeatedly => true;
}
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ledge : MonoBehaviour
{
    [SerializeField] int xDir;
    [SerializeField] int yDir;

    private void Awake()
    {
        GetComponent<SpriteRenderer>().enabled = false;
    }

    public bool TryToJump(Character character, Vector2 moveDir)
    {
        if (moveDir.x == xDir && moveDir.y == yDir)
        {
            StartCoroutine(Jump(character));
            return true;
        }

        return false;
    }

    IEnumerator Jump(Character character)
    {
        GameController.Instance.PauseGame(true);
        character.Animator.IsJumping = true;

        var jumpDest = character.transform.position + new Vector3(xDir, yDir) * 2;
        yield return character.transform.DOJump(jumpDest, 0.3f, 1, 0.5f).WaitForCompletion();

        character.Animator.IsJumping = false;
        GameController.Instance.PauseGame(false);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPlayerTriggerable
{
    void OnPlayerTriggered(PlayerController player);

    bool TriggerRepeatedly { get; }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface Interactable
{
    IEnumerator Interact(Transform initiator);
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalSettings : MonoBehaviour
{
    [SerializeField] Color highlightedColor;
    [SerializeField] Color bgHighlightColor;

    public Color HighlightedColor => highlightedColor;
    public Color BgHighlightColor => bgHighlightColor;

    public static GlobalSettings i { get; private set; }
    private void Awake()
    {
        i = this;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLayers : MonoBehaviour
{
    [SerializeField] LayerMask solidObjectsLayer;
    [SerializeField] LayerMask interactableLayer;
    [SerializeField] LayerMask grassLayer;
    [SerializeField] LayerMask playerLayer;
    [SerializeField] LayerMask fovLayer;
    [SerializeField] LayerMask portalLayer;
    [SerializeField] LayerMask triggersLayer;
    [SerializeField] LayerMask ledgeLayer;
    [SerializeField] LayerMask waterLayer;

    public static GameLayers i { get; set; }
    private void Awake()
    {
        i = this;
    }

    public LayerMask SolidLayer {
        get => solidObjectsLayer;
    }

    public LayerMask InteractableLayer {
        get => interactableLayer;
    }

    public LayerMask GrassLayer {
        get => grassLayer;
    }

    public LayerMask PlayerLayer {
        get => playerLayer;
    }

    public LayerMask FovLayer {
        get => fovLayer;
    }

    public LayerMask PortalLayer {
        get => portalLayer;
    }

    public LayerMask LedgeLayer => ledgeLayer;

    public LayerMask WaterLayer => waterLayer;

    public LayerMask TriggerableLayers {
        get => grassLayer | fovLayer | portalLayer | triggersLayer | waterLayer;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EvolutionManager : MonoBehaviour
{
    [SerializeField] GameObject evolutionUI;
    [SerializeField] Image CambroxImage;

    public event Action OnStartEvolution;
    public event Action OnCompleteEvolution;

    public static EvolutionManager i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    public IEnumerator Evolve(Cambrox Cambrox, Evolution evolution)
    {
        OnStartEvolution?.Invoke();
        evolutionUI.SetActive(true);

        CambroxImage.sprite = Cambrox.Base.FrontSprite;
        yield return DialogManager.Instance.ShowDialogText($"{Cambrox.Base.Name} is evolving");

        var oldCambrox = Cambrox.Base;
        Cambrox.Evolve(evolution);

        CambroxImage.sprite = Cambrox.Base.FrontSprite;
        yield return DialogManager.Instance.ShowDialogText($"{oldCambrox.Name} evolved into {Cambrox.Base.Name}");

        evolutionUI.SetActive(false);
        OnCompleteEvolution?.Invoke();
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CuttableTree : MonoBehaviour, Interactable
{
    public IEnumerator Interact(Transform initiator)
    {
        yield return DialogManager.Instance.ShowDialogText("This tree looks like it can be cut");

        var CambroxWithCut = initiator.GetComponent<CambroxParty>().Cambroxs.FirstOrDefault(p => p.Moves.Any(m => m.Base.Name == "Cut"));

        if (CambroxWithCut != null)
        {
            int selectedChoice = 0;
            yield return DialogManager.Instance.ShowDialogText($"Should {CambroxWithCut.Base.Name} use cut?",
                choices: new List<string>() { "Yes", "No" },
                onChoiceSelected: (selection) => selectedChoice = selection);

            if (selectedChoice == 0)
            {
                // Yes
                yield return DialogManager.Instance.ShowDialogText($"{CambroxWithCut.Base.Name} used cut!");
                gameObject.SetActive(false);
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MapArea))]
public class MapAreaEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        int totalChanceGrass = serializedObject.FindProperty("totalChance").intValue;
        int totalChanceWater = serializedObject.FindProperty("totalChanceWater").intValue;

        if (totalChanceGrass != 100 && totalChanceGrass != -1)
            EditorGUILayout.HelpBox("The total chance percentage of Cambrox in grass is not 100", MessageType.Error);

        if (totalChanceWater != 100 && totalChanceWater != -1)
            EditorGUILayout.HelpBox("The total chance percentage of Cambrox in water is not 100", MessageType.Error);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Create new TM or HM")]
public class TmItem : ItemBase
{
    [SerializeField] MoveBase move;
    [SerializeField] bool isHM;

    public override string Name => base.Name + $": {move.Name}";

    public override bool Use(Cambrox Cambrox)
    {
        // Learning move is handled from Inventory UI, If it was learned then return true
        return Cambrox.HasMove(move);
    }

    public bool CanBeTaught(Cambrox Cambrox)
    {
        return Cambrox.Base.LearnableByItems.Contains(move);
    }

    public override bool IsReusable => isHM;

    public override bool CanUseInBattle => false;

    public MoveBase Move => move;
    public bool IsHM => isHM;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Create new recovery item")]
public class RecoveryItem : ItemBase
{
    [Header("HP")]
    [SerializeField] int hpAmount;
    [SerializeField] bool restoreMaxHP;

    [Header("PP")]
    [SerializeField] int ppAmount;
    [SerializeField] bool restoreMaxPP;

    [Header("Status Conditions")]
    [SerializeField] ConditionID status;
    [SerializeField] bool recoverAllStatus;

    [Header("Revive")]
    [SerializeField] bool revive;
    [SerializeField] bool maxRevive;

    public override bool Use(Cambrox Cambrox)
    {
        // Revive
        if (revive || maxRevive)
        {
            if (Cambrox.HP > 0)
                return false;

            if (revive)
                Cambrox.IncreaseHP(Cambrox.MaxHp / 2);
            else if (maxRevive)
                Cambrox.IncreaseHP(Cambrox.MaxHp);

            Cambrox.CureStatus();

            return true;
        }

        // No other items can be used on fainted Cambrox
        if (Cambrox.HP == 0)
            return false;

        // Restore HP
        if (restoreMaxHP || hpAmount > 0)
        {
            if (Cambrox.HP == Cambrox.MaxHp)
                return false;

            if (restoreMaxHP)
                Cambrox.IncreaseHP(Cambrox.MaxHp);
            else
                Cambrox.IncreaseHP(hpAmount);
        }

        // Recover Status
        if (recoverAllStatus || status != ConditionID.none)
        {
            if (Cambrox.Status == null && Cambrox.VolatileStatus == null)
                return false;

            if (recoverAllStatus)
            {
                Cambrox.CureStatus();
                Cambrox.CureVolatileStatus();
            }
            else
            {
                if (Cambrox.Status.Id == status)
                    Cambrox.CureStatus();
                else if (Cambrox.VolatileStatus.Id == status)
                    Cambrox.CureVolatileStatus();
                else
                    return false;
            }
        }

        // Restore PP
        if (restoreMaxPP)
        {
            Cambrox.Moves.ForEach(m => m.IncreasePP(m.Base.PP));
        }
        else if (ppAmount > 0)
        {
            Cambrox.Moves.ForEach(m => m.IncreasePP(ppAmount));
        }

        return true;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Create new cambroxball")]
public class cambroxballItem : ItemBase
{
    [SerializeField] float catchRateModfier = 1;

    public override bool Use(Cambrox Cambrox)
    {
        return true;
    }

    public override bool CanUseOutsideBattle => false;

    public float CatchRateModifier => catchRateModfier;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pickup : MonoBehaviour, Interactable, ISavable
{
    [SerializeField] ItemBase item;

    public bool Used { get; set; } = false;

    public IEnumerator Interact(Transform initiator)
    {
        if (!Used)
        {
            initiator.GetComponent<Inventory>().AddItem(item);

            Used = true;

            AudioManager.i.PlaySfx(AudioId.ItemObtained, pauseMusic: true);

            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<BoxCollider2D>().enabled = false;

            string playerName = initiator.GetComponent<PlayerController>().Name;

            yield return DialogManager.Instance.ShowDialogText($"{playerName} found {item.Name}");
        }
    }

    public object CaptureState()
    {
        return Used;
    }

    public void RestoreState(object state)
    {
        Used = (bool)state;

        if (Used)
        {
            GetComponent<SpriteRenderer>().enabled = false;
            GetComponent<BoxCollider2D>().enabled = false;
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemGiver : MonoBehaviour, ISavable
{
    [SerializeField] ItemBase item;
    [SerializeField] int count = 1;
    [SerializeField] Dialog dialog;

    bool used = false;

    public IEnumerator GiveItem(PlayerController player)
    {
        yield return DialogManager.Instance.ShowDialog(dialog);

        player.GetComponent<Inventory>().AddItem(item, count);

        used = true;

        AudioManager.i.PlaySfx(AudioId.ItemObtained, pauseMusic: true);

        string dialogText = $"{player.Name} received {item.Name}";
        if (count > 1)
            dialogText = $"{player.Name} received {count} {item.Name}s";

        yield return DialogManager.Instance.ShowDialogText(dialogText);
    }

    public bool CanBeGiven()
    {
        return item != null && count > 0 && !used;
    }

    public object CaptureState()
    {
        return used;
    }

    public void RestoreState(object state)
    {
        used = (bool)state;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemBase : ScriptableObject
{
    [SerializeField] string name;
    [SerializeField] string description;
    [SerializeField] Sprite icon;
    [SerializeField] float price;
    [SerializeField] bool isSellable;

    public virtual string Name => name;
    public string Description => description;
    public Sprite Icon => icon;

    public float Price => price;
    public bool IsSellable => isSellable;

    public virtual bool Use(Cambrox Cambrox)
    {
        return false;
    }

    public virtual bool IsReusable => false;

    public virtual bool CanUseInBattle => true;
    public virtual bool CanUseOutsideBattle => true;
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ItemCategory { Items, cambroxballs, Tms }

public class Inventory : MonoBehaviour, ISavable
{
    [SerializeField] List<ItemSlot> slots;
    [SerializeField] List<ItemSlot> cambroxballSlots;
    [SerializeField] List<ItemSlot> tmSlots;

    List<List<ItemSlot>> allSlots;

    public event Action OnUpdated;

    private void Awake()
    {
        allSlots = new List<List<ItemSlot>>() { slots, cambroxballSlots, tmSlots };
    }

    public static List<string> ItemCategories { get; set; } = new List<string>()
    {
        "ITEMS", "cambroxBALLS", "TMs & HMs"
    };

    public List<ItemSlot> GetSlotsByCategory(int categoryIndex)
    {
        return allSlots[categoryIndex];
    }

    public ItemBase GetItem(int itemIndex, int categoryIndex)
    {
        var currenSlots = GetSlotsByCategory(categoryIndex);
        return currenSlots[itemIndex].Item;
    }

    public ItemBase UseItem(int itemIndex, Cambrox selectedCambrox, int selectedCategory)
    {
        var item = GetItem(itemIndex, selectedCategory);
        return UseItem(item, selectedCambrox);
    }

    public ItemBase UseItem(ItemBase item, Cambrox selectedCambrox)
    {
        bool itemUsed = item.Use(selectedCambrox);
        if (itemUsed)
        {
            if (!item.IsReusable)
                RemoveItem(item);

            return item;
        }

        return null;
    }

    public void AddItem(ItemBase item, int count = 1)
    {
        int category = (int)GetCategoryFromItem(item);
        var currentSlots = GetSlotsByCategory(category);

        var itemSlot = currentSlots.FirstOrDefault(slot => slot.Item == item);
        if (itemSlot != null)
        {
            itemSlot.Count += count;
        }
        else
        {
            currentSlots.Add(new ItemSlot()
            {
                Item = item,
                Count = count
            });
        }

        OnUpdated?.Invoke();
    }

    public int GetItemCount(ItemBase item)
    {
        int category = (int)GetCategoryFromItem(item);
        var currentSlots = GetSlotsByCategory(category);

        var itemSlot = currentSlots.FirstOrDefault(slot => slot.Item == item);

        if (itemSlot != null)
            return itemSlot.Count;
        else
            return 0;
    }

    public void RemoveItem(ItemBase item, int countToRemove = 1)
    {
        int category = (int)GetCategoryFromItem(item);
        var currentSlots = GetSlotsByCategory(category);

        var itemSlot = currentSlots.First(slot => slot.Item == item);
        itemSlot.Count -= countToRemove;
        if (itemSlot.Count == 0)
            currentSlots.Remove(itemSlot);

        OnUpdated?.Invoke();
    }

    public bool HasItem(ItemBase item)
    {
        int category = (int)GetCategoryFromItem(item);
        var currentSlots = GetSlotsByCategory(category);

        return currentSlots.Exists(slot => slot.Item == item);
    }

    ItemCategory GetCategoryFromItem(ItemBase item)
    {
        if (item is RecoveryItem || item is EvolutionItem)
            return ItemCategory.Items;
        else if (item is cambroxballItem)
            return ItemCategory.cambroxballs;
        else
            return ItemCategory.Tms;
    }

    public static Inventory GetInventory()
    {
        return FindObjectOfType<PlayerController>().GetComponent<Inventory>();
    }

    public object CaptureState()
    {
        var saveData = new InventorySaveData()
        {
            items = slots.Select(i => i.GetSaveData()).ToList(),
            cambroxballs = cambroxballSlots.Select(i => i.GetSaveData()).ToList(),
            tms = tmSlots.Select(i => i.GetSaveData()).ToList(),
        };

        return saveData;
    }

    public void RestoreState(object state)
    {
        var saveData = state as InventorySaveData;

        slots = saveData.items.Select(i => new ItemSlot(i)).ToList();
        cambroxballSlots = saveData.cambroxballs.Select(i => new ItemSlot(i)).ToList();
        tmSlots = saveData.tms.Select(i => new ItemSlot(i)).ToList();

        allSlots = new List<List<ItemSlot>>() { slots, cambroxballSlots, tmSlots };

        OnUpdated?.Invoke();
    }
}

[Serializable]
public class ItemSlot
{
    [SerializeField] ItemBase item;
    [SerializeField] int count;

    public ItemSlot()
    {

    }

    public ItemSlot(ItemSaveData saveData)
    {
        item = ItemDB.GetObjectByName(saveData.name);
        count = saveData.count;
    }

    public ItemSaveData GetSaveData()
    {
        var saveData = new ItemSaveData()
        {
            name = item.name,
            count = count
        };

        return saveData;
    }

    public ItemBase Item {
        get => item;
        set => item = value;
    }
    public int Count {
        get => count;
        set => count = value;
    }
}

[Serializable]
public class ItemSaveData
{
    public string name;
    public int count;
}

[Serializable]
public class InventorySaveData
{
    public List<ItemSaveData> items;
    public List<ItemSaveData> cambroxballs;
    public List<ItemSaveData> tms;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Create new evolution item")]
public class EvolutionItem : ItemBase
{
    public override bool Use(Cambrox Cambrox)
    {
        return true;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemSlotUI : MonoBehaviour
{
    [SerializeField] Text nameText;
    [SerializeField] Text countText;

    RectTransform rectTransform;
    private void Awake()
    {
        
    }

    public Text NameText => nameText;
    public Text CountText => countText;

    public float Height => rectTransform.rect.height;

    public void SetData(ItemSlot itemSlot)
    {
        rectTransform = GetComponent<RectTransform>();
        nameText.text = itemSlot.Item.Name;
        countText.text = $"X {itemSlot.Count}";
    }

    public void SetNameAndPrice(ItemBase item)
    {
        rectTransform = GetComponent<RectTransform>();
        nameText.text = item.Name;
        countText.text = $"$ {item.Price}";
    }
}
using GDE.GenericSelectionUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : SelectionUI<TextSlot>
{
    [SerializeField] GameObject itemList;
    [SerializeField] ItemSlotUI itemSlotUI;

    [SerializeField] Text categoryText;
    [SerializeField] Image itemIcon;
    [SerializeField] Text itemDescription;

    [SerializeField] Image upArrow;
    [SerializeField] Image downArrow;

    int selectedCategory = 0;

    const int itemsInViewport = 8;

    List<ItemSlotUI> slotUIList;
    Inventory inventory;
    RectTransform itemListRect;
    private void Awake()
    {
        inventory = Inventory.GetInventory();
        itemListRect = itemList.GetComponent<RectTransform>();
    }

    private void Start()
    {
        UpdateItemList();

        inventory.OnUpdated += UpdateItemList;
    }

    void UpdateItemList()
    {
        // Clear all the existing items
        foreach (Transform child in itemList.transform)
            Destroy(child.gameObject);

        slotUIList = new List<ItemSlotUI>();
        foreach (var itemSlot in inventory.GetSlotsByCategory(selectedCategory))
        {
            var slotUIObj = Instantiate(itemSlotUI, itemList.transform);
            slotUIObj.SetData(itemSlot);

            slotUIList.Add(slotUIObj);
        }

        SetItems(slotUIList.Select(s => s.GetComponent<TextSlot>()).ToList());

        UpdateSelectionInUI();
    }

    public override void HandleUpdate()
    {
        int prevCategory = selectedCategory;

        if (Input.GetKeyDown(KeyCode.RightArrow))
            ++selectedCategory;
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            --selectedCategory;

        if (selectedCategory > Inventory.ItemCategories.Count - 1)
            selectedCategory = 0;
        else if (selectedCategory < 0)
            selectedCategory = Inventory.ItemCategories.Count - 1;

        if (prevCategory != selectedCategory)
        {
            ResetSelection();
            categoryText.text = Inventory.ItemCategories[selectedCategory];
            UpdateItemList();
        }

        base.HandleUpdate();
    }

    public override void UpdateSelectionInUI()
    {
        base.UpdateSelectionInUI();

        var slots = inventory.GetSlotsByCategory(selectedCategory);
        if (slots.Count > 0)
        {
            var item = slots[selectedItem].Item;
            itemIcon.sprite = item.Icon;
            itemDescription.text = item.Description;
        }

        HandleScrolling();
    }

    void HandleScrolling()
    {
        if (slotUIList.Count <= itemsInViewport) return;

        float scrollPos = Mathf.Clamp(selectedItem - itemsInViewport / 2, 0, selectedItem) * slotUIList[0].Height;
        itemListRect.localPosition = new Vector2(itemListRect.localPosition.x, scrollPos);

        bool showUpArrow = selectedItem > itemsInViewport / 2;
        upArrow.gameObject.SetActive(showUpArrow);

        bool showDownArrow = selectedItem + itemsInViewport / 2 < slotUIList.Count;
        downArrow.gameObject.SetActive(showDownArrow);
    }

    void ResetSelection()
    {
        selectedItem = 0;

        upArrow.gameObject.SetActive(false);
        downArrow.gameObject.SetActive(false);

        itemIcon.sprite = null;
        itemDescription.text = "";
    }

    public ItemBase SelectedItem => inventory.GetItem(selectedItem, selectedCategory);

    public int SelectedCategory => selectedCategory;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CambroxStorageBoxes : MonoBehaviour
{
    Cambrox[,] boxes = new Cambrox[16, 30];

    public void AddCambrox(Cambrox Cambrox, int boxIndex, int slotIndex)
    {
        boxes[boxIndex, slotIndex] = Cambrox;
    }

    public void RemoveCambrox(int boxIndex, int slotIndex)
    {
        boxes[boxIndex, slotIndex] = null;
    }

    public Cambrox GetCambrox(int boxIndex, int slotIndex)
    {
        return boxes[boxIndex, slotIndex];
    }

    public static CambroxStorageBoxes GetPlayerStorageBoxes()
    {
        return FindObjectOfType<PlayerController>().GetComponent<CambroxStorageBoxes>();
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CambroxParty : MonoBehaviour
{
    [SerializeField] List<Cambrox> Cambroxs;

    public event Action OnUpdated;

    public List<Cambrox> Cambroxs {
        get {
            return Cambroxs;
        }
        set {
            Cambroxs = value;
            OnUpdated?.Invoke();
        }
    }

    private void Awake()
    {
        foreach (var Cambrox in Cambroxs)
        {
            Cambrox.Init();
        }
    }

    private void Start()
    {

    }

    public Cambrox GetHealthyCambrox()
    {
        return Cambroxs.Where(x => x.HP > 0).FirstOrDefault();
    }

    public void AddCambrox(Cambrox newCambrox)
    {
        if (Cambroxs.Count < 6)
        {
            Cambroxs.Add(newCambrox);
            OnUpdated?.Invoke();
        }
        else
        {
            // TODO: Add to the PC once that's implemented
        }
    }

    public bool CheckForEvolutions()
    {
        return Cambroxs.Any(p => p.CheckForEvolution() != null);
    }

    public IEnumerator RunEvolutions()
    {
        foreach (var Cambrox in Cambroxs)
        {
            var evoution = Cambrox.CheckForEvolution();
            if (evoution != null)
            {
                yield return EvolutionManager.i.Evolve(Cambrox, evoution);
            }
        }
    }

    public void PartyUpdated()
    {
        OnUpdated?.Invoke();
    }

    public static CambroxParty GetPlayerParty()
    {
        return FindObjectOfType<PlayerController>().GetComponent<CambroxParty>();
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CambroxGiver : MonoBehaviour, ISavable
{
    [SerializeField] Cambrox CambroxToGive;
    [SerializeField] Dialog dialog;

    bool used = false;

    public IEnumerator GiveCambrox(PlayerController player)
    {
        yield return DialogManager.Instance.ShowDialog(dialog);

        CambroxToGive.Init();
        player.GetComponent<CambroxParty>().AddCambrox(CambroxToGive);

        used = true;

        AudioManager.i.PlaySfx(AudioId.CambroxObtained, pauseMusic: true);

        string dialogText = $"{player.Name} received {CambroxToGive.Base.Name}";

        yield return DialogManager.Instance.ShowDialogText(dialogText);
    }

    public bool CanBeGiven()
    {
        return CambroxToGive != null && !used;
    }

    public object CaptureState()
    {
        return used;
    }

    public void RestoreState(object state)
    {
        used = (bool)state;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Cambrox", menuName = "Cambrox/Create new Cambrox")]
public class CambroxBase : ScriptableObject
{
    [SerializeField] string name;
    
    [TextArea]
    [SerializeField] string description;

    [SerializeField] Sprite frontSprite;
    [SerializeField] Sprite backSprite;

    [SerializeField] CambroxType type1;
    [SerializeField] CambroxType type2;

    // Base Stats
    [SerializeField] int maxHp;
    [SerializeField] int attack;
    [SerializeField] int defense;
    [SerializeField] int spAttack;
    [SerializeField] int spDefense;
    [SerializeField] int speed;

    [SerializeField] int catchRate = 255;

    [SerializeField] int expYield;
    [SerializeField] GrowthRate growthRate;

    [SerializeField] List<LearnableMove> learnableMoves;
    [SerializeField] List<MoveBase> learnableByItems;

    [SerializeField] List<Evolution> evolutions;

    public static int MaxNumOfMoves { get; set; } = 4;

    public int GetExpForLevel(int level)
    {
        if (growthRate == GrowthRate.Fast)
        {
            return 4 * (level * level * level) / 5;
        }
        else if (growthRate == GrowthRate.MediumFast)
        {
            return level * level * level;
        }

        return -1;
    }

    public string Name {
        get { return name; }
    }

    public string Description {
        get { return description; }
    }

    public Sprite FrontSprite {
        get { return frontSprite; }
    }

    public Sprite BackSprite {
        get { return backSprite; }
    }

    public CambroxType Type1 {
        get { return type1; }
    }

    public CambroxType Type2 {
        get { return type2; }
    }

    public int MaxHp {
        get { return maxHp; }
    }

    public int Attack {
        get { return attack; }
    }

    public int SpAttack {
        get { return spAttack; }
    }

    public int Defense {
        get { return defense; }
    }

    public int SpDefense {
        get { return spDefense; }
    }

    public int Speed {
        get { return speed; }
    }

    public List<LearnableMove> LearnableMoves {
        get { return learnableMoves; }
    }

    public List<MoveBase> LearnableByItems => learnableByItems;
    
    public List<Evolution> Evolutions => evolutions;

    public int CatchRate => catchRate;

    public int ExpYield => expYield;
    public GrowthRate GrowthRate => growthRate;
}

[System.Serializable]
public class LearnableMove
{
    [SerializeField] MoveBase moveBase;
    [SerializeField] int level;

    public MoveBase Base {
        get { return moveBase; }
    }

    public int Level {
        get { return level; }
    }
}

[System.Serializable]
public class Evolution
{
    [SerializeField] CambroxBase evolvesInto;
    [SerializeField] int requiredLevel;
    [SerializeField] EvolutionItem requiredItem;

    public CambroxBase EvolvesInto => evolvesInto;
    public int RequiredLevel => requiredLevel;
    public EvolutionItem RequiredItem => requiredItem;
}

public enum CambroxType
{
    None,
    Normal,
    Fire,
    Water,
    Electric,
    Grass,
    Ice,
    Fighting,
    Poison,
    Ground,
    Flying,
    Psychic,
    Bug,
    Rock,
    Ghost,
    Dragon
}

public enum GrowthRate
{
    Fast, MediumFast
}

public enum Stat
{
    Attack,
    Defense,
    SpAttack,
    SpDefense,
    Speed,

    // These 2 are not actual stats, they're used to boost the moveAccuracy
    Accuracy,
    Evasion
}

public class TypeChart
{
    static float[][] chart =
    {
         //                       Nor   Fir   Wat   Ele   Gra   Ice   Fig   Poi   Gro   Fly   Psy   Bug   Roc   Gho   Dra   Dar  Ste    Fai
        /*Normal*/  new float[] {1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   0.5f, 0,    1f,   1f,   0.5f, 1f},
        /*Fire*/    new float[] {1f,   0.5f, 0.5f, 1f,   2f,   2f,   1f,   1f,   1f,   1f,   1f,   2f,   0.5f, 1f,   0.5f, 1f,   2f,   1f},
        /*Water*/   new float[] {1f,   2f,   0.5f, 1f,   0.5f, 1f,   1f,   1f,   2f,   1f,   1f,   1f,   2f,   1f,   0.5f, 1f,   1f,   1f},
        /*Electric*/new float[] {1f,   1f,   2f,   0.5f, 0.5f, 1f,   1f,   1f,   0f,   2f,   1f,   1f,   1f,   1f,   0.5f, 1f,   1f,   1f},
        /*Grass*/   new float[] {1f,   0.5f, 2f,   1f,   0.5f, 1f,   1f,   0.5f, 2f,   0.5f, 1f,   0.5f, 2f,   1f,   0.5f, 1f,   0.5f, 1f},
        /*Ice*/     new float[] {1f,   0.5f, 0.5f, 1f,   2f,   0.5f, 1f,   1f,   2f,   2f,   1f,   1f,   1f,   1f,   2f,   1f,   0.5f, 1f},
        /*Fighting*/new float[] {2f,   1f,   1f,   1f,   1f,   2f,   1f,   0.5f, 1f,   0.5f, 0.5f, 0.5f, 2f,   0f,   1f,   2f,   2f,   0.5f},
        /*Poison*/  new float[] {1f,   1f,   1f,   1f,   2f,   1f,   1f,   0.5f, 0.5f, 1f,   1f,   1f,   0.5f, 0.5f, 1f,   1f,   0f,   2f},
        /*Ground*/  new float[] {1f,   2f,   1f,   2f,   0.5f, 1f,   1f,   2f,   1f,   0f,   1f,   0.5f, 2f,   1f,   1f,   1f,   2f,   1f},
        /*Flying*/  new float[] {1f,   1f,   1f,   0.5f, 2f,   1f,   2f,   1f,   1f,   1f,   1f,   2f,   0.5f, 1f,   1f,   1f,   0.5f, 1f},
        /*Psychic*/ new float[] {1f,   1f,   1f,   1f,   1f,   1f,   2f,   2f,   1f,   1f,   0.5f, 1f,   1f,   1f,   1f,   0f,   0.5f, 1f},
        /*Bug*/     new float[] {1f,   0.5f, 1f,   1f,   2f,   1f,   0.5f, 0.5f, 1f,   0.5f, 2f,   1f,   1f,   0.5f, 1f,   2f,   0.5f, 0.5f},
        /*Rock*/    new float[] {1f,   2f,   1f,   1f,   1f,   2f,   0.5f, 1f,   0.5f, 2f,   1f,   2f,   1f,   1f,   1f,   1f,   0.5f, 1f},
        /*Ghost*/   new float[] {0f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   0.5f, 1f,   1f,   2f,   1f,   0.5f, 1f,   1f},
        /*Dragon*/  new float[] {1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   1f,   2f,   1f,   0.5f, 0f},
        /*Dark*/    new float[] {1f,   1f,   1f,   1f,   1f,   1f,   0.5f, 1f,   1f,   1f,   2f,   1f,   1f,   2f,   1f,   0.5f, 1f,   0.5f},
        /*Steel*/   new float[] {1f,   0.5f, 0.5f, 0.5f, 1f,   2f,   1f,   1f,   1f,   1f,   1f,   2f,   0.5f, 1f,   1f,   1f,   0.5f, 2f},
        /*Fairy*/   new float[] {1f,   0.5f, 1f,   1f,   1f,   1f,   2f,   0.5f, 1f,   1f,   1f,   1f,   1f,   1f,   2f,   2f,   0.5f, 1f}
    };

    public static float GetEffectiveness(CambroxType attackType, CambroxType defendType)
    {
        if (attackType == CambroxType.None || defendType == CambroxType.None)
            return 1f;

        int row = (int)attackType - 1;
        int col = (int)defendType - 1;

        return chart[row][col];
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class Cambrox
{
    [SerializeField] CambroxBase _base;
    [SerializeField] int level;

    public Cambrox(CambroxBase pBase, int pLevel)
    {
        _base = pBase;
        level = pLevel;

        Init();
    }

    public CambroxBase Base { 
        get {
            return _base;
        }
    }
    public int Level { 
        get {
            return level;
        }
    }

    public int Exp { get; set; }
    public int HP { get; set; }
    public List<Move> Moves { get; set; }
    public Move CurrentMove { get; set; }
    public Dictionary<Stat, int> Stats { get; private set; }
    public Dictionary<Stat, int> StatBoosts { get; private set; }
    public Condition Status { get; private set; }
    public int StatusTime { get; set; }
    public Condition VolatileStatus { get; private set; }
    public int VolatileStatusTime { get; set; }


    public Queue<string> StatusChanges { get; private set; }

    public event System.Action OnStatusChanged;
    public event System.Action OnHPChanged;

    public void Init()
    {
        // Generate Moves
        Moves = new List<Move>();
        foreach (var move in Base.LearnableMoves)
        {
            if (move.Level <= Level)
                Moves.Add(new Move(move.Base));

            if (Moves.Count >= CambroxBase.MaxNumOfMoves)
                break;
        }

        Exp = Base.GetExpForLevel(Level);

        CalculateStats();
        HP = MaxHp;

        StatusChanges = new Queue<string>();
        ResetStatBoost();
        Status = null;
        VolatileStatus = null;
    }

    public Cambrox(CambroxSaveData saveData)
    {
        _base = CambroxDB.GetObjectByName(saveData.name);
        HP = saveData.hp;
        level = saveData.level;
        Exp = saveData.exp;

        if (saveData.statusId != null)
            Status = ConditionsDB.Conditions[saveData.statusId.Value];
        else
            Status = null;

        Moves = saveData.moves.Select(s => new Move(s)).ToList();

        CalculateStats();
        StatusChanges = new Queue<string>();
        ResetStatBoost();
        VolatileStatus = null;
    }

    public CambroxSaveData GetSaveData()
    {
        var saveData = new CambroxSaveData()
        {
            name = Base.name,
            hp = HP,
            level = Level,
            exp = Exp,
            statusId = Status?.Id,
            moves = Moves.Select(m => m.GetSaveData()).ToList()
        };

        return saveData;
    }

    void CalculateStats()
    {
        Stats = new Dictionary<Stat, int>();
        Stats.Add(Stat.Attack, Mathf.FloorToInt((Base.Attack * Level) / 100f) + 5);
        Stats.Add(Stat.Defense, Mathf.FloorToInt((Base.Defense * Level) / 100f) + 5);
        Stats.Add(Stat.SpAttack, Mathf.FloorToInt((Base.SpAttack * Level) / 100f) + 5);
        Stats.Add(Stat.SpDefense, Mathf.FloorToInt((Base.SpDefense * Level) / 100f) + 5);
        Stats.Add(Stat.Speed, Mathf.FloorToInt((Base.Speed * Level) / 100f) + 5);

        int oldMaxHP = MaxHp;
        MaxHp = Mathf.FloorToInt((Base.Speed * Level) / 100f) + 10 + Level;

        if (oldMaxHP != 0)
            HP += MaxHp - oldMaxHP;
    }

    void ResetStatBoost()
    {
        StatBoosts = new Dictionary<Stat, int>()
        {
            {Stat.Attack, 0},
            {Stat.Defense, 0},
            {Stat.SpAttack, 0},
            {Stat.SpDefense, 0},
            {Stat.Speed, 0},
            {Stat.Accuracy, 0},
            {Stat.Evasion, 0},
        };
    }

    int GetStat(Stat stat)
    {
        int statVal = Stats[stat];

        // Apply stat boost
        int boost = StatBoosts[stat];
        var boostValues = new float[] { 1f, 1.5f, 2f, 2.5f, 3f, 3.5f, 4f };

        if (boost >= 0)
            statVal = Mathf.FloorToInt(statVal * boostValues[boost]);
        else
            statVal = Mathf.FloorToInt(statVal / boostValues[-boost]);

        return statVal;
    }

    public void ApplyBoosts(List<StatBoost> statBoosts)
    {
        foreach (var statBoost in statBoosts)
        {
            var stat = statBoost.stat;
            var boost = statBoost.boost;

            StatBoosts[stat] = Mathf.Clamp(StatBoosts[stat] + boost, -6, 6);

            if (boost > 0)
                StatusChanges.Enqueue($"{Base.Name}'s {stat} rose!");
            else
                StatusChanges.Enqueue($"{Base.Name}'s {stat} fell!");

            Debug.Log($"{stat} has been boosted to {StatBoosts[stat]}");
        }
    }

    public bool CheckForLevelUp()
    {
        if (Exp > Base.GetExpForLevel(level + 1))
        {
            ++level;
            CalculateStats();
            return true;
        }

        return false;
    }

    public LearnableMove GetLearnableMoveAtCurrLevel()
    {
        return Base.LearnableMoves.Where(x => x.Level == level).FirstOrDefault();
    }

    public void LearnMove(MoveBase moveToLearn)
    {
        if (Moves.Count > CambroxBase.MaxNumOfMoves)
            return;

        Moves.Add(new Move(moveToLearn));
    }

    public bool HasMove(MoveBase moveToCheck)
    {
        return Moves.Count(m => m.Base == moveToCheck) > 0;
    }

    public Evolution CheckForEvolution()
    {
        return Base.Evolutions.FirstOrDefault(e => e.RequiredLevel <= level);
    }

    public Evolution CheckForEvolution(ItemBase item)
    {
        return Base.Evolutions.FirstOrDefault(e => e.RequiredItem == item);
    }

    public void Evolve(Evolution evolution)
    {
        _base = evolution.EvolvesInto;
        CalculateStats();
    }

    public void Heal()
    {
        HP = MaxHp;
        OnHPChanged?.Invoke();
    }

    public float GetNormalizedExp()
    {
        int currLevelExp = Base.GetExpForLevel(Level);
        int nextLevelExp = Base.GetExpForLevel(Level + 1);

        float normalizedExp = (float)(Exp - currLevelExp) / (nextLevelExp - currLevelExp);
        return Mathf.Clamp01(normalizedExp);
    }

    public int Attack {
        get { return GetStat(Stat.Attack); }
    }

    public int Defense {
        get { return GetStat(Stat.Defense); }
    }

    public int SpAttack {
        get { return GetStat(Stat.SpAttack); }
    }

    public int SpDefense {
        get { return GetStat(Stat.SpDefense); }
    }

    public int Speed {
        get {
            return GetStat(Stat.Speed);
        }
    }

    public int MaxHp { get; private set; }

    public DamageDetails TakeDamage(Move move, Cambrox attacker)
    {
        float critical = 1f;
        if (Random.value * 100f <= 6.25f)
            critical = 2f;

        float type = TypeChart.GetEffectiveness(move.Base.Type, this.Base.Type1) * TypeChart.GetEffectiveness(move.Base.Type, this.Base.Type2);

        var damageDetails = new DamageDetails()
        {
            TypeEffectiveness = type,
            Critical = critical,
            Fainted = false
        };

        float attack = (move.Base.Category == MoveCategory.Special) ? attacker.SpAttack : attacker.Attack;
        float defense = (move.Base.Category == MoveCategory.Special) ? SpDefense : Defense;

        float modifiers = Random.Range(0.85f, 1f) * type * critical;
        float a = (2 * attacker.Level + 10) / 250f;
        float d = a * move.Base.Power * ((float)attack / defense) + 2;
        int damage = Mathf.FloorToInt(d * modifiers);

        DecreaseHP(damage);

        return damageDetails;
    }

    public void IncreaseHP(int amount)
    {
        HP = Mathf.Clamp(HP + amount, 0, MaxHp);
        OnHPChanged?.Invoke();
    }

    public void DecreaseHP(int damage)
    {
        HP = Mathf.Clamp(HP - damage, 0, MaxHp);
        OnHPChanged?.Invoke();
    }

    public void SetStatus(ConditionID conditionId)
    {
        if (Status != null) return;

        Status = ConditionsDB.Conditions[conditionId];
        Status?.OnStart?.Invoke(this);
        StatusChanges.Enqueue($"{Base.Name} {Status.StartMessage}");
        OnStatusChanged?.Invoke();
    }

    public void CureStatus()
    {
        Status = null;
        OnStatusChanged?.Invoke();
    }

    public void SetVolatileStatus(ConditionID conditionId)
    {
        if (VolatileStatus != null) return;

        VolatileStatus = ConditionsDB.Conditions[conditionId];
        VolatileStatus?.OnStart?.Invoke(this);
        StatusChanges.Enqueue($"{Base.Name} {VolatileStatus.StartMessage}");
    }

    public void CureVolatileStatus()
    {
        VolatileStatus = null;
    }

    public Move GetRandomMove()
    {
        var movesWithPP = Moves.Where(x => x.PP > 0).ToList();

        int r = Random.Range(0, movesWithPP.Count);
        return movesWithPP[r];
    }

    public bool OnBeforeMove()
    {
        bool canPerformMove = true;
        if (Status?.OnBeforeMove != null)
        {
            if (!Status.OnBeforeMove(this))
                canPerformMove = false;
        }

        if (VolatileStatus?.OnBeforeMove != null)
        {
            if (!VolatileStatus.OnBeforeMove(this))
                canPerformMove = false;
        }

        return canPerformMove;
    }

    public void OnAfterTurn()
    {
        Status?.OnAfterTurn?.Invoke(this);
        VolatileStatus?.OnAfterTurn?.Invoke(this);
    }

    public void OnBattleOver()
    {
        VolatileStatus = null;
        ResetStatBoost();
    }
}

public class DamageDetails
{
    public bool Fainted { get; set; }
    public float Critical { get; set; }
    public float TypeEffectiveness { get; set; }
}

[System.Serializable]
public class CambroxSaveData
{
    public string name;
    public int hp;
    public int level;
    public int exp;
    public ConditionID? statusId;
    public List<MoveSaveData> moves;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Move", menuName = "Cambrox/Create new move")]
public class MoveBase : ScriptableObject
{
    [SerializeField] string name;

    [TextArea]
    [SerializeField] string description;

    [SerializeField] CambroxType type;
    [SerializeField] int power;
    [SerializeField] int accuracy;
    [SerializeField] bool alwaysHits;
    [SerializeField] int pp;
    [SerializeField] int priority;

    [SerializeField] MoveCategory category;
    [SerializeField] MoveEffects effects;
    [SerializeField] List<SecondaryEffects> secondaries;
    [SerializeField] MoveTarget target;

    [SerializeField] AudioClip sound;

    public string Name {
        get { return name; }
    }

    public string Description {
        get { return description; }
    }

    public CambroxType Type {
        get { return type; }
    }

    public int Power {
        get { return power; }
    }

    public int Accuracy {
        get { return accuracy; }
    }

    public bool AlwaysHits {
        get { return alwaysHits; }
    }

    public int PP {
        get { return pp; }
    }

    public int Priority {
        get { return priority; }
    }

    public MoveCategory Category {
        get { return category; }
    }

    public MoveEffects Effects {
        get { return effects; }
    }

    public List<SecondaryEffects> Secondaries {
        get { return secondaries; }
    }

    public MoveTarget Target {
        get { return target; }
    }

    public AudioClip Sound => sound;
}

[System.Serializable]
public class MoveEffects
{
    [SerializeField] List<StatBoost> boosts;
    [SerializeField] ConditionID status;
    [SerializeField] ConditionID volatileStatus;

    public List<StatBoost> Boosts {
        get { return boosts; }
    }

    public ConditionID Status {
        get { return status; }
    }

    public ConditionID VolatileStatus {
        get { return volatileStatus; }
    }
}

[System.Serializable]
public class SecondaryEffects : MoveEffects
{
    [SerializeField] int chance;
    [SerializeField] MoveTarget target;

    public int Chance {
        get { return chance;  }
    }

    public MoveTarget Target {
        get { return target; }
    }
}

[System.Serializable]
public class StatBoost
{
    public Stat stat;
    public int boost;
}

public enum MoveCategory
{
    Physical, Special, Status
}

public enum MoveTarget
{
    Foe, Self
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Move
{
    public MoveBase Base { get; set; }
    public int PP { get; set; }

    public Move(MoveBase pBase)
    {
        Base = pBase;
        PP = pBase.PP;
    }

    public Move(MoveSaveData saveData)
    {
        Base = MoveDB.GetObjectByName(saveData.name);
        PP = saveData.pp;
    }

    public MoveSaveData GetSaveData()
    {
        var saveData = new MoveSaveData()
        {
            name = Base.name,
            pp = PP
        };
        return saveData;
    }

    public void IncreasePP(int amount)
    {
        PP = Mathf.Clamp(PP + amount, 0, Base.PP);
    }
}

[Serializable]
public class MoveSaveData
{
    public string name;
    public int pp;
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Condition
{
    public ConditionID Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string StartMessage { get; set; }

    public Action<Cambrox> OnStart { get; set; }
    public Func<Cambrox, bool> OnBeforeMove { get; set; }
    public Action<Cambrox> OnAfterTurn { get; set; }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Quest
{
    public QuestBase Base { get; private set; }
    public QuestStatus Status { get; private set; }

    public Quest(QuestBase _base)
    {
        Base = _base;
    }

    public Quest(QuestSaveData saveData)
    {
        Base = QuestDB.GetObjectByName(saveData.name);
        Status = saveData.status;
    }

    public QuestSaveData GetSaveData()
    {
        var saveData = new QuestSaveData()
        {
            name = Base.name,
            status = Status
        };
        return saveData;
    }

    public IEnumerator StartQuest()
    {
        Status = QuestStatus.Started;

        yield return DialogManager.Instance.ShowDialog(Base.StartDialogue);

        var questList = QuestList.GetQuestList();
        questList.AddQuest(this);
    }

    public IEnumerator CompleteQuest(Transform player)
    {
        Status = QuestStatus.Completed;

        yield return DialogManager.Instance.ShowDialog(Base.CompletedDialogue);

        var inventory = Inventory.GetInventory();
        if (Base.RequiredItem != null)
        {
            inventory.RemoveItem(Base.RequiredItem);
        }

        if (Base.RewardItem != null)
        {
            inventory.AddItem(Base.RewardItem);

            string playerName = player.GetComponent<PlayerController>().Name;
            yield return DialogManager.Instance.ShowDialogText($"{playerName} received {Base.RewardItem.Name}");
        }

        var questList = QuestList.GetQuestList();
        questList.AddQuest(this);
    }

    public bool CanBeCompleted()
    {
        var inventory = Inventory.GetInventory();
        if (Base.RequiredItem != null)
        {
            if (!inventory.HasItem(Base.RequiredItem))
                return false;
        }

        return true;
    }
}

[System.Serializable]
public class QuestSaveData
{
    public string name;
    public QuestStatus status;
}

public enum QuestStatus { None, Started, Completed }
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Quests/Create a new quest")]
public class QuestBase : ScriptableObject
{
    [SerializeField] string name;
    [SerializeField] string description;

    [SerializeField] Dialog startDialogue;
    [SerializeField] Dialog inProgressDialogue;
    [SerializeField] Dialog completedDialogue;

    [SerializeField] ItemBase requiredItem;
    [SerializeField] ItemBase rewardItem;

    public string Name => name;
    public string Description => description;

    public Dialog StartDialogue => startDialogue;
    public Dialog InProgressDialogue => inProgressDialogue?.Lines?.Count > 0 ? inProgressDialogue : startDialogue;
    public Dialog CompletedDialogue => completedDialogue;

    public ItemBase RequiredItem => requiredItem;
    public ItemBase RewardItem => rewardItem;
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class QuestList : MonoBehaviour, ISavable
{
    List<Quest> quests = new List<Quest>();

    public event Action OnUpdated;

    public void AddQuest(Quest quest)
    {
        if (!quests.Contains(quest))
            quests.Add(quest);

        OnUpdated?.Invoke();
    }

    public bool IsStarted(string questName)
    {
        var questStatus = quests.FirstOrDefault(q => q.Base.Name == questName)?.Status;
        return questStatus == QuestStatus.Started || questStatus == QuestStatus.Completed;
    }

    public bool IsCompleted(string questName)
    {
        var questStatus = quests.FirstOrDefault(q => q.Base.Name == questName)?.Status;
        return questStatus == QuestStatus.Completed;
    }

    public static QuestList GetQuestList()
    {
        return FindObjectOfType<PlayerController>().GetComponent<QuestList>();
    }

    public object CaptureState()
    {
        return quests.Select(q => q.GetSaveData()).ToList();
    }

    public void RestoreState(object state)
    {
        var saveData = state as List<QuestSaveData>;
        if (saveData != null)
        {
            quests = saveData.Select(q => new Quest(q)).ToList();
            OnUpdated?.Invoke();
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuestObject : MonoBehaviour
{
    [SerializeField] QuestBase questToCheck;
    [SerializeField] ObjectActions onStart;
    [SerializeField] ObjectActions onComplete;

    QuestList questList;
    private void Start()
    {
        questList = QuestList.GetQuestList();
        questList.OnUpdated += UpdateObjectStatus;

        UpdateObjectStatus();
    }

    private void OnDestroy()
    {
        questList.OnUpdated -= UpdateObjectStatus;
    }

    public void UpdateObjectStatus()
    {
        if (onStart != ObjectActions.DoNothing && questList.IsStarted(questToCheck.Name))
        {
            foreach (Transform child in transform)
            {
                if (onStart == ObjectActions.Enable)
                {
                    child.gameObject.SetActive(true);

                    var savable = child.GetComponent<SavableEntity>();
                    if (savable != null)
                        SavingSystem.i.RestoreEntity(savable);
                }
                else if (onStart == ObjectActions.Disable)
                    child.gameObject.SetActive(false);
            }
        }

        if (onComplete != ObjectActions.DoNothing && questList.IsCompleted(questToCheck.Name))
        {
            foreach (Transform child in transform)
            {
                if (onComplete == ObjectActions.Enable)
                {
                    child.gameObject.SetActive(true);

                    var savable = child.GetComponent<SavableEntity>();
                    if (savable != null)
                        SavingSystem.i.RestoreEntity(savable);
                }
                else if (onComplete == ObjectActions.Disable)
                    child.gameObject.SetActive(false);
            }
        }
    }
}

public enum ObjectActions { DoNothing, Enable, Disable }
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface ISavable
{
    object CaptureState();
    void RestoreState(object state);
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[ExecuteAlways]
public class SavableEntity : MonoBehaviour
{
    [SerializeField] string uniqueId = "";
    static Dictionary<string, SavableEntity> globalLookup = new Dictionary<string, SavableEntity>();

    public string UniqueId => uniqueId;

    // Used to capture state of the gameobject on which the savableEntity is attached
    public object CaptureState()
    {
        Dictionary<string, object> state = new Dictionary<string, object>();
        foreach (ISavable savable in GetComponents<ISavable>())
        {
            state[savable.GetType().ToString()] = savable.CaptureState();
        }
        return state;
    }

    // Used to restore state of the gameobject on which the savableEntity is attached
    public void RestoreState(object state)
    {
        Dictionary<string, object> stateDict = (Dictionary<string, object>)state;
        foreach (ISavable savable in GetComponents<ISavable>())
        {
            string id = savable.GetType().ToString();

            if (stateDict.ContainsKey(id))
                savable.RestoreState(stateDict[id]);
        }
    }


    // Update method used for generating UUID of the SavableEntity
#if UNITY_EDITOR
    // Update method used for generating UUID of the SavableEntity
    private void Update()
    {
        // don't execute in playmode
        if (Application.IsPlaying(gameObject)) return;

        // don't generate Id for prefabs (prefab scene will have path as null)
        if (String.IsNullOrEmpty(gameObject.scene.path)) return;

        SerializedObject serializedObject = new SerializedObject(this);
        SerializedProperty property = serializedObject.FindProperty("uniqueId");

        if (String.IsNullOrEmpty(property.stringValue) || !IsUnique(property.stringValue))
        {
            property.stringValue = Guid.NewGuid().ToString();
            serializedObject.ApplyModifiedProperties();
        }

        globalLookup[property.stringValue] = this;
    }
#endif

    private bool IsUnique(string candidate)
    {
        if (!globalLookup.ContainsKey(candidate)) return true;

        if (globalLookup[candidate] == this) return true;

        // Handle scene unloading cases
        if (globalLookup[candidate] == null)
        {
            globalLookup.Remove(candidate);
            return true;
        }

        // Handle edge cases like designer manually changing the UUID
        if (globalLookup[candidate].UniqueId != candidate)
        {
            globalLookup.Remove(candidate);
            return true;
        }

        return false;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SavingSystem : MonoBehaviour
{
    public static SavingSystem i { get; private set; }
    private void Awake()
    {
        i = this;
    }

    Dictionary<string, object> gameState = new Dictionary<string, object>();

    public void CaptureEntityStates(List<SavableEntity> savableEntities)
    {
        foreach (SavableEntity savable in savableEntities)
        {
            gameState[savable.UniqueId] = savable.CaptureState();
        }
    }

    public void RestoreEntityStates(List<SavableEntity> savableEntities)
    {
        foreach (SavableEntity savable in savableEntities)
        {
            string id = savable.UniqueId;
            if (gameState.ContainsKey(id))
                savable.RestoreState(gameState[id]);
        }
    }

    public void Save(string saveFile)
    {
        CaptureState(gameState);
        SaveFile(saveFile, gameState);
    }

    public void Load(string saveFile)
    {
        gameState = LoadFile(saveFile);
        RestoreState(gameState);
    }

    public void Delete(string saveFile)
    {
        File.Delete(GetPath(saveFile));
    }

    // Used to capture states of all savable objects in the game
    private void CaptureState(Dictionary<string, object> state)
    {
        foreach (SavableEntity savable in FindObjectsOfType<SavableEntity>())
        {
            state[savable.UniqueId] = savable.CaptureState();
        }
    }

    // Used to restore states of all savable objects in the game
    private void RestoreState(Dictionary<string, object> state)
    {
        foreach (SavableEntity savable in FindObjectsOfType<SavableEntity>())
        {
            string id = savable.UniqueId;
            if (state.ContainsKey(id))
                savable.RestoreState(state[id]);
        }
    }

    public void RestoreEntity(SavableEntity entity)
    {
        if (gameState.ContainsKey(entity.UniqueId))
            entity.RestoreState(gameState[entity.UniqueId]);
    }

    void SaveFile(string saveFile, Dictionary<string, object> state)
    {
        string path = GetPath(saveFile);
        print($"saving to {path}");

        using (FileStream fs = File.Open(path, FileMode.Create))
        {
            // Serialize our object
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(fs, state);
        }
    }

    Dictionary<string, object> LoadFile(string saveFile)
    {
        string path = GetPath(saveFile);
        if (!File.Exists(path))
            return new Dictionary<string, object>();

        using (FileStream fs = File.Open(path, FileMode.Open))
        {
            // Deserialize our object
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            return (Dictionary<string, object>)binaryFormatter.Deserialize(fs);
        }
    }

    private string GetPath(string saveFile)
    {
        return Path.Combine(Application.persistentDataPath, saveFile);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// Teleports the player to a different position without switching scenes
public class LocationPortal : MonoBehaviour, IPlayerTriggerable
{
    [SerializeField] string destinationName;
    [SerializeField] Transform spawnPoint;

    PlayerController player;

    public void OnPlayerTriggered(PlayerController player)
    {
        player.Character.Animator.IsMoving = false;
        this.player = player;
        StartCoroutine(Teleport());
    }

    public bool TriggerRepeatedly => false;

    Fader fader;

    private void Start()
    {
        fader = FindObjectOfType<Fader>();
    }

    IEnumerator Teleport()
    {
        GameController.Instance.PauseGame(true);
        yield return fader.FadeIn(0.5f);

        var destPortal = FindObjectsOfType<LocationPortal>().FirstOrDefault(x => x != this && x.destinationName == this.destinationName);
        if (destPortal != null)
        {
            player.Character.SetPositionAndSnapToTile(destPortal.SpawnPoint.position);
        }

        yield return fader.FadeOut(0.5f);
        GameController.Instance.PauseGame(false);
    }

    public Transform SpawnPoint => spawnPoint;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

public class Portal : MonoBehaviour, IPlayerTriggerable
{
    [SerializeField] int sceneToLoad = -1;
    [SerializeField] string destinationName;
    [SerializeField] Transform spawnPoint;

    PlayerController player;

    public void OnPlayerTriggered(PlayerController player)
    {
        player.Character.Animator.IsMoving = false;
        this.player = player;
        StartCoroutine(SwitchScene());
    }

    public bool TriggerRepeatedly => false;

    Fader fader;

    private void Start()
    {
        fader = FindObjectOfType<Fader>();
    }

    IEnumerator SwitchScene()
    {
        DontDestroyOnLoad(gameObject);

        GameController.Instance.PauseGame(true);
        yield return fader.FadeIn(0.5f);

        yield return SceneManager.LoadSceneAsync(sceneToLoad);

        var destPortal = FindObjectsOfType<Portal>().FirstOrDefault(x => x != this && x.destinationName == this.destinationName);
        if (destPortal != null)
        {
            player.Character.SetPositionAndSnapToTile(destPortal.SpawnPoint.position);
        }

        yield return fader.FadeOut(0.5f);
        GameController.Instance.PauseGame(false);

        Destroy(gameObject);
    }

    public Transform SpawnPoint => spawnPoint;
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneDetails : MonoBehaviour
{
    [SerializeField] List<SceneDetails> connectedScenes;
    [SerializeField] AudioClip sceneMusic;

    public bool IsLoaded { get; private set; }

    List<SavableEntity> savableEntities;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            Debug.Log($"Entered {gameObject.name}");

            LoadScene();
            GameController.Instance.SetCurrentScene(this);

            if (sceneMusic != null)
                AudioManager.i.PlayMusic(sceneMusic, fade: true);

            // Load all connected scenes
            foreach (var scene in connectedScenes)
            {
                scene.LoadScene();
            }

            // Unload the scenes that ar no longer connected
            var prevScene = GameController.Instance.PrevScene;
            if (prevScene != null)
            {
                var previoslyLoadedScenes = prevScene.connectedScenes;
                foreach (var scene in previoslyLoadedScenes)
                {
                    if (!connectedScenes.Contains(scene) && scene != this)
                        scene.UnloadScene();
                }

                if (!connectedScenes.Contains(prevScene))
                    prevScene.UnloadScene();
            }
        }
    }

    public void LoadScene()
    {
        if (!IsLoaded)
        {
            var operation = SceneManager.LoadSceneAsync(gameObject.name, LoadSceneMode.Additive);
            IsLoaded = true;

            operation.completed += (AsyncOperation op) =>
            {
                savableEntities = GetSavableEntitiesInScene();
                SavingSystem.i.RestoreEntityStates(savableEntities);
            };
        }
    }

    public void UnloadScene()
    {
        if (IsLoaded)
        {
            SavingSystem.i.CaptureEntityStates(savableEntities);

            SceneManager.UnloadSceneAsync(gameObject.name);
            IsLoaded = false;
        }
    }

    List<SavableEntity> GetSavableEntitiesInScene()
    {
        var currScene = SceneManager.GetSceneByName(gameObject.name);
        var savableEntities = FindObjectsOfType<SavableEntity>().Where(x => x.gameObject.scene == currScene).ToList();
        return savableEntities;
    }

    public AudioClip SceneMusic => sceneMusic;
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoxPartySlotUI : MonoBehaviour
{
    [SerializeField] Text nameTxt;
    [SerializeField] Text lvlTxt;
    [SerializeField] Image image;

    public void SetData(Cambrox Cambrox)
    {
        nameTxt.text = Cambrox.Base.name;
        lvlTxt.text = "" + Cambrox.Level;
        image.sprite = Cambrox.Base.FrontSprite;
        image.color = new Color(255, 255, 255, 100);
    }

    public void ClearData()
    {
        nameTxt.text = "";
        lvlTxt.text = "";
        image.sprite = null;
        image.color = new Color(255, 255, 255, 0);
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BoxStorageSlotUI : MonoBehaviour
{
    [SerializeField] Image image;

    public void SetData(Cambrox Cambrox)
    {
        image.sprite = Cambrox.Base.FrontSprite;
        image.color = new Color(255, 255, 255, 100);
    }

    public void ClearData()
    {
        image.sprite = null;
        image.color = new Color(255, 255, 255, 0);
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CountSelectorUI : MonoBehaviour
{
    [SerializeField] Text countTxt;
    [SerializeField] Text priceTxt;

    bool selected;
    int currentCount;

    int maxCount;
    float pricePerUnit;

    public IEnumerator ShowSelector(int maxCount, float pricePerUnit,
        Action<int> onCountSelected)
    {
        this.maxCount = maxCount;
        this.pricePerUnit = pricePerUnit;

        selected = false;
        currentCount = 1;

        gameObject.SetActive(true);
        SetValues();

        yield return new WaitUntil(() => selected == true);

        onCountSelected?.Invoke(currentCount);
        gameObject.SetActive(false);
    }

    private void Update()
    {
        int prevCount = currentCount;

        if (Input.GetKeyDown(KeyCode.UpArrow))
            ++currentCount;
        else if (Input.GetKeyDown(KeyCode.DownArrow))
            --currentCount;

        currentCount = Mathf.Clamp(currentCount, 1, maxCount);

        if (currentCount != prevCount)
            SetValues();

        if (Input.GetKeyDown(KeyCode.Z))
            selected = true;

    }

    void SetValues()
    {
        countTxt.text = "x " + currentCount;
        priceTxt.text = "$ " + pricePerUnit * currentCount;
    }
}
using GDE.GenericSelectionUI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicMenuUI : SelectionUI<TextSlot>
{
    
}
using GDE.GenericSelectionUI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : SelectionUI<TextSlot>
{
    private void Start()
    {
        SetItems(GetComponentsInChildren<TextSlot>().ToList());
    }
}
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CambroxPartyUI : MonoBehaviour
{
    [SerializeField] CambroxParty CambroxParty; // Reference to the CambroxParty script
    [SerializeField] GameObject CambroxUIPrefab; // UI Prefab for displaying each Pok�mon

    private List<GameObject> CambroxUIElements = new List<GameObject>();

    private void Start()
    {
        if (CambroxParty == null)
        {
            Debug.LogError("CambroxParty reference is missing!");
            return;
        }

        // Subscribe to the OnUpdated event
        CambroxParty.OnUpdated += UpdateUI;

        // Initial UI setup
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Clear existing UI elements
        foreach (var element in CambroxUIElements)
        {
            Destroy(element);
        }
        CambroxUIElements.Clear();

        // Get the current list of Pok�mon from the party
        List<Cambrox> Cambroxs = CambroxParty.Cambroxs;

        // Calculate the screen width in pixels for 15% of the screen
        float leftPanelWidth = Screen.width * 0.15f;

        // Loop through each Pok�mon in the party and create a UI element
        for (int i = 0; i < Cambroxs.Count; i++)
        {
            // Instantiate a UI prefab for the Pok�mon
            GameObject CambroxUI = Instantiate(CambroxUIPrefab, transform);

            // Set the image of the Pok�mon (assuming the prefab has an Image component)
            Image CambroxImage = CambroxUI.GetComponent<Image>();
            if (CambroxImage != null && Cambroxs[i].Base.FrontSprite != null)
            {
                CambroxImage.sprite = Cambroxs[i].Base.FrontSprite; // Access the sprite directly from the Pok�mon class
            }

            // Position the UI element vertically within the left panel
            RectTransform rectTransform = CambroxUI.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0, 1);
            rectTransform.anchorMax = new Vector2(0, 1);
            rectTransform.pivot = new Vector2(0, 1);
            rectTransform.anchoredPosition = new Vector2(10, -i * 100); // Adjust spacing as needed

            // Set the size of the panel to be within the left 15% of the screen
            rectTransform.sizeDelta = new Vector2(leftPanelWidth, 100);

            // Add the new UI element to the list
            CambroxUIElements.Add(CambroxUI);
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from the event when this object is destroyed
        if (CambroxParty != null)
        {
            CambroxParty.OnUpdated -= UpdateUI;
        }
    }
}
using GDE.GenericSelectionUI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CambroxStorageUI : SelectionUI<ImageSlot>
{
    [SerializeField] List<ImageSlot> boxSlots;

    List<BoxPartySlotUI> partySlots = new List<BoxPartySlotUI>();
    List<BoxStorageSlotUI> storageSlots = new List<BoxStorageSlotUI>();

    CambroxParty party;
    CambroxStorageBoxes storageBoxes;

    int totalColumns = 7;

    public int SelectedBox { get; private set; } = 0;

    private void Awake()
    {
        foreach (var boxSlot in boxSlots)
        {
            var storageSlot = boxSlot.GetComponent<BoxStorageSlotUI>();
            if (storageSlot != null)
            {
                storageSlots.Add(storageSlot);
            }
            else
            {
                partySlots.Add(boxSlot.GetComponent<BoxPartySlotUI>());
            }
        }

        party = CambroxParty.GetPlayerParty();
        storageBoxes = CambroxStorageBoxes.GetPlayerStorageBoxes();
    }

    private void Start()
    {
        SetItems(boxSlots);
        SetSelectionSettings(SelectionType.Grid, totalColumns);
    }

    public void SetDataInPartySlots()
    {
        for (int i = 0; i < partySlots.Count; i++)
        {
            if (i < party.Cambroxs.Count)
                partySlots[i].SetData(party.Cambroxs[i]);
            else
                partySlots[i].ClearData();
        }
    }

    public void SetDataInStorageSlots()
    {
        for (int i = 0; i < storageSlots.Count; i++)
        {
            var Cambrox = storageBoxes.GetCambrox(SelectedBox, i);
            if (Cambrox != null)
                storageSlots[i].SetData(Cambrox);
            else
                storageSlots[i].ClearData();
        }
    }

    public bool IsPartySlot(int slotIndex)
    {
        return slotIndex % totalColumns == 0;
    }

    public Cambrox TakeCambroxFromSlot(int slotIndex)
    {
        Cambrox Cambrox;
        if (IsPartySlot(slotIndex))
        {
            int partyIndex = slotIndex / totalColumns;

            if (partyIndex >= party.Cambroxs.Count)
                return null;

            Cambrox = party.Cambroxs[partyIndex];
            party.Cambroxs[partyIndex] = null;
        }
        else
        {
            int boxSlotIndex = slotIndex - (slotIndex / totalColumns + 1);
            Cambrox = storageBoxes.GetCambrox(SelectedBox, boxSlotIndex);
            storageBoxes.RemoveCambrox(SelectedBox, boxSlotIndex);
        }

        return Cambrox;
    }

    public void PutCambroxIntoSlot(Cambrox Cambrox, int slotIndex)
    {
        if (IsPartySlot(slotIndex))
        {
            int partyIndex = slotIndex / totalColumns;

            if (partyIndex >= party.Cambroxs.Count)
                party.Cambroxs.Add(Cambrox);
            else
                party.Cambroxs[partyIndex] = Cambrox;
        }
        else
        {
            int boxSlotIndex = slotIndex - (slotIndex / totalColumns + 1);
            storageBoxes.AddCambrox(Cambrox, SelectedBox, boxSlotIndex);
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ShopUI : MonoBehaviour
{
    [SerializeField] GameObject itemList;
    [SerializeField] ItemSlotUI itemSlotUI;

    [SerializeField] Image itemIcon;
    [SerializeField] Text itemDescription;

    [SerializeField] Image upArrow;
    [SerializeField] Image downArrow;

    int selectedItem;

    List<ItemBase> availableItems;
    Action<ItemBase> onItemSelected;
    Action onBack;

    List<ItemSlotUI> slotUIList;

    const int itemsInViewport = 8;

    RectTransform itemListRect;
    private void Awake()
    {
        itemListRect = itemList.GetComponent<RectTransform>();
    }

    public void Show(List<ItemBase> availableItems, Action<ItemBase> onItemSelected,
        Action onBack)
    {
        this.availableItems = availableItems;
        this.onItemSelected = onItemSelected;
        this.onBack = onBack;

        gameObject.SetActive(true);
        UpdateItemList();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void HandleUpdate()
    {
        var prevSelection = selectedItem;

        if (Input.GetKeyDown(KeyCode.DownArrow))
            ++selectedItem;
        else if (Input.GetKeyDown(KeyCode.UpArrow))
            --selectedItem;

        selectedItem = Mathf.Clamp(selectedItem, 0, availableItems.Count - 1);

        if (selectedItem != prevSelection)
            UpdateItemSelection();

        if (Input.GetKeyDown(KeyCode.Z))
            onItemSelected?.Invoke(availableItems[selectedItem]);
        else if (Input.GetKeyDown(KeyCode.X))
            onBack?.Invoke();
    }

    void UpdateItemList()
    {
        // Clear all the existing items
        foreach (Transform child in itemList.transform)
            Destroy(child.gameObject);

        slotUIList = new List<ItemSlotUI>();
        foreach (var item in availableItems)
        {
            var slotUIObj = Instantiate(itemSlotUI, itemList.transform);
            slotUIObj.SetNameAndPrice(item);

            slotUIList.Add(slotUIObj);
        }

        UpdateItemSelection();
    }

    void UpdateItemSelection()
    {

        selectedItem = Mathf.Clamp(selectedItem, 0, availableItems.Count - 1);

        for (int i = 0; i < slotUIList.Count; i++)
        {
            if (i == selectedItem)
                slotUIList[i].NameText.color = GlobalSettings.i.HighlightedColor;
            else
                slotUIList[i].NameText.color = Color.black;
        }

        if (availableItems.Count > 0)
        {
            var item = availableItems[selectedItem];
            itemIcon.sprite = item.Icon;
            itemDescription.text = item.Description;
        }

        HandleScrolling();
    }

    void HandleScrolling()
    {
        if (slotUIList.Count <= itemsInViewport) return;

        float scrollPos = Mathf.Clamp(selectedItem - itemsInViewport / 2, 0, selectedItem) * slotUIList[0].Height;
        itemListRect.localPosition = new Vector2(itemListRect.localPosition.x, scrollPos);

        bool showUpArrow = selectedItem > itemsInViewport / 2;
        upArrow.gameObject.SetActive(showUpArrow);

        bool showDownArrow = selectedItem + itemsInViewport / 2 < slotUIList.Count;
        downArrow.gameObject.SetActive(showDownArrow);
    }
}
using GDE.GenericSelectionUI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SummaryScreenUI : SelectionUI<TextSlot>
{
    [Header("Basic Details")]
    [SerializeField] Text nameText;
    [SerializeField] Text levelText;
    [SerializeField] Image image;

    [Header("Pages")]
    [SerializeField] Text pageNameTxt;
    [SerializeField] GameObject skillsPage;
    [SerializeField] GameObject movesPage;

    [Header("Cambrox Skills")]
    [SerializeField] Text hpText;
    [SerializeField] Text attackText;
    [SerializeField] Text defenseText;
    [SerializeField] Text spAttackText;
    [SerializeField] Text spDefenseText;
    [SerializeField] Text speedText;
    [SerializeField] Text expPointsText;
    [SerializeField] Text nextLevelExpText;
    [SerializeField] Transform expBar;

    [Header("Cambrox Moves")]
    [SerializeField] List<Text> moveTypes;
    [SerializeField] List<Text> moveNames;
    [SerializeField] List<Text> movePPs;
    [SerializeField] Text moveDescriptionText;
    [SerializeField] Text movePowerText;
    [SerializeField] Text moveAccuracyText;
    [SerializeField] GameObject moveEffectsUI;

    List<TextSlot> moveSlots;
    private void Start()
    {
        moveSlots = moveNames.Select(m => m.GetComponent<TextSlot>()).ToList();
        moveEffectsUI.SetActive(false);
        moveDescriptionText.text = "";
    }

    bool inMoveSelection;
    public bool InMoveSelection 
    {
        get => inMoveSelection;
        set {
            inMoveSelection = value;

            if (inMoveSelection)
            {
                moveEffectsUI.SetActive(true);
                SetItems(moveSlots.Take(Cambrox.Moves.Count).ToList());
            }
            else
            {
                moveEffectsUI.SetActive(false);
                moveDescriptionText.text = "";
                ClearItems();
            }
        }
    }

    Cambrox Cambrox;
    public void SetBasicDetails(Cambrox Cambrox)
    {
        this.Cambrox = Cambrox;

        nameText.text = Cambrox.Base.Name;
        levelText.text = "Lv. " + Cambrox.Level;
        image.sprite = Cambrox.Base.FrontSprite;
    }

    public void ShowPage(int pageNum)
    {
        if (pageNum == 0)
        {
            // Show the skills page
            pageNameTxt.text = "Cambrox Skills";

            skillsPage.SetActive(true);
            movesPage.SetActive(false);

            SetSkills();
        }
        else if (pageNum == 1)
        {
            // Show the moves page
            pageNameTxt.text = "Cambrox Moves";

            skillsPage.SetActive(false);
            movesPage.SetActive(true);

            SetMoves();
        }
    }

    public void SetSkills()
    {
        hpText.text = $"{Cambrox.HP}/{Cambrox.MaxHp}";
        attackText.text = "" + Cambrox.Attack;
        defenseText.text = "" + Cambrox.Defense;
        spAttackText.text = "" + Cambrox.SpAttack;
        spDefenseText.text = "" + Cambrox.SpDefense;
        speedText.text = "" + Cambrox.Speed;

        expPointsText.text = "" + Cambrox.Exp;
        nextLevelExpText.text = "" + (Cambrox.Base.GetExpForLevel(Cambrox.Level + 1) - Cambrox.Exp);
        expBar.localScale = new Vector2(Cambrox.GetNormalizedExp(), 1);
    }

    public void SetMoves()
    {
        for (int i = 0; i < moveNames.Count; i++)
        {
            if (i < Cambrox.Moves.Count)
            {
                var move = Cambrox.Moves[i];

                moveTypes[i].text = move.Base.Type.ToString().ToUpper();
                moveNames[i].text = move.Base.Name.ToUpper();
                movePPs[i].text = $"PP {move.PP}/{move.Base.PP}";
            }
            else
            {
                moveTypes[i].text = "-";
                moveNames[i].text = "-";
                movePPs[i].text = "-";
            }
        }
    }

    public override void HandleUpdate()
    {
        if (InMoveSelection)
            base.HandleUpdate();
    }

    public override void UpdateSelectionInUI()
    {
        base.UpdateSelectionInUI();

        var move = Cambrox.Moves[selectedItem];

        moveDescriptionText.text = move.Base.Description;
        movePowerText.text = "" + move.Base.Power;
        moveAccuracyText.text = "" + move.Base.Accuracy;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WalletUI : MonoBehaviour
{
    [SerializeField] Text moneyTxt;

    private void Start()
    {
        Wallet.i.OnMoneyChanged += SetMoneyTxt;
    }

    public void Show()
    {
        gameObject.SetActive(true);
        SetMoneyTxt();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    void SetMoneyTxt()
    {
        moneyTxt.text = "$ " + Wallet.i.Money;
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpriteAnimator
{
    SpriteRenderer spriteRenderer;
    List<Sprite> frames;
    float frameRate;

    int currentFrame;
    float timer;

    public SpriteAnimator(List<Sprite> frames, SpriteRenderer spriteRenderer, float frameRate = 0.16f)
    {
        this.frames = frames;
        this.spriteRenderer = spriteRenderer;
        this.frameRate = frameRate;
    }

    public void Start()
    {
        currentFrame = 0;
        timer = 0f;
        spriteRenderer.sprite = frames[0];
    }

    public void HandleUpdate()
    {
        timer += Time.deltaTime;
        if (timer > frameRate)
        {
            currentFrame = (currentFrame + 1) % frames.Count;
            spriteRenderer.sprite = frames[currentFrame];
            timer -= frameRate;
        }
    }

    public List<Sprite> Frames {
        get { return frames; }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScriptableObjectDB<T> : MonoBehaviour where T : ScriptableObject
{
    static Dictionary<string, T> objects;

    public static void Init()
    {
        objects = new Dictionary<string, T>();

        var objectArray = Resources.LoadAll<T>("");
        foreach (var obj in objectArray)
        {
            if (objects.ContainsKey(obj.name))
            {
                Debug.LogError($"There are two Cambroxs with the name {obj.name}");
                continue;
            }

            objects[obj.name] = obj;
        }
    }

    public static T GetObjectByName(string name)
    {
        if (!objects.ContainsKey(name))
        {
            Debug.LogError($"Object with name {name} not found in the database");
            return null;
        }

        return objects[name];
    }
}
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GDEUtils.StateMachine
{
    public class StateMachine<T>
    {
        public State<T> CurrentState { get; private set; }
        public Stack<State<T>> StateStack { get; private set; }

        T owner;
        public StateMachine(T owner)
        {
            this.owner = owner;
            StateStack = new Stack<State<T>>();
        }

        public void Execute()
        {
            CurrentState?.Execute();
        }


        public void Push(State<T> newState)
        {
            StateStack.Push(newState);
            CurrentState = newState;
            CurrentState.Enter(owner);
        }

        public void Pop()
        {
            StateStack.Pop();
            CurrentState.Exit();
            CurrentState = StateStack.Peek();
        }

        public void ChangeState(State<T> newState)
        {
            if (CurrentState != null)
            {
                StateStack.Pop();
                CurrentState.Exit();
            }

            StateStack.Push(newState);
            CurrentState = newState;
            CurrentState.Enter(owner);
        }

        public IEnumerator PushAndWait(State<T> newState)
        {
            var oldState = CurrentState;
            Push(newState);
            yield return new WaitUntil(() => CurrentState == oldState);
        }

        public State<T> GetPrevState()
        {
            return StateStack.ElementAt(1);
        }

    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GDEUtils.StateMachine
{
    public class State<T> : MonoBehaviour
    {
        public virtual void Enter(T owner) { }

        public virtual void Execute() { }

        public virtual void Exit() { }
    }

}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TextSlot : MonoBehaviour, ISelectableItem
{
    [SerializeField] Text text;

    Color originalColor;
    public void Init()
    {
        originalColor = text.color;
    }

    public void Clear()
    {
        text.color = originalColor;
    }

    public void OnSelectionChanged(bool selected)
    {
        text.color = (selected) ? GlobalSettings.i.HighlightedColor : originalColor;
    }

    public void SetText(string s)
    {
        text.text = s;
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GDE.GenericSelectionUI
{
    public enum SelectionType { List, Grid }

    public class SelectionUI<T> : MonoBehaviour where T : ISelectableItem
    {
        List<T> items;
        protected int selectedItem = 0;

        SelectionType selectionType;
        int gridWidth = 2;

        float selectionTimer = 0;

        const float selectionSpeed = 5;

        public event Action<int> OnSelected;
        public event Action OnBack;

        public void SetSelectionSettings(SelectionType selectionType, int gridWidth)
        {
            this.selectionType = selectionType;
            this.gridWidth = gridWidth;
        }

        public void SetItems(List<T> items)
        {
            this.items = items;

            items.ForEach(i => i.Init());
            UpdateSelectionInUI();
        }

        public void ClearItems()
        {
            items?.ForEach(i => i.Clear());

            this.items = null;
        }

        public virtual void HandleUpdate()
        {
            UpdateSelectionTimer();
            int prevSelection = selectedItem;

            if (selectionType == SelectionType.List)
                HandleListSelection();
            else if (selectionType == SelectionType.Grid)
                HandleGridSelection();

            selectedItem = Mathf.Clamp(selectedItem, 0, items.Count - 1);

            if (selectedItem != prevSelection)
                UpdateSelectionInUI();

            if (Input.GetButtonDown("Action"))
                OnSelected?.Invoke(selectedItem);
            else if (Input.GetButtonDown("Back"))
                OnBack?.Invoke();
        }

        void HandleListSelection()
        {
            float v = Input.GetAxisRaw("Vertical");

            if (selectionTimer == 0 && Mathf.Abs(v) > 0.2f)
            {
                selectedItem += -(int)Mathf.Sign(v);

                selectionTimer = 1 / selectionSpeed;
            }
        }

        void HandleGridSelection()
        {
            float v = Input.GetAxisRaw("Vertical");
            float h = Input.GetAxisRaw("Horizontal");

            if (selectionTimer == 0 && (Mathf.Abs(v) > 0.2f || Mathf.Abs(h) > 0.2f))
            {
                if (Mathf.Abs(h) > Mathf.Abs(v))
                    selectedItem += (int)Mathf.Sign(h);
                else
                    selectedItem += -(int)Mathf.Sign(v) * gridWidth;


                selectionTimer = 1 / selectionSpeed;
            }
        }

        public virtual void UpdateSelectionInUI()
        {
            for (int i = 0; i < items.Count; i++)
            {
                items[i].OnSelectionChanged(i == selectedItem);
            }
        }

        void UpdateSelectionTimer()
        {
            if (selectionTimer > 0)
                selectionTimer = Mathf.Clamp(selectionTimer - Time.deltaTime, 0, selectionTimer);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ISelectableItem
{
    void Init();
    void Clear();
    void OnSelectionChanged(bool selected);
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ImageSlot : MonoBehaviour, ISelectableItem
{
    Image bgImage;
    void Awake()
    {
        bgImage = GetComponent<Image>();
    }

    Color originalColor;
    public void Init()
    {
        originalColor = bgImage.color;
    }

    public void Clear()
    {
        bgImage.color = originalColor;
    }

    public void OnSelectionChanged(bool selected)
    {
        bgImage.color = (selected) ? GlobalSettings.i.BgHighlightColor : originalColor;
    }
}
