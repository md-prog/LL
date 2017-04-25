using System;
using System.Collections.Generic;
using Omu.ValueInjecter;

public class IgnoreNulls : ConventionInjection
{
    protected override bool Match(ConventionInfo c)
    {
        return c.SourceProp.Name == c.TargetProp.Name
              && c.SourceProp.Value != null;
    }
}