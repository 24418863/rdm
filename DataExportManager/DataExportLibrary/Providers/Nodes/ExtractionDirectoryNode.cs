﻿using System.IO;
using CatalogueLibrary.Data.Cohort;
using CatalogueLibrary.Nodes.LoadMetadataNodes;
using DataExportLibrary.Data.DataTables;
using DataExportLibrary.ExtractionTime.ExtractionPipeline.Destinations;

namespace DataExportLibrary.Providers.Nodes
{
    /// <summary>
    /// Location on disk in which linked project extracts are generated for a given <see cref="Project"/> (assuming you are extracting to disk
    /// e.g. with an <see cref="ExecuteDatasetExtractionFlatFileDestination"/>).
    /// </summary>
    public class ExtractionDirectoryNode : IDirectoryInfoNode, IOrderable
    {
        public Project Project { get; private set; }

        public ExtractionDirectoryNode(Project project)
        {
            Project = project;
        }

        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(Project.ExtractionDirectory))
                return "???";

            return Project.ExtractionDirectory;
        }

        protected bool Equals(ExtractionDirectoryNode other)
        {
            return Equals(Project, other.Project);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ExtractionDirectoryNode) obj);
        }

        public override int GetHashCode()
        {
            return (Project != null ? Project.GetHashCode() : 0);
        }

        public DirectoryInfo GetDirectoryInfoIfAny()
        {
            if (string.IsNullOrWhiteSpace(Project.ExtractionDirectory))
                return null;

            return new DirectoryInfo(Project.ExtractionDirectory);
        }

        public int Order{ get { return 4; } set { }}
    }
}
