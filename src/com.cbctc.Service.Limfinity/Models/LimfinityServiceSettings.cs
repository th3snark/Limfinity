namespace com.cbctc.Service.Limfinity.Models
{
   public class LimfinityServiceSettings
   {
      public LimfinityServiceSettings()
      {
         Configuration = new LimfinityConfiguration();
         Credentials = new LimfinityCredentials();
      }

      public string Key { get; set; }
      public string Name { get; set; }
      public bool Enabled { get; set; }
      public string BaseUrl { get; set; }
      public string LoginWallpaper { get; set; }
      public LimfinityConfiguration Configuration { get; set; }
      public LimfinityCredentials Credentials { get; set; }

      public override string ToString()
      {
         return $"{Name}";
      }
   }
}
