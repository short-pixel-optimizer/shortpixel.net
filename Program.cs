using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShortPixel
{
    class Program
    {
        private static String API_KEY = "__YOUR_API_KEY_HERE_";
        static void Main(string[] args)
        {
            TestShortPixelReducer();
            TestShortPixelReducerWebP();
            TestShortPixelPostReducer();
            TestShortPixelPostReducerWebP_AVIF();
        }

        private static void TestShortPixelReducer()
        {
            ShortPixelLib.ShortPixel shortPixel = new ShortPixelLib.ShortPixel();
            ShortPixelLib.ShortPixelOptions shortPixelOptions = new ShortPixelLib.ShortPixelOptions() { 
                key = API_KEY, 
                resize_width = 800, 
                resize_height = 600 
            };
            List<string> urls = new List<string>() { "https://shortpixel.com/img/shortpixel-on-wp.png",
                "https://shortpixel.com/img/shortpixel-ai.png" };
            shortPixelOptions.urllist = urls;
            List<string> filepaths = new List<string>() { @"c:\temp\file1.jpg", @"c:\temp\file2.jpg" };
            shortPixel.Reducer(shortPixelOptions, filepaths);
        }

        private static void TestShortPixelReducerWebP()
        {
            ShortPixelLib.ShortPixel shortPixel = new ShortPixelLib.ShortPixel();
            ShortPixelLib.ShortPixelOptions shortPixelOptions = new ShortPixelLib.ShortPixelOptions() { 
                key = API_KEY,
                convertto = "+webp",
                resize = "1",
                resize_width = 800, 
                resize_height = 600 
            };
            List<string> urls = new List<string>() { "https://shortpixel.com/img/shortpixel-on-wp.png",
                "https://shortpixel.com/img/shortpixel-ai.png" };
            shortPixelOptions.urllist = urls;
            List<string> filepaths = new List<string>() { @"c:\temp\file1w.jpg", @"c:\temp\file2w.jpg" };
            shortPixel.Reducer(shortPixelOptions, filepaths);
        }

        private static void TestShortPixelPostReducer()
        {
            ShortPixelLib.ShortPixel shortPixel = new ShortPixelLib.ShortPixel();
            ShortPixelLib.ShortPixelOptions shortPixelOptions = new ShortPixelLib.ShortPixelOptions() {
                key = API_KEY,
                resize = "1",
                resize_width = 800, 
                resize_height = 600 
            };
            string fileName = "file3.jpg";
            using (Stream uploadStream = File.OpenRead(@"c:\temp\file3.jpg"))
            {
                ShortPixelLib.ShortPixelResult res = shortPixel.PostReducer(shortPixelOptions, uploadStream, fileName);
                SaveTo(res.Optimized, @"c:\temp\file3opt.jpg");
            }
        }

        private static void TestShortPixelPostReducerWebP_AVIF()
        {
            ShortPixelLib.ShortPixel shortPixel = new ShortPixelLib.ShortPixel();
            ShortPixelLib.ShortPixelOptions shortPixelOptions = new ShortPixelLib.ShortPixelOptions() {
                key = API_KEY,
                lossy = "0",
                convertto = "+webp|+avif"
            };
            string fileName = "file4.jpg";
            using (Stream uploadStream = File.OpenRead(@"c:\temp\file4.jpg"))
            {
                ShortPixelLib.ShortPixelResult res = shortPixel.PostReducer(shortPixelOptions, uploadStream, fileName);
                SaveTo(res.Optimized, @"c:\temp\file4opt.jpg");
                SaveTo(res.OptimizedWebP, @"c:\temp\file4opt.webp");
                SaveTo(res.OptimizedAVIF, @"c:\temp\file4opt.avif");
            }
        }

        private static void SaveTo(Stream from, String toPath)
        {
            if (from != null)
            {
                using (Stream to = File.Create(toPath))
                {
                    from.Seek(0, SeekOrigin.Begin);
                    from.CopyTo(to);
                }
            }
        }
    }
}
