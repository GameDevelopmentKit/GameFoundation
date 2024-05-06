using System;
using System.Collections.Generic;

public class DelegateEqualityComparer : IEqualityComparer<Delegate>
{
    public bool Equals(Delegate x, Delegate y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x == null || y == null) return false;
        if (x.Target == null || y.Target == null) return false;

        return x.Method.Equals(y.Method) && x.Target.Equals(y.Target);
    }

    public int GetHashCode(Delegate obj)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + (obj.Method?.GetHashCode() ?? 0);
            hash = hash * 23 + (obj.Target?.GetHashCode() ?? 0);
            return hash;
        }
    }
}