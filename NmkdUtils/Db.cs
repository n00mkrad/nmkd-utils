using LiteDB;
using System.Diagnostics;

namespace NmkdUtils;

public class Db
{
    public static string Filename = "main.db"; // Default filename for the database
    public static string DbFile => Path.Combine(PathUtils.GetCommonSubdir(PathUtils.CommonDir.Db), Filename);
    private static readonly LiteDatabase _db = new LiteDatabase(DbFile);

    ///<summary> Save the DB - <inheritdoc cref="LiteDatabase.Checkpoint"/> </summary>/>
    public static void Save() => _db?.Checkpoint();

    /// <summary> Gets a key/value <paramref name="collection"/> by name. If no name is provided, the type name is used. </summary>
    public static ILiteCollection<KvEntry<TKey, TVal>> GetKv<TKey, TVal>(object collection) where TKey : notnull
        => Get<KvEntry<TKey, TVal>>(collection);

    /// <summary> Gets a <paramref name="collection"/> by name. If no name is provided, the type name is used. </summary>
    public static ILiteCollection<T> Get<T> (object? collection = null)
    {
        collection ??= typeof(T).Name + "s";
        return _db.GetCollection<T>(collection.ToString());
    }

    /// <inheritdoc cref="ILiteCollection{T}.Insert(T)"/>
    public static void Insert<T>(T item, string? collection = null) => Get<T>(collection).Insert(item);

    /// <inheritdoc cref="ILiteCollection{T}.InsertBulk(IEnumerable{T}, int)"/>
    public static void Insert<T>(IEnumerable<T> items, string? collection = null) => Get<T>(collection).InsertBulk(items);

    /// <summary> Deletes a collection and saves the DB. </summary>
    public static void DropCollection(string collection)
    {
        _db.DropCollection(collection);
        Save();
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
