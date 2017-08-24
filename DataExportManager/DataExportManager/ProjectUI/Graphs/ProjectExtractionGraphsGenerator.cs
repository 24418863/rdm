﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using CatalogueLibrary;
using CatalogueManager.AggregationUIs;
using DataExportLibrary.Data.DataTables;
using Microsoft.Office.Interop.Word;
using ReusableLibraryCode.Checks;

namespace DataExportManager.ProjectUI.Graphs
{
    public class ProjectExtractionGraphsGenerator
    {
        private readonly DirectoryInfo _root;
        /*

        /// <summary>
        /// Key is the name of the dataset being extracted, the list is the graphs for that dataset
        /// </summary>
        private readonly Dictionary<string, List<LiveVsCohortGraphs>> _toExtract;
        private readonly ExtractionConfiguration _configuration;


        /// <summary>
        /// Set this to true if you want microsoft word to be visible while it is running Interop commands (will be very confusing for users so never ship this with true)
        /// </summary>
        public bool DEBUG_WORD = false;


        #region stuff for Word
        object oTrue = true;
        object oFalse = false;
        Object oMissing = System.Reflection.Missing.Value;

        Microsoft.Office.Interop.Word.Application wrdApp;
        Microsoft.Office.Interop.Word._Document wrdDoc;

        #endregion

        public ProjectExtractionGraphsGenerator(Project project, Dictionary<string, List<LiveVsCohortGraphs>> toExtract, ExtractionConfiguration configuration)
        {
            _toExtract = toExtract;
            _configuration = configuration;
            _root = PrepareRootFolder(project);
        }

        private DirectoryInfo PrepareRootFolder(Project project)
        {
            string targetDir = Path.Combine(project.ExtractionDirectory, "ExtractionGraphs");
            var dir = new DirectoryInfo(targetDir);

            //ensure the root ExtractionGraphs directory exists
            if (!dir.Exists)
                dir.Create();

            //now create a cohort specific graph
            string cohortDir = "Cohort" + _configuration.Cohort_ID;

            var alreadyExisting = dir.GetDirectories().SingleOrDefault(d => d.Name.Equals(cohortDir));
            if(alreadyExisting != null)
                if (
                    MessageBox.Show("Folder " + alreadyExisting.FullName + " already exists, delete it?",
                        "Delete Existing Project Graphs", MessageBoxButtons.YesNoCancel) == DialogResult.Yes)
                {
                    alreadyExisting.Delete(true);
                }
                else
                    return alreadyExisting;

            return dir.CreateSubdirectory(cohortDir);
        }

        public void Generate(ICheckNotifier notifier)
        {
            if (_root.Exists)
                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        "Root target directory " + _root.FullName + (_root.Exists ? " Exists" : " Doesn't Exist"),
                        _root.Exists ? CheckResult.Success : CheckResult.Fail));
            try
            {
                Dictionary<AggregateGraph,string> graphSaveLocations = new Dictionary<AggregateGraph, string>();

                foreach (var kvp in _toExtract)
                {
                    var subdir = _root.CreateSubdirectory(kvp.Key);
                    notifier.OnCheckPerformed(new CheckEventArgs("Created dataset folder " + kvp.Key, CheckResult.Success));
                
                    foreach (LiveVsCohortGraphs vsGraph in kvp.Value)
                    {
                        string nameOfAggregate = vsGraph.LiveAggregateConfiguration.Name;

                        vsGraph.Live.SaveTo(subdir, "LIVE_" + nameOfAggregate, notifier, graphSaveLocations);
                        vsGraph.Extract.SaveTo(subdir, "EXTRACT_" + nameOfAggregate, notifier, graphSaveLocations);
                    }
                }

                CreateWordDocument(graphSaveLocations,notifier);

            }
            catch (Exception e)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("Failed to generate graphs of live vs extract",CheckResult.Fail, e));
            }
        }

        private void CreateWordDocument(Dictionary<AggregateGraph, string> graphSaveLocations, ICheckNotifier notifier)
        {

            // Create an instance of Word  and make it visible.=
            wrdApp = new Microsoft.Office.Interop.Word.Application();

            //normally we hide word and suppress popups but it might be that word is being broken in which case we would want to watch it as it outputs stuff
            if (!DEBUG_WORD)
            {
                wrdApp.Visible = false;
                wrdApp.DisplayAlerts = WdAlertLevel.wdAlertsNone;
            }
            else
            {
                wrdApp.Visible = true;
            }
            //add blank new word
            wrdDoc = wrdApp.Documents.Add(ref oMissing, ref oMissing, ref oMissing, ref oMissing);

            try
            {
                wrdDoc.Select();
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("RETRYLATER"))
                    Thread.Sleep(2000);

                wrdDoc.Select();
            }

            var c = _configuration.Cohort;

            WordHelper wordHelper = new WordHelper(wrdApp);

            wordHelper.WriteLine("Your Extraction Vs HIC Main Repository Report For Cohort " + c + " (ID=" + c.ID+")", WdBuiltinStyle.wdStyleHeading1);

            bool wroteAtLeastOneGraph = false;

            foreach (KeyValuePair<string, List<LiveVsCohortGraphs>>kvp in _toExtract)
            {
                //if none of the graph pairs were successfully extracted
                if(!kvp.Value.Any(g=>graphSaveLocations.ContainsKey(g.Live) && graphSaveLocations.ContainsKey(g.Extract)))
                    continue;
                
                wordHelper.GoToEndOfDocument();
                wordHelper.WriteLine(kvp.Key, WdBuiltinStyle.wdStyleHeading2);
                
                object start = wrdDoc.Content.End - 1;
                object end = wrdDoc.Content.End - 1;

                Range tableLocation = wrdDoc.Range(ref start, ref end);
                
                Table table = wrdDoc.Tables.Add(tableLocation, kvp.Value.Count+1, 2);
                
                table.set_Style("Table Grid");
                table.Range.Font.Size = 5;

                int tableLine = 1;
                table.Cell(tableLine, 1).Range.Text = "HIC Live Repository";
                table.Cell(tableLine, 2).Range.Text = "Your Extract";

                foreach (LiveVsCohortGraphs graph in kvp.Value)
                {
                    //if both of the graph pairs were successfully extracted
                    if((graphSaveLocations.ContainsKey(graph.Extract) && graphSaveLocations.ContainsKey( graph.Live)) == false)
                        continue;

                    tableLine++;
                    table.Cell(tableLine, 1).Range.InlineShapes.AddPicture(graphSaveLocations[graph.Live]);
                    table.Cell(tableLine, 2).Range.InlineShapes.AddPicture(graphSaveLocations[graph.Extract]);
                    wroteAtLeastOneGraph = true;
                }
            }

            if (wroteAtLeastOneGraph)
            {
                notifier.OnCheckPerformed(new CheckEventArgs("successfully generated Live Vs Extract Graphs",CheckResult.Success));
                string saveLocation = Path.Combine(_root.FullName, "GraphSummaryForCohort" + _configuration.Cohort_ID + ".docx");
                wrdDoc.SaveAs(saveLocation);

                wrdDoc.Close();
                ((_Application)wrdApp).Quit();

                notifier.OnCheckPerformed(new CheckEventArgs("Saved Word file to " + saveLocation, CheckResult.Success));
            }
            else
            {

                notifier.OnCheckPerformed(new CheckEventArgs("Failed to generate any Extract Graphs",CheckResult.Fail));
                ((_Application)wrdApp).Quit(oFalse); //do not save changes
            }
        }*/
    }
}