﻿using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReusableLibraryCode;

namespace DataQualityEngine.Data
{
    public class ColumnState
    {
        private int _countCorrect;
        private int _countDbNull;
        private int _countMissing;
        private int _countWrong;
        private int _countInvalidatesRow;
        public string TargetProperty { get; set; }
        public int DataLoadRunID { get; set; }

        public int? ID { get; private set; }
        public int? Evaluation_ID { get;private set; }
        
        public string ItemValidatorXML { get; set; }

        public int CountMissing
        {
            get { return _countMissing; }
            set
            {
                if (IsCommitted)
                    throw new NotSupportedException("Can only edit these values while the ColumnState is being computed in memory, this ColumnState came from the database and was committed long ago");
                _countMissing = value;
            }
        }

        public int CountWrong
        {
            get { return _countWrong; }
            set
            {
                if (IsCommitted)
                    throw new NotSupportedException("Can only edit these values while the ColumnState is being computed in memory, this ColumnState came from the database and was committed long ago");
                _countWrong = value;
                
            }
        }

        public int CountInvalidatesRow
        {
            get { return _countInvalidatesRow; }
            set
            {
                if (IsCommitted)
                    throw new NotSupportedException("Can only edit these values while the ColumnState is being computed in memory, this ColumnState came from the database and was committed long ago");
                _countInvalidatesRow = value;
            }
        }

        public int CountCorrect
        {
            get { return _countCorrect; }
            set
            {
                if (IsCommitted)
                    throw new NotSupportedException("Can only edit these values while the ColumnState is being computed in memory, this ColumnState came from the database and was committed long ago");

                _countCorrect = value; 
            }
        }

        public int CountDBNull
        {
            get { return _countDbNull; }
            set
            {
                if(IsCommitted)
                    throw new NotSupportedException("Can only edit these values while the ColumnState is being computed in memory, this ColumnState came from the database and was committed long ago");

                _countDbNull = value;
            }
        }
        
        public string PivotCategory { get; private set; }

        public bool IsCommitted { get; private set; }
        public ColumnState(string targetProperty, int dataLoadRunID, string itemValidatorXML)
        {
            TargetProperty = targetProperty;
            DataLoadRunID = dataLoadRunID;
            ItemValidatorXML = itemValidatorXML;

            IsCommitted = false;
        }

        public ColumnState(DbDataReader r)
        {
            TargetProperty = r["TargetProperty"].ToString();
            DataLoadRunID = Convert.ToInt32(r["DataLoadRunID"]);
            Evaluation_ID = Convert.ToInt32(r["Evaluation_ID"]);
            ID = Convert.ToInt32(r["ID"]);
            CountCorrect = Convert.ToInt32(r["CountCorrect"]);
            CountDBNull = Convert.ToInt32(r["CountDBNull"]);
            ItemValidatorXML = r["ItemValidatorXML"].ToString();

            CountMissing = Convert.ToInt32(r["CountMissing"]);
            CountWrong = Convert.ToInt32(r["CountWrong"]);
            CountInvalidatesRow = Convert.ToInt32(r["CountInvalidatesRow"]);

            PivotCategory = (string)r["PivotCategory"];

            IsCommitted = true;

        }

        

        public void Commit(Evaluation evaluation,string pivotCategory, DbConnection con, DbTransaction transaction)
        {
            if(IsCommitted)
                throw new NotSupportedException("ColumnState was already committed");

            var sql = string.Format(
               "INSERT INTO [dbo].[ColumnState]([TargetProperty],[DataLoadRunID],[Evaluation_ID],[CountCorrect],[CountDBNull],[ItemValidatorXML],[CountMissing],[CountWrong],[CountInvalidatesRow],[PivotCategory])VALUES({0},{1},{2},{3},{4},{5},{6},{7},{8},{9})",
               "@TargetProperty",
               DataLoadRunID
               ,evaluation.ID
               ,CountCorrect
               ,CountDBNull
               ,"@ItemValidatorXML"
               ,CountMissing
               ,CountWrong
               ,CountInvalidatesRow
               , "@PivotCategory"
               );

            var cmd = DatabaseCommandHelper.GetCommand(sql, con, transaction);
            DatabaseCommandHelper.AddParameterWithValueToCommand("@ItemValidatorXML", cmd, ItemValidatorXML);
            DatabaseCommandHelper.AddParameterWithValueToCommand("@TargetProperty", cmd, TargetProperty);
            DatabaseCommandHelper.AddParameterWithValueToCommand("@PivotCategory", cmd, pivotCategory);
            cmd.ExecuteNonQuery();

            IsCommitted = true;
        }

        
    }
}