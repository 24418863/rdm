using System;
using System.Linq;
using System.Windows.Forms;
using CatalogueLibrary.Data.DataLoad;
using CatalogueLibrary.Nodes.LoadMetadataNodes;
using CatalogueLibrary.Repositories;
using CatalogueManager.CommandExecution.AtomicCommands;
using CatalogueManager.CommandExecution.AtomicCommands.UIFactory;
using CatalogueManager.Icons.IconOverlays;
using CatalogueManager.ItemActivation;
using CatalogueManager.ItemActivation.Emphasis;
using CatalogueManager.Refreshing;
using DataLoadEngine.Attachers;
using DataLoadEngine.DataProvider;
using DataLoadEngine.Mutilators;

namespace CatalogueManager.Menus
{
    internal class LoadStageNodeMenu : RDMPContextMenuStrip
    {
        private readonly LoadStageNode _loadStageNode;
        private MEF _mef;

        public LoadStageNodeMenu(IActivateItems activator, LoadStageNode loadStageNode):base(activator,null)
        {
            _loadStageNode = loadStageNode;
            _mef = activator.RepositoryLocator.CatalogueRepository.MEF;

            AtomicCommandUIFactory factory = new AtomicCommandUIFactory(_activator.CoreIconProvider);


            
           AddMenu<IDataProvider>("Add New Data Provider");
           AddMenu<IAttacher>("Add New Attacher");
           AddMenu<IMutilateDataTables>("Add New Mutilator");

           Items.Add(factory.CreateMenuItem(new ExecuteCommandCreateNewProcessTask(activator, ProcessTaskType.SQLFile,loadStageNode.LoadMetadata, loadStageNode.LoadStage)));
           Items.Add(factory.CreateMenuItem(new ExecuteCommandCreateNewProcessTask(activator, ProcessTaskType.Executable, loadStageNode.LoadMetadata, loadStageNode.LoadStage)));
        }
        
        private void AddMenu<T>(string menuName)
        {
            var types = _mef.GetTypes<T>().ToArray();
            var menu = new ToolStripMenuItem(menuName);

            ProcessTaskType taskType;

            if(typeof(T) == typeof(IDataProvider))
                taskType = ProcessTaskType.DataProvider;
            else
                if (typeof(T) == typeof(IAttacher))
                    taskType = ProcessTaskType.Attacher;
                else if (typeof (T) == typeof (IMutilateDataTables))
                    taskType = ProcessTaskType.MutilateDataTable;
                else
                    throw new ArgumentException("Type '" + typeof (T) + "' was not expected", "T");
            
            foreach (Type type in types)
            {
                Type toAdd = type;
                menu.DropDownItems.Add(type.Name, null, (s, e) => AddTypeIntoStage(toAdd, taskType));
            }

            menu.Enabled = ProcessTask.IsCompatibleStage(taskType, _loadStageNode.LoadStage) && types.Any();

            Items.Add(menu);
        }



        private void AddTypeIntoStage(Type type, ProcessTaskType taskType)
        {
            var lmd = _loadStageNode.LoadMetadata;
            var stage = _loadStageNode.LoadStage;

            ProcessTask newTask = new ProcessTask((ICatalogueRepository)lmd.Repository, lmd, stage);
            newTask.Path = type.FullName;
            newTask.ProcessTaskType = taskType;
            newTask.Order = 0;
            newTask.Name = type.Name;

            newTask.SaveToDatabase();
            _activator.RefreshBus.Publish(this,new RefreshObjectEventArgs(lmd));
            _activator.ActivateProcessTask(this,newTask);
        }
    }
}