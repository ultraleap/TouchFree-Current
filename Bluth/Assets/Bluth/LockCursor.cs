using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LockCursor : MonoBehaviour
{
    public RectTransform cursorCanvas;
    
    public TransparentWindow window;

    public float screenScaler = 2;

    private Cursor _cursor;
    private Vector2 _position;

    void Start()
    {
        _position = new Vector2(GlobalSettings.CursorWindowSize / 2, GlobalSettings.CursorWindowSize / 2);
        screenScaler = GlobalSettings.ScreenHeight / GlobalSettings.CursorWindowSize;
    }

    void LateUpdate()
    {
        if (_cursor == null)
        {
            _cursor = cursorCanvas.GetComponentInChildren<Cursor>();
        }

        if (window.clickThroughEnabled)
        {
            _cursor.OverridePosition(true, _position);
            window.SetPosition(_cursor.TargetPosition());
            _cursor.SetScreenScale(screenScaler);
        }
        else
        {
            _cursor.OverridePosition(false, Vector3.zero);
            _cursor.SetScreenScale(1);
        }
    }
}