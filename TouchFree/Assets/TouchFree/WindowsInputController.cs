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
                //TouchInjector.MoveTo((int)_cursorPosition.x, GlobalSettings.ScreenHeight - (int)_cursorPosition.y);
                //TouchInjector.TouchDown();
                MouseOperations.SetCursorPosition((int)_cursorPosition.x, GlobalSettings.ScreenHeight - (int)_cursorPosition.y);
                MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftDown);
                break;
            case InputType.HOLD:
            case InputType.DRAG:
                //TouchInjector.MoveTo((int)_cursorPosition.x, GlobalSettings.ScreenHeight - (int)_cursorPosition.y);
                //TouchInjector.TouchHold();
                MouseOperations.SetCursorPosition((int)_cursorPosition.x, GlobalSettings.ScreenHeight - (int)_cursorPosition.y);
                break;
            case InputType.UP:
                //TouchInjector.MoveTo((int)_cursorPosition.x, GlobalSettings.ScreenHeight - (int)_cursorPosition.y);
                //TouchInjector.TouchUp();
                MouseOperations.SetCursorPosition((int)_cursorPosition.x, GlobalSettings.ScreenHeight - (int)_cursorPosition.y);
                MouseOperations.MouseEvent(MouseOperations.MouseEventFlags.LeftUp);
                break;
            case InputType.HOVER:
                //TouchInjector.MoveTo((int)_cursorPosition.x, GlobalSettings.ScreenHeight - (int)_cursorPosition.y);
                //TouchInjector.TouchHover();
                MouseOperations.SetCursorPosition((int)_cursorPosition.x, GlobalSettings.ScreenHeight - (int)_cursorPosition.y);
                break;
            case InputType.CANCEL:
                //TouchInjector.TouchCancel();
                break;
        }
    }
}