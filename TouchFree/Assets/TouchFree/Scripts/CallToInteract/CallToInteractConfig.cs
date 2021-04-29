﻿public class CallToInteractSettings
{
    public bool Enabled = false;
    public float ShowTimeAfterNoHandPresent = 10f;
    public float HideTimeAfterHandPresent = 0.5f;
    public string CurrentFileName = "1 Push in mid-air to start.mp4";
    public HideRequirement hideType = HideRequirement.INTERACTION;
}

public class CallToInteractConfig
{
    public string ConfigFileName => "CallToInteractSettings.json";
    public static CallToInteractSettings Config;
}

public enum HideRequirement
{
    PRESENT,
    INTERACTION
}