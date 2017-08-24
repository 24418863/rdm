﻿using CatalogueLibrary.Data;
using ReusableUIComponents.Copying;

namespace RDMPObjectVisualisation.Copying.Commands
{
    public class TableInfoCommand : ICommand
    {
        private TableInfo _tableInfo;

        public TableInfoCommand(TableInfo tableInfo)
        {
            _tableInfo = tableInfo;
        }

        public string GetSqlString()
        {
            return _tableInfo.Name;
        }
    }
}