using UnityEngine;

public class WindowsInputController : InputController
{
    public VirtualTouchInjector TouchInjector;

    protected override void HandleInputAction(InputActionData _inputData)
    {
        InputType _type = _inputData.Type;
        Vector2 _cursorPosition = _inputData.CursorPosition;

        switch (_type)
        {
            case InputType.DOWN:
                TouchInjector.MoveTo((int)_cursorPosition.x, GlobalSettings.ScreenHeight - (int)_cursorPosition.y);
                TouchInjector.TouchDown();
                break;
            case InputType.HOLD:
            case InputType.DRAG:
                TouchInjector.MoveTo((int)_cursorPosition.x, GlobalSettings.ScreenHeight - (int)_cursorPosition.y);
                TouchInjector.TouchHold();
                break;
            case InputType.UP:
                TouchInjector.MoveTo((int)_cursorPosition.x, GlobalSettings.ScreenHeight - (int)_cursorPosition.y);
                TouchInjector.TouchUp();
                break;
            case InputType.HOVER:
                TouchInjector.MoveTo((int)_cursorPosition.x, GlobalSettings.ScreenHeight - (int)_cursorPosition.y);
                TouchInjector.TouchHover();
                break;
            case InputType.CANCEL:
                TouchInjector.TouchCancel();
                break;
        }
    }
}