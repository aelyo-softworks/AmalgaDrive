using System.Collections.Concurrent;

namespace AmalgaDrive.Utilities
{
    public interface IChangeTrackingDictionaryObject : IDictionaryObject
    {
        ConcurrentDictionary<string, DictionaryObjectProperty> ChangedProperties { get; }

        void CommitChanges();
        void RollbackChanges(DictionaryObjectPropertySetOptions options);
    }
}
