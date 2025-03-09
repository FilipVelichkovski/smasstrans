namespace SMassTrans.Shared.ModeShift;

public class Settings
{
    public App app { get; set; }
    public Map map { get; set; }
    public Auth auth { get; set; }
    public Redir redir { get; set; }
    public Firebase firebase { get; set; }
}

public class App
{
    public string apiUrl { get; set; }
    public string tenantId { get; set; }
    public string locale { get; set; }
    public string timezone { get; set; }
    public string currency { get; set; }
    public string contactEmail { get; set; }
    public string[] services { get; set; }
    public string[] ticketTypes { get; set; }
    public InformationPage informationPage { get; set; }
    public Show show { get; set; }
    public Hide hide { get; set; }
    public int hideOnWidth { get; set; }
    public Wallet wallet { get; set; }
}

public class InformationPage
{
    public string url { get; set; }
    public string navigationTextKey { get; set; }
    public string icon { get; set; }
}

public class Show
{
    public object accessibility { get; set; }
    public object walkSpeed { get; set; }
    public object toggleMapButton { get; set; }
    public bool checkYourEmailMessageOnPurchase { get; set; }
    public object showServiceAlerts { get; set; }
    public object fastestBadge { get; set; }
}

public class Hide
{
    public object saveCardSwitch { get; set; }
    public object hideTickets { get; set; }
    public bool tripDuration { get; set; }
    public int singleRouteName { get; set; }
    public object plannerButton { get; set; }
}

public class Wallet
{
    public bool isEnabled { get; set; }
    public int[] topUpValues { get; set; }
}

public class Map
{
    public string apiKey { get; set; }
    public string mapLanguage { get; set; }
    public string mapRegion { get; set; }
    public string countryRestriction { get; set; }
    public int defaultZoom { get; set; }
    public double[] defaultCenter { get; set; }
    public double[] cityBoundsSW { get; set; }
    public double[] cityBoundsNE { get; set; }
}

public class Auth
{
    public string cityHostName { get; set; }
    public string cityTenantId { get; set; }
    public string cityAppId { get; set; }
    public string scope { get; set; }
    public string culture { get; set; }
}

public class Redir
{
    public string login { get; set; }
    public string logout { get; set; }
    public string order { get; set; }
    public string cancelOrder { get; set; }
    public string ios { get; set; }
    public string android { get; set; }
    public string faq { get; set; }
}

public class Firebase
{
    public object apiKey { get; set; }
    public object projectId { get; set; }
}

