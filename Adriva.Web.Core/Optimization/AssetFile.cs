using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.IO;

namespace Adriva.Web.Core.Optimization
{

    public enum AssetFileType
    {
        Javascript = 1,
        Stylesheet = 2,
        Html = 3,

    }

    public sealed class AssetFile
    {
        public AssetFileType FileType { get; private set; }

        public string Path { get; private set; }

        public string Content { get; private set; }

        internal AssetFile(AssetFileType fileType, string pathOrUrl)
        {
            this.FileType = fileType;
            this.Path = pathOrUrl;
        }

        public AssetFile(AssetFileType fileType, string pathOrUrl, string content)
            : this(fileType, pathOrUrl)
        {
            this.Content = content;
        }

        public AssetFile WithContent(string content)
        {
            return new AssetFile(this.FileType, this.Path, content);
        }

        public override string ToString()
        {
            return $"{this.Path}, [AssetFile]";
        }

    }
}
