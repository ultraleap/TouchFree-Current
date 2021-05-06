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

    [DllImport("user32.dll")]
	static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

	[DllImport("user32.dll", EntryPoint = "SetLayeredWindowAttributes")]
	static extern int SetLayeredWindowAttributes(IntPtr hwnd, int crKey, byte bAlpha, int dwFlags);

	[DllImport("user32.dll", EntryPoint = "SetWindowPos")]
	private static extern int SetWindowPos(IntPtr hwnd, int hwndInsertAfter, int x, int y, int cx, int cy, int uFlags);

	[DllImport("Dwmapi.dll")]
	private static extern uint DwmExtendFrameIntoClientArea(IntPtr hWnd, ref MARGINS margins);

    [Flags]
    public enum SetWindowPosFlags : uint
    {
        // ReSharper disable InconsistentNaming

        /// <summary>
        ///     If the calling thread and the thread that owns the window are attached to different input queues, the system posts the request to the thread that owns the window. This prevents the calling thread from blocking its execution while other threads process the request.
        /// </summary>
        SWP_ASYNCWINDOWPOS = 0x4000,

        /// <summary>
        ///     Prevents generation of the WM_SYNCPAINT message.
        /// </summary>
        SWP_DEFERERASE = 0x2000,

        /// <summary>
        ///     Draws a frame (defined in the window's class description) around the window.
        /// </summary>
        SWP_DRAWFRAME = 0x0020,

        /// <summary>
        ///     Applies new frame styles set using the SetWindowLong function. Sends a WM_NCCALCSIZE message to the window, even if the window's size is not being changed. If this flag is not specified, WM_NCCALCSIZE is sent only when the window's size is being changed.
        /// </summary>
        SWP_FRAMECHANGED = 0x0020,

        /// <summary>
        ///     Hides the window.
        /// </summary>
        SWP_HIDEWINDOW = 0x0080,

        /// <summary>
        ///     Does not activate the window. If this flag is not set, the window is activated and moved to the top of either the topmost or non-topmost group (depending on the setting of the hWndInsertAfter parameter).
        /// </summary>
        SWP_NOACTIVATE = 0x0010,

        /// <summary>
        ///     Discards the entire contents of the client area. If this flag is not specified, the valid contents of the client area are saved and copied back into the client area after the window is sized or repositioned.
        /// </summary>
        SWP_NOCOPYBITS = 0x0100,

        /// <summary>
        ///     Retains the current position (ignores X and Y parameters).
        /// </summary>
        SWP_NOMOVE = 0x0002,

        /// <summary>
        ///     Does not change the owner window's position in the Z order.
        /// </summary>
        SWP_NOOWNERZORDER = 0x0200,

        /// <summary>
        ///     Does not redraw changes. If this flag is set, no repainting of any kind occurs. This applies to the client area, the nonclient area (including the title bar and scroll bars), and any part of the parent window uncovered as a result of the window being moved. When this flag is set, the application must explicitly invalidate or redraw any parts of the window and parent window that need redrawing.
        /// </summary>
        SWP_NOREDRAW = 0x0008,

        /// <summary>
        ///     Same as the SWP_NOOWNERZORDER flag.
        /// </summary>
        SWP_NOREPOSITION = 0x0200,

        /// <summary>
        ///     Prevents the window from receiving the WM_WINDOWPOSCHANGING message.
        /// </summary>
        SWP_NOSENDCHANGING = 0x0400,

        /// <summary>
        ///     Retains the current size (ignores the cx and cy parameters).
        /// </summary>
        SWP_NOSIZE = 0x0001,

        /// <summary>
        ///     Retains the current Z order (ignores the hWndInsertAfter parameter).
        /// </summary>
        SWP_NOZORDER = 0x0004,

        /// <summary>
        ///     Displays the window.
        /// </summary>
        SWP_SHOWWINDOW = 0x0040,
    }

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
        StartCoroutine(EnableDisableChecker());
        SetCursorWindow(true);
    }

    public void DisableClickThrough()
    {
        if (enableTimer != -100)
        {
            enablers.Add(false);
            return;
        }

#if !UNITY_EDITOR
        if (!clickThroughEnabled)
            return;

        clickThroughEnabled = false;
        SetConfigWindow(true);
#endif
    }

    public void EnableClickThrough()
    {
        if (enableTimer != -100)
        {
            enablers.Add(true);
            return;
        }

#if !UNITY_EDITOR
        if (clickThroughEnabled || CallToInteractController.IsShowing)
            return;

        clickThroughEnabled = true;
        SetCursorWindow(true);
#endif
    }

    private void OnEnable()
    {
        ConfigurationSetupController.OnConfigActive += DisableClickThrough;
        ConfigurationSetupController.OnConfigInactive += EnableClickThrough;

        CallToInteractController.OnCTIActive += CTIActivated;
        CallToInteractController.OnCTIInactive += CTIDeactivated;
    }

    private void OnDisable()
    {
        ConfigurationSetupController.OnConfigActive -= DisableClickThrough;
        ConfigurationSetupController.OnConfigInactive -= EnableClickThrough;

        CallToInteractController.OnCTIActive -= CTIActivated;
        CallToInteractController.OnCTIInactive -= CTIDeactivated;
    }

    void SetCursorWindow(bool setResolution)
    {
        if (setResolution)
        {
            Screen.SetResolution(GlobalSettings.CursorWindowSize, GlobalSettings.CursorWindowSize, FullScreenMode.Windowed);
        }

        SetWindowLong(hwnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);
        SetLayeredWindowAttributes(hwnd, 0, 255, 2);// Transparency=51=20%, LWA_ALPHA=2
        SetWindowPos(hwnd, HWND_TOPMOST, Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), GlobalSettings.CursorWindowSize, GlobalSettings.CursorWindowSize, 32 | 64);//SWP_FRAMECHANGED = 0x0020 (32); //SWP_SHOWWINDOW = 0x0040 (64)s

        var margins = new MARGINS() { cxLeftWidth = -1 };
        SetWindowLong(hwnd, -20, WS_EX_LAYERED | WS_EX_TRANSPARENT);
        SetWindowPos(hwnd, HWND_TOPMOST, Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.y), GlobalSettings.CursorWindowSize, GlobalSettings.CursorWindowSize, 32 | 64);//SWP_FRAMECHANGED = 0x0020 (32); //SWP_SHOWWINDOW = 0x0040 (64)s

        DwmExtendFrameIntoClientArea(hwnd, ref margins);
    }

    void SetConfigWindow(bool setResolution)
    {
        if (setResolution)
        {
            Screen.SetResolution(GlobalSettings.ScreenWidth, GlobalSettings.ScreenHeight, FullScreenMode.Windowed);
        }

        SetWindowLong(hwnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);
        SetLayeredWindowAttributes(hwnd, 0, 255, 2);// Transparency=51=20%, LWA_ALPHA=2
        SetWindowPos(hwnd, -2, 0, 0, GlobalSettings.ScreenWidth, GlobalSettings.ScreenHeight, 32 | 64);//SWP_FRAMECHANGED = 0x0020 (32); //SWP_SHOWWINDOW = 0x0040 (64)

        var margins = new MARGINS();

        // get the current -20 window and remove the layerd and transparent parts
        long style = GetWindowLong(hwnd, -20);
        style &= ~WS_EX_TRANSPARENT;
        style &= ~WS_EX_LAYERED;

        SetWindowLong(hwnd, -20, style);
        SetWindowPos(hwnd, -2, 0, 0, GlobalSettings.ScreenWidth, GlobalSettings.ScreenHeight, 32 | 64);//SWP_FRAMECHANGED = 0x0020 (32); //SWP_SHOWWINDOW = 0x0040 (64)

        DwmExtendFrameIntoClientArea(hwnd, ref margins);
    }

    List<bool> enablers = new List<bool>();

    IEnumerator EnableDisableChecker()
    {
        while (true)
        {
            if(enableTimer == 0 && enablers.Count > 0)
            {
                if(enablers[0])
                {
                    StartCoroutine(EnableClickthroughAfterDelay());
                }
                else
                {
                    StartCoroutine(DisableClickthroughAfterDelay());
                }

                enablers.RemoveAt(0);
            }

            yield return null;
        }
    }

    float enableTimer = 0;
    IEnumerator EnableClickthroughAfterDelay()
    {
        enableTimer = 0.01f;
        while(enableTimer > 0)
        {
            enableTimer -= Time.deltaTime;
            yield return null;
        }

        enableTimer = -100;
        EnableClickThrough();
        enableTimer = 0;
    }

    IEnumerator DisableClickthroughAfterDelay()
    {
        enableTimer = 0.01f;
        while (enableTimer > 0)
        {
            enableTimer -= Time.deltaTime;
            yield return null;
        }

        enableTimer = -100;
        DisableClickThrough();
        enableTimer = 0;
    }


    [DllImport("user32.dll", SetLastError = true)]
    static extern IntPtr FindWindowA(string lpClassName, string lpWindowName);
    void Update()
    {
        var handle = FindWindowA(null, "On-Screen Keyboard");
        if (handle != IntPtr.Zero)
        {
            SetWindowPos(handle, HWND_TOPMOST, 0, 0, 0, 0, (int)(SetWindowPosFlags.SWP_SHOWWINDOW | SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE));
            SetWindowPos(handle, -2, 0, 0, 0, 0, (int)(SetWindowPosFlags.SWP_SHOWWINDOW | SetWindowPosFlags.SWP_NOMOVE | SetWindowPosFlags.SWP_NOSIZE));
        }
#if !UNITY_EDITOR
		if (clickThroughEnabled)
		{
            if (Screen.width != GlobalSettings.CursorWindowSize)
            {
                //EnableClickThrough();
            }

            int xPos = Mathf.RoundToInt(position.x);
            int yPos = Mathf.RoundToInt(position.y);
            if (xPos + GlobalSettings.CursorWindowSize > 0 && xPos < GlobalSettings.ScreenWidth && yPos + GlobalSettings.CursorWindowSize > 0 && yPos < GlobalSettings.ScreenHeight)
            {
                SetWindowPos(hwnd,
                    HWND_TOPMOST,
                    Mathf.RoundToInt(position.x),
                    Mathf.RoundToInt(position.y),
                    GlobalSettings.CursorWindowSize,
                    GlobalSettings.CursorWindowSize,
                    32 | 64);
            }
		}
#endif
    }

    public void SetPosition(Vector2 value)
    {
        position = value;

        position.x = position.x - (GlobalSettings.CursorWindowSize/2);
        position.y = GlobalSettings.ScreenHeight - position.y - (GlobalSettings.CursorWindowSize/2);
    }
    
    void CTIActivated()
    {
        DisableClickThrough();
    }

    void CTIDeactivated()
    {
        if (!ConfigurationSetupController.isActive)
        {
            EnableClickThrough();
        }
    }

    private void OnApplicationQuit()
    {
        SetConfigWindow(true);
    }
}