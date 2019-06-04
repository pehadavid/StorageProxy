namespace StorageProxy.Engines
{
    public interface IChainable
    {
        IChainable GoNext(IChainable next);
    }
}
