#nullable enable

namespace Edanoue.SaveData
{
    public interface ISaveDataElement
    {
        string Key { get; }
    }

    public interface ISaveDataElement<T> : ISaveDataElement
    {
        T Value { get; }
    }
}
