namespace ClientCore
{
    public interface IHttpEncoder
    {
        bool Encode(HttpContent content);
    }
}