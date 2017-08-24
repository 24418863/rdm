﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CatalogueManager.Icons.IconProvision;

namespace CatalogueManager.DataLoadUIs.LoadMetadataUIs.LoadDiagram.StateDiscovery
{
    /// <summary>
    /// Tells you what state the LoadDiagram is in.  This starts at 'Unknown' which means no database requests have been sent and the visible tables are the 'Anticipated' state of the tables
    /// during a load.  Checking the state when RAW/STAGING do not exist indicates that no load is underway and that the last load was succesful (or RAW/STAGING were cleaned up after a problem
    /// was resolved).  The final state is 'Load Underway/Crashed' this indicates that RAW and/or STAGING exist which means that either a data load is in progress (not nesessarily started by you)
    /// or one has completed with an error and has therefore left RAW/STAGING for debugging (See LoadDiagram).
    /// </summary>
    public partial class LoadStateUI : UserControl
    {
        private Bitmap _unknown;
        private Bitmap _noLoadUnderway;
        private Bitmap _executingOrCrashed;
        public LoadState State { get; private set; }
        public LoadStateUI()
        {
            InitializeComponent();

            _unknown = CatalogueIcons.OrangeIssue;
            _noLoadUnderway = CatalogueIcons.Tick;
            _executingOrCrashed = CatalogueIcons.ExecuteArrow;

            BackColor = Color.Wheat;
            SetStatus(LoadState.Unknown);
        }


        public void SetStatus(LoadState state)
        {
            State = state;
            switch (state)
            {
                case LoadState.Unknown:
                    pictureBox1.Image = _unknown;
                    lblStatus.Text = "State Unknown";
                    break;
                case LoadState.NotStarted:
                    lblStatus.Text = "No Loads Underway";
                    pictureBox1.Image = _noLoadUnderway;
                    break;
                case LoadState.StartedOrCrashed:
                    lblStatus.Text = "Load Underway/Crashed";
                    pictureBox1.Image = _executingOrCrashed;
                    break;
                default:
                    throw new ArgumentOutOfRangeException("state");
            }
        }
        
        public enum LoadState
        {
            Unknown,
            NotStarted,
            StartedOrCrashed
        }
    }

    
}