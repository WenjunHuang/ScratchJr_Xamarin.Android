using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Text.Method;
using Android.Util;
using Android.Views;
using Android.Widget;
using Debug = System.Diagnostics.Debug;

namespace ScratchJr.Android
{
    /// <summary>
    /// Manages file storage for ScratchJr.
    /// Also interfaces with the DatabaseManager to clean assets.
    /// </summary>
    /// <author>Wenjun Huang</author>
    public class IOManager
    {
        public const string LogTag = nameof(IOManager);
        public readonly char[] HexArray = "0123456789abcdef".ToCharArray();

        private readonly Activity _application;
        private readonly DatabaseManager _databaseManager;

        /// <summary>
        /// Cache of key to base64-encoded media value
        /// </summary>
        private readonly Dictionary<string, string> _mediaStrings = new Dictionary<string, string>();

        public IOManager(Activity application, DatabaseManager databaseManager)
        {
            _application = application;
            _databaseManager = databaseManager;
        }

        /// <summary>
        /// Clean any assets that are not referenced in the database
        /// </summary>
        /// <param name="fileType">The extension of the type of file to clean</param>
        public async Task CleanAssetsAsync(string fileType)
        {
            var suffix = $".{fileType}";
            Log.Info(LogTag, $"Cleaning files of type '{fileType}'");
            var dir = _application.FilesDir;

            foreach (var file in dir.ListFiles())
            {
                var fileName = file.Name;
                if (fileName.EndsWith(suffix))
                {
                    var statement = "select count(*) from projects where Json like @a";
                    var param = new { a = $"%{fileName}%" };
                    var count = await _databaseManager.QueryFirstOrDefaultAsync<int>(statement, param);
                    if (count > 0) continue;

                    count = await _databaseManager.QueryFirstOrDefaultAsync<int>(
                        "select count(*) from usershapes where MD5=@a", new { a = fileName });
                    if (count > 0) continue;

                    count = await _databaseManager.QueryFirstOrDefaultAsync<int>(
                        "select count(*) from userbkgs where MD5=@a", new { a = fileName });
                    if (count > 0) continue;

                    Log.Info(LogTag, $"Deleting because not found anywhere: {fileName}");
                    file.Delete();
                }
            }
        }

        public async Task<string> SetFileAsync(string fileName, string base64ContentStr)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));
            if (base64ContentStr == null) throw new ArgumentNullException(nameof(base64ContentStr));


            var content = Convert.FromBase64String(base64ContentStr);
            using (var stream = _application.OpenFileOutput(fileName, FileCreationMode.Private))
            {
                await stream.WriteAsync(content, 0, content.Length);
            }
            return fileName;
        }

        public async Task<string> GetFileAsync(string fileName)
        {
            if (fileName == null) throw new ArgumentNullException(nameof(fileName));

            using (var stream = _application.OpenFileInput(fileName))
            using (var buffer = new MemoryStream())
            {
                await stream.CopyToAsync(buffer);
                var data = buffer.ToArray();
                return Convert.ToBase64String(data, Base64FormattingOptions.None);
            }
        }

        /// <summary>
        /// Returns the media data associated with the given filename and return base64-encoded result
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<string> GetMediaAsync(string fileName)
        {
            using (var stream = _application.OpenFileInput(fileName))
            using (var buffer = new MemoryStream())
            {
                await stream.CopyToAsync(buffer);
                var data = buffer.ToArray();
                return Convert.ToBase64String(data, Base64FormattingOptions.None);
            }
        }
    }
}