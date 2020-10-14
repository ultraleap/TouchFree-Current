﻿using UnityEngine;
using Stopwatch = System.Diagnostics.Stopwatch;

public class HoverAndHoldInteractionModule : InteractionModule
{
    public override InteractionType InteractionType {get;} = InteractionType.Hover;
    public bool InteractionEnabled { get; set; } = true;

    public ProgressTimer progressTimer;

    public float hoverDeadzoneEnlargementDistance = 0.02f;
    public float timerDeadzoneEnlargementDistance = 0.02f;

    public float deadzoneShrinkSpeed = 0.3f;
    private Vector2 previousHoverPosDeadzone = Vector2.zero;
    private Vector2 previousHoverPosScreen = Vector2.zero;
    private Vector2 previousScreenPos = Vector2.zero;

    public float hoverTriggerTime = 200f;
    private bool hoverTriggered = false;
    private float hoverTriggeredDeadzoneRadius = 0f;
    private Stopwatch hoverTriggerTimer = new Stopwatch();

    public float clickHoldTime = 200f;
    private bool clickHeld = false;
    private bool clickAlreadySent = false;
    private Stopwatch clickingTimer = new Stopwatch();

    void Update()
    {
        if (SingleHandManager.Instance.CurrentHand == null)
        {
            return;
        }

        if (!InteractionEnabled)
        {
            return;
        }

        positions = positioningModule.CalculatePositions();
        Vector3 cursorPosition = positions.CursorPosition;
        Vector3 clickPosition = positions.ClickPosition;
        float distanceFromScreen = clickPosition.z;
        Vector2 cursorPositionM = GlobalSettings.virtualScreen.PixelsToMeters(cursorPosition);
        Vector2 hoverPosM = ApplyHoverzone(cursorPositionM);
        Vector2 hoverPos = GlobalSettings.virtualScreen.MetersToPixels(hoverPosM);
        
        HandleInteractions(cursorPosition, clickPosition, distanceFromScreen, hoverPos);
    }

    private Vector2 ApplyHoverzone(Vector2 _screenPosM)
    {
        float deadzoneRad = positioningModule.Stabiliser.defaultDeadzoneRadius + hoverDeadzoneEnlargementDistance;
        previousHoverPosDeadzone = PositionStabiliser.ApplyDeadzoneSized(previousHoverPosDeadzone, _screenPosM, deadzoneRad);
        return previousHoverPosDeadzone;
    }

    private void HandleInteractions(Vector2 _cursorPosition, Vector3 _clickPosition, float _distanceFromScreen, Vector2 _hoverPosition)
    {
        SendInputAction(InputType.MOVE, _cursorPosition, _clickPosition, _distanceFromScreen);
        HandleInputHoverAndHold(_cursorPosition, _clickPosition, _distanceFromScreen, _hoverPosition);
    }

    private void HandleInputHoverAndHold(Vector2 _cursorPosition, Vector3 _clickPosition, float _distanceFromScreen, Vector2 _hoverPosition)
    {
        if (!clickHeld && !hoverTriggered && _hoverPosition == previousHoverPosScreen)
        {
            if (!hoverTriggerTimer.IsRunning)
            {
                hoverTriggerTimer.Restart();
            }
            else if (hoverTriggerTimer.ElapsedMilliseconds > hoverTriggerTime)
            {
                hoverTriggered = true;
                hoverTriggerTimer.Stop();
                hoverTriggeredDeadzoneRadius = positioningModule.Stabiliser.GetCurrentDeadzoneRadius();
                previousScreenPos = _cursorPosition; // To prevent instant-abandonment of hover
            }
        }

        bool sendHoverAction = true;

        if (hoverTriggered)
        {
            if (_cursorPosition == previousScreenPos)
            {
                if (!clickHeld)
                {
                    if (!progressTimer.IsRunning && progressTimer.Progress == 0f)
                    {
                        progressTimer.StartTimer();
                    }
                    else if (progressTimer.IsRunning && progressTimer.Progress == 1f)
                    {
                        positioningModule.Stabiliser.SetCurrentDeadzoneRadius(timerDeadzoneEnlargementDistance + positioningModule.Stabiliser.defaultDeadzoneRadius);
                        progressTimer.StopTimer();
                        clickHeld = true;
                        clickingTimer.Restart();
                        SendInputAction(InputType.DOWN, _cursorPosition, _clickPosition, _distanceFromScreen);
                        sendHoverAction = false;
                    }
                    else
                    {
                        float maxDeadzoneRadius = timerDeadzoneEnlargementDistance + positioningModule.Stabiliser.defaultDeadzoneRadius;
                        float deadzoneRadius = Mathf.Lerp(hoverTriggeredDeadzoneRadius, maxDeadzoneRadius, progressTimer.Progress);
                        positioningModule.Stabiliser.SetCurrentDeadzoneRadius(deadzoneRadius);
                    }
                }
                else
                {
                    if (!clickAlreadySent && clickingTimer.ElapsedMilliseconds > clickHoldTime)
                    {
                        SendInputAction(InputType.UP, _cursorPosition, _clickPosition, _distanceFromScreen);
                        sendHoverAction = false;
                        clickAlreadySent = true;
                    }
                    else if (!clickAlreadySent)
                    {
                        SendInputAction(InputType.HOLD, _cursorPosition, _clickPosition, _distanceFromScreen);
                        sendHoverAction = false;
                    }
                }
            }
            else
            {
                if (clickHeld && !clickAlreadySent)
                {
                    // Handle unclick if move before timer's up
                    SendInputAction(InputType.UP, _cursorPosition, _clickPosition, _distanceFromScreen);
                    sendHoverAction = false;
                }

                progressTimer.ResetTimer();

                hoverTriggered = false;
                hoverTriggerTimer.Stop();

                clickHeld = false;
                clickAlreadySent = false;
                clickingTimer.Stop();
                
                positioningModule.Stabiliser.StartShrinkingDeadzone(ShrinkType.MOTION_BASED, deadzoneShrinkSpeed);
            }
        }

        if (sendHoverAction && allowHover)
        {
            SendInputAction(InputType.HOVER, _cursorPosition, _clickPosition, _distanceFromScreen);
        }

        previousHoverPosScreen = _hoverPosition;
        previousScreenPos = _cursorPosition;
    }

    protected override void OnSettingsUpdated()
    {
        base.OnSettingsUpdated();
        hoverTriggerTime = SettingsConfig.Config.HoverCursorStartTimeS * 1000; // s to ms
        progressTimer.timeLimit = SettingsConfig.Config.HoverCursorCompleteTimeS * 1000; // s to ms
    }
}