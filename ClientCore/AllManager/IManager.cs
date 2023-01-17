namespace ClientCore
{
    public interface IManager
    {
        void Initialize();
        void Dispose();
    }

    public interface ITicker
    {
        void Tick(float delta);
    }
    
}