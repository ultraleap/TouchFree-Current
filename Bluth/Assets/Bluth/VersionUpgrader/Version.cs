using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using UnityEngine;

public enum Maturity
{
    None,
    Alpha,
    Beta,
    Prototype
};

[Serializable]
public struct Version
{
    [SerializeField] private int major;
    [SerializeField] private int minor;
    [SerializeField] private int revision;
    [SerializeField] private Maturity maturity;
    [SerializeField] private int iteration;
        
    [JsonProperty("major")] public int Major
    {
        get => major;
        set => major = value;
    }
        
    [JsonProperty("minor")] public int Minor 
    {
        get => minor;
        set => minor = value;
    }
        
    [JsonProperty("revision")] public int Revision
    {
        get => revision;
        set => revision = value;
    }
        
    [JsonProperty("maturity"), JsonConverter(typeof(StringEnumConverter))] public Maturity Maturity
    {
        get => maturity;
        set => maturity = value;
    }
        
    [JsonProperty("iteration")] public int Iteration
    {
        get => iteration;
        set => iteration = value;
    }

    public Version(int major = 1, int minor = 0, int revision = 0, Maturity maturity = Maturity.Alpha, int iteration = 1)
    {
        this.major = major;
        this.minor = minor;
        this.revision = revision;
        this.maturity = maturity;
        this.iteration = iteration;
    }

    public static bool TryParse(string input, out Version output)
    {            
        var tokens = input.Split('.', '-');

        try
        {
            var major = int.Parse(tokens[0]);
            var minor = int.Parse(tokens[1]);
            var revision = int.Parse(tokens[2]);
            var maturity = Maturity.None;
            var iteration = 0;
            if (tokens.Length > 3)
            {
                (Maturity maturity, int iteration) info = ParseMaturityAndIteration(tokens[3]);
                maturity = info.maturity;
                iteration = info.iteration;
            }

            output = new Version(major, minor, revision, maturity, iteration);
        }
        catch (Exception e)
        {
            Debug.LogError($"Couldn't parse enum of input {input}: {e}");
            output = new Version();
            return false;
        }

        return true;
    }

    public static Version Parse(string input)
    {
        if (TryParse(input, out Version output))
        {
            return output;
        }

        throw new FormatException($"FormatException: Could not parse string {input} to type Version.");
    }
        
    public static (Maturity, int) ParseMaturityAndIteration(string combined)
    {
        foreach (var maturityEnum in Enum.GetNames(typeof(Maturity)))
        {
            if (combined.IndexOf(maturityEnum, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                var split = combined.ToLower().Split(new []{ maturityEnum.ToLower() }, StringSplitOptions.RemoveEmptyEntries);

                return (
                    (Maturity)Enum.Parse(typeof(Maturity), maturityEnum, true),
                    int.Parse(split[0])
                );
            }
        }

        return (Maturity.None, 0);
    }

    public static bool operator ==(Version a, Version b) => a.Equals(b);
    public static bool operator !=(Version a, Version b) => !a.Equals(b);

    public override bool Equals(object obj)
    {
        if (!(obj is Version))
        {
            return false;
        }

        Version v = (Version)obj;
        return this.major == v.major
            && this.minor == v.minor
            && this.revision == v.revision
            && this.maturity == v.maturity
            && this.iteration == v.iteration;
    }

    public override int GetHashCode()
    {
        return Tuple.Create(this.major, this.minor, this.revision, (int)this.maturity, this.iteration).GetHashCode();
    }

    public override string ToString() => Maturity == Maturity.None ? $"{Major}.{Minor}.{Revision}" : $"{Major}.{Minor}.{Revision}-{Maturity.ToString().ToLower()}{Iteration}";
}