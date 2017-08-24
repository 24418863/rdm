﻿using System;
using CatalogueLibrary.Data;
using MapsDirectlyToDatabaseTable;
using ReusableLibraryCode.Checks;
using ReusableLibraryCode.Exceptions;

namespace CatalogueLibrary.Checks
{
    public class ClonedFilterChecker:ICheckable
    {
        private readonly IFilter _child;
        private int? _allegedParent;
        private readonly IRepository _catalogueDatabaseRepository;

        public ClonedFilterChecker(IFilter child, int? allegedParentExtractionFilterID, IRepository catalogueDatabaseRepository)
        {
            _child = child;
            _allegedParent = allegedParentExtractionFilterID;
            _catalogueDatabaseRepository = catalogueDatabaseRepository;
        }

        public void Check(ICheckNotifier notifier)
        {
            if (_allegedParent == null)
            {

                notifier.OnCheckPerformed(
                    new CheckEventArgs(
                        "Filter " + _child +
                        " is not cloned from a Catalogue filter so does not need checking for synchronicity",
                        CheckResult.Success));

                return;
            }

            //we were cloned from a filter in the Catalogue
            bool exist = _catalogueDatabaseRepository.StillExists<ExtractionFilter>((int)_allegedParent);

            //tell them if it has been nuked
            notifier.OnCheckPerformed(
                new CheckEventArgs(
                    "Catalogue reports that the original filter we were cloned from " +
                    (exist ? " still exists " : " no longer exists"), exist ? CheckResult.Success : CheckResult.Fail));

            //it hasn't been nuked
            if (exist)
            {
                //get it
                var parent = _catalogueDatabaseRepository.GetObjectByID<ExtractionFilter>((int) _allegedParent);

                //see if someone has been monkeying with the parent (or the child) in which case warn them about the disparity
                if (parent.WhereSQL.Equals(_child.WhereSQL))
                    notifier.OnCheckPerformed(new CheckEventArgs(
                        "Filter " + _child + " has the same WhereSQL as parent",
                        CheckResult.Success));
                else
                {
                    try
                    {
                        throw new ExpectedIdenticalStringsException("Expected WHERE SQL to be identical", parent.WhereSQL,_child.WhereSQL);
                    }
                    catch (ExpectedIdenticalStringsException ex)
                    { 
                        notifier.OnCheckPerformed(
                        new CheckEventArgs(
                            _child.GetType().Name + " called '" + _child + "' (ID=" + _child.ID +
                            ") WhereSQL does not match the parent it was originally cloned from (ExtractionFilter ID=" +
                            _allegedParent +
                            ").  You might have made a deliberate change in your copy or it might mean that someone has corrected the parent since you first cloned it",
                            CheckResult.Warning, ex));
                    }
                }
            }
        }
    }
}