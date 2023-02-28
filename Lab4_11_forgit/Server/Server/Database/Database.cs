using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;

namespace Server
{
    public class ImagesContext : DbContext
    {
        public DbSet<Image> Images { get; set; }
        public DbSet<Result> Results { get; set; }
        public ImagesContext() => Database.EnsureCreated();
        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        {
            builder.UseSqlite("Data Source=ImgDB1.db");
        }
    }
    public class Image
    {
        [Key]
        public int ImageId { get; set; }
        public string name { get; set; }
        public string path { get; set; }
        public byte[] hash { get; set; }
        public byte[] bytes { get; set; }
        public Image() { }
        public Image(string nameimg, string pathimg, byte[] hashimg, byte[] byteimg)
        {
            name = nameimg;
            path = pathimg;
            hash = hashimg;
            bytes = byteimg;
        }
    }

    public class Result
    {
        [Key]
        [ForeignKey(nameof(Image))]
        public int ImageId { get; set; }
        public string name { get; set; }

        public List<(string, double)> emotions;
        public Result() { emotions = new List<(string, double)>(); }
        public Result(int imageId, string nameimg, List<(string, double)> emotionsimg)
        {
            //ImageId = imageId;
            name = nameimg;
            emotions = new List<(string, double)>();
            emotions = emotionsimg;
        }
    }


}
