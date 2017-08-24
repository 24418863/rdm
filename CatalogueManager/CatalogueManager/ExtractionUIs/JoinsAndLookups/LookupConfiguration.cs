﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using CatalogueLibrary.Data;
using CatalogueLibrary.Repositories;
using CatalogueManager.Icons.IconOverlays;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;
using CatalogueManager.MainFormUITabs.SubComponents;
using CatalogueManager.Menus;
using CatalogueManager.Refreshing;
using CatalogueManager.TestsAndSetup.ServicePropogation;
using MapsDirectlyToDatabaseTableUI;
using ReusableUIComponents;
using ScintillaNET;
using DragDropEffects = System.Windows.Forms.DragDropEffects;
using Point = System.Drawing.Point;

namespace CatalogueManager.ExtractionUIs.JoinsAndLookups
{

    /// <summary>
    /// A Lookup in RDMP is a relationship between three columns.  The 'Foreign Key' column must come from a normal dataset table e.g. 'Prescribing.DrugCode', the 'Primary Key' must come
    /// from a different table (usually prefixed z_ to indicate it is a lookup table) e.g. 'z_DrugsLookup.DrugCode' and then a 'Description' column from the same table e.g. 
    /// 'z_DrugsLookup.DrugName'.  This is maintained in the RDMP Catalogue database and does not result in any changes / constraints on your actual data repository.  
    /// 
    /// While it might seem redundant to have to configure this logic in the RDMP as well as (if you choose to) constraints in your data repository, this approach allows for 
    /// flexibility when it comes to incomplete/corrupt lookup tables (common in the research data management domain) as well as letting us bundle lookups with data extracts etc.
    /// 
    /// This window is a low level alternative to AdvancedLookupConfiguration (the recommended way of creating these Lookup relationships), this form lets you explicitly create a Lookup
    /// relationship using the supplied columns.  First of all you should make sure that the column you right clicked to activate the Form is the Description column.  Then select the
    /// 'Primary Key' and 'Foreign Key' as described above.  
    /// 
    /// If you have a particularly insane database design you can configure composite joins (where there are multiple columns that make up a composite 'Foreign Key' / 'Primary Key'.  For 
    /// example if there was crossover in 'DrugCode' between two countries then the Lookup relationship would need 'Primary Key' Prescribing.DrugCode + Prescribing.Country and the 
    /// 'Foreign Key' would need to be z_DrugsLookup.DrugCode + z_DrugsLookup.Country.
    ///
    /// Allows you to rapidly import and configure lookup table relationships into the RDMP.  This has two benefits, firstly lookup tables will be automatically included in project extracts
    /// of the dataset you are editing.  Secondly lookup columns will be available for inclusion directly into the extraction on a per row basis (for researchers who can't deal with having
    /// to lookup the meaning of codes in separate files).
    /// 
    /// Start by identifying a lookup table and click Import Lookup.  Then drag the primary key of the lookup into the PrimaryKey box.  Then drag the description column of the lookup onto the
    /// Foreign key field in the dataset you are modifying.  If you have multiple foreign keys (e.g. two columns SendingLocation and DischargeLocation both of which are location codes) then 
    /// join them both up (this will give you two lookup description fields SendingLocation_Desc and DischargeLocation_Desc).  
    /// 
    /// All Lookups and Lookup column description configurations are artifacts in the RDMP database and no actual changes will take place on your data repository (i.e. no constraints will be added
    /// to the underlying data database). 
    /// </summary>
    public partial class LookupConfiguration : AdvancedLookupConfiguration_Design
    {
        private Catalogue _catalogue;
        private ToolTip toolTip = new ToolTip();

        //constructor
        public LookupConfiguration()
        {
            InitializeComponent();
            olvLookupColumns.RowHeight = 19;
            olvExtractionInformations.RowHeight = 19;
            olvSelectedDescriptionColumns.RowHeight = 19;

            olvLookupColumns.IsSimpleDragSource = true;
            olvExtractionInformations.IsSimpleDragSource = true;

            pk1.KeyType = JoinKeyType.PrimaryKey;
            pk1.SelectedColumnChanged +=pk1_SelectedColumnChanged;
            
            pk2.KeyType = JoinKeyType.PrimaryKey;
            pk2.SelectedColumnChanged += UpdateValidityAssesment;

            pk3.KeyType = JoinKeyType.PrimaryKey;
            pk3.SelectedColumnChanged += UpdateValidityAssesment;

            fk1.KeyType = JoinKeyType.ForeignKey;
            fk1.SelectedColumnChanged += fk1_SelectedColumnChanged;
            
            fk2.KeyType = JoinKeyType.ForeignKey;
            fk2.SelectedColumnChanged += UpdateValidityAssesment;

            fk3.KeyType = JoinKeyType.ForeignKey;
            fk3.SelectedColumnChanged += UpdateValidityAssesment;
        }

        private void UpdateValidityAssesment()
        {
            UpdateValidityAssesment(false);
        }

        void fk1_SelectedColumnChanged()
        {
            SetStage(pk1.SelectedColumn == null ? LookupCreationStage.DragAForeignKey:LookupCreationStage.DragADescription);
            UpdateValidityAssesment();
        }
        private void pk1_SelectedColumnChanged()
        {
            SetStage(pk1.SelectedColumn == null ? LookupCreationStage.DragAPrimaryKey : LookupCreationStage.DragAForeignKey);
            UpdateValidityAssesment();
        }

        public override void SetDatabaseObject(IActivateItems activator, Catalogue databaseObject)
        {
            base.SetDatabaseObject(activator, databaseObject);
            _catalogue = databaseObject;
            
            olvLookupNameColumn.ImageGetter = o => activator.CoreIconProvider.GetImage(o);
            olvExtractionInformationsNameColumn.ImageGetter = o => activator.CoreIconProvider.GetImage(o);
            olvDescriptionsColumn.ImageGetter = o => activator.CoreIconProvider.GetImage(o);
            
            //add the currently configured extraction informations in the order they appear in the dataset
            List<ExtractionInformation> allExtractionInformationFromCatalogue = new List<ExtractionInformation>(_catalogue.GetAllExtractionInformation(ExtractionCategory.Any));
            allExtractionInformationFromCatalogue.Sort();
            
            olvExtractionInformations.ClearObjects();
            olvExtractionInformations.AddObjects(allExtractionInformationFromCatalogue.ToArray());
            
            btnAddExistingTableInfo.Image = activator.CoreIconProvider.GetImage(RDMPConcept.TableInfo);
            toolTip.SetToolTip(btnAddExistingTableInfo,"Choose existing TableInfo");
            
            btnImportNewTableInfo.Image = activator.CoreIconProvider.GetImage(RDMPConcept.TableInfo, OverlayKind.Import);
            toolTip.SetToolTip(btnImportNewTableInfo, "Import new...");

            btnPrimaryKeyCompositeHelp.Image = FamFamFamIcons.help;
            
            pictureBox1.Image = activator.CoreIconProvider.GetImage(RDMPConcept.Catalogue);
            tbCatalogue.Text = databaseObject.ToString();

            UpdateValidityAssesment();
        }
        
        private void btnAddExistingTableInfo_Click(object sender, EventArgs e)
        {
            var dialog = new SelectIMapsDirectlyToDatabaseTableDialog(RepositoryLocator.CatalogueRepository.GetAllObjects<TableInfo>(), false, false);

            if (dialog.ShowDialog() == DialogResult.OK)
                SetLookupTableInfo((TableInfo) dialog.Selected);
        }

        public void SetLookupTableInfo(TableInfo t)
        {
            if(t.IsTableValuedFunction)
            {
                WideMessageBox.Show("Table '" + t + "' is a TableValuedFunction, you cannot use it as a lookup table");
                return;
            }

            tbLookupTableInfo.Text = t.ToString();

            olvLookupColumns.ClearObjects();
            olvLookupColumns.AddObjects(t.ColumnInfos);

            SetStage(LookupCreationStage.DragAPrimaryKey);
        }

        private void btnImportNewTableInfo_Click(object sender, EventArgs e)
        {
            var importDialog = new ImportSQLTable(_activator,false);

            if(importDialog.ShowDialog() == DialogResult.OK)
                if(importDialog.TableInfoCreatedIfAny != null)
                    SetLookupTableInfo(importDialog.TableInfoCreatedIfAny);
        }

        private void SetStage(LookupCreationStage newStage)
        {
            _currentStage = newStage;
            Invalidate(true);
        }

        enum LookupCreationStage
        {
            ChooseLookupTable,
            DragAPrimaryKey,
            DragADescription,
            DragAForeignKey
        }

        LookupCreationStage _currentStage = LookupCreationStage.ChooseLookupTable;

        private void AdvancedLookupConfiguration_Paint(object sender, PaintEventArgs e)
        {
            Point drawTaskListAt = new Point(500,10);


            string[] lines = new[]
            {
                "Defining a lookup relationship:",
                "  1. Choose Lookup Table",
                "  2. Choose the Code column (e.g. T/F)",
                "  3. Choose the dataset column containing a matching code (T/F)",
                "  4. Choose the Description column (e.g. Tayside,Fife)",
            };


            float lineHeight = e.Graphics.MeasureString(lines[0], Font).Height;
            
            for (int i = 0; i < lines.Length; i++)
                e.Graphics.DrawString(lines[i], Font, Brushes.Black,new PointF(drawTaskListAt.X, drawTaskListAt.Y + (lineHeight*i)));

            int bulletLineIndex;

            switch (_currentStage)
            {
                case LookupCreationStage.ChooseLookupTable:
                    bulletLineIndex = 1;
                    break;
                case LookupCreationStage.DragAPrimaryKey:
                    bulletLineIndex = 2;
                    break;
                case LookupCreationStage.DragAForeignKey:
                    bulletLineIndex = 3;
                    break;
                case LookupCreationStage.DragADescription:
                    bulletLineIndex = 4;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            DrawArrows(e.Graphics);

            var triangleBasePoints = new[]
            {
                //basically (where 1 is the line height)
                //0,0
                //.5.5
                //0,1
                //offset by the drawing start location + the appropriate line number

                new PointF(drawTaskListAt.X, drawTaskListAt.Y + (bulletLineIndex * lineHeight) ),
                new PointF(drawTaskListAt.X + (lineHeight/2) , drawTaskListAt.Y + (bulletLineIndex * lineHeight)  + (lineHeight/2)),
                new PointF(drawTaskListAt.X, drawTaskListAt.Y + lineHeight + (bulletLineIndex *lineHeight))
            };

            e.Graphics.FillPolygon(Brushes.Black,triangleBasePoints);

        }
        void DrawArrows(Graphics graphics)
        {
            var arrowPen = new Pen(Color.DarkGray,2);

            GraphicsPath capPath = new GraphicsPath();
            
            // Create the outline for our custom end cap.
            capPath.AddLine(new Point(0, 0), new Point(2, -2));
            capPath.AddLine(new Point(2, -2), new Point(0, 0));
            capPath.AddLine(new Point(0, 0), new Point(-2, -2));
            capPath.AddLine(new Point(-2, -2),new Point(0, 0));

            arrowPen.CustomEndCap = new CustomLineCap(null, capPath);
    
            
            switch (_currentStage)
            {
                case LookupCreationStage.ChooseLookupTable:
                    break;
                case LookupCreationStage.DragAPrimaryKey:

                    DrawCurveWithLabel(
                        new PointF(groupBox1.Right + 10, groupBox1.Top + (groupBox1.Height / 2)),
                        new PointF(pk1.Left - 10, pk1.Top - 2),
                        "2. Drag Primary Key Column", graphics, arrowPen);
                    break;
                case LookupCreationStage.DragAForeignKey:

                    DrawCurveWithLabel(
                        new PointF(olvExtractionInformations.Right + 10, olvExtractionInformations.Bottom - (olvExtractionInformations.Height / 10)),
                        new PointF(olvSelectedDescriptionColumns.Right + 100, olvSelectedDescriptionColumns.Bottom + 200),
                        new PointF(fk1.Right + 500, fk1.Top + 100),
                        new PointF(fk1.Right + 15, fk1.Bottom - 10),
                        "3. Drag Matching Foreign Key Column", graphics, arrowPen);
                    break;
                case LookupCreationStage.DragADescription:
                    DrawCurveWithLabel(
                    new PointF(groupBox1.Right + 10, groupBox1.Top + (groupBox1.Height / 2)),
                    new PointF(olvSelectedDescriptionColumns.Left - 10, olvSelectedDescriptionColumns.Top - 2),
                    "4. Drag a Description Column", graphics, arrowPen);

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        private void DrawCurveWithLabel(PointF start, PointF end, string label,Graphics g, Pen p)
        {
            float w = end.X - start.X;
            float h = end.Y - start.Y;

            DrawCurveWithLabel(start, new PointF(start.X + w, start.Y),new PointF(start.X, start.Y + h),end,label,g,p);
        }

        private bool debugPoints = false;
        private void DrawCurveWithLabel(PointF start,PointF mid1, PointF mid2, PointF end, string label, Graphics g, Pen p)
        {
            g.DrawBezier(p,
                start,
                mid1,
                mid2,
                end);
            
            if (debugPoints)
            {
                g.FillEllipse(Brushes.Red, start.X -2, start.Y -2, 5, 5);
                g.FillEllipse(Brushes.Red, mid1.X - 2, mid1.Y - 2, 5, 5);
                g.FillEllipse(Brushes.Red, mid2.X - 2, mid2.Y - 2, 5, 5);
                g.FillEllipse(Brushes.Red, end.X - 2, end.Y - 2, 5,5);
            }

            g.DrawString(label, Font, Brushes.Black, new PointF(start.X, start.Y));
        }


        private void btnPrimaryKeyCompositeHelp_Click(object sender, EventArgs e)
        {
            WideMessageBox.Show(
@"Usually you only need one primary key/foreign key relationship e.g. M=Male, F=Female in which z_GenderLookup..Sex is the primary key and Demography..PatientGender is the foreign key.  However sometimes you need additional lookup joins.

For example:
if the Drug Code 'TIB' is reused in Tayside and Fife healthboard with different meanings then the primary key/foreign key would of the Lookup table would have to be both the 'Code' (TIB) and the 'Prescribing Healthboard' (T or F).

Only define secondary columns if you really need them! if any of the key fields do not match between the Lookup table and the Dataset table then no lookup description will be recorded");
        }

        private void olvLookupColumns_CellRightClick(object sender, BrightIdeasSoftware.CellRightClickEventArgs e)
        {
            var c = e.Model as ColumnInfo;

            if(c == null)
                return;

            e.MenuStrip = new ColumnInfoMenu(_activator,c);
        }

        private void olvSelectedDescriptionColumns_KeyUp(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Delete)
            {
                olvSelectedDescriptionColumns.RemoveObject(olvSelectedDescriptionColumns.SelectedObject);
                UpdateValidityAssesment();
            }
        }

        private void olvSelectedDescriptionColumns_ModelDropped(object sender, BrightIdeasSoftware.ModelDropEventArgs e)
        {
            olvSelectedDescriptionColumns.AddObject(e.SourceModels[0]);

            UpdateValidityAssesment();
        }

        private void olvSelectedDescriptionColumns_ModelCanDrop(object sender, BrightIdeasSoftware.ModelDropEventArgs e)
        {
            if(e.SourceModels.Count == 1)
                if(e.SourceModels[0] is ColumnInfo)
                {
                    var c = e.SourceModels[0] as ColumnInfo;
                    
                    //it's already in it
                    if (olvSelectedDescriptionColumns.IndexOf(c) != -1)
                    {
                        e.InfoMessage = "ColumnInfo is already selected as a Description";
                        return;
                    }

                    e.Effect = DragDropEffects.Copy;
                }
        }

        private void btnCreateLookup_Click(object sender, EventArgs e)
        {
            UpdateValidityAssesment(true);
        }

        private void UpdateValidityAssesment(bool actuallyDoIt)
        {
            btnCreateLookup.Enabled = false;
            ragSmiley1.Reset();

            try
            {
                if (pk1.SelectedColumn == null)
                    throw new Exception("No Primary key column selected");

                if (fk1.SelectedColumn == null)
                    throw new Exception("No Foreign key column selected");

                var allExtractionInformations = olvExtractionInformations.Objects.Cast<ExtractionInformation>().ToArray();
                var foreignKeyExtractionInformation = allExtractionInformations.SingleOrDefault(e => e.ColumnInfo.Equals(fk1.SelectedColumn));

                if (foreignKeyExtractionInformation == null)
                    throw new Exception("Foreign key column(s) must come from the Catalogue ExtractionInformation columns");

                if ((pk2.SelectedColumn == null) != (fk2.SelectedColumn == null))
                    throw new Exception("If you want to have secondary joins you must have them in pairs");

                if ((pk3.SelectedColumn == null) != (fk3.SelectedColumn == null))
                    throw new Exception("If you want to have secondary joins you must have them in pairs");

                var p1 = pk1.SelectedColumn;
                var f1 = fk1.SelectedColumn;

                var p2 = pk2.SelectedColumn;
                var f2 = fk2.SelectedColumn;

                var p3 = pk3.SelectedColumn;
                var f3 = fk3.SelectedColumn;

                var uniqueIDs = new[] { p1, p2, p3, f1, f2, f3 }.Where(o => o != null).Select(c => c.ID).ToArray();

                if (uniqueIDs.Distinct().Count() != uniqueIDs.Count())
                    throw new Exception("Columns can only appear once in any given key box");

                if (new[] { p1, p2, p3 }.Where(o => o != null).Select(c => c.TableInfo_ID).Distinct().Count() != 1)
                    throw new Exception("All primary key columns must come from the same Lookup table");

                if (new[] { f1, f2, f3 }.Where(o => o != null).Select(c => c.TableInfo_ID).Distinct().Count() != 1)
                    throw new Exception("All foreign key columns must come from the same Lookup table");

                var descs = olvSelectedDescriptionColumns.Objects.Cast<ColumnInfo>().ToArray();

                if (!descs.Any())
                    throw new Exception("You must have at least one Description column from the Lookup table");

                if (descs.Any(d => d.TableInfo_ID != p1.TableInfo_ID))
                    throw new Exception("All Description columns must come from the Lookup table");

                if (actuallyDoIt)
                {
                    foreach (var descCol in descs)
                    {
                        var repo = (CatalogueRepository)_catalogue.Repository;
                        Lookup lookup = new Lookup(repo, descCol, f1, p1, ExtractionJoinType.Left, tbCollation.Text);

                        if (p2 != null)
                            new LookupCompositeJoinInfo(repo, lookup, f2, p2, tbCollation.Text);

                        if (p3 != null)
                            new LookupCompositeJoinInfo(repo, lookup, f3, p3, tbCollation.Text);

                        var proposedName = foreignKeyExtractionInformation.GetRuntimeName() + "_Desc";

                        var newCatalogueItem = new CatalogueItem(repo, _catalogue, proposedName);
                        newCatalogueItem.SetColumnInfo(descCol);

                        if (
                            MessageBox.Show(
                                "Also create a virtual extractable column in Catalogue '" + _catalogue + "' called '" +
                                proposedName + "'", "Create Extractable Column?", MessageBoxButtons.YesNo) ==
                            DialogResult.Yes)
                        {
                            //bump everyone down 1
                            foreach (var toBumpDown in allExtractionInformations.Where(e => e.Order > foreignKeyExtractionInformation.Order))
                            {
                                toBumpDown.Order++;
                                toBumpDown.SaveToDatabase();
                            }


                            var newExtractionInformation = new ExtractionInformation(repo, newCatalogueItem, descCol, descCol.ToString());
                            newExtractionInformation.ExtractionCategory = ExtractionCategory.Supplemental;
                            newExtractionInformation.Alias = newCatalogueItem.Name;
                            newExtractionInformation.Order = foreignKeyExtractionInformation.Order + 1;
                            newExtractionInformation.SaveToDatabase();
                        }

                        _activator.RefreshBus.Publish(this, new RefreshObjectEventArgs(_catalogue));
                        SetDatabaseObject(_activator,_catalogue);

                        MessageBox.Show("Lookup created successfully, fields will now be cleared");
                        pk2.Clear();
                        pk3.Clear();
                        
                        fk1.Clear();
                        fk2.Clear();
                        fk3.Clear();

                        olvSelectedDescriptionColumns.ClearObjects();

                    }
                }
                btnCreateLookup.Enabled = true;

            }
            catch (Exception e)
            {
                if (actuallyDoIt)
                    ExceptionViewer.Show(e);

                ragSmiley1.Fatal(e);
            }

        }
    }

    [TypeDescriptionProvider(typeof(AbstractControlDescriptionProvider<AdvancedLookupConfiguration_Design, UserControl>))]
    public abstract class AdvancedLookupConfiguration_Design : RDMPSingleDatabaseObjectControl<Catalogue>
    {
    }
}