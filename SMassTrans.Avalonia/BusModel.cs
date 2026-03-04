using System.Collections.Generic;

namespace SMassTrans.Avalonia;

public enum BusModel
{
    /// <summary>
    /// no info
    /// </summary>
    Unknown,
    /// <summary>
    /// Tiny minibus
    /// </summary>
    YutongZK6770HGA,
    /// <summary>
    /// out of service, dead
    /// </summary>
    SanosS415,
    /// <summary>
    /// out of service, dead
    /// </summary>
    SanosS115,
    /// <summary>
    /// out of service, dead
    /// </summary>
    SanosS215,
    /// <summary>
    /// legit no clue
    /// </summary>
    MCIIM715GRN,
    /// <summary>
    /// The lazs, ukranian 2011
    /// </summary>
    LAZA183CityLAZ,
    /// <summary>
    /// The city master, most of the busses
    /// </summary>
    YutongZK6116HGSCityMaster,
    /// <summary>
    /// 2020 man busses
    /// </summary>
    MANA21LionsCityCNG,
    /// <summary>
    /// 2020 man bendy buesses
    /// </summary>
    MANA23LionsCityGCNG,
    /// <summary>
    /// 2025 BMC busses
    /// </summary>
    BMCProcityII12CNG
}

public class BusFleet
{
    public static Dictionary<int, BusModel> Mapping = GetFleetMapping();

    public static BusModel GetFromId(int id) => Mapping.GetValueOrDefault(id, BusModel.Unknown);

    public static Dictionary<int, BusModel> GetFleetMapping()
    {
        var fleet = new Dictionary<int, BusModel>();
        void AddRange(int start, int end, BusModel model)
        {
            for (int i = start; i <= end; i++)
            {
                fleet[i] = model;
            }
        }
        AddRange(101, 116, BusModel.YutongZK6770HGA);
        AddRange(1001, 1080, BusModel.LAZA183CityLAZ);
        AddRange(1081, 1286, BusModel.YutongZK6116HGSCityMaster);
        AddRange(2001, 2016, BusModel.YutongZK6116HGSCityMaster);
        AddRange(2101, 2121, BusModel.MANA21LionsCityCNG);
        AddRange(2122, 2123, BusModel.MANA23LionsCityGCNG); // Articulated
        AddRange(2124, 2134, BusModel.MANA21LionsCityCNG);
        AddRange(3001, 3006, BusModel.BMCProcityII12CNG);
        return fleet;
    }
}