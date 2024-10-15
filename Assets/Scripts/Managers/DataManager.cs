using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class DataManager
{
    #region Fields

    private string idToken;

    private UITopScene topScene;

    #endregion

    public GameUserProfile Profile { get; private set; }
    public InventoryData Inventory { get; private set; }
    public List<UserItemData> WeaponInvenList = new();
    public List<UserItemData> ArmorInvenList = new();
    public Dictionary<string, UserItemData> WeaponInvenDictionary = new();
    public Dictionary<string, UserItemData> ArmorInvenDictionary = new();

    public UserSkillData UserSkillData { get; private set; }
    public List<UserInvenSkillData> SkillInvenList = new();
    public Dictionary<string, UserInvenSkillData> SkillInvenDictionary = new();
    public UserFollowerData FollowerData { get; private set; }
    public List<UserInvenFollowerData> FollowerInvenList = new();
    public Dictionary<string, UserInvenFollowerData> FollowerInvenDictionary = new();

    #region Create

    /// <summary>
    /// 게스트 로그인, 로컬 데이터 생성
    /// </summary>
    private void CreateUserLocalFile()
    {
        Profile = new()
        {
            Uid = Utilities.GenerateID(),
            Nickname = $"Guest-Test",
            Date_Login = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
            Date_Logout = string.Empty,
            Date_Idle_ClickTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
            Date_Bonus_ClickTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
            Date_Bonus_Check = false,
            Gold = 0,
            Gems = 0,
            Stat_Level_AtkDamage = 1,
            Stat_Level_AtkSpeed = 1,
            Stat_Level_CritChance = 1,
            Stat_Level_CritDamage = 1,
            Stat_Level_Hp = 1,
            Stat_Level_HpRecovery = 1,
            Stage_Chapter = 1,
            Stage_Level = 0,
            Stage_WaveLoop = false,
            Quest_Complete = 0,
            Quest_Current_Progress = 0,
            Summon_Progress_Equipment = 0,
            Summon_Progress_Skills = 0,
            Summon_Progress_Follower = 0
        };

        SaveToUserProfile();
    }

    /// <summary>
    /// 첫 로그인 시, 기본 데이터 부여
    /// </summary>
    public GameUserProfile CreateUserServerFile()
    {
        Profile = new()
        {
            Uid = idToken,
            Nickname = $"Guest-{idToken}",
            Date_Login = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
            Date_Logout = string.Empty,
            Date_Idle_ClickTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
            Date_Bonus_ClickTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss"),
            Date_Bonus_Check = false,
            Gold = 0,
            Gems = 0,
            Stat_Level_AtkDamage = 1,
            Stat_Level_AtkSpeed = 1,
            Stat_Level_CritChance = 1,
            Stat_Level_CritDamage = 1,
            Stat_Level_Hp = 1,
            Stat_Level_HpRecovery = 1,
            Stage_Chapter = 1,
            Stage_Level = 0,
            Stage_WaveLoop = false,
            Quest_Complete = 0,
            Quest_Current_Progress = 0,
            Summon_Progress_Equipment = 0,
            Summon_Progress_Skills = 0,
            Summon_Progress_Follower = 0
        };

        return Profile;
    }

    public void CreateUserEquipment()
    {
        var jsonData = Manager.Asset.GetText("UserTableEquipment");
        Inventory = JsonUtility.FromJson<InventoryData>(jsonData);

        SaveToUserEquipment();
    }

    public void CreateUserSkill()
    {
        var jsonData = Manager.Asset.GetText("UserTableSkill");
        UserSkillData = JsonUtility.FromJson<UserSkillData>(jsonData);

        SaveToUserSkill();
    }

    public void CreateUserFollower()
    {
        var jsonData = Manager.Asset.GetText("UserTableFollower");
        FollowerData = JsonUtility.FromJson<UserFollowerData>(jsonData);

        SaveToUserFollower();
    }

    #endregion

    #region Load

    public void Load()
    {
        LoadFromUserProfile();
        LoadFromUserEquipment();
        LoadFromUserSkill();
        LoadFromUserFollower();

        topScene = GameObject.FindObjectOfType<UITopScene>();
        topScene.DataLoadFinished();
    }

    public void LoadFromUserProfile(string fileName = "game_user.dat")
    {
        string filePath = $"{Application.persistentDataPath}/{fileName}";
        if (!File.Exists(filePath)) { CreateUserLocalFile(); return; }
        string jsonRaw = File.ReadAllText(filePath);
        Profile = JsonConvert.DeserializeObject<GameUserProfile>(jsonRaw);
    }

    public void LoadFromUserEquipment(string fileName = "game_equipment.dat")
    {
        string filePath = $"{Application.persistentDataPath}/{fileName}";
        if (!File.Exists(filePath)) { CreateUserEquipment(); }
        else
        {
            string jsonRaw = File.ReadAllText(filePath);
            Inventory = JsonConvert.DeserializeObject<InventoryData>(jsonRaw);
        }

        foreach (var item in Inventory.UserItemData)
        {
            if (item.itemID[0] == 'W')
            {
                WeaponInvenDictionary.Add(item.itemID, item);
                WeaponInvenList.Add(item);
            }

            else
            {
                ArmorInvenDictionary.Add(item.itemID, item);
                ArmorInvenList.Add(item);
            }
        }

        int _levelOverCount = 0;
        foreach (var item in WeaponInvenList)
        {
            item.hasCount += _levelOverCount;

            if (item.level > 100)
            {
                _levelOverCount = item.level - 100;
                item.level = 100;
            }
            else
            {
                _levelOverCount = 0;
            }
        }

        _levelOverCount = 0;
        foreach (var item in ArmorInvenList)
        {
            item.hasCount += _levelOverCount;

            if (item.level > 100)
            {
                _levelOverCount = item.level - 100;
                item.level = 100;
            }
            else
            {
                _levelOverCount = 0;
            }
        }
    }

    public void LoadFromUserSkill(string fileName = "game_skill.dat")
    {
        string filePath = $"{Application.persistentDataPath}/{fileName}";
        if (!File.Exists(filePath)) { CreateUserSkill(); }
        else
        {
            string jsonRaw = File.ReadAllText(filePath);
            UserSkillData = JsonConvert.DeserializeObject<UserSkillData>(jsonRaw);
        }

        int _levelOverCount = 0;

        foreach (var item in UserSkillData.UserInvenSkill)
        {
            SkillInvenDictionary.Add(item.itemID, item);
            SkillInvenList.Add(item);

            item.hasCount += _levelOverCount;

            if (item.level > 100)
            {
                _levelOverCount = item.level - 100;
                item.level = 100;
            }
            else
            {
                _levelOverCount = 0;
            }
        }
    }

    public void LoadFromUserFollower(string fileName = "game_follower.dat")
    {
        string filePath = $"{Application.persistentDataPath}/{fileName}";
        if (!File.Exists(filePath)) { CreateUserFollower(); }
        else
        {
            string jsonRaw = File.ReadAllText(filePath);
            FollowerData = JsonConvert.DeserializeObject<UserFollowerData>(jsonRaw);
        }

        int _levelOverCount = 0;

        foreach (var item in FollowerData.UserInvenFollower)
        {
            FollowerInvenDictionary.Add(item.itemID, item);
            FollowerInvenList.Add(item);

            item.hasCount += _levelOverCount;

            if (item.level > 100)
            {
                _levelOverCount = item.level - 100;
                item.level = 100;
            }
            else
            {
                _levelOverCount = 0;
            }
        }
    }

    #endregion

    #region Save

    public void Save()
    {
        SaveProfile();
        SaveToUserProfile();
        SaveToUserEquipment();
        SaveToUserSkill();
        SaveToUserFollower();
    }

    private void SaveProfile()
    {
        Profile.Date_Logout = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
        Profile.Date_Idle_ClickTime = Manager.Game.Player.IdleCheckTime.ToString("yyyy/MM/dd HH:mm:ss");
        Profile.Date_Bonus_ClickTime = Manager.Game.Player.BonusCheckTime.ToString("yyyy/MM/dd HH:mm:ss");
        Profile.Date_Bonus_Check = Manager.Game.Player.IsBonusCheck;
        Profile.Gold = Manager.Game.Player.Gold;
        Profile.Gems = Manager.Game.Player.Gems;
        Profile.Stat_Level_AtkDamage = Manager.Game.Player.AtkDamage.Level;
        Profile.Stat_Level_AtkSpeed = Manager.Game.Player.AtkSpeed.Level;
        Profile.Stat_Level_CritChance = Manager.Game.Player.CritChance.Level;
        Profile.Stat_Level_CritDamage = Manager.Game.Player.CritDamage.Level;
        Profile.Stat_Level_Hp = Manager.Game.Player.Hp.Level;
        Profile.Stat_Level_HpRecovery = Manager.Game.Player.HpRecovery.Level;
        Profile.Stage_Chapter = Manager.Stage.Chapter;
        Profile.Stage_Level = Manager.Stage.StageLevel;
        Profile.Stage_WaveLoop = Manager.Stage.WaveLoop;
        Profile.Quest_Complete = Manager.Quest.QuestNum;
        Profile.Quest_Current_Progress = Manager.Quest.DefeatQuestValue;
        Profile.Summon_Progress_Equipment = Manager.Summon.GetSummonCounts("Equipment");
        Profile.Summon_Progress_Skills = Manager.Summon.GetSummonCounts("Skills");
        Profile.Summon_Progress_Follower = Manager.Summon.GetSummonCounts("Follower");
    }

    public void SaveToUserProfile(string fileName = "game_user.dat")
    {
        string filePath = $"{Application.persistentDataPath}/{fileName}";
        string json = JsonConvert.SerializeObject(Profile, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    public void SaveToUserEquipment(string fileName = "game_equipment.dat")
    {
        string filePath = $"{Application.persistentDataPath}/{fileName}";
        string json = JsonConvert.SerializeObject(Inventory, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    public void SaveToUserSkill(string fileName = "game_skill.dat")
    {
        string filePath = $"{Application.persistentDataPath}/{fileName}";
        string json = JsonConvert.SerializeObject(UserSkillData, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    public void SaveToUserFollower(string fileName = "game_follower.dat")
    {
        string filePath = $"{Application.persistentDataPath}/{fileName}";
        string json = JsonConvert.SerializeObject(FollowerData, Formatting.Indented);
        File.WriteAllText(filePath, json);
    }

    #endregion
}
