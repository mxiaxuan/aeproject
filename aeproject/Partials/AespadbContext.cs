using Microsoft.EntityFrameworkCore;

namespace aeproject.Models
{
    public partial class AespadbContext : DbContext
    {
        //定義OnConfiguring函式
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                IConfiguration Config = new ConfigurationBuilder()
                    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                    .AddJsonFile("appsetting.json")
                    .Build();
                optionsBuilder.UseSqlServer(Config.GetConnectionString("aespadb"));
            }
        }
    }
}
