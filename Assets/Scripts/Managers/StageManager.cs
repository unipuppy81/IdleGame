using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StageManager
{
    #region Fields

    // json 경로 관련
    private string _tableText;
    private string _UItableText;

    private Dictionary<int, StageData> stageTable;
    private Dictionary<int, StageUIData> stageUITable;
    private StageData stageData;
    private List<BaseEnemy> enemyList = new();
    private Transform[] spawnPoint;
    private Transform bossSpawnPoint;
    private Coroutine stageCoroutine;
    private UISceneMain uISceneMain;

    private BackgroundControl backgroundControl;

    // 스케일 비율 
    private float ratio;
    #endregion

    #region Properties

    // 플레이 데이터 프로퍼티
    public int Chapter { get; private set; }
    public int StageLevel { get; private set; }
    public bool WaveLoop { get; private set; }

    // 관리용 프로퍼티
    public bool BossAppearance => StageLevel == StageConfig.BattleCount;
    public bool StageClear => StageLevel > StageConfig.BattleCount;
    public bool WaveClear => enemyList.Count == 0;
    public bool PlayerReset { get; private set; }

    // 스테이지 정보 로드용 프로퍼티
    public StageBlueprint StageConfig => Manager.Asset.GetBlueprint(stageData.StageConfig) as StageBlueprint;
    public string StageBackground => string.Empty;
    public long EnemyHpRate => stageData.EnemyHpRate;
    public long EnemyAttackRate => stageData.EnemyAttackRate;
    public long EnemyGoldRate => stageData.EnemyGoldRate;
    public int EnemySpawnCount => stageData.EnemySpawnCount;
    public long IdleGoldReward => stageData.IdleGoldReward * EnemyGoldRate;

    #endregion

    #region Table Reference


    private void StageDataChange(int index)
    {
        stageTable.TryGetValue(index, out var data);
        stageData = data;
    }

    #endregion

    #region Init

    public void Initialize()
    {
        // json 파일 로딩, 딕셔너리에 인덱스 그룹 넣기
        _tableText = Manager.Asset.GetText("ItemTableStage");
        var stageDataTable = JsonUtility.FromJson<StageDataTable>($"{{\"stageDataTable\":{_tableText}}}");

        _UItableText = Manager.Asset.GetText("ItemTableStage_Hud");
        var stageUIDataTable = JsonUtility.FromJson<StageUIDataTable>($"{{\"stageUIDataTable\":{_UItableText}}}");

        stageTable = stageDataTable.stageDataTable.ToDictionary(group => group.Index, group => group);
        stageUITable = stageUIDataTable.stageUIDataTable.ToDictionary(group => group.Index, group => group);

        var profile = Manager.Data.Profile;
        Chapter = profile.Stage_Chapter;
        StageLevel = profile.Stage_Level;
        WaveLoop = profile.Stage_WaveLoop;

        ratio = Manager.Game.screenRatio;

        backgroundControl = Object.FindObjectOfType<BackgroundControl>();
        backgroundControl.Initiailize();
    }

    public void SetStage(Transform[] spawnPoint, Transform bossSpawnPoint)
    {
        StageDataChange(Chapter);
        Manager.Game.Player.IdleRewardInit();

        this.spawnPoint = spawnPoint;
        this.bossSpawnPoint = bossSpawnPoint;

        uISceneMain = Manager.UI.CurrentScene as UISceneMain;
    }

    public List<BaseEnemy> GetEnemyList()
    {
        return enemyList;
    }

    public StageUIData UITextReturn()
    {
        var UITable = stageUITable.Select(x => x.Key).ToList();
        var CurChapter = UITable.OrderBy(x => (x - Chapter <= 0)).Last();
        stageUITable.TryGetValue(CurChapter, out var chapterUI);
        return chapterUI;
    }

    #endregion

    #region Stage Progress

    public void BattleStart()
    {
        stageCoroutine ??= CoroutineHelper.StartCoroutine(BattleCycle());
    }

    public void BattleStop()
    {
        if(stageCoroutine != null)
        {
            CoroutineHelper.StopCoroutine(stageCoroutine);
            stageCoroutine = null;
        }
    }

    public IEnumerator StageFailed()
    {
        BattleStop();
        EnemyReset();
        Manager.Game.Player.GetComponent<PlayerSkillHandler>().ResetSkillCondition();

        yield return CoroutineHelper.StartCoroutine(StageTransitionDelay(3));

        // 보스 잡다 죽었으면 루프랑 버튼 켜주고 진행도만 하나 뒤로 물리기
        if (BossAppearance)
        {
            WaveLoop = true;
            StageLevel--;
            uISceneMain.RetryBossButtonToggle();
            uISceneMain.WaveLoopImageToggle();
        }
        else if (StageLevel > 0)
        {
            StageLevel--;
        }
        else
        {
            // 스테이지 처음에서 죽었으면 아예 뒤로 물리기
            Chapter--;
            StageLevel = 3;

            ChapterCheck();
            StageDataChange(Chapter);
            Manager.Game.Player.IdleRewardPopupUpdate();
        }

        uISceneMain.UpdateCurrentStage();
        uISceneMain.UpdateStageLevel(StageLevel);
        BattleStart();
    }

     private IEnumerator StageTransitionDelay(float delaySec)
    {
        yield return new WaitForSeconds(delaySec);
    }


    private IEnumerator BattleCycle()
    {
        while (true)
        {
            if (PlayerReset)
            {
                var Player = Manager.Game.Player;
                Player.SetCurrentHp(Player.ModifierHp);
                PlayerReset = false;
            }

            yield return new WaitForSeconds(0.5f);
            if (!BossAppearance)
            {
                var delay = 3.0f / EnemySpawnCount;
                for (int i = 0; i < EnemySpawnCount; i++)
                {
                    yield return new WaitForSeconds(delay);
                    EnemyWaveSpawn();
                }
            }
            else
            {
                Manager.Game.Player.GetComponent<PlayerSkillHandler>().ResetSkillCondition();
                BossWaveSpawn();
            }

            yield return new WaitUntil(()=> enemyList.Count == 0);
            WaveCompleted();
        }
    }

    private void EnemyWaveSpawn()
    {
        var randomEnemyName = StageConfig.Enemies[Random.Range(0, StageConfig.Enemies.Length)];
        var enemyBlueprint = Manager.Asset.GetBlueprint(randomEnemyName) as EnemyBlueprint;

        var randomYPos = Random.Range(spawnPoint[0].position.y, spawnPoint[3].position.y);
        var randomPos = new Vector2(spawnPoint[0].position.x, Mathf.Round(randomYPos * 10.0f) * 0.1f);

        var enemyObject = Manager.ObjectPool.GetGo("EnemyFrame");
        var enemySprite = enemyObject.GetComponent<SpriteRenderer>();
        enemySprite.sortingOrder = (int)Mathf.Ceil(spawnPoint[0].position.y * 10.0f - (randomPos.y * 10.0f));
        var enemy = enemyObject.GetComponent<BaseEnemy>();

        enemy.SetEnemy(enemyBlueprint, randomPos, EnemyHpRate, EnemyAttackRate, EnemyGoldRate);
        enemyList.Add(enemy);

        enemyObject.transform.localScale = new Vector2(1 - ratio, 1 - ratio);
    }

    private void BossWaveSpawn()
    {
        uISceneMain.StageLevelGaugeToggle(false);
        uISceneMain.TimerOn();

        var enemyBlueprint = Manager.Asset.GetBlueprint(StageConfig.Boss) as EnemyBlueprint;
        var bossObject = Manager.ObjectPool.GetGo("EnemyFrame");
        var enemy = bossObject.GetComponent<BaseEnemy>();
        enemy.SetEnemy(enemyBlueprint, bossSpawnPoint.position, EnemyHpRate, EnemyAttackRate, EnemyGoldRate);
        enemyList.Add(enemy);

        bossObject.transform.localScale = new Vector2(2.0f - ratio, 2.0f - ratio);
    }

    private void WaveCompleted()
    {
        if (!WaveLoop)        
            StageLevel++;
        
        if (StageClear)
        {
            uISceneMain.TimerOff();
            StageLevel = 0;
            PlayerReset = true;

            Chapter++;
            ChapterCheck();
            StageDataChange(Chapter);
            Manager.Game.Player.IdleRewardPopupUpdate();
            uISceneMain.StageLevelGaugeToggle();
            backgroundControl.ChangeSprite();
        }

        uISceneMain.UpdateStageLevel(StageLevel);
        SaveStage();

        Manager.Quest.QuestDB[3].currentValue = Chapter;
    }

    private void EnemyReset()
    {
        for (int i = 0; i < enemyList.Count; i++)
        {
            GameObject.Destroy(enemyList[i].gameObject);
        }
        enemyList.Clear();
    }

    public void RetryBossBattle()
    {
        BattleStop();
        EnemyReset();

        uISceneMain.RetryBossButtonToggle();
        uISceneMain.WaveLoopImageToggle();

        WaveLoop = false;
        StageLevel++;

        BattleStart();
    }

    private void ChapterCheck()
    {
        if (Chapter == 0)
        {
            Chapter = 1;
            StageLevel = 0;
        }
        
        if (Chapter > stageTable.Count)
        {
            Chapter = stageTable.Count;
        }
    }

    private void SaveStage()
    {
        uISceneMain.UpdateCurrentStage();

        Manager.Data.Save();
    }

    #endregion
}

#region Table Serializable Class

[System.Serializable]
public class StageDataTable
{
    public List<StageData> stageDataTable;
}

[System.Serializable]
public class StageData
{
    public int Index;
    public string StageConfig;
    public string StageBackground;
    public long EnemyHpRate;
    public long EnemyAttackRate;
    public long EnemyGoldRate;
    public int EnemySpawnCount;
    public long IdleGoldReward;
}

[System.Serializable]
public class StageUIDataTable
{
    public List<StageUIData> stageUIDataTable;
}

[System.Serializable]
public class StageUIData
{
    public int Index;
    public string UIText;
}

#endregion