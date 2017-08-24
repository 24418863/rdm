﻿using System;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using CatalogueLibrary.Repositories;
using CatalogueManager.CommandExecution.AtomicCommands;
using CatalogueManager.Icons;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;
using CatalogueManager.LocationsMenu;
using MapsDirectlyToDatabaseTableUI;
using RDMPStartup;
using RDMPStartup.Events;
using ReusableLibraryCode.Checks;
using ReusableUIComponents;

using Timer = System.Windows.Forms.Timer;

namespace CatalogueManager.TestsAndSetup.StartupUI
{
    /// <summary>
    /// Shows every time an RDMP application is launched.  The 'User Friendly' view tells you whether there are any problems with your current platform databases / plugins by way of a large
    /// smiley face.  If you get an error (Red face) then there may be a hyperlink to resolve the problem (e.g. if a platform database needs patching or you have not yet configured your 
    /// platform databases (See ChoosePlatformDatabases).
    /// 
    /// Green means that everything is working just fine.
    /// 
    /// Yellow means that something non-critical is not working e.g. a specific plugin is not working correctly
    /// 
    /// Red means that something critical is not working (Check for a fix hyperlink or look at the 'Technical' view to see the exact nature of the problem).
    /// 
    /// The 'Technical' view shows the progress of the discovery / version checking of all tiers of platform databases.  This includes checking that the software version matches the database
    /// schema version  (See ManagedDatabaseUI) and that plugins have loaded correctly (See MEFStartupUI).
    /// </summary>
    public partial class StartupUIMainForm : Form, ICheckNotifier
    {
        private readonly Startup _startup;

        private Icon _red;
        private Icon _yellow;
        private Icon _green;
        
        //Constructor
        public StartupUIMainForm(Startup startup)
        {
            _startup = startup;
            
            InitializeComponent();
            
            if(_startup == null)
                return;
            
            if(Screen.PrimaryScreen.WorkingArea.Width < 768 || Screen.PrimaryScreen.WorkingArea.Height < 1024)
                WindowState = FormWindowState.Maximized;
            
            _startup.DatabaseFound += _startup_DatabaseFound;
            _startup.MEFFileDownloaded += _startup_MEFFileDownloaded;
            _startup.PluginPatcherFound += _startup_PluginPatcherFound;

            var factory = new IconFactory();
            _red = factory.GetIcon(FamFamFamIcons.RedFace);
            _green = factory.GetIcon(FamFamFamIcons.GreenFace);
            _yellow = factory.GetIcon(FamFamFamIcons.YellowFace);

            this.Icon = _green;
            Catalogue.RequestRestart += ()=> StartOrRestart(true);
            DataExport.RequestRestart += () => StartOrRestart(true);
        }

        void _startup_DatabaseFound(object sender, PlatformDatabaseFoundEventArgs eventArgs)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => _startup_DatabaseFound(sender, eventArgs)));
                return;
            }

            HandleDatabaseFoundOnSimpleUI(eventArgs);

            //now we are on teh correct UI thread.
            if (eventArgs.DatabaseType == RDMPPlatformType.Catalogue)
            {
                Catalogue.Visible = true;
                Catalogue.HandleDatabaseFound(eventArgs);

                if (eventArgs.Status == RDMPPlatformDatabaseStatus.Broken ||
                    eventArgs.Status == RDMPPlatformDatabaseStatus.Unreachable)
                {
                    llChoosePlatformDatabases.Visible = true;
                    pbWhereIsDatabase.Visible = true;
                    this.Icon = _red;
                }
                else

                    pbWhereIsDatabase.Visible = false;
            }
            
            if (eventArgs.DatabaseType == RDMPPlatformType.DataExport)
            {
                DataExport.Visible = true;
                DataExport.HandleDatabaseFound(eventArgs);
            }

            if (eventArgs.Tier == 2)
            {
                var ctrl = new ManagedDatabaseUI();
                flpTier2Databases.Controls.Add(ctrl);
                ctrl.HandleDatabaseFound(eventArgs);
                ctrl.RequestRestart += () => StartOrRestart(true);
            }

            if (eventArgs.Tier == 3)
            {
                var ctrl = new ManagedDatabaseUI();
                flpTier3Databases.Controls.Add(ctrl);
                ctrl.HandleDatabaseFound(eventArgs);
                ctrl.RequestRestart += () => StartOrRestart(true);

                pbLoadProgress.Value = 900;//90% done
            }
        }

        private void _startup_MEFFileDownloaded(object sender, MEFFileDownloadProgressEventArgs eventArgs)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => _startup_MEFFileDownloaded(sender, eventArgs)));
                return;
            }

            mefStartupUI1.Visible = true;
            
            //25% to 50% is downloading MEF
            pbLoadProgress.Value = (int) (250 + ((float)eventArgs.CurrentDllNumber / (float)eventArgs.DllsSeenInCatalogue * 250f));

            lblProgress.Text = "Downloading MEF File " + eventArgs.FileBeingProcessed;

            mefStartupUI1.HandleDownload(eventArgs);

            if (eventArgs.Status == MEFFileDownloadEventStatus.OtherError)
                Fatal(eventArgs.Exception);
        }

        private void _startup_PluginPatcherFound(object sender, PluginPatcherFoundEventArgs eventArgs)
        {
            if (InvokeRequired)
            {
                Invoke(new MethodInvoker(() => _startup_PluginPatcherFound(sender, eventArgs)));
                return;
            }

            pbPluginPatchersArrow.Visible = true;


            var ctrl = new PluginPatcherUI();
            flowLayoutPanel1.Controls.Add(ctrl);
            ctrl.HandlePatcherFound(eventArgs);
            
            pbLoadProgress.Value = 800;//80% done
        }

        private bool escapePressed = false;
        private int countDownToClose = 5;

        private void StartupComplete()
        {
            if(InvokeRequired)
            {
                this.Invoke(new MethodInvoker(StartupComplete));
                return;
            }

            if (pbRed.Visible || pbRedDead.Visible)
                return;

            
            Timer t = new Timer();
            t.Interval = 1000;
            t.Tick += t_Tick;
            t.Start();

            pbLoadProgress.Value = 1000;


            if (_startup != null && _startup.RepositoryLocator != null && _startup.RepositoryLocator.CatalogueRepository != null)
                SetupHelpKeywords(_startup.RepositoryLocator.CatalogueRepository);
        }


        private void SetupHelpKeywords(CatalogueRepository repo)
        {
            foreach (var kvp in repo.HelpText)
                KeywordHelpTextListbox.AddToHelpDictionaryIfNotExists(kvp.Key,kvp.Value);
        }

        void t_Tick(object sender, EventArgs e)
        {
            var t = (Timer) sender;
            
            if(escapePressed)
            {
                t.Stop();
                lblStartupComplete1.Visible = false;
                lblStartupComplete2.Visible = false;
                return;
            }

            countDownToClose --;

            string message = string.Format("Startup Complete... Closing in {0}s (Esc to cancel)",countDownToClose);
            lblStartupComplete1.Text = message;
            lblStartupComplete2.Text = message;

            lblStartupComplete1.Visible = true;
            lblStartupComplete2.Visible = true;

            if (countDownToClose == 0)
            {
                t.Stop();
                Close();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (_startup == null)
                return;

            StartOrRestart(false);
            
        }

        private void StartOrRestart(bool forceClearRepositorySettings)
        {
            pbLoadProgress.Maximum = 1000;

            if (_startup.RepositoryLocator == null || forceClearRepositorySettings)
            {
                try
                {
                    lblProgress.Text = "Constructing RegistryRepositoryFinder";
                    RegistryRepositoryFinder finder = new RegistryRepositoryFinder();
                    _startup.RepositoryLocator = finder;
                }
                catch (Exception ex)
                {
                    lblProgress.Text = "Constructing RegistryRepositoryFinder Failed";
                    Fatal(ex);
                }
            }
            else
                if(!(_startup.RepositoryLocator is RegistryRepositoryFinder))
                    throw new NotSupportedException("You created Startup with an existing repository finder so we were going to reuse that one but it wasn't a RegistryRepositoryFinder (it was a " + _startup.RepositoryLocator.GetType().Name + "!)");
            

            Catalogue.Visible = false;
            Catalogue.Reset();

            DataExport.Visible = false;
            DataExport.Reset();

            mefStartupUI1.Visible = false;
            mefStartupUI1.Reset();
            flpTier2Databases.Controls.Clear();

            flpTier3Databases.Controls.Clear();

            escapePressed = false;
            countDownToClose = 5;
            lastStatus = RDMPPlatformDatabaseStatus.Healthy;
            
            pbGreen.Visible = true;
            pbRed.Visible = false;
            pbYellow.Visible = false;
            llException.Visible = false;
            llChoosePlatformDatabases.Visible = false;

            registryRepositoryFinderUI1.SetRegistryRepositoryFinder((RegistryRepositoryFinder)_startup.RepositoryLocator);

            lblStartupComplete1.Visible = false;
            lblStartupComplete2.Visible = false;

            //10% progress because we connected to registry
            pbLoadProgress.Value = 100;

            lblProgress.Text = "Awaiting Platform Database Discovery...";

            Task t = new Task(
                () =>
                    {
                        try
                        {
                            _startup.DoStartup(this);
                            StartupComplete();
                        }
                        catch (Exception ex)
                        {
                            if(IsDisposed || !IsHandleCreated)
                                ExceptionViewer.Show(ex);
                            else
                                Invoke(new MethodInvoker(() => Fatal(ex)));
                        }

                    }
                );
            t.Start();
        }


        private void Fatal(Exception exception)
        {
            lastStatus = RDMPPlatformDatabaseStatus.Broken;
            
            pbGreen.Visible = false;
            pbYellow.Visible = false;
            pbRed.Visible = true;

            if(exception == null)
                pbRed.Visible = true;
            else
            {
                pbRedDead.Visible = true;

                llException.Visible = true;
                llException.Text = exception.Message;
                llException.LinkClicked += (s,e) => ExceptionViewer.Show(exception);
            }
        }

        RDMPPlatformDatabaseStatus lastStatus = RDMPPlatformDatabaseStatus.Healthy;
        

        private void HandleDatabaseFoundOnSimpleUI(PlatformDatabaseFoundEventArgs eventArgs)
        {

            //if status got worse
            if (eventArgs.Status < lastStatus )
                lastStatus = eventArgs.Status;
            else
                return;//we are broken and found more broken stuff!

            lblProgress.Text = eventArgs.DatabaseType + " database status was " + eventArgs.Status;

            switch (eventArgs.Status)
            {
                case RDMPPlatformDatabaseStatus.Unreachable:

                    if (eventArgs.Tier == 1)
                        Angry();
                    else
                        Warning();

                    break;
                case RDMPPlatformDatabaseStatus.Broken:
                    Angry();
                    break;
                case RDMPPlatformDatabaseStatus.RequiresPatching:
                    Warning();
                    
                    llException.Visible = true;
                    llException.Text = "Patching Required on database of type " + eventArgs.DatabaseType;
                    llException.LinkClicked += (s, e) => PatchingUI.ShowIfRequired((SqlConnectionStringBuilder)eventArgs.Repository.ConnectionStringBuilder, eventArgs.Repository, eventArgs.DatabaseAssembly, eventArgs.HostAssembly);

                    break;
                case RDMPPlatformDatabaseStatus.Healthy:
                    return;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        //MEF only!
        public bool OnCheckPerformed(CheckEventArgs args)
        {
            if(InvokeRequired)
            {

                Invoke(new MethodInvoker(() => OnCheckPerformed(args)));
                return false;
            }


            //if the message starts with a percentage translate it into the progress bars movement
            Regex progressHackMessage = new Regex("^(\\d+)%");
            var match = progressHackMessage.Match(args.Message);

            if (match.Success)
            {
                var percent = float.Parse(match.Groups[1].Value);
                pbLoadProgress.Value = (int) (500 + (percent*2.5));//500-750
            }
             
            switch (args.Result)
            {
                case CheckResult.Success:
                    break;
                case CheckResult.Warning:
                case CheckResult.Fail:
                    Warning();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            lblProgress.Text = args.Message;

            return mefStartupUI1.OnCheckPerformed(args);
        }

        private void Angry()
        {
            if (pbRed.Visible || pbRedDead.Visible)
                return;


            if (pbGreen.Visible)
                pbGreen.Visible = false;

            if (pbYellow.Visible)
                pbYellow.Visible = false;

            this.Icon = _red;
            pbRed.Visible = true;
        }
        private void Warning()
        {
            if (pbRed.Visible || pbRedDead.Visible)
                return;

            if (pbGreen.Visible)
                pbGreen.Visible = false;

            if (!pbYellow.Visible)
            {
                this.Icon = _yellow;
                pbYellow.Visible = true;
            }
        }

        private void StartupUIMainForm_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                escapePressed = true;
        }

        private void StartupUIMainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
                if (pbRed.Visible || pbRedDead.Visible)
                {
                    bool loadAnyway = 
                    MessageBox.Show(
                        "Setup failed in a serious way, do you want to try to load the rest of the program anyway?",
                        "Try to load anyway?", MessageBoxButtons.YesNo) == DialogResult.Yes;

                    if(!loadAnyway)
                        Process.GetCurrentProcess().Kill();
                }
        }

        private void llChoosePlatformDatabases_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            var cmd = new ExecuteCommandChoosePlatformDatabase(new RegistryRepositoryFinder());
            cmd.Execute();
            StartOrRestart(true);
        }
    }
}