namespace StorageProxy
{
    public class RedisConfigurationSettings
    {
        public bool Enabled { get; set; }
        public string Host { get; set; }
        public string Password { get; set; }
        public int Db { get; set; }
    }
}