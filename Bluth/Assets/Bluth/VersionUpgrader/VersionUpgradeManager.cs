using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class VersionUpgradeAttribute : Attribute
{
    public Version FromVersion { get; private set; }
    public Version ToVersion { get; private set; }

    public VersionUpgradeAttribute(string fromVersion, string toVersion)
    {
        FromVersion = Version.Parse(fromVersion);
        ToVersion = Version.Parse(toVersion);
    }
}
/// <summary>
/// Handle upgrading for both persistentdatapath files AND streamingassets 'Custom Presets' files
/// </summary>
public static class VersionUpgradeManager
{
    #region Upgrade Methods

    static void OnUpgradeFailed()
    {
        // Delete all files to allow auto-recreate. User options will be lost but application will be in valid state.
        File.Delete(PhysicalConfigurable.ConfigFilePath);
        File.Delete(SettingsConfig.ConfigFilePath);
    }

    [VersionUpgrade("1.0.0-alpha1", "1.0.0-alpha3")]
    static void Update_100a1_to_100a3()
    {
        var companyDataPath = Environment.ExpandEnvironmentVariables("%userprofile%/AppData/LocalLow/Ultraleap");
        var oldFilesDirectory = Path.Combine(companyDataPath, "Bluth");
        var newFilesDirectory = Path.Combine(companyDataPath, "TouchFree");

        var oldTouchlessConfigPath = Path.Combine(oldFilesDirectory, "TouchlessConfig.json");
        var newTouchlessConfigPath = Path.Combine(newFilesDirectory, "TouchlessConfig.json");
        var newSettingsPath = Path.Combine(newFilesDirectory, "Settings.json");

        // Get the existing touchless config properties
        var touchlessConfigProps = JObject.Parse(File.ReadAllText(oldTouchlessConfigPath));

        // Store migrated properties for later
        var migrateProperties = new Dictionary<string, object>
        {
            { "CursorDefaultColor", touchlessConfigProps.Value<string>("CursorDefaultColor") },
            { "CursorTapColor", touchlessConfigProps.Value<string>("CursorTapColor") },
            { "CursorDragColor", touchlessConfigProps.Value<string>("CursorDragColor") }
        };

        // Remove properties that are no longer in this file.
        touchlessConfigProps.Remove("CursorDefaultColor");
        touchlessConfigProps.Remove("CursorTapColor");
        touchlessConfigProps.Remove("CursorDragColor");

        // Convert X rotation to new mode (positive X rotations now alway tilt towards the screen, top mounting therefore needs negating).
        var xVal = touchlessConfigProps["LeapRotationD"].Value<float>("x");
        var isTopMounted = Mathf.Approximately(touchlessConfigProps["LeapRotationD"].Value<float>("z"), 180f);
        xVal = isTopMounted ? -xVal : xVal;
        touchlessConfigProps["LeapRotationD"]["x"] = xVal;

        // Save the file to the new location.
        File.WriteAllText(newTouchlessConfigPath, touchlessConfigProps.ToString(Formatting.Indented));

        // Create new settings file with default values and migrated properties.
        var settingsConfigProps = new JObject();
        settingsConfigProps.Add("CursorDefaultColor", (string)migrateProperties["CursorDefaultColor"]);
        settingsConfigProps.Add("CursorTapColor", (string)migrateProperties["CursorTapColor"]);
        settingsConfigProps.Add("CursorDragColor", (string)migrateProperties["CursorDragColor"]);
        settingsConfigProps.Add("CursorDotSizeM", 0.006f);
        settingsConfigProps.Add("CursorRingMaxScale", 5.0f);
        settingsConfigProps.Add("CursorMaxRingScaleAtDistanceM", 0.1f);
        settingsConfigProps.Add("UseScrollingOrDragging", true);
        File.WriteAllText(newSettingsPath, settingsConfigProps.ToString(Formatting.Indented));

        // Delete entire Bluth directory
        DirectoryInfo di = new DirectoryInfo(oldFilesDirectory);
        foreach (FileInfo file in di.GetFiles()) file.Delete();
        foreach (DirectoryInfo dir in di.GetDirectories()) dir.Delete(true);
        Directory.Delete(oldFilesDirectory);
    }

    [VersionUpgrade("1.0.0-beta4", "1.0.0-beta5")]
    static void Update_100b4_to_100b5()
    {
        var companyDataPath = Environment.ExpandEnvironmentVariables("%userprofile%/AppData/LocalLow/Ultraleap");
        var settingsPath = Path.Combine(companyDataPath, "TouchFree", "Settings.json");
        var item = JObject.Parse(File.ReadAllText(settingsPath));

        if(item.Value<int>("InteractionSelection") == 1)
        {
            item["InteractionSelection"] = 2;
        }

        File.WriteAllText(settingsPath, item.ToString());
    }

    [VersionUpgrade("1.0.0-beta5", "1.0.0-beta6")]
    static void Update_100b5_to_100b6()
    {
        var companyDataPath = Environment.ExpandEnvironmentVariables("%userprofile%/AppData/LocalLow/Ultraleap");
        var settingsPath = Path.Combine(companyDataPath, "TouchFree", "Settings.json");
        var item = JObject.Parse(File.ReadAllText(settingsPath));

        // force the interaction to be poke
        item["InteractionSelection"] = 2;
        item["CursorVerticalOffset"] = 0.0f;

        File.WriteAllText(settingsPath, item.ToString());
    }

    [VersionUpgrade("1.0.0-beta6", "1.0.0")]
    static void Update_100b6_to_100()
    {
    }

    #endregion

    #region Upgrader

    static readonly string VersionFileDirectory = PhysicalConfigurable.ConfigFileDirectory;
    static readonly string VersionFilePath = Path.Combine(VersionFileDirectory, "version.txt");

    // This must happen before any other config system to ensure it updates before they make default files
#if !UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#endif
    public static void RunVersionUpgrader()
    {
        Version previousVersion;
        Version buildVersion;

        buildVersion = Version.Parse(Application.version);

        // look for an existing version.txt file
        if (File.Exists(VersionFilePath))
        {
            previousVersion = Version.Parse(File.ReadAllText(VersionFilePath));
        }
        // if there is no version file, we are updating through all versions
        else
        {
            previousVersion = new Version(1, 0, 0, Maturity.Alpha, 1);
            Directory.CreateDirectory(VersionFileDirectory);
            File.WriteAllText(VersionFilePath, buildVersion.ToString());
        }

        // Store current new build version to txt.
        File.WriteAllText(VersionFilePath, buildVersion.ToString());

        try
        {
            var upgradeMethods = GetVersionUpgradeMethodsInRange(previousVersion, buildVersion);

            foreach (var method in upgradeMethods)
            {
                try
                {
                    Debug.Log($"Upgrade: {method.GetCustomAttribute<VersionUpgradeAttribute>().FromVersion} -> {method.GetCustomAttribute<VersionUpgradeAttribute>().ToVersion}");
                    method.Invoke(null, null);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to upgrade from {method.GetCustomAttribute<VersionUpgradeAttribute>().FromVersion} to {method.GetCustomAttribute<VersionUpgradeAttribute>().ToVersion}");
                    throw;
                }
            }

        }
        catch (Exception e)
        {
            Debug.LogError(e);
            OnUpgradeFailed();
        }
    }

    /// <summary>
    /// Returns a version-sorted list of method infos from the currently executing assembly that have the VersionUpgradeAttrubute on them.
    /// </summary>
    /// <returns></returns>
    static List<MethodInfo> GetVersionUpgradeMethods()
    {
        var upgradeMethods = Assembly.GetExecutingAssembly().GetTypes()
                      .SelectMany(t => t.GetMethods(BindingFlags.NonPublic|BindingFlags.Static))
                      .Where(m => m.GetCustomAttributes(typeof(VersionUpgradeAttribute), false).Length > 0)
                      .ToList();

        upgradeMethods.Sort((a, b) =>
        {
            var aVer = a.GetCustomAttribute<VersionUpgradeAttribute>().FromVersion;
            var bVer = b.GetCustomAttribute<VersionUpgradeAttribute>().FromVersion;

            return aVer.IsNewerThan(bVer) ? 1 : -1;
        });

        return upgradeMethods;
    }

    /// <summary>
    /// Returns a version-sorted list of method infos from the currently executing assembly that have the VersionUpgradeAttrubute on them in and including the specified range.
    /// </summary>
    /// <param name="fromVersion">Range start version. Inclusive.</param>
    /// <param name="toVersion">Range end version. Inclusive.</param>
    /// <returns></returns>
    static List<MethodInfo> GetVersionUpgradeMethodsInRange(Version fromVersion, Version toVersion)
    {
        var methods = GetVersionUpgradeMethods();
        List<MethodInfo> range = new List<MethodInfo>();

        if (fromVersion == toVersion)
        {
            return range;
        }

        foreach (var info in methods)
        {
            var attr = info.GetCustomAttribute<VersionUpgradeAttribute>();

            var isMethodVersionsInRange = (attr.FromVersion == fromVersion || fromVersion.IsNewerThan(attr.FromVersion))
                && (attr.ToVersion == toVersion || !toVersion.IsNewerThan(attr.ToVersion));

            if (isMethodVersionsInRange)
            {
                range.Add(info);
            }
        }

        return range;
    }

#endregion
}