using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NuGet.Frameworks;

namespace XamlViewer.Extensions;
public static class NuGetFrameworkExtensions
{
    public static bool IsSupport(this NuGetFramework source, NuGetFramework target)
    {
        if (source.Equals(target))
            return true;

        //if (x.Version == y.Version && StringComparer.OrdinalIgnoreCase.Equals(x.Framework, y.Framework) && StringComparer.OrdinalIgnoreCase.Equals(x.Profile, y.Profile) && StringComparer.OrdinalIgnoreCase.Equals(x.Platform, y.Platform) && x.PlatformVersion == y.PlatformVersion)
        //{
        //    return !x.IsUnsupported;
        //}

        //完全匹配,不匹配，判断target是否时基础包
        if (!StringComparer.OrdinalIgnoreCase.Equals(source.Framework, target.Framework))
        {
            if (!target.IsPackageBased) return false;

            if (!StringComparer.OrdinalIgnoreCase.Equals(target.Framework, ".NETStandard")) return false;
        }

        if (!StringComparer.OrdinalIgnoreCase.Equals(source.Platform, target.Platform))
            return false;

        if (source.PlatformVersion < target.PlatformVersion) return false;

        if (source.Version < target.Version) return false;





        return true;
    }
}
