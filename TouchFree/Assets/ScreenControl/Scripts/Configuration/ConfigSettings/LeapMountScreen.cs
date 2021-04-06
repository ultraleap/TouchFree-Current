using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class LeapMountScreen : MonoBehaviour
{
    public GameObject guideWarning;

    public GameObject HMDMountedCurrent;
    public GameObject bottomMountedCurrent;
    public GameObject screenTopMountedCurrent;

    public GameObject screenTopOption;

    private void OnEnable()
    {
        // only show users the screentop option if they have the correct leap service
        if (SingleHandManager.Instance.screenTopAvailable)
        {
            screenTopOption.SetActive(true);
        }
        else
        {
            screenTopOption.SetActive(false);
        }

        ShowCurrentMount();

        // find the leap config path to look for auto orientation
        string appdatapath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData);
        string leapConfigPath = Path.Combine(appdatapath, "Leap Motion", "Config.json");

        if (File.Exists(leapConfigPath))
        {
            foreach(var line in File.ReadAllLines(leapConfigPath))
            {
                if(line.Contains("image_processing_auto_flip"))
                {
                    // check if auto orientation is true and warn against it
                    if(line.Contains("true"))
                    {
                        StartCoroutine(EnableWarningAfterWait());
                        return;
                    }
                    else
                    {
                        guideWarning.SetActive(false);
                    }

                    break;
                }
            }
        }

        // we still think the warning should not be shown, double check by:
        // Check if the physicalconfig is set to default
        var defaultConfig = PhysicalConfigurable.GetDefaultValues();

        if (PhysicalConfigurable.Config.ScreenHeightM == defaultConfig.ScreenHeightM &&
            PhysicalConfigurable.Config.LeapPositionRelativeToScreenBottomM == defaultConfig.LeapPositionRelativeToScreenBottomM)
        {
            StartCoroutine(EnableWarningAfterWait());
        }
        else
        {
            guideWarning.SetActive(false);
        }
    }

    void ShowCurrentMount()
    {
        bottomMountedCurrent.SetActive(false);
        HMDMountedCurrent.SetActive(false);
        screenTopMountedCurrent.SetActive(false);

        // leap is looking down
        if (Mathf.Abs(PhysicalConfigurable.Config.LeapRotationD.z) > 90f)
        {
            if (SingleHandManager.Instance.screenTopAvailable && PhysicalConfigurable.Config.LeapRotationD.x <= 0f)
            {   //Screentop
                screenTopMountedCurrent.SetActive(true);
            }
            else
            {   //HMD
                HMDMountedCurrent.SetActive(true);
            }

        }
        else
        {   //Desktop
            bottomMountedCurrent.SetActive(true);
        }
    }

    IEnumerator EnableWarningAfterWait(float _wait = 0.5f)
    {
        yield return new WaitForSeconds(_wait);
        guideWarning.SetActive(true);
    }
}
