using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace ExcellentEmailExperience.Helpers
{
    static class ThumbnailFromPath
    {
        public static BitmapImage GetThumbnailFromPath(string x)
        {
            StorageFile attachment = StorageFile.GetFileFromPathAsync(x).AsTask().Result;
            var thumbnail = attachment.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem).AsTask().Result;

            BitmapImage bitmapImage = new();
            bitmapImage.SetSource(thumbnail);
            return bitmapImage;
        }
    }
}
