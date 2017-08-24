﻿namespace ReusableLibraryCode.VisualStudioSolutionFileProcessing
{
    public class VisualStudioProjectReference
    {
        public string Guid;
        public string Path;
        public string Name;

        public VisualStudioProjectReference(string name, string path, string guid)
        {
            Name = name.Trim();
            Path = path.Trim();
            Guid = guid.Trim();
        }
    }
}