using ANNProductSync.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ANNProductSync.Utils
{
    public class Thumbnail
    {
        public enum Size
        {
            Source,
            Micro,
            Small,
            Normal,
            Large
        }

        public static string getURL(string image, Size size)
        {
            if (String.IsNullOrEmpty(image))
            {
                return "/App_Themes/Ann/image/placeholder.png";
            }

            var directory = String.Empty;
            switch (size)
            {
                case Size.Micro:
                    directory = "/85x113";
                    break;
                case Size.Small:
                    directory = "/159x212";
                    break;
                case Size.Normal:
                    directory = "/240x320";
                    break;
                case Size.Large:
                    directory = "/350x467";
                    break;
                default:
                    directory = String.Empty;
                    break;
            }
            return String.Format("/uploads/images{0}/{1}", directory, image);
        }

        public static List<string> getURLs(List<string> images, Size size)
        {
            var result = new List<string>();

            foreach (var item in images)
            {
                var image = getURL(item, size);

                if (!String.IsNullOrEmpty(image))
                    result.Add(image);
            }

            return result;
        }

        public static List<ThumbnailModel> getALL(string image)
        {
            var thumbnails = new List<ThumbnailModel>();
            // 85x113
            thumbnails.Add(new ThumbnailModel() { size = "85x113", url = getURL(image, Size.Micro) });
            // 159x212
            thumbnails.Add(new ThumbnailModel() { size = "159x212", url = getURL(image, Size.Small) });
            // 240x320
            thumbnails.Add(new ThumbnailModel() { size = "240x320", url = getURL(image, Size.Normal) });
            // 350x467
            thumbnails.Add(new ThumbnailModel() { size = "350x467", url = getURL(image, Size.Large) });

            return thumbnails;
        }
    }
}
