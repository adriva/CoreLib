namespace Adriva.Extensions.Social
{
    public class BinaryMedia : Media
    {
        public byte[] Data { get; private set; }

        public BinaryMedia(string url, byte[] data) : base(url)
        {
            this.Data = data;
        }
    }
}