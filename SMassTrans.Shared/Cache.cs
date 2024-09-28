using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Serialization;

namespace SMassTrans.Shared;
public interface ICache<T>
{
    public T Value { get; set; }
    public DateTime LastModified { get;  }
    public TimeSpan TimeToLive { get;  } 
    public bool IsValid { get; }
}
public struct Cache<T>(T v, TimeSpan timeToLive):ICache<T>
{
    public T Value { get; set; } = v;
    public DateTime LastModified { get; set; } = DateTime.UtcNow;
    public TimeSpan TimeToLive { get; set; } = timeToLive;
    public bool IsValid => LastModified + TimeToLive > DateTime.UtcNow;
}

public class FileBackedCache<T>(TimeSpan timeToLive, string name=null) : ICache<T>
{
    static FileBackedCache()
    {
        if (!Path.Exists(folder))
        {
            Directory.CreateDirectory(folder);
        }
    }
    public T Value
    {
        get => GetValue();
        set => SetValue(value);
    }

   
static string folder =   Path.Combine ( Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"smasstrans") ;
    string _fileName = Path.Combine (folder,  (name??  typeof(T).GUID.ToString()) + ".cache.xml.br");
    XmlSerializer _serializer = new(typeof(T));

    T? GetValue()
    {
        if (File.Exists(_fileName))
        {
            using FileStream fileStream = new FileStream(_fileName, FileMode.Open);
            using BrotliStream brotliStream = new(fileStream, CompressionMode.Decompress);
            return (T)_serializer.Deserialize(brotliStream);
        }

        return default;
    }

    void SetValue(T value)
    {
        Debug.WriteLine(_fileName);
        using FileStream fileStream = new FileStream(_fileName, FileMode.OpenOrCreate);
        using BrotliStream brotliStream = new(fileStream, CompressionMode.Compress);
        _serializer.Serialize(brotliStream, value);
    }

    DateTime GetLastModified()
    {
        return File.Exists(_fileName) ? File.GetLastWriteTime(_fileName) : DateTime.MinValue;
    }

    public DateTime LastModified => GetLastModified();
    public TimeSpan TimeToLive { get; } = timeToLive;
    public bool IsValid => LastModified + TimeToLive > DateTime.UtcNow;
}