using LiteDB;
using System.Diagnostics;

namespace NmkdUtils;

public class Db
{
    public static string Filename = "main.db"; // Default filename for the database
    public static string DbFile => Path.Combine(PathUtils.GetCommonSubdir(PathUtils.CommonDir.Db), Filename);
    private static readonly LiteDatabase _db = new LiteDatabase(DbFile);

    public static void Save() => _db?.Checkpoint();

    public static ILiteCollection<KvEntry<TKey, TVal>> GetKv<TKey, TVal>(object collection) where TKey : notnull
        => _db.GetCollection<KvEntry<TKey, TVal>>($"{collection}");

    public static ILiteCollection<T> Get<T> (object? collection = null)
    {
        collection ??= typeof(T).Name + "s";
        return _db.GetCollection<T>(collection.ToString());
    }

    public static void Insert<T>(T item, string? collection = null)
    {
        collection ??= typeof(T).Name + "s";
        _db.GetCollection<T>(collection).Insert(item);
    }

    public static void Insert<T>(IEnumerable<T> items, string? collection = null)
    {
        collection ??= typeof(T).Name + "s";
        _db.GetCollection<T>(collection).InsertBulk(items);
    }

    #region Standard Models

    [DebuggerDisplay("{Key} = {Value}")]
    public class KvEntry<TKey, TValue>(TKey key, TValue value)
    {
        [BsonId] public TKey Key { get; set; } = key;
        public TValue Value { get; set; } = value;
    }

    #endregion
}
