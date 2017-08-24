﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using CatalogueLibrary.Reports.Exceptions;
using MapsDirectlyToDatabaseTable;
using ReusableLibraryCode.Checks;

namespace CatalogueLibrary.Reports
{
    public class DocumentationReportMapsDirectlyToDatabase: ICheckable
    {
        private readonly Assembly _assembly;
        private const int NumberOfNewlinesPerParagraph = 1;

        public Dictionary<Type, string> Summaries = new Dictionary<Type, string>();

        public DocumentationReportMapsDirectlyToDatabase(Assembly assembly)
        {
            _assembly = assembly;
            
        }

        public void Check(ICheckNotifier notifier)
        {
            string zipArchive = "SourceCodeForSelfAwareness.zip";

            if (!File.Exists(zipArchive))
                throw new SourceCodeNotFoundException("Could not find file'" + zipArchive + "' use CatalogueLibrary.Repositories.CatalogueRepository.SuppressHelpLoading=true to avoid this problem");

            using (var z = ZipFile.Open(zipArchive,ZipArchiveMode.Read))
            {
                foreach (Type t in _assembly.GetTypes())
                    if (typeof (IMapsDirectlyToDatabaseTable).IsAssignableFrom(t))
                    {
                        if (t.IsInterface || t.IsAbstract)
                            continue;
                        try
                        {
                            //spontaneous objects don't exist in the database.
                            if(t.Name.StartsWith("Spontaneous"))
                                continue;
                        }
                        catch(Exception)
                        {
                            continue;
                        }
                        notifier.OnCheckPerformed(new CheckEventArgs("Found type " + t, CheckResult.Success));

                        string toFind = t.Name + ".cs";

                        var entries = z.Entries.Where(e => e.Name == toFind).ToArray();

                        if (entries.Length != 1)
                        {
                            notifier.OnCheckPerformed(
                                new CheckEventArgs("Found " + entries.Length + " files called " + toFind,
                                    CheckResult.Fail));
                            continue;
                        }

                        string classSourceCode = new StreamReader(entries[0].Open()).ReadToEnd();

                        try
                        {
                            string definition = GetSummaryFromContent(t, classSourceCode, notifier);
                            if (definition != null)
                                Summaries.Add(t, definition);
                        }
                        catch (Exception e)
                        {
                            notifier.OnCheckPerformed(
                                new CheckEventArgs("Failed to get definition for class " + t.FullName, CheckResult.Fail,
                                    e));
                        }

                    }
            }

        }

        public static string GetSummaryFromContent(Type t, string classSourceCode, ICheckNotifier notifier)
        {

            string pattern = @"class\s+" + t.Name;

            //if it's a generic
            if (pattern.EndsWith("`1"))
                pattern = pattern.Substring(0, pattern.Length - "`1".Length);//trim off the generic bit

            Regex rFirstSummary = new Regex(pattern);


            MatchCollection matches = rFirstSummary.Matches(classSourceCode);

            if (matches.Count == 0)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Found 0 matches for regex " + pattern, CheckResult.Fail));
                return null;
            }

            string definition = "";
            int characterIndex = matches[0].Index;

            while (!definition.Contains("<summary>") && characterIndex > 0)
            {
                definition = classSourceCode[characterIndex] + definition;//add it to the front
                characterIndex--;
            }

            if (definition.LastIndexOf("</summary>") == -1)
                return null;

            //trim off <summary>
            definition = definition.Substring("<summary>".Length);
            definition = definition.Substring(0,definition.LastIndexOf("</summary>"));


            //get rid of triple slashes
            definition = definition.Replace(@"///"," ");
            
            //convert all double newlines into ppp
            definition = Regex.Replace(definition, @"\n[^\S\n]*\n", "<ppp>");
            
            //now replace all multiple whitespace with space
            definition = Regex.Replace(definition, @"[\s]+", " ");


            string newlines ="";

            for (int i = 0; i < NumberOfNewlinesPerParagraph; i++)
                newlines += "\r\n";
            
            //now put back paragraphs
            definition = definition.Replace("<ppp>", newlines);

            definition = definition.Trim();
            
            //trim preceeding whitespace from lines
            definition = new Regex(@"^ ", RegexOptions.Multiline).Replace(definition, "");
            
            if (characterIndex == 0)
                notifier.OnCheckPerformed(new CheckEventArgs("Did not find <summary> in file ", CheckResult.Fail));

            return definition;
        }
    }
}