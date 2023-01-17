namespace ClientCore
{
    public interface IHttpDecoder
    {
        bool Decode(HttpContent content);
    }
}