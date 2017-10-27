﻿using CatalogueLibrary.Data.DataLoad;
using ReusableUIComponents.CommandExecution;

namespace RDMPObjectVisualisation.Copying.Commands
{
    public class ProcessTaskCommand : ICommand
    {
        public ProcessTask ProcessTask { get; set; }

        public ProcessTaskCommand(ProcessTask processTask)
        {
            ProcessTask = processTask;
        }

        public string GetSqlString()
        {
            return null;
        }
    }
}