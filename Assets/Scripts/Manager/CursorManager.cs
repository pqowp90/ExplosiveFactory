using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SingletonLifeTime(LifeTime.Application)]
public class CursorManager : MonoSingleton<CursorManager>
{
    public struct CursorData
    {
        public CursorType CursorType;
        public object Source;
    }
    private List<CursorData> CursorStack = new();
    public CursorType CurrentCursor => CursorStack.Count > 0 ? CursorStack[^1].CursorType : CursorType.UI;
    protected override void Awake()
    {
        base.Awake();
        SetCursor(CursorType.UI, this);
    }
    public void SetCursor(CursorType cursorType, object obj = null)
    {
        CursorStack.Add(new CursorData { CursorType = cursorType, Source = obj });
        ApplyCursor(cursorType);
    }
    public void UnsetCursorFromSource(object obj)
    {
        if (obj != null)
        {
            for (int i = 0; i < CursorStack.Count; i++)
            {
                if (CursorStack[i].Source == obj)
                {
                    CursorStack.RemoveAt(i);
                    i--;
                }
            }
        }
        else
        {
            Debug.LogError("UnsetCursorFromSource is called with null object");
        }

        CursorType nextCursor = CursorStack.Count > 0 ? CursorStack[^1].CursorType : CursorType.UI;
        ApplyCursor(nextCursor);
    }
    private void ApplyCursor(CursorType cursorType)
    {
        switch (cursorType)
        {
            case CursorType.Player:

                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
                break;
            case CursorType.UI:

                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
                break;
        }
    }
}

public enum CursorType
{
    Player,
    UI
}
