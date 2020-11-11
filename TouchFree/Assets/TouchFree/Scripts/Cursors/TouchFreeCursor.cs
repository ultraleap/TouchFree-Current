using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Ultraleap.ScreenControl.Client;

public class TouchFreeCursor : DotCursor
{
    bool positionOverride;
    Vector2 overridePosition;

    protected override void Update()
    {
        base.Update();
        cursorTransform.anchoredPosition = positionOverride ? overridePosition : _targetPos;
    }

    public void OverridePosition(bool _active, Vector2 _position)
    {
        positionOverride = _active;
        overridePosition = _position;
    }

    public Vector2 TargetPosition()
    {
        return _targetPos;
    }
}