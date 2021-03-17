using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Adriva.Web.Controls
{
    internal class ResourceLoader
    {
        private static readonly Assembly CurrentAssembly = typeof(ResourceLoader).Assembly;

        public async Task<string> LoadAsync(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName)) return string.Empty;

            var stream = ResourceLoader.CurrentAssembly.GetManifestResourceStream($"Adriva.Web.Controls.{resourceName}");

            if (null == stream) throw new IOException($"Resource '{resourceName}' could not be found.");

            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                return await reader.ReadToEndAsync();
            }
        }

        public Stream LoadBinary(string resourceName)
        {
            if (string.IsNullOrWhiteSpace(resourceName)) return new MemoryStream(Array.Empty<byte>());

            var stream = ResourceLoader.CurrentAssembly.GetManifestResourceStream($"Adriva.Web.Controls.{resourceName}");

            if (null == stream) throw new IOException($"Resource '{resourceName}' could not be found.");

            return stream;
        }
    }
}