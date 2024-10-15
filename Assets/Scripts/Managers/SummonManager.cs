using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class SummonManager
{
    #region Fields

    private Player _player;
    private InventoryManager _inventoryManager;
    private FollowerDataManager _followerDataManager;
    private UISubSceneShopSummon _shopSummon;
    private SummonConfig _summonConfig;

    private List<int> summonResult = new(200);
    private List<string> resultIdList = new(200);
    
    private Coroutine _repeatCoroutine;

    private Dictionary<string, SummonTable> _tables = new Dictionary<string, SummonTable>();
    public Dictionary<string, SummonTable> SummonTables => _tables;
    


    #endregion

    #region Properties

    public bool SummonRepeatCheck => _repeatCoroutine != null;

    #endregion

    #region Initialize

    public void SetSummon()
    {
        _player = Manager.Game.Player;
        _inventoryManager = Manager.Inventory;
    }

    public void Initialize()
    {
        _summonConfig = Manager.Asset.GetBlueprint("SummonConfig") as SummonConfig;

        foreach (var list in _summonConfig.SummonLists)
        {
            TableInitalize(list);
        }
    }

    public void SetShopPopup(UISubSceneShopSummon uIPopupShopSummon)
    {
        _shopSummon = uIPopupShopSummon;
    }
    public void TableInitalize(SummonList summonList)
    {
        SummonTable table;

        if (summonList.TypeLink == "Equipment")
            table = new(summonList, Manager.Data.Profile.Summon_Progress_Equipment);
        else if (summonList.TypeLink == "Skills")
            table = new(summonList, Manager.Data.Profile.Summon_Progress_Skills);
        else
            table = new(summonList, Manager.Data.Profile.Summon_Progress_Follower);

        _tables[summonList.TypeLink] = table;
    }

    public int GetSummonCounts(string key)
    {
        if (!_tables.TryGetValue(key, out SummonTable summonTable)) return 0;

        return summonTable.SummonCounts;
    }
    #endregion

    #region Summon

    public bool SummonTry(int addcount, string tableLink, UIBtn_Check_Gems btnUI)
    {
        switch (btnUI.ButtonInfo.ResourceType)
        {
            case ResourceType.Gold:
                if (_player.IsTradeGold(btnUI.ButtonInfo.Amount))
                {
                    btnUI.ApplyRestriction();
                    if (btnUI.ButtonInfo.OnEvent)
                    {
                        SummonTables.TryGetValue(tableLink, out var summonTable);
                        summonTable.ApplySummonCountAdd();
                    }

                    Summon(btnUI.ButtonInfo.SummonCount + addcount, tableLink);
                    return true;
                }
                break;

            case ResourceType.Gems:
                if (_player.IsTradeGems(btnUI.ButtonInfo.Amount))
                {
                    btnUI.ApplyRestriction();
                    if (btnUI.ButtonInfo.OnEvent)
                    {
                        SummonTables.TryGetValue(tableLink, out var summonTable);
                        summonTable.ApplySummonCountAdd();
                    }

                    Summon(btnUI.ButtonInfo.SummonCount + addcount, tableLink);
                    return true;
                }
                break;
        }

        return false;
    }

    private void Summon(int count, string typeLink)
    {
        // 팝업이 존재할 경우 => 제거
        Manager.UI.CloseCurrentSummonPopup();

        for (int i = 0; i < count; i++)
        {
            summonResult.Add(Random.Range(0, 1000000));
        }

        int[] summonResultValue = summonResult.ToArray();
        summonResult.Clear();

        SummonTables.TryGetValue(typeLink, out var summonTable);

        var curLevelTable = summonTable.GetProbabilityTable();
        var curprobability = curLevelTable.Select(x => x.Key).ToArray();

       
        int idx = 0; 

        while (count > 0)
        {
            int getResultKey = curprobability.OrderBy(x => (summonResultValue[idx] - x >= 0)).First(); 
            curLevelTable.TryGetValue(getResultKey, out string index);
            resultIdList.Add(index);
           
            count--;
            idx++;
            if (summonTable.ApplySummonCount())
            {
                SummonTables.TryGetValue(typeLink, out var newSummonTable);
                curLevelTable = newSummonTable.GetProbabilityTable();
                curprobability = curLevelTable.Select(x => x.Key).ToArray();
            }
        }
       
        // 최종 획득한 아이템 목록 인벤토리에 넣고 팝업 실행
        string[] finalResult = resultIdList.ToArray();

        switch (typeLink)
        {
            case "Equipment":
                EquipmentAdd(finalResult);
                break;
            case "Skills":
                SkillAdd(finalResult);
                break;
            case "Follower":
                FollowerAdd(finalResult);
                break;
        }
        
        var popup = Manager.UI.ShowSummonPopup<UIPopupRewardsSummon>("UIPopupRewardsSummon");
        popup.DataInit(typeLink, finalResult);
        popup.PlayStart();
        popup.SummonButtonInit(summonTable.SummonList);
        _shopSummon.BannerUpdate(typeLink, summonTable.SummonCountsAdd);
        summonResult.Clear();
        resultIdList.Clear();

        Manager.Notificate.SetPlayerStateNoti();
        Manager.Data.Save();
    }

    private void EquipmentAdd(string[] summonResult)
    {
        for (int i = 0; i < summonResult.Length; i++)
        {
            UserItemData itemData = _inventoryManager.SearchItem(summonResult[i]);
            itemData.hasCount++;
        }
    }

    private void FollowerAdd(string[] summonResult)
    {
        for (int i = 0; i < summonResult.Length; i++)
        {
            UserInvenFollowerData followerData = Manager.FollowerData.SearchFollower(summonResult[i]);
            followerData.hasCount++;
        }
    }

    private void SkillAdd(string[] summonResult)
    {
        for (int i = 0; i < summonResult.Length; i++)
        {
            UserInvenSkillData skillData = Manager.SkillData.SearchSkill(summonResult[i]);
            skillData.hasCount++;
        }
    }
    #endregion

    #region Summon Repeat

    public void SetSummonRepeat(string tableLink, UIBtn_Check_Gems btnUI)
    {
        _repeatCoroutine = CoroutineHelper.StartCoroutine(SummonRepeat(tableLink, btnUI));
    }

    public void StopSummonRepeat()
    {
        CoroutineHelper.StopCoroutine(_repeatCoroutine);
        _repeatCoroutine = null;
    }

    private IEnumerator SummonRepeat(string tableLink, UIBtn_Check_Gems btnUI)
    {
        while (true)
        {
            if (!SummonTry(0, tableLink, btnUI))
            {
                _repeatCoroutine = null;
                Manager.UI.CurrentSummonPopup.NotEnoughResource();
                yield break;
            }

            yield return new WaitUntil(() => Manager.UI.CurrentSummonPopup.IsSkip);
            yield return new WaitForSecondsRealtime(1.0f);
        }
    }

    #endregion
}


public class SummonTable
{
    #region Fields

    private Dictionary<int, Dictionary<int, string>> probabilityTable = new();
    private List<int> _gradeUpCount;
    private SummonList _summonList;

    #endregion

    #region Properties

    // 유저 데이터 프로퍼티
    public int SummonGrade { get; private set; }
    public int SummonCounts { get; private set; }
    public int SummonCountsAdd { get; private set; }

    // 카운트 계산 프로퍼티
    public bool IsMaxGrade => SummonGrade == _gradeUpCount.Count;
    public int GetCurCount => SummonCounts - _gradeUpCount[SummonGrade - 1];
    public int GetNextCount => _gradeUpCount[SummonGrade] - _gradeUpCount[SummonGrade - 1];
    public SummonList SummonList => _summonList;

    #endregion

    public SummonTable(SummonList summonList, int summonCounts)
    {
        SummonCounts = summonCounts;
        _summonList = summonList;

        ProbabilityInit(_summonList.TypeLink);
        GradeCountInit(_summonList.TypeLink);
        SummonGradeInit();
    }

    #region Initialize

    private void ProbabilityInit(string tableLink)
    {
        string _tabletext = Manager.Asset.GetText($"SummonTable{tableLink}");
        var probabilityDataTable = JsonUtility.FromJson<ProbabilityDataTable>($"{{\"probabilityDataTable\":{_tabletext}}}");

        // <등급(그룹), <아이템, 확률>>
        var gradeValue = probabilityDataTable.probabilityDataTable
            .GroupBy(data => data.SummonGrade)
            .ToDictionary(grade => grade.Key, group => group.ToDictionary(x => x.ItemId, x => x.Probability));

        // <등급(그룹), <확률 누계, 아이템>>
        probabilityTable = gradeValue
            .ToDictionary(gradeGroup => gradeGroup.Key, gradeGroup =>
            {
                var cumulativeDict = new Dictionary<int, string>();
                int sum = 0;

                foreach (var probData in gradeGroup.Value)
                {
                    sum += probData.Value; 
                    cumulativeDict[sum] = probData.Key; 
                }

                return cumulativeDict;
            }
            );
    }

    private void GradeCountInit(string tableLink)
    {
        string _tabletext = Manager.Asset.GetText($"SummonCount{tableLink}");
        var gradeUpDataTable = JsonUtility.FromJson<GradeUpDataTable>($"{{\"gradeUpDataTable\":{_tabletext}}}");

        _gradeUpCount = gradeUpDataTable.gradeUpDataTable.Select(x => x.needCounts).ToList();
    }

    private void SummonGradeInit()
    {
        int CurCount = _gradeUpCount.OrderBy(x => (SummonCounts - x >= 0)).First();

        for (int i = 0; i < _gradeUpCount.Count; i++)
        {
            if (_gradeUpCount[i] == CurCount)
            {
                SummonGrade = (_gradeUpCount[0] == CurCount) ? _gradeUpCount.Count : i;
            }
        }
    }

    #endregion

    #region Control Method

    public Dictionary<int, string> GetProbabilityTable()
    {
        probabilityTable.TryGetValue(SummonGrade, out var summonProbability);
        return summonProbability;
    }

    public bool ApplySummonCount()
    {
        if (!IsMaxGrade)
        {
            SummonCounts++;

            if (SummonCounts >= _gradeUpCount[SummonGrade] && !IsMaxGrade)
            {
                SummonGrade++;
                return true;
            }
        }

        return false;
    }

    public void ApplySummonCountAdd()
    {
        if (SummonCountsAdd < _summonList.CountAddLimit)
        {
            SummonCountsAdd++;
        }
    }

    #endregion
}

#region Table Serializable Class

[System.Serializable]
public class ProbabilityDataTable
{
    public List<ProbabilityData> probabilityDataTable;
}

[System.Serializable]
public class ProbabilityData
{
    public int SummonGrade;
    public string ItemId;
    public int Probability;
}

[System.Serializable]
public class GradeUpDataTable
{
    public List<GradeUpData> gradeUpDataTable;
}

[System.Serializable]
public class GradeUpData
{
    public int SummonGrade;
    public int needCounts;
}

#endregion