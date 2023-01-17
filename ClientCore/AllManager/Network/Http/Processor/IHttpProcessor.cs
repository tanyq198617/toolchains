namespace ClientCore
{
    public class IHttpProcessor
    {
        public virtual bool ProcessBeforeEncoding(HttpContent content)
        {
            return true;
        }

        public virtual bool ProcessAfterEncoding(HttpContent content)
        {
            return true;
        }

        public virtual bool ProcessBeforeDecoding(HttpContent content)
        {
            return true;
        }

        public virtual bool ProcessAfterDecoding(HttpContent content)
        {
            return true;
        }

        public virtual bool ProcessBeforeDispose(HttpContent content)
        {
            return true;
        }
        
        public virtual bool ProcessAfterTimeout(HttpContent content)
        {
            return true;
        }
        
        
        
    }
}