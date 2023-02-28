using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    public class ImageInfo
    {
        public string path { get; set; }
        public List<(string, double)> emotions;
        public ImageInfo() { emotions = new List<(string, double)>(); }
        public ImageInfo(string str, List<(string, double)> lst)
        {
            path = str;
            emotions = new List<(string, double)>();
            foreach (var i in lst)
            {
                emotions.Add(i);
            }
            //emotions = lst;
        }
    }
    public struct ImgData
    {
        public int ImageId { get; set; }
        public string name { get; set; }
        public string path { get; set; }
        public byte[] bytes { get; set; }
        public List<(string, double)> emotions;
        public ImgData() { emotions = new List<(string, double)>(); }
        public ImgData(string str, List<(string, double)> lst)
        {
            path = str;
            emotions = new List<(string, double)>();
            foreach (var i in lst)
            {
                emotions.Add(i);
            }
            //emotions = lst;
        }
    }
    public class ImgPost
    {
        public byte[] byte_img { get; set; }
        public string path_img { get; set; }

        public ImgPost() { }

        public ImgPost(byte[] bytes, string path)
        {
            this.byte_img = bytes;
            this.path_img = path;
        }
    }
}