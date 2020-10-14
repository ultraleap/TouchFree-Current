using UnityEngine;

[DefaultExecutionOrder(-1)]
public class TouchFreeMain : MonoBehaviour
{
    [Header("Debugging")]
    public bool EnableDebugging;
    [Space]
    public int OverrideScreenWidth;
    public int OverrideScreenHeight;

    bool showConfigDEBUG;

    void Awake()
    {
#if UNITY_EDITOR
        if (EnableDebugging)
        {
            InitialiseDebugging();
        }
#endif
        Application.targetFrameRate = 60;
    }

    private void InitialiseDebugging()
    {
        Debug.LogWarning("TouchFreeMain.InitialiseDebugging(): Debugging mode initialised! Override values will be used.");
        GlobalSettings.ScreenWidth = OverrideScreenWidth;
        GlobalSettings.ScreenHeight = OverrideScreenHeight;
    }

    void OnGUI()
    {
        if (showConfigDEBUG)
        {
            GUI.skin.label.fontSize = 24;
            GUI.color = Color.red;
            GUILayout.BeginArea(new Rect(10f, Screen.height * 0.5f, 1000f, Screen.height * 0.5f));
            GUILayout.Label($"ScreenHeightM: {PhysicalConfigurable.Config.ScreenHeightM:0.000}");
            GUILayout.Label($"LeapOffset: {PhysicalConfigurable.Config.LeapPositionRelativeToScreenBottomM.ToString("0.000")}");
            GUILayout.EndArea();
        }

#if UNITY_EDITOR
        if (EnableDebugging)
        {
            GUI.color = Color.red;
            GUI.skin.label.fontSize = 48;
            GUILayout.BeginArea(new Rect(10f, 10f, 1000f, 1000f));
            GUILayout.Label("TouchFree Debugging Mode Enabled");
            GUILayout.EndArea();
        }
#endif
    }

}