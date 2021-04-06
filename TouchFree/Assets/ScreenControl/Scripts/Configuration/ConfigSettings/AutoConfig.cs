﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Leap.Unity;

public class AutoConfig : MonoBehaviour
{
    public GameObject step1;
    public GameObject step2;
    public GameObject trackingLost;

    Vector3 bottomPosM;
    Vector3 topPosM;

    private void OnEnable()
    {
        // reset the auto config
        bottomPosM = Vector3.zero;
        topPosM = Vector3.zero;
        step1.SetActive(true);
        step2.SetActive(false);
        SingleHandManager.Instance.useTrackingTransform = false;
        ConfigurationSetupController.SetCursorVisual(false);
        DisplayTrackingLost(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (bottomPosM == Vector3.zero)
            {
                if (SingleHandManager.Instance.CurrentHand != null)
                {
                    SetBottomPos(SingleHandManager.Instance.CurrentHand.GetIndex().TipPosition.ToVector3());
                    // display second autoconfig screen
                    step1.SetActive(false);
                    step2.SetActive(true);
                }
                else
                {
                    // Display a notification that tracking was lost
                    DisplayTrackingLost();
                }
            }
            else if (topPosM == Vector3.zero)
            {
                if (SingleHandManager.Instance.CurrentHand != null)
                {
                    SetTopPos(SingleHandManager.Instance.CurrentHand.GetIndex().TipPosition.ToVector3());
                    CompleteAutoConfig(bottomPosM, topPosM);
                }
                else
                {
                    // Display a notification that tracking was lost
                    DisplayTrackingLost();
                }
            }
        }
    }

    void CompleteAutoConfig(Vector3 bottomPos, Vector3 topPos)
    {
        PhysicalConfigurable.SetAllValuesToDefault();
        var setup = CalculateConfigurationValues(bottomPos, topPos);

        // Make sure that past this point the selected mount type has been reset as it has been used above
        ConfigurationSetupController.selectedMountType = MountingType.NONE;

        PhysicalConfigurable.UpdateConfig(setup);
        PhysicalConfigurable.SaveConfig();
        ConfigurationSetupController.Instance.ChangeState(ConfigState.AUTO_COMPLETE);
    }

    public PhysicalSetup CalculateConfigurationValues(Vector3 bottomPos, Vector3 topPos)
    {
        var setup = new PhysicalSetup();
        Vector3 bottomNoX = new Vector3(0, bottomPos.y, bottomPos.z);
        Vector3 topNoX = new Vector3(0, topPos.y, topPos.z);

        setup.ScreenHeightM = Vector3.Distance(bottomNoX, topNoX) * 1.25f;

        var bottomEdge = BottomCentreFromTouches(bottomPos, topPos);
        var topEdge = TopCentreFromTouches(bottomPos, topPos);

        setup.LeapRotationD = LeapRotationRelativeToScreen(bottomPos, topPos);
        setup.LeapPositionRelativeToScreenBottomM = LeapPositionInScreenSpace(bottomEdge, setup.LeapRotationD);

        return setup;
    }

    /// <summary>
    /// Find the position of the camera relative to the screen, using the screen position relative to the camera.
    /// </summary>
    private Vector3 LeapPositionInScreenSpace(Vector3 bottomEdgeRef, Vector3 leapRotation)
    {

        // In Leap Co-ords we know the Leap is at Vector3.zero, and that the bottom of the screen is at "bottomEdgeRef"

        // We know the Leap is rotated at "leapRotation" from the screen.
        // We want to calculate the Vector from the bottom of the screen to the Leap in this rotated co-ord system.

        Vector3 rotationAngles = leapRotation;
        if (ConfigurationSetupController.selectedMountType == MountingType.OVERHEAD || ConfigurationSetupController.selectedMountType == MountingType.SCREENTOP)
        {
            // In overhead mode, the stored 'x' angle is inverted so that positive angles always mean
            // the camera is pointed towards the screen. Multiply by -1 here so that it can be used
            // in a calculation. 
            rotationAngles.x *= -1f;
        }
        Vector3 rotatedVector = Quaternion.Euler(rotationAngles) * bottomEdgeRef;

        // Multiply by -1 so the vector is from screen to camera
        Vector3 leapPosition = rotatedVector * -1f;
        return leapPosition;
    }

    /// <summary>
    /// BottomTouch -> TopTouch is 1/8th screen height as touch points are placed 10% in from the edge.
    /// We need to offset the touch point by 1/10th of screen height = 1/8th of the distance between touch points.
    /// For this we can Lerp from bottom to top touch travelling an extra 8th distance.
    /// </summary>
    public Vector3 TopCentreFromTouches(Vector3 bottomTouch, Vector3 topTouch)
    {
        return Vector3.LerpUnclamped(bottomTouch, topTouch, 1.125f);
    }

    /// <summary>
    /// TopTouch -> BottomTouch is 1/8th screen height as touch points are placed 10% in from the edge.
    /// We need to offset the touch point by 1/10th of screen height = 1/8th of the distance between touch points.
    /// For this we can Lerp from top to bottom touch travelling an extra 8th distance
    /// </summary>
    public Vector3 BottomCentreFromTouches(Vector3 bottomTouch, Vector3 topTouch)
    {

        return Vector3.LerpUnclamped(topTouch, bottomTouch, 1.125f);
    }

    /// <summary>
    /// Find the angle between the camera and the screen.
    /// Ensure a positive angle always means rotation towards the screen.
    /// </summary>
    public Vector3 LeapRotationRelativeToScreen(Vector3 bottomCentre, Vector3 topCentre)
    {
        Vector3 directionBottomToTop = topCentre - bottomCentre;
        Vector3 rotation = Vector3.zero;

        if (ConfigurationSetupController.selectedMountType == MountingType.OVERHEAD || ConfigurationSetupController.selectedMountType == MountingType.SCREENTOP)
        {
            rotation.x = -Vector3.SignedAngle(Vector3.up, directionBottomToTop, Vector3.right) + 180;
            rotation.z = 180;
        }
        else
        {
            rotation.x = Vector3.SignedAngle(Vector3.up, directionBottomToTop, Vector3.left);
        }

        rotation.x = CentreRotationAroundZero(rotation.x);

        return rotation;
    }

    /// <summary>
    ///    Ensure the calculated rotations make sense to the UI by avoiding large values.
    ///    Angles are centred around 0, with the smallest representation of the value
    /// </summary>
    public float CentreRotationAroundZero(float angle)
    {
        if(angle > 180) 
        {
            return angle - 360;
        }
        else if(angle < -180)
        {
            return angle + 360;
        }
        else 
        {
            return angle;
        }
    }

    void DisplayTrackingLost(bool _display = true)
    {
        trackingLost.SetActive(_display);

        if (_display)
            StartCoroutine(HideTrackingLostAfterTime());
    }

    IEnumerator HideTrackingLostAfterTime()
    {
        yield return new WaitForSeconds(1);
        DisplayTrackingLost(false);
    }

    public void SetTopPos(Vector3 position)
    {
        topPosM = position;
    }

    public void SetBottomPos(Vector3 position)
    {
        bottomPosM = position;
    }
}