using System;
using System.Collections.Generic;

[Serializable]
public class Serialization<T>
{
    public Serialization(List<T> _target) => target = _target;
    public List<T> target;
}

public class GameUserProfile
{
    // 사용자 정보
    public string Uid { get; set; }
    public string Nickname { get; set; }
    public long Gold { get; set; }
    public int Gems { get; set; }

    // 로그인, 로그아웃 시간
    public string Date_Login { get; set; }
    public string Date_Logout { get; set; }
    public string Date_Idle_ClickTime { get; set; }
    public string Date_Bonus_ClickTime { get; set; }
    public bool Date_Bonus_Check { get; set; }

    // 사용자 스탯
    public int Stat_Level_AtkDamage { get; set; }
    public int Stat_Level_AtkSpeed { get; set; }
    public int Stat_Level_CritChance { get; set; }
    public int Stat_Level_CritDamage { get; set; }
    public int Stat_Level_Hp { get; set; }
    public int Stat_Level_HpRecovery { get; set; }

    // 스테이지 정보
    public int Stage_Chapter { get; set; }
    public int Stage_Level { get; set; }
    public bool Stage_WaveLoop { get; set; }

    // 퀘스트 정보
    public int Quest_Complete { get; set; }
    public int Quest_Current_Progress { get; set; }

    // 소환 정보
    public int Summon_Progress_Equipment { get; set; }
    public int Summon_Progress_Skills { get; set; }
    public int Summon_Progress_Follower { get; set; }
}
