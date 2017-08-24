﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace ReusableLibraryCode.VisualStudioSolutionFileProcessing
{
    public class VisualStudioSolutionFile
    {
        public VisualStudioSolutionFolder[] RootFolders { get; set; }
        public VisualStudioProjectReference[] RootProjects { get; set; }

        public List<VisualStudioProjectReference> Projects { get; private set; }
        public List<VisualStudioSolutionFolder> Folders { get; private set; }

        public VisualStudioSolutionFile(FileInfo slnFile)
        {

            var slnFileContents = File.ReadAllText(slnFile.FullName);

            //                                                           Group 1             Group 2                       Group4
            Regex projReg = new Regex(@"Project\(\""\{[\w-]*\}\""\) = \""([\w _]*.*)\"", \""(.*\.(cs|vcx|vb)proj)\"", \""({[\w-]*\})\""", RegexOptions.Compiled);
            //                                                           Group 1                Group 2            Group3
            Regex folderReg = new Regex(@"Project\(\""\{[\w-]*\}\""\) = \""([\w _]*.*)\"", \""([\w\.]*)\"", \""({[\w-]*\})\""", RegexOptions.Compiled);

            Projects = new List<VisualStudioProjectReference>();
            Folders = new List<VisualStudioSolutionFolder>();

            //VisualStudioProjectReference("{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}") = "CachingUI", "Caching\CachingUI\CachingUI.csproj", "{D91F4688-A8BD-4B60-97B6-DFA64A875CFF}"
            foreach (Match m in projReg.Matches(slnFileContents))
                Projects.Add(new VisualStudioProjectReference(m.Groups[1].Value, m.Groups[2].Value, m.Groups[4].Value));

            foreach (Match m in folderReg.Matches(slnFileContents))
            {
                if(!m.Groups[1].Value.Equals(m.Groups[2].Value))
                    throw new Exception("Expected folder name to be the same as folder text when evaluating the projects in solution " + slnFile.FullName + " (Regex matches were'" + m.Groups[1].Value + "' and '" + m.Groups[2].Value + "')");

                Folders.Add(new VisualStudioSolutionFolder(m.Groups[2].Value, m.Groups[3].Value));
            }

            string[] slnFileLines = File.ReadAllLines(slnFile.FullName);

            /*
	GlobalSection(NestedProjects) = preSolution
		{FDF08240-2AFC-48A4-8F48-37CC7EA3000B} = {264C99E2-E3F5-4001-87EA-9CB1B06204AA}
		{16187832-4783-4FD5-A4C7-76E5E3254749} = {264C99E2-E3F5-4001-87EA-9CB1B06204AA}
             EndGlobalSection*/

            bool enteredRelationshipsBit = false;
            for (int i = 0; i < slnFileLines.Length; i++)
            {
                if (slnFileLines[i].Trim().Equals(@"GlobalSection(NestedProjects) = preSolution"))
                {
                    enteredRelationshipsBit = true;
                    continue;
                }

                if (slnFileLines[i].Trim().Equals("EndGlobalSection") && enteredRelationshipsBit)
                    break;

                if (string.IsNullOrWhiteSpace(slnFileLines[i]))
                    continue;

                if (enteredRelationshipsBit)
                {
                    var split = slnFileLines[i].Split('=');

                    var thingInside = split[0].Trim();
                    var thingHoldingIt = split[1].Trim();

                    var folderInside = Folders.SingleOrDefault(f => f.Guid.Equals(thingInside));
                    var folderHoldingIt = Folders.Single(f => f.Guid.Equals(thingHoldingIt));

                    if (folderInside != null)
                        folderHoldingIt.ChildrenFolders.Add(folderInside);
                    else
                    {
                        var visualStudioProjectReferenceInside = Projects.Single(p => p.Guid.Equals(thingInside));
                        Folders.Single(f => f.Guid.Equals(thingHoldingIt)).ChildrenProjects.Add(visualStudioProjectReferenceInside);
                    }
                }
            }

            RootFolders = Folders.Where(f => !Folders.Any(f2 => f2.ChildrenFolders.Contains(f))).ToArray();
            RootProjects = Projects.Where(p => !Folders.Any(f => f.ChildrenProjects.Contains(p))).ToArray();
        }

        
    }
}