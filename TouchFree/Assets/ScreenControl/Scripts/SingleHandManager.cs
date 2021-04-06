using Leap;
using Leap.Unity;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class SingleHandManager : MonoBehaviour
{
    public static SingleHandManager Instance;

    public Hand CurrentHand { get; private set; }

    bool CurrentIsLeft => CurrentHand != null && CurrentHand.IsLeft;
    bool CurrentIsRight => CurrentHand != null && !CurrentHand.IsLeft;

    [HideInInspector] public bool useTrackingTransform = true;
    LeapTransform TrackingTransform;

    [HideInInspector] public bool screenTopAvailable = false;

    void Awake()
    {
        if(Instance != null)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        PhysicalConfigurable.OnConfigUpdated += UpdateTrackingTransform;

        CheckLeapVersionForScreentop();
    }

    void CheckLeapVersionForScreentop()
    {
        // find the LeapSvc.exe as it has the current version
        string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Leap Motion", "Core Services", "LeapSvc.exe");

        if (File.Exists(path))
        {   
            // get the version info from the service
            FileVersionInfo myFileVersionInfo = FileVersionInfo.GetVersionInfo(path);

            // parse the version or use default (1.0.0)
            Version version = new Version();
            Version.TryParse(myFileVersionInfo.FileVersion, out version);

            // Virsion screentop is introduced
            Version screenTopVersionMin = new Version(4, 9, 2);

            if (version != null)
            {
                if (version.IsNewerThan(screenTopVersionMin))
                {
                    screenTopAvailable = true;
                }
            }
        }
    }

    private void Start()
    {
        UpdateTrackingTransform();
    }

    void OnDestroy()
    {
        PhysicalConfigurable.OnConfigUpdated -= UpdateTrackingTransform;
    }

    IEnumerator UpdateTrackingAfterLeapInit()
    {
        while(((LeapServiceProvider)Hands.Provider).GetLeapController() == null)
        {
            yield return null;
        }

        // To simplify the configuration values, positive X angles tilt the Leap towards the screen no matter how its mounted.
        // Therefore, we must convert to the real values before using them.
        // If top mounted, the X rotation should be negative if tilted towards the screen so we must negate the X rotation in this instance.
        var isTopMounted = Mathf.Approximately(PhysicalConfigurable.Config.LeapRotationD.z, 180f);
        float xAngleDegree = isTopMounted ? -PhysicalConfigurable.Config.LeapRotationD.x : PhysicalConfigurable.Config.LeapRotationD.x;

        UpdateLeapTrackingMode();
        TrackingTransform = new LeapTransform(
            PhysicalConfigurable.Config.LeapPositionRelativeToScreenBottomM.ToVector(),
            Quaternion.Euler(xAngleDegree, PhysicalConfigurable.Config.LeapRotationD.y, PhysicalConfigurable.Config.LeapRotationD.z).ToLeapQuaternion()
        );
    }

    private void UpdateTrackingTransform()
    {
        StartCoroutine(UpdateTrackingAfterLeapInit());
    }

    public void UpdateLeapTrackingMode()
    {
        // leap is looking down

        if (Mathf.Abs(PhysicalConfigurable.Config.LeapRotationD.z) > 90f)
        {
            if (screenTopAvailable && PhysicalConfigurable.Config.LeapRotationD.x <= 0f)
            {   //Screentop
                SetLeapTrackingMode(MountingType.SCREENTOP);
            }
            else
            {   //HMD
                SetLeapTrackingMode(MountingType.OVERHEAD);
            }
        }
        else
        {   //Desktop
            SetLeapTrackingMode(MountingType.DESKTOP);
        }
    }

    public void SetLeapTrackingMode(MountingType _mount)
    {
        switch (_mount)
        {
            case MountingType.NONE:
            case MountingType.DESKTOP:
                ((LeapServiceProvider)Hands.Provider).GetLeapController().ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
                ((LeapServiceProvider)Hands.Provider).GetLeapController().ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_SCREENTOP);
                break;
            case MountingType.SCREENTOP:
                ((LeapServiceProvider)Hands.Provider).GetLeapController().SetPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_SCREENTOP);
                ((LeapServiceProvider)Hands.Provider).GetLeapController().ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
                break;
            case MountingType.OVERHEAD:
                ((LeapServiceProvider)Hands.Provider).GetLeapController().SetPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_HMD);
                ((LeapServiceProvider)Hands.Provider).GetLeapController().ClearPolicy(Controller.PolicyFlag.POLICY_OPTIMIZE_SCREENTOP);

                break;
        }
    }

    private void Update()
    {
        bool foundLeft = false;
        bool foundRight = false;

        Hand left = null;
        Hand right = null;

       if (useTrackingTransform)
            Hands.Provider.CurrentFrame.Transform(TrackingTransform);

        foreach (var hand in Hands.Provider.CurrentFrame.Hands)
        {
            if (hand.IsLeft)
            {
                if (CurrentIsLeft) // left hand is already active and was found, ignore everything
                    return;

                foundLeft = true;
                left = hand;
            }

            if (hand.IsRight)
            {
                if (CurrentIsRight) // right hand is already active and was found, ignore everything
                    return;

                foundRight = true;
                right = hand;
            }
        }

        // if we are here, we might need to set a new hand to be active

        if (foundRight) // prioritise right hand as it is standard.
        {
            // Set it to be active
            CurrentHand = right;
        }
        else if(foundLeft)
        {
            // Set it to be active
            CurrentHand = left;
        }
        else
        {
            CurrentHand = null;
        }
    }

    public Vector3 GetTrackedPointingJoint()
    {
        const float trackedJointDistanceOffset = 0.0533f;

        var bones = CurrentHand.GetIndex().bones;

        Vector3 trackedJointVector = (bones[0].NextJoint.ToVector3() + bones[1].NextJoint.ToVector3()) / 2;
        trackedJointVector.z += trackedJointDistanceOffset;
        return trackedJointVector;
    }

    public long GetTimestamp()
    {
        // Returns the timestamp of the latest frame in microseconds
        Controller leapController = ((LeapServiceProvider)Hands.Provider).GetLeapController();
        return leapController.Frame(0).Timestamp;
    }

    public bool IsLeapServiceConnected()
    {
        return ((LeapServiceProvider)Hands.Provider).IsConnected();
    }
}