using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ultraleap.ScreenControl.Client;

public class LockCursor : MonoBehaviour
{
    public RectTransform cursorCanvas;
    
    public TransparentWindow window;

    private TouchFreeCursor _cursor;
    private Vector2 _position;

    void Start()
    {
        _position = new Vector2(GlobalSettings.CursorWindowSize / 2, GlobalSettings.CursorWindowSize / 2);
    }

    void LateUpdate()
    {
        if (_cursor == null)
        {
            _cursor = cursorCanvas.GetComponentInChildren<TouchFreeCursor>();
        }

        if (window.clickThroughEnabled)
        {
            _cursor.OverridePosition(true, _position);
            window.SetPosition(_cursor.TargetPosition());
        }
        else
        {
            _cursor.OverridePosition(false, Vector3.zero);
        }
    }
}