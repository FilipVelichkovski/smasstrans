namespace SMassTrans.Shared;

public interface IGps : IDisposable
{
    public Task SubscribeToLine(string Line);
    public Task UnSubscribeToLine(string Line);
    public event EventHandler<GpsData> DataRecieved;
}