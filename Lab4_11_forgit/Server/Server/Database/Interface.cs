using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using SixLabors.ImageSharp;
using System.Collections.ObjectModel;
using NuGetEmotionFP;
using System.Net.NetworkInformation;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Net.Mime.MediaTypeNames;
using Contracts;
using NuGetEmotionFP;
using SixLabors.ImageSharp.PixelFormats;
using System.IO;
using System.Threading.Tasks.Dataflow;

namespace Server
{
    public interface IImagesInterface
    {
        Task<int> PostImage(byte[] img, string local_fileName, CancellationToken ct);
        List<int> GetAllImagesId();
        ImgData? GetImageById(int id);
        Task DeleteAllImages();
    }
    public class DbWorker : IImagesInterface
    {
        private EmotionFP Emotion;
        private SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        public async Task<int> PostImage(byte[] img, string imgname, CancellationToken token)
        {
            Emotion = new EmotionFP(token);
            try
            {
                await semaphore.WaitAsync();
                int id = -1;
                using (var db = new ImagesContext())
                {
                    HashAlgorithm sha = SHA256.Create();
                    var imghash = sha.ComputeHash(img);
                    if (db.Images.Any(x => x.name == imgname))
                    {
                        var query = db.Images.Where(x => x.bytes == img && x.hash == imghash).FirstOrDefault();
                        if (query != null)
                        {
                            id = query.ImageId;
                        }
                    }
                    else
                    {
                        string[] keys = { "neutral", "happiness", "surprise", "sadness", "anger", "disgust", "fear", "contempt" };

                        var result = await Task.Run(async () =>
                        {
                            //Thread.Sleep(100);
                            using SixLabors.ImageSharp.Image<Rgb24> img = SixLabors.ImageSharp.Image.Load<Rgb24>(imgname);
                            var buffer = new BufferBlock<float[]>();
                            Emotion.GetEmotions(img, buffer);
                            float[] emotions = buffer.Receive();
                            var res = keys.Zip(emotions);
                            List<(string, double)> result = new List<(string, double)>();
                            foreach (var i in keys.Zip(emotions))
                            {
                                result.Add(i);
                            }
                            result.Sort((x, y) => -(x.Item2.CompareTo(y.Item2)));
                            return result;
                        }, token);
                        Image image = new Image
                        (
                            imgname,
                            imgname,
                            img,
                            imghash
                        );
                        db.Add(image);
                        db.Add(new Result
                        (
                            image.ImageId,
                            imgname,
                            result
                        ));
                        db.SaveChanges();
                        id = image.ImageId;
                    }
                }
                semaphore.Release();
                return id;
            }
            catch (OperationCanceledException)
            {
                throw new OperationCanceledException("Data processing has been canceled.");
            }
            catch (Exception)
            {
                throw new Exception("Caught an error while processing image.");
            }
        }
        public List<int> GetAllImagesId()
        {
            try
            {
                using (var db = new ImagesContext())
                {
                    var images = db.Images.Select(item => item.ImageId).ToList();
                    return images;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Caught an error while getting images' ids: "+ex.Message);
            }
        }
        public ImgData? GetImageById(int id)
        {
            try
            {
                ImgData result = new ImgData();
                using (var db = new ImagesContext())
                {
                    var query1 = db.Images.Where(x => x.ImageId == id).FirstOrDefault();
                    if (query1 != null)
                    {
                        result.name = query1.name;
                        result.path = query1.path;
                        result.bytes = query1.bytes;
                    }
                    var query2 = db.Results.Where(x => x.ImageId == id).FirstOrDefault();
                    if (query2 != null)
                    {
                        result.name = query2.name;
                        foreach(var i in result.emotions)
                        {
                            query2.emotions.Add(i);
                        }
                        //result.emotions = query2.emotions;
                    }
                }
                return result;
            }
            catch (Exception)
            {
                throw new Exception("Caught an error while getting image.");
            }
        }
        public async Task DeleteAllImages()
        {
            try
            {
                await semaphore.WaitAsync();
                using (var db = new ImagesContext())
                {
                    await db.Database.ExecuteSqlRawAsync("DELETE FROM [Images]");
                    await db.Database.ExecuteSqlRawAsync("DELETE FROM [Results]");
                }
                semaphore.Release();
            }
            catch (Exception)
            {
                throw new Exception("Caught an error while delliting.");
            }
        }
    }

}
