public static class VersionExtension
{
    public static bool IsNewerThan(this Version thisVersion, Version comparisonVersion)
        {
            if (thisVersion.Major > comparisonVersion.Major)
            {
                return true;
            }
            else if(thisVersion.Major == comparisonVersion.Major)
            {
                if (thisVersion.Minor > comparisonVersion.Minor)
                {
                    return true;
                }
                else if (thisVersion.Minor == comparisonVersion.Minor)
                {
                    if (thisVersion.Revision > comparisonVersion.Revision)
                    {
                        return true;
                    }
                    else if (thisVersion.Revision == comparisonVersion.Revision)
                    {
                        if(thisVersion.IsMoreMature(comparisonVersion))
                        {
                            return true;
                        }
                        else if (thisVersion.Maturity == comparisonVersion.Maturity)
                        {
                            if (thisVersion.Iteration > comparisonVersion.Iteration)
                            {
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        private static bool IsMoreMature(this Version thisVersion, Version comparisonVersion)
        {
            if (thisVersion.Maturity == Maturity.None)
            {
                if (comparisonVersion.Maturity != Maturity.None)
                {
                    return true;
                }
            }
            else if (thisVersion.Maturity == Maturity.Beta)
            {
                if (comparisonVersion.Maturity == Maturity.Alpha || comparisonVersion.Maturity == Maturity.Prototype)
                {
                    return true;
                }
            }
            else if (thisVersion.Maturity == Maturity.Alpha)
            {
                if (comparisonVersion.Maturity == Maturity.Prototype)
                {
                    return true;
                }
            }
            return false;
        }
}
