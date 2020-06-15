using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;

namespace Adriva.Extensions.Reports
{
    public sealed class ReportingServiceOptions
    {
        private string RootPathField;
        private List<Type> DataSourceTypeList = new List<Type>();

        public string RootPath
        {
            get => this.RootPathField;
            set => this.RootPathField = Path.GetFullPath(value);
        }

        public bool DisableCache { get; set; }

        public CultureInfo Culture { get; set; }

        public string EnvironmentName { get; set; }

        public bool IsDebugMode { get; set; }

        public ReadOnlyCollection<Type> DataSourceTypes => new ReadOnlyCollection<Type>(this.DataSourceTypeList);

        public ReportingServiceOptions()
        {
            this.RootPath = Directory.GetCurrentDirectory();
            this.Culture = CultureInfo.CurrentCulture;
            this.DataSourceTypeList.Add(typeof(DataSources.EnumDataSource));
            this.DataSourceTypeList.Add(typeof(DataSources.ObjectDataSource));
        }

        public ReportingServiceOptions UseDataSource<TDataSource, TParameters>() where TDataSource : DataSource<TParameters> where TParameters : class, new()
        {
            this.DataSourceTypeList.Add(typeof(TDataSource));
            return this;
        }

        public ReportingServiceOptions UseRenderer<TRenderer>()
        {
            return this;
        }
    }
}