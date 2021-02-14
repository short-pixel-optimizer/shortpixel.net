using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace ShortPixelLib
{
    /// <summary>
    /// Developed by Michael Dyhr Iversen @ QCompany.dk for ShortPixel
    /// Updates by Simon Duduica
    /// </summary>
    public class ShortPixel
    {
        /// <summary>
        /// Sends urls to ShortPixel and waits for a response.
        /// </summary>
        /// <param name="shortPixelOptions">Options to send, like APIKey, file URLs and more, see https://shortpixel.com/api-docs for full list.</param>
        /// <param name="filepaths">File names to save to. If null, returns streams</param>
        /// <returns>List<ShortPixelResult> that also contains the streams if filepaths not provided</returns>
        public List<ShortPixelResult> Reducer(ShortPixelOptions shortPixelOptions, List<string> filepaths = null)
        {
            if (filepaths != null && filepaths.Count != shortPixelOptions.urllist.Count)
                throw new Exception("Count of filepaths and list of urls doesn't match");
            Uri uRI = new Uri("https://api.shortpixel.com/v2/reducer.php");
            List<ShortPixelResult> ret = new List<ShortPixelResult>();

            using (HttpClient client = new HttpClient())
            {
                client.DefaultRequestHeaders.Accept.Clear();
                using (var formData = new MultipartFormDataContent())
                {
                    string json = JsonConvert.SerializeObject(shortPixelOptions);
                    var response = client.PostAsync("https://api.shortpixel.com/v2/reducer.php", new StringContent(json, Encoding.UTF8, "application/json")).Result;
                    var responseContent = response.Content.ReadAsStringAsync().Result;

                    if (responseContent != null)
                    {
                        // var spo = JsonConvert.DeserializeObject<ShortPixelModel>(responseContent);
                        var array = (JArray)JsonConvert.DeserializeObject(responseContent);
                        var serializer = new JsonSerializer();
                        for (int i = 0; i < array.Count; i++)
                        {
                            var spo = serializer.Deserialize<ShortPixelModel>(array[i].CreateReader());
                            if (spo != null)
                            {
                                if (spo.Status.Code == 2)
                                {
                                    bool LossyFields = (shortPixelOptions.lossy == "1" || shortPixelOptions.lossy == "2");
                                    var OptURL = LossyFields ? spo.LossyURL : spo.LosslessURL;
                                    if (OptURL.Length > 0)
                                    {
                                        using (WebClient webClient = new WebClient())
                                        {
                                            ShortPixelResult res = new ShortPixelResult();
                                            var filepath = filepaths != null ? filepaths[i] : null;
                                            var filepathBase = filepaths != null ? Path.GetDirectoryName(filepath) + "\\" + Path.GetFileNameWithoutExtension(filepath) : null;
                                            res.meta = spo;
                                            var optURL = LossyFields ? spo.LossyURL : spo.LosslessURL;
                                            res.Optimized = GetFromUrl(webClient, optURL, filepath);
                                            var WebPURL = LossyFields ? spo.WebPLossyURL : spo.WebPLosslessURL;
                                            res.OptimizedWebP = (WebPURL != "NA") ? GetFromUrl(webClient, WebPURL, filepathBase != null ? filepathBase + ".webp" : null) : null;
                                            var AVIFURL = LossyFields ? spo.AVIFLossyURL : spo.AVIFLosslessURL;
                                            res.OptimizedAVIF = (AVIFURL != "NA") ? GetFromUrl(webClient, AVIFURL, filepathBase != null ? filepathBase + ".avif" : null) : null;
                                            ret.Add(res);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return ret;
        }

        /// <summary>
        /// Sends the image to ShortPixel and waits for a response.
        /// </summary>
        /// <param name="shortPixelOptions">Options to send, like APIKey with more.</param>
        /// <param name="fileStream">The image as a fileStream</param>
        /// <param name="filename">The name of the image</param>
        /// <returns>File Stream</returns>
        public ShortPixelResult PostReducer(ShortPixelOptions shortPixelOptions, Stream fileStream, string filename)
        {
            ShortPixelModel shortPixelObject = new ShortPixelModel();
            int width = shortPixelOptions.resize_width;
            int height = shortPixelOptions.resize_height;
            try
            {
                HttpContent imageBytesContent = new ByteArrayContent(ReadFully(fileStream));
                imageBytesContent.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Clear();
                    using (var formData = new MultipartFormDataContent())
                    {
                        formData.Add(new StringContent(shortPixelOptions.key), "key");
                        formData.Add(new StringContent("C3API"), "plugin_version");
                        formData.Add(new StringContent(shortPixelOptions.lossy), "lossy");
                        formData.Add(new StringContent(shortPixelOptions.wait.ToString()), "wait");
                        formData.Add(new StringContent(shortPixelOptions.convertto), "convertto");
                        // Resizing options
                        if (shortPixelOptions.resize != "0")
                        {
                            if (height > 0 || width > 0)
                            {
                                formData.Add(new StringContent(shortPixelOptions.resize.ToString()), "resize");
                                if (height > 0)
                                    formData.Add(new StringContent(height.ToString()), "resize_height");
                                if (width > 0)
                                    formData.Add(new StringContent(width.ToString()), "resize_width");
                            }
                        }
                        else
                            formData.Add(new StringContent("0"), "resize");
                        formData.Add(new StringContent(@"{""file1"": """ + filename + @"""}"), "file_paths");
                        formData.Add(imageBytesContent, "file1", filename);

                        var response = client.PostAsync("https://api.shortpixel.com/v2/post-reducer.php", formData).Result;
                        var responseContent = response.Content.ReadAsStringAsync().Result;

                        if (responseContent != null)
                        {
                            // var spo = JsonConvert.DeserializeObject<ShortPixelModel>(responseContent);
                            var array = (JArray)JsonConvert.DeserializeObject(responseContent);
                            var serializer = new JsonSerializer();
                            var spo = serializer.Deserialize<ShortPixelModel>(array[0].CreateReader());
                            if (spo != null)
                            {
                                if (spo.Status.Code == 1 || spo.Status.Code == 2)
                                {
                                    if (spo.Status.Code == 2 && spo.LossyURL.Length > 0)
                                    {
                                        bool LossyFields = (shortPixelOptions.lossy == "1" || shortPixelOptions.lossy == "2");
                                        using (WebClient webClient = new WebClient())
                                        {
                                            ShortPixelResult res = new ShortPixelResult();
                                            res.meta = spo;
                                            var optURL = LossyFields ? spo.LossyURL : spo.LosslessURL;
                                            res.Optimized = GetFromUrl(webClient, optURL);
                                            var WebPURL = LossyFields ? spo.WebPLossyURL : spo.WebPLosslessURL;
                                            res.OptimizedWebP = (WebPURL != "NA") ? GetFromUrl(webClient, WebPURL) :null;
                                            var AVIFURL = LossyFields ? spo.AVIFLossyURL : spo.AVIFLosslessURL;
                                            res.OptimizedAVIF = (AVIFURL != "NA") ? GetFromUrl(webClient, AVIFURL) : null;
                                            return res;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception err)
            {
                throw new Exception("Error in ShortPixel API", err);
            }
            return null;
        }

        private Stream GetFromUrl(WebClient webClient, String URL, String filepath = null) {
            if(filepath == null) {
                byte[] imageBytesAVIF = webClient.DownloadData(URL);
                return (imageBytesAVIF.Length > 0) ? new MemoryStream(imageBytesAVIF) : null;
            } else {
                webClient.DownloadFile(URL, filepath);
                return null;
            }
        }

        private byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }
    }
    public class ShortPixelOptions
    {
        /// <summary>
        /// Max 5 chars
        /// </summary>
        public string plugin_version = "DOTNET";

        /// <summary>
        /// Get your api key here: https://shortpixel.com/show-api-key
        /// </summary>
        public string key;

        /// <summary>
        /// The type of resizing applied to the image.
        /// 0 - no resizing, the current size is preserved.
        /// 1 - outer resizing(image will contain the given resize_width x resize_height rectangle).
        /// 3 means inner resizing(image will be contained in the given resize_width x resize_height rectangle).
        /// </summary>
        public string resize = "0";

        /// <summary>
        /// Controls whether the image compression will use a lossy or lossless algorithm.
        // 1 - Lossy compression.Lossy has a better compression rate than lossless compression. The resulting image is not 100% identical with the original. Works well for photos taken with your camera.
        // 2 - Glossy compression. Creates images that are almost pixel-perfect identical to the originals.Best option for photographers and other professionals that use very high quality images on their sites and want best compression while keeping the quality untouched.
        // 0 - Lossless compression. The shrunk image will be identical with the original and smaller in size.Use this when you do not want to loose any of the original image's details. Works best for technical drawings, clip art and comics.
        /// Default value: 1
        /// </summary>
        public string lossy = "1";

        /// <summary>
        /// Set it between 1 and 30 to wait for that number of seconds for the conversion to be done before the API returns, 0 to return immediately.
        /// If the optimization is not done in the given amount of wait time, Code 1 (see below) is returned and you can just redo the same post later to find out if the image is ready.
        /// </summary>
        public int wait = 30;

        /// <summary>
        /// Width of the image
        /// </summary>
        public int resize_width = 0;

        /// <summary>
        /// Height of the image
        /// </summary>
        public int resize_height = 0;

        /// <summary>
        /// Convert CMYK to RGB, default is set to 1. Images for the web only need RGB format and converting them from CMYK to RGB makes them smaller.
        /// </summary>
        public string cmyk2rgb = "1";

        /// <summary>
        /// 1: refresh, 0: refresh if not found in cache
        /// </summary>
        public string refresh = "1";

        /// <summary>
        /// Keep the EXIF tag of the image. Default is set to 0, meaning the EXIF tag is removed.
        /// </summary>
        public string keep_exif = "0";

        /// <summary>
        /// ConvertTo. Default empty, no conversion. '+webp' also creates WebP version, '+avif' also AVIF and '+webp|+avif' both.
        /// </summary>
        public string convertto = "";

        /// <summary>
        /// List of urls to convert for Reducer, not used in post-reducer
        /// </summary>
        public List<string> urllist = new List<string>();
    }

    public class ShortPixelModel
    {
        [JsonProperty("Status")]
        public ShortPixelStatus Status;
        [JsonProperty("OriginalURL")]
        public string OriginalURL = string.Empty;
        [JsonProperty("LossyURL")]
        public string LossyURL = string.Empty;
        [JsonProperty("LosslessURL")]
        public string LosslessURL = string.Empty;
        [JsonProperty("WebPLossyURL")]
        public string WebPLossyURL = string.Empty;
        [JsonProperty("WebPLosslessURL")]
        public string WebPLosslessURL = string.Empty;
        [JsonProperty("AVIFLossyURL")]
        public string AVIFLossyURL = string.Empty;
        [JsonProperty("AVIFLosslessURL")]
        public string AVIFLosslessURL = string.Empty;
        [JsonProperty("OriginalSize")]
        public int OriginalSize = 0;
        [JsonProperty("LossySize")]
        public int LossySize = 0;
        [JsonProperty("LosslessSize")]
        public int LosslessSize = 0;
        [JsonProperty("TimeStamp")]
        public string TimeStamp = string.Empty;
        [JsonProperty("PercentImprovement")]
        public string PercentImprovement = string.Empty;
        /*
        "OriginalURL": "http://api.shortpixel.com/u/*.jpg",
        "LosslessURL": "http://api.shortpixel.com/f/*.jpg",
        "LossyURL": "http://api.shortpixel.com/f/*-lossy.jpg",
        "WebPLosslessURL": "NA",
        "WebPLossyURL": "NA",
        "AVIFLosslessURL": "NA",
        "AVIFLossyURL": "NA",
        "OriginalSize": "14488498",
        "LosslessSize": "300554",
        "LoselessSize": "300554",
        "LossySize": "189899",
        "WebPLosslessSize": "NA",
        "WebPLoselessSize": "NA",
        "WebPLossySize": "NA",
        "AVIFLosslessSize": "NA",
        "AVIFLossySize": "NA",
        "TimeStamp": "2021-01-19 14:52:48",
        "PercentImprovement": "98.69",
        "Key": "file1",
        "localPath": "/usr/local/ssd-drive/shortpixel/*"*/
    }

    public class ShortPixelStatus
    {
        [JsonProperty("Code")]
        public int Code;
        [JsonProperty("Message")]
        public string Message;
    }

    public class ShortPixelResult
    {
        public ShortPixelModel meta;
        public Stream Optimized;
        public Stream OptimizedWebP;
        public Stream OptimizedAVIF;
    }
}