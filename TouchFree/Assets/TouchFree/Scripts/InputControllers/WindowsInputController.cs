using UnityEngine;
using Ultraleap.ScreenControl.Client.InputControllers;
using Ultraleap.ScreenControl.Client.ScreenControlTypes;

public class WindowsInputController : InputController
{
    public VirtualTouchInjector TouchInjector;

    protected override void HandleInputAction(ClientInputAction _inputData)
    {
        InputType _type = _inputData.InputType;
        Vector2 _cursorPosition = _inputData.CursorPosition;

        switch (_type)
        {
            case InputType.MOVE:
                TouchInjector.MoveTo((int)_cursorPosition.x, GlobalSettings.ScreenHeight - (int)_cursorPosition.y);
                TouchInjector.TouchHold();
                break;
            case InputType.DOWN:
                TouchInjector.MoveTo((int)_cursorPosition.x, GlobalSettings.ScreenHeight - (int)_cursorPosition.y);
                TouchInjector.TouchDown();
                break;
            case InputType.UP:
                TouchInjector.MoveTo((int)_cursorPosition.x, GlobalSettings.ScreenHeight - (int)_cursorPosition.y);
                TouchInjector.TouchUp();
                break;
            case InputType.CANCEL:
                TouchInjector.TouchCancel();
                break;
        }
    }
}