namespace SMassTrans.Shared;

/// <param name="LineId"> The line's id </param>
/// <param name="RouteId"> The line's route</param>
public readonly record struct LineRouteId(string LineId, string RouteId);