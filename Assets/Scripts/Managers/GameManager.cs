using UnityEngine;

public class GameManager
{
    #region Properties

    public Player Player { get; private set; }
    public MainScene Main { get; private set; }

    public float screenRatio { get; private set; } =  Screen.height / Screen.width * 0.1f;

    #endregion

    #region Init

    /// <summary>
    /// 데이터 동기화 후 => 게임 세팅
    /// </summary>
    public void Initialize()
    {
        var playerClone = Manager.Asset.InstantiatePrefab("PlayerFrame");
        Player = playerClone.GetComponent<Player>();

        Manager.ObjectPool.Initialize();
        Manager.Stage.Initialize();
        Manager.Summon.Initialize();
        Manager.Inventory.Initialize();

        Main = GameObject.FindObjectOfType<MainScene>();
        Main.SceneStart();

        Manager.Quest.InitQuest();
        Manager.SysAlert.InitAlert();
    }

    // 플레이어 데이터 초기화
    public void PlayerInit(Vector2 position)
    {
        Vector2 positionRatio = new Vector2(screenRatio, -screenRatio);
        Vector2 scaleRatio = new Vector2(screenRatio, screenRatio);

        Player.transform.position = position;
        Player.Initialize();
    }

    #endregion
}
