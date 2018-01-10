using System.Drawing;
using CatalogueLibrary.CommandExecution.AtomicCommands;
using CatalogueLibrary.Data;
using CatalogueLibrary.Data.DataLoad;
using CatalogueManager.Icons.IconOverlays;
using CatalogueManager.Icons.IconProvision;
using CatalogueManager.ItemActivation;
using ReusableLibraryCode.CommandExecution;
using ReusableLibraryCode.Icons.IconProvision;

namespace CatalogueManager.CommandExecution.AtomicCommands.WindowArranging
{
    public class ExecuteCommandExecuteLoadMetadata : BasicUICommandExecution, IAtomicCommandWithTarget
    {
        public LoadMetadata LoadMetadata{ get; set; }

        public ExecuteCommandExecuteLoadMetadata(IActivateItems activator) : base(activator)
        {
            
        }

        public Image GetImage(IIconProvider iconProvider)
        {
            return CatalogueIcons.ExecuteArrow;
        }

        public IAtomicCommandWithTarget SetTarget(DatabaseEntity target)
        {
            LoadMetadata = (LoadMetadata)target;
            return this;
        }

        public override string GetCommandHelp()
        {
            return "This will take you to the Data Load Configurations list and allow you to Edit the Load (change which modules execute, which Catalogues are loaded etc)." +
                   "\r\n" +
                   "You must choose a Load from the list before proceeding.";
        }

        public override string GetCommandName()
        {
            return "Execute Data Load Configuration";
        }

        public override void Execute()
        {
            if (LoadMetadata == null)
                SetImpossible("You must choose a LoadMetadata.");

            base.Execute();
            Activator.WindowArranger.SetupEditLoadMetadata(this, LoadMetadata);
        }
    }
}