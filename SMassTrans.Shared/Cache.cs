using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Serialization;

namespace SMassTrans.Shared;

/// <summary>
/// Defines the interface for a cache entry that holds typed data and metadata about its validity.
/// </summary>
/// <typeparam name="T">The type of value stored in the cache.</typeparam>
public interface ICache<T>
{
    /// <summary>
    /// Gets or sets the cached value of type <typeparamref name="T"/>.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Gets the date and time when this cache entry was last modified or set.
    /// </summary>
    public DateTime LastModified { get;  }

    /// <summary>
    /// Gets the predetermined duration for which the cached value is considered valid before expiration.
    /// </summary>
    public TimeSpan TimeToLive { get;  }

    /// <summary>
    /// Gets a value indicating whether the current object instance represents valid data.
    /// </summary>
    public bool IsValid { get; }
}

/// <summary>
/// Represents a simple in-memory cache entry holding a value of type <typeparamref name="T"/> and its associated lifetime parameters.
/// </summary>
/// <typeparam name="T">The type of value stored in the cache.</typeparam>
public struct Cache<T>(T v, TimeSpan timeToLive):ICache<T>
{
    /// <summary>
    /// Gets or sets the cached value of type <typeparamref name="T"/>.
    /// </summary>
    /// <remarks>
    /// If set, the value is written to both memory and the configured file system location (if applicable).
    /// Getting the value attempts to retrieve it from memory first, then checks the persistent storage.
    /// </remarks>
    public T Value { get; init; } = v;

    /// <summary>
    /// Gets the last time the cache entry was modified, which can be either when it was cached in memory or persisted to the file system.
    /// </summary>
    public DateTime LastModified { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets the duration for which the cached value remains valid.
    /// </summary>
    public TimeSpan TimeToLive { get; set; } = timeToLive;

    /// <summary>
    /// Gets a value indicating whether the cache entry is currently valid based on its last modified time and time to live.
    /// </summary>
    public bool IsValid => LastModified + TimeToLive > DateTime.UtcNow;
}

/// <summary>
/// Provides a cache implementation that stores data both in memory and persists it to a file system location.
/// </summary>
/// <typeparam name="T">The type of value stored in the cache.</typeparam>
public class FileBackedCache<T>(TimeSpan timeToLive, string name=null, bool allowMemCache=true) : ICache<T>
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
        if (allowMemCache && memCache is { IsValid: true } x)
        {
           return x.Value;
        }

        if (!File.Exists(_fileName)) return default;
        try
        {
            using var fileStream = new FileStream(_fileName, FileMode.Open);
            using BrotliStream brotliStream = new(fileStream, CompressionMode.Decompress);
            var value = (T?)_serializer.Deserialize(brotliStream);
            if (allowMemCache)
            {
                memCache = new Cache<T>(value, TimeToLive);
            }
            return value;
        }
        catch (Exception e)
        {
            Debug.WriteLine(e);
            return default;
        }
    }
    Cache<T>? memCache = null;
    void SetValue(T value)
    {
        if (allowMemCache)
        {
            memCache = new Cache<T>(value, TimeToLive);
        }
        using var fileStream = new FileStream(_fileName, FileMode.OpenOrCreate);
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