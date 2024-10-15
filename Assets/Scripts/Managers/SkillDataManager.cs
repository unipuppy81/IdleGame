using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SkillDataManager
{
    private SkillContainerBlueprint _skillDataContainer;

    private Dictionary<string, SkillBlueprint> _skillDataDictionary = new();
    public Dictionary<string, SkillBlueprint> SkillDataDictionary => _skillDataDictionary;

    public UserInvenSkillData ReplaceSkill;

    public void InitSkill()
    {
        ParseSkillData();
    }

    public void ParseSkillData()
    {
        _skillDataContainer = Manager.Asset.GetBlueprint("SkillDataContainer") as SkillContainerBlueprint;
        foreach (var skillData in _skillDataContainer.skillDatas)
        {
            _skillDataDictionary.Add(skillData.ItemID, skillData);
        }
    }

    #region slot info update

    private event Action<int> SetSkillUIEquipSlot;
    private event Action<string> SetSkillUIInvenSlot;
    private event Action SetAllSkillUIEquipSlot;

    // 스킬 팝업 상단
    public void AddSetSkillUIEquipSlot(Action<int> handler)
    {
        SetSkillUIEquipSlot += handler;
    }
    public void RemoveSetSkillUIEquipSlot(Action<int> handler)
    {
        SetSkillUIEquipSlot -= handler;
    }

    public void CallSetUISkillEquipSlot(int index)
    {
        SetSkillUIEquipSlot?.Invoke(index);
    }

    public void AddSetAllSkillUIEquipSlot(Action handler)
    {
        SetAllSkillUIEquipSlot += handler;
    }
    public void RemoveSetAllSkillUIEquipSlot(Action handler)
    {
        SetAllSkillUIEquipSlot -= handler;
    }

    public void CallSetAllSkillUIEquipSlot()
    {
        SetAllSkillUIEquipSlot.Invoke();
    }


    //스킬 팝업 하단 
    public void AddSetSkillUIInvenSlot(Action<string> handler)
    {
        SetSkillUIInvenSlot += handler;
    }
    public void RemoveSetSkillUIInvenSlot(Action<string> handler)
    {
        SetSkillUIInvenSlot -= handler;
    }

    public void CallSetUISkillInvenSlot(string id)
    {
        SetSkillUIInvenSlot.Invoke(id);
    }

    #endregion

    #region Equip Method

    /// <summary>
    /// 스킬 장착 성공 시 슬롯의 인덱스, 실패 시 감시값을 반환
    /// -100 : 해당 스킬 보유 X, -200 : 장착 가능 슬롯 X </para>
    /// </summary>
    public int EquipSkill(UserInvenSkillData userInvenSkillData)
    {
        if (userInvenSkillData.level == 1 && userInvenSkillData.hasCount == 0)
        {
            SystemAlertFloating.Instance.ShowMsgAlert(MsgAlertType.CanNotEquip);
            return -100;
        }

        int index = Manager.Data.UserSkillData.UserEquipSkill.FindIndex(data => data.itemID == "Empty");
        if (index > -1)
        {
            Manager.Data.UserSkillData.UserEquipSkill[index].itemID = userInvenSkillData.itemID;
            Manager.Game.Player.gameObject.GetComponent<PlayerSkillHandler>().ChangeEquipSkillData(index);
            userInvenSkillData.equipped = true;
            return index;
        }

        ReplaceSkill = userInvenSkillData;
        return -200;
    }

    public int UnEquipSkill(UserInvenSkillData userInvenSkillData)
    {
        int index = Manager.Data.UserSkillData.UserEquipSkill.FindIndex(data => data.itemID == userInvenSkillData.itemID);

        Manager.Data.UserSkillData.UserEquipSkill[index].itemID = "Empty";
        Manager.Game.Player.gameObject.GetComponent<PlayerSkillHandler>().ChangeEquipSkillData(index);
        userInvenSkillData.equipped = false;
        return index;
    }

    #endregion

    #region Reinforce Method

    // 강화 로직
    private void ReinforceSkill(UserInvenSkillData userInvenSkillData)
    {
        while (userInvenSkillData.hasCount >= Mathf.Min(userInvenSkillData.level + 1, 15))
        {
            if (userInvenSkillData.level < 100)
            {
                userInvenSkillData.hasCount -= Mathf.Min(userInvenSkillData.level + 1, 15);
                userInvenSkillData.level += 1;
            }
            //최고 레벨
            else
            {
                int index = Manager.Data.SkillInvenList.FindIndex(item => item.itemID == userInvenSkillData.itemID);
                if (Manager.Data.SkillInvenList.Count - 1 > index)
                {
                    userInvenSkillData.hasCount -= Mathf.Min(userInvenSkillData.level + 1, 15);
                    Manager.Data.SkillInvenList[index + 1].hasCount += 1;
                }
                else if (Manager.Data.SkillInvenList.Last().level < 100)
                {
                    userInvenSkillData.hasCount -= Mathf.Min(userInvenSkillData.level + 1, 15);
                    Manager.Data.SkillInvenList[index + 1].hasCount += 1;
                }
                else
                {
                    break;
                }
            }
        }

        CallSetUISkillInvenSlot(userInvenSkillData.itemID);
        CallSetAllSkillUIEquipSlot();
    }


    public void ReinforceSelectSkill(UserInvenSkillData userInvenSkillData)
    {
        if (userInvenSkillData.hasCount < Mathf.Min(userInvenSkillData.level + 1, 15))
        {
            SystemAlertFloating.Instance.ShowMsgAlert(MsgAlertType.CanNotReinforce);
            return;
        }
        else if (userInvenSkillData.level >= 100 & (userInvenSkillData.itemID == Manager.Data.SkillInvenList.Last().itemID))
        {
            SystemAlertFloating.Instance.ShowMsgAlert(MsgAlertType.ItemLimitLevel);
            return;
        }

        ReinforceSkill(userInvenSkillData);
    }


    public void ReinforceAllSkill()
    {
        var list = Manager.Data.SkillInvenList.Where(item => item.hasCount >= Mathf.Min(item.level + 1, 15));

        if (list.Count() == 0 || (list.First().itemID == Manager.Data.SkillInvenList.Last().itemID & list.First().level >= 100))
        {
            Debug.Log(list.Count());
            SystemAlertFloating.Instance.ShowMsgAlert(MsgAlertType.CanNotAllReinforce);
            return;
        }

        foreach (var item in Manager.Data.UserSkillData.UserInvenSkill)
        {
            ReinforceSkill(item);
        }
        CallSetAllSkillUIEquipSlot();
    }

    #endregion

    public UserInvenSkillData SearchSkill(string id)
    {
        return Manager.Data.SkillInvenDictionary[id];
    }
}

[System.Serializable]
public class UserSkillData
{
    public List<UserEquipSkillData> UserEquipSkill;
    public List<UserInvenSkillData> UserInvenSkill;
}

[System.Serializable]
public class UserEquipSkillData
{
    public string itemID;
}

[System.Serializable]
public class UserInvenSkillData
{
    public string itemID;
    public int level;
    public int hasCount;
    public bool equipped;
}