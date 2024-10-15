using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIBase : MonoBehaviour
{
    #region Fields

    private readonly Dictionary<Type, Dictionary<string, UnityEngine.Object>> _objects = new();

    #endregion

    #region Init

    private void Start()
    {
        Init();
    }

    protected virtual void Init() { }

    #endregion

    #region Properties

    protected void SetUI<T>() where T : UnityEngine.Object => Binding<T>(gameObject);
    protected T GetUI<T>(string componentName) where T : UnityEngine.Object => GetComponent<T>(componentName);

    #endregion

    #region Binding

    public void Binding<T>(GameObject parent) where T : UnityEngine.Object
    {
        T[] objects = parent.GetComponentsInChildren<T>(true);

        Dictionary<string, UnityEngine.Object> objectDict = objects
            .GroupBy(comp => comp.name)
            .ToDictionary(group => group.Key, group => group.First() as UnityEngine.Object);

        _objects[typeof(T)] = objectDict;
        AssignComponentsDirectChild<T>(parent);
    }

    private void AssignComponentsDirectChild<T>(GameObject parent) where T : UnityEngine.Object
    {
        if (!_objects.TryGetValue(typeof(T), out var objects)) return;

        foreach (var key in objects.Keys.ToList())
        {
            if (objects[key] != null) continue;

            UnityEngine.Object component = typeof(T) == typeof(GameObject)
                ? FindComponentDirectChild<GameObject>(parent, key)
                : FindComponentDirectChild<T>(parent, key);

            if (component != null)
            {
                objects[key] = component;
            }
            else
            {
                Debug.Log($"Binding failed for Object : {key}");
            }
        }
    }


    // 직계 자식 중에서 이름이 일치하는 컴포넌트 반환
    private T FindComponentDirectChild<T>(GameObject parent, string name) where T : UnityEngine.Object
    {
        return parent.transform
            .Cast<Transform>()
            .FirstOrDefault(child => child.name == name)
            ?.GetComponent<T>();
    }


    // 딕셔너리에서 이름에 해당하는 컴포넌트 반환
    public T GetComponent<T>(string componentName) where T : UnityEngine.Object
    {
        if (_objects.TryGetValue(typeof(T), out var components) && components.TryGetValue(componentName, out var component))
        {
            return component as T;
        }

        return null;
    }

    #endregion

    #region Action Binding


    // 버튼에 함수 연결
    protected Button SetButtonEvent(string buttonName, UIEventType uIEventType, Action<PointerEventData> action)
    {
        Button button = GetUI<Button>(buttonName);
        button.gameObject.SetEvent(uIEventType, action);
        return button;
    }

    #endregion
}
