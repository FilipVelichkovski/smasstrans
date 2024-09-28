namespace SMassTrans.Shared;

public record LineInfo(LineRouteId[] lineRouteIds, string Name, string Number, string Carrier, bool Nightly) : ILineInfo;