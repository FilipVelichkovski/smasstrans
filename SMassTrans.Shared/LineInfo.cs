namespace SMassTrans.Shared;

public class LineInfo : ILineInfo, IComparable<LineInfo>, IEqualityComparer<LineInfo>
{
    public LineInfo()
    {
        
    }

    public LineInfo(LineRouteId[] lineRouteIds, string name, string number, string carrier, bool nightly)
    {
        LineRouteIds = lineRouteIds;
        Name = name;
        Number = number;
        Carrier = carrier;
        Nightly= nightly;
    }
    public LineRouteId[] LineRouteIds { get; set; } 
    public string Name { get; init; } 
    public string Number { get; init; } 
    public string Carrier { get; init; }
    public bool Nightly { get; set; }

    public void Deconstruct(out LineRouteId[] lineRouteIds, out string Name, out string Number, out string Carrier, out bool Nightly)
    {
        lineRouteIds = this.LineRouteIds;
        Name = this.Name;
        Number = this.Number;
        Carrier = this.Carrier;
        Nightly = this.Nightly;
    }

    public int CompareTo(LineInfo? other)
    {
        if (ReferenceEquals(this, other)) return 0;
        if (other is null) return 1;
        var nameComparison = string.Compare(Name, other.Name, StringComparison.Ordinal);
        if (nameComparison != 0) return nameComparison;
        var numberComparison = string.Compare(Number, other.Number, StringComparison.Ordinal);
        if (numberComparison != 0) return numberComparison;
        var carrierComparison = string.Compare(Carrier, other.Carrier, StringComparison.Ordinal);
        if (carrierComparison != 0) return carrierComparison;
        return Nightly.CompareTo(other.Nightly);
    }

    public bool Equals(LineInfo? x, LineInfo? y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x is null) return false;
        if (y is null) return false;
        if (x.GetType() != y.GetType()) return false;
        return x.LineRouteIds.Equals(y.LineRouteIds) && x.Name == y.Name && x.Number == y.Number && x.Carrier == y.Carrier && x.Nightly == y.Nightly;
    }

    public int GetHashCode(LineInfo obj)
    {
        return HashCode.Combine(obj.LineRouteIds, obj.Name, obj.Number, obj.Carrier, obj.Nightly);
    }
}