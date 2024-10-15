using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIPopupOptions : UIPopup
{
    #region Initialize

    protected override void Init()
    {
        base.Init();
        SetButtonEvents();
    }

    private void SetButtonEvents()
    {
        SetUI<Button>();
        SetButtonEvent("Option_Setting_Btn", UIEventType.Click, OnSettingPopup);
        SetButtonEvent("Option_Quit_Btn", UIEventType.Click, ShowQuitPopup);

        SetButtonEvent("Btn_Close", UIEventType.Click, ClosePopup);
        SetButtonEvent("DimScreen", UIEventType.Click, ClosePopup);
    }

    #endregion

    #region Button Events

    private void OnSettingPopup(PointerEventData eventData) => Manager.UI.ShowPopup<UIPopupSettings>();

    private void ShowQuitPopup(PointerEventData eventData)
    {
        var alertPopup = Manager.UI.ShowPopup<UIPopupSystemAlert>();
        alertPopup.SetData(PopupAlertType.ApplicationQuit, GameQuit);
    }

    private void GameQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void ClosePopup(PointerEventData eventData)
    {
        Manager.UI.ClosePopup();
    }

#endregion
}
