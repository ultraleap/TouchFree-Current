﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class TransparentWindow : MonoBehaviour
{
	private struct MARGINS
	{
		public int cxLeftWidth;
		public int cxRightWidth;
		public int cyTopHeight;
		public int cyBottomHeight;
	}

	[DllImport("user32.dll")]
	private static extern IntPtr GetActiveWindow();

	[DllImport("user32.dll")]
	private static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

    [DllImport("user32.dll")]
    private static extern uint SetWindowLong(IntPtr hWnd, int nIndex, long dwNewLong);

    [DllImport("user32.dll")]
    private static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("user32.dll", EntryPoint = "SetLayeredWindowAttributes")]
	static extern int SetLayeredWindowAttributes(IntPtr hwnd, int crKey, byte bAlpha, int dwFlags);

	[DllImport("user32.dll", EntryPoint = "SetWindowPos")]
	private static extern int SetWindowPos(IntPtr hwnd, int hwndInsertAfter, int x, int y, int cx, int cy, int uFlags);

	[DllImport("Dwmapi.dll")]
	private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

	const int GWL_STYLE = -16;
	const uint WS_POPUP = 0x80000000;
	const uint WS_VISIBLE = 0x10000000;
	const int HWND_TOPMOST = -1;

    public const uint WS_EX_LAYERED = 0x00080000;
    public const uint WS_EX_TRANSPARENT = 0x00000020;

    public IntPtr hwnd;

    private Vector2 position;

    public bool clickThroughEnabled = false;

    void Start()
	{
#if !UNITY_EDITOR // You really don't want to enable this in the editor..
		hwnd = GetActiveWindow();
#endif
        SetCursorWindow(true);
    }

    public void DisableClickThrough()
    {
#if !UNITY_EDITOR
        if (!clickThroughEnabled)
        {    return;
        }
        clickThroughEnabled = false;
        SetConfigWindow(true);
#endif
    }

    public void EnableClickThrough()
    {
#if !UNITY_EDITOR
        if (clickThroughEnabled || CallToInteractController.IsShowing)
        {            return;
        }
        clickThroughEnabled = true;
        SetCursorWindow(true);
#endif
    }

    private void OnEnable()
    {
        UIManager.Instance.UIActivated += DisableClickThrough;
        UIManager.Instance.UIDeactivated += EnableClickThrough;

        CallToInteractController.OnCTIActive += CTIActivated;
        CallToInteractController.OnCTIInactive += CTIDeactivated;
    }

    private void OnDisable()
    {
        UIManager.Instance.UIActivated -= DisableClickThrough;
        UIManager.Instance.UIDeactivated -= EnableClickThrough;

        CallToInteractController.OnCTIActive -= CTIActivated;
        CallToInteractController.OnCTIInactive -= CTIDeactivated;
    }

    void SetCursorWindow(bool setResolution)
    {
        if (setResolution)
        {
            Screen.SetResolution(TouchFreeMain.CursorWindowSize, TouchFreeMain.CursorWindowSize, FullScreenMode.Windowed);
        }

        SetWindowLong(hwnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);
        SetLayeredWindowAttributes(hwnd, 0, 255, 2);// Transparency=51=20%, LWA_ALPHA=2
        SetWindowPos(hwnd, HWND_TOPMOST, Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), TouchFreeMain.CursorWindowSize, TouchFreeMain.CursorWindowSize, 32 | 64);//SWP_FRAMECHANGED = 0x0020 (32); //SWP_SHOWWINDOW = 0x0040 (64)s

        var margins = new MARGINS() { cxLeftWidth = -1 };
        SetWindowLong(hwnd, -20, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        SetWindowPos(hwnd, HWND_TOPMOST, Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), TouchFreeMain.CursorWindowSize, TouchFreeMain.CursorWindowSize, 32 | 64);//SWP_FRAMECHANGED = 0x0020 (32); //SWP_SHOWWINDOW = 0x0040 (64)s

        DwmExtendFrameIntoClientArea(hwnd, ref margins);
    }

    void SetConfigWindow(bool setResolution)
    {
        if (setResolution)
        {
            Screen.SetResolution(Display.main.systemWidth, Display.main.systemHeight, FullScreenMode.Windowed);
        }

        SetWindowLong(hwnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);
        SetLayeredWindowAttributes(hwnd, 0, 255, 2);// Transparency=51=20%, LWA_ALPHA=2
        SetWindowPos(hwnd, -2, 0, 0, Display.main.systemWidth, Display.main.systemHeight, 32 | 64);//SWP_FRAMECHANGED = 0x0020 (32); //SWP_SHOWWINDOW = 0x0040 (64)

        var margins = new MARGINS();

        // get the current -20 window and remove the layerd and transparent parts
        long style = GetWindowLong(hwnd, -20);
        style &= ~WS_EX_TRANSPARENT;
        style &= ~WS_EX_LAYERED;

        SetWindowLong(hwnd, -20, style);
        SetWindowPos(hwnd, -2, 0, 0, Display.main.systemWidth, Display.main.systemHeight, 32 | 64);//SWP_FRAMECHANGED = 0x0020 (32); //SWP_SHOWWINDOW = 0x0040 (64)

        DwmExtendFrameIntoClientArea(hwnd, ref margins);
    }

    void Update()
    {
#if !UNITY_EDITOR
		if (clickThroughEnabled)
		{
            int xPos = Mathf.RoundToInt(position.x);
            int yPos = Mathf.RoundToInt(position.y);
            if (xPos + TouchFreeMain.CursorWindowSize > 0 && xPos < Display.main.systemWidth && yPos + TouchFreeMain.CursorWindowSize > 0 && yPos < Display.main.systemHeight)
            {
                SetWindowPos(hwnd,
                    HWND_TOPMOST,
                    Mathf.RoundToInt(position.x),
                    Mathf.RoundToInt(position.y),
                    TouchFreeMain.CursorWindowSize,
                    TouchFreeMain.CursorWindowSize,
                    32 | 64);
            }
		}
#endif
    }

    public void SetPosition(Vector2 value)
    {
        position = value;

        position.x = position.x - (TouchFreeMain.CursorWindowSize/2);
        position.y = Display.main.systemHeight - position.y - (TouchFreeMain.CursorWindowSize/2);
    }
    
    void CTIActivated()
    {
        DisableClickThrough();
    }

    void CTIDeactivated()
    {
        if (!UIManager.Instance.isActive)
        {
            EnableClickThrough();
        }
    }

    private void OnApplicationQuit()
    {
        SetConfigWindow(true);
    }
}