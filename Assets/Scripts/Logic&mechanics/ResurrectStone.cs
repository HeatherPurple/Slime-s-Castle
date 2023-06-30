using UnityEngine;

public class ResurrectStone : MonoBehaviour
{
    private Animator anim;

    [SerializeField] private bool canHealing;
    [SerializeField] private float healingRateStone;
    private float healingRatePlayer;
    [SerializeField] [Range(0, 4)] private int bossFireflyStage;
    [SerializeField] private Transform savePosition;

    scr_SaveController SaveController;
    scr_GameManager GameManager;
    scr_TimeManager TimeManager;
    scr_Player Player;

    // Start is called before the first frame update
    void Start()
    {
        anim=GetComponent<Animator>();

        if (bossFireflyStage != 0)
        {
            GetComponent<SpriteRenderer>().enabled = false;
        }

        SaveController = scr_SaveController.instance;
        GameManager = scr_GameManager.instance;
        TimeManager = scr_TimeManager.instance;
        Player = scr_Player.instance;
    }

    // Update is called once per frame
    void Update()
    {

    }
    private void OnTriggerEnter2D(Collider2D myTrigger)
    {
        if (myTrigger.CompareTag("Player"))
        {
            Player.spawnPosition = transform;

            if (canHealing)
            {
                healingRatePlayer = Player.healingRate;
                Player.healingRate = healingRateStone;
            }

            AutoSave();

            if (!anim.GetBool("Active"))
            {
                anim.SetBool("Active",true);

                if (bossFireflyStage == 0)
                {
                    scr_AudioManager.instance.PlaySound("Save", gameObject);
                }
            }
            
        }
    }

    private void OnTriggerStay2D(Collider2D other) {
        if(other.CompareTag("Player")){
            
        }
    }

    private void OnTriggerExit2D(Collider2D other) {
        if(other.CompareTag("Player")){
            if(canHealing){
                Player.healingRate = healingRatePlayer;
            }
        }
    }

    public void AutoSave()
    {
        int numberOfSave = GameManager.currentSaveGame.numberOfSave;
        SaveGame save = SaveController.GetSaveGame(numberOfSave);
        save.UpdateTimeSave();
        save.newGame = false;
        save.position = transform.position;
        save.playerCoins = scr_Player.instance.currentNumberOfCoins;
        save.totalTime = GameManager.currentSaveGame.totalTime;
        save.totalTime += TimeManager.GetTimeSinceGetLastTime();
        save.nameScene = GameManager.nameScene;

        save.bossFireflyStage = 0;
        save.bossFireflyFirstTryPhase1 = true;
        save.bossFireflyFirstTryPhase2 = true;

        switch (bossFireflyStage)
        {
            case 1:
                save.bossFireflyStage = 1;
                save.bossFireflyFirstTryPhase1 = false;
                break;
            case 2:
                save.bossFireflyStage = 2;
                save.bossFireflyFirstTryPhase2 = false;
                break;
            case 3:
                save.bossFireflyStage = 3;
                break;
            case 4:
                save.position = savePosition.position;
                save.bossFireflyStage = 4;
                break;
            default:
                break;
        }

        if (numberOfSave == 0)
        {
            save.nameOfSave = "saveGame0";
        }

        GameManager.currentSaveGame=save;
        SaveController.SetSaveGame(numberOfSave,save);

    }

}
