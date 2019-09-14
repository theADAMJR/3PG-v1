namespace Bot3PG.DataStructs
{
    public abstract class GlobalEntity<T>
    {
        internal static T _ID { get; set; }
        public T ID { get => _ID; internal set => _ID = value; }
    }
}