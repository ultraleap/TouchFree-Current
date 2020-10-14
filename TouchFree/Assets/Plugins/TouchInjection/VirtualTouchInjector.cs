using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TCD.System.TouchInjection;

// Makes use of a dll from NuGet package: https://www.nuget.org/packages/TCD.System.TouchInjection/

// We may want to convert to HIMETRIC coordinates https://docs.microsoft.com/en-us/cpp/atl/reference/pixel-himetric-conversion-global-functions?view=vs-2019

public class VirtualTouchInjector : MonoBehaviour
{
    PointerTouchInfo[] touches;
    PointerTouchInfo touchInfo;
    PointerInfo pointerInfo;

    public int contactX;
    public int contactY;

    private int downX;
    private int downY;

    private int upX;
    private int upY;

    int counter;
    bool updated;
    bool touchDown;

    // Start is called before the first frame update
    void Start()
    {
        TouchInjector.InitializeTouchInjection(10, TouchFeedback.NONE);
        touches = new PointerTouchInfo[1];
        touchInfo = new PointerTouchInfo();
        pointerInfo = new PointerInfo();

        touchInfo.TouchFlags = TouchFlags.NONE;
        touchInfo.TouchMasks = TouchMask.NONE;
        
        pointerInfo.pointerType = PointerInputType.TOUCH;
        pointerInfo.PointerId = 0;
        pointerInfo.PointerFlags = PointerFlags.INRANGE | PointerFlags.UPDATE;

        touchInfo.PointerInfo = pointerInfo;
    }

    void LateUpdate()
    {
        updated = false;
    }

    public void MoveTo(int x, int y)
    {
        contactX = x;
        contactY = y;
    }

    public bool TouchDown()
    {
        if (updated) return false;
        updated = true;
        touchDown = true;

        pointerInfo.PointerFlags = PointerFlags.INRANGE | PointerFlags.DOWN | PointerFlags.INCONTACT;
        pointerInfo.PtPixelLocation.X = contactX;
        pointerInfo.PtPixelLocation.Y = contactY;
        downX = contactX;
        downY = contactY;

        touchInfo.PointerInfo = pointerInfo;
        touches[0] = touchInfo;

        return TouchInjector.InjectTouchInput(touches.Length, touches);
    }

    public bool TouchHold()
    {
        if (updated) return false;
        updated = true;

        pointerInfo.PointerFlags = PointerFlags.INRANGE | PointerFlags.UPDATE | PointerFlags.INCONTACT;
        if (touchDown)
        {
            pointerInfo.PtPixelLocation.X = downX;
            pointerInfo.PtPixelLocation.Y = downY;
            touchDown = false;
        }
        else
        {
            pointerInfo.PtPixelLocation.X = contactX;
            pointerInfo.PtPixelLocation.Y = contactY;
        }

        upX = pointerInfo.PtPixelLocation.X;
        upY = pointerInfo.PtPixelLocation.Y;

        touchInfo.PointerInfo = pointerInfo;
        touches[0] = touchInfo;

        return TouchInjector.InjectTouchInput(touches.Length, touches);
    }

    public bool TouchUp()
    {
        if (updated) return false;
        updated = true;

        pointerInfo.PointerFlags = PointerFlags.INRANGE | PointerFlags.UP;
        pointerInfo.PtPixelLocation.X = upX;
        pointerInfo.PtPixelLocation.Y = upY;
        
        touchInfo.PointerInfo = pointerInfo;
        touches[0] = touchInfo;
        
        return TouchInjector.InjectTouchInput(touches.Length, touches);
    }

    public bool TouchHover()
    {
        if (updated) return false;
        updated = true;

        pointerInfo.PointerFlags = PointerFlags.INRANGE | PointerFlags.UPDATE;
        pointerInfo.PtPixelLocation.X = contactX;
        pointerInfo.PtPixelLocation.Y = contactY;
        
        touchInfo.PointerInfo = pointerInfo;
        touches[0] = touchInfo;

        return TouchInjector.InjectTouchInput(touches.Length, touches);
    }

    public bool TouchCancel()
    {
        pointerInfo.PointerFlags = PointerFlags.CANCELLED | PointerFlags.UPDATE;

        touchInfo.PointerInfo = pointerInfo;

        touches[0] = touchInfo;
        return TouchInjector.InjectTouchInput(touches.Length, touches);
    }
}
