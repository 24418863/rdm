// Copyright (c) The University of Dundee 2018-2019
// This file is part of the Research Data Management Platform (RDMP).
// RDMP is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
// RDMP is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
// You should have received a copy of the GNU General Public License along with RDMP. If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Drawing;
using BrightIdeasSoftware;
using Rdmp.Core.Curation.Data;
using Rdmp.UI.Icons.IconProvision;
using Rdmp.UI.ItemActivation;
using ReusableLibraryCode.Settings;

namespace Rdmp.UI.Collections.Providers
{
    /// <summary>
    /// Handles creating the 'Favourite' column in <see cref="TreeListView"/>.  This column depicts whether a given RDMP object is a favourite
    /// of the user (see <see cref="Favourite"/>).
    /// </summary>
    public class FavouriteColumnProvider
    {
        private readonly IActivateItems _activator;
        private readonly TreeListView _tlv;
        OLVColumn _olvFavourite;

        private Bitmap _starFull;
        private Bitmap _starHollow;



        public FavouriteColumnProvider(IActivateItems activator,TreeListView tlv)
        {
            _activator = activator;
            _tlv = tlv;

            _starFull = CatalogueIcons.Favourite;
            _starHollow = CatalogueIcons.StarHollow;
        }

        public OLVColumn CreateColumn()
        {
            _olvFavourite = new OLVColumn("Favourite", null);
            _olvFavourite.Text = "Favourite";
            _olvFavourite.ImageGetter += FavouriteImageGetter;
            _olvFavourite.IsEditable = false;
            _olvFavourite.Sortable = false;
            _tlv.CellClick += OnCellClick;
            
            _tlv.AllColumns.Add(_olvFavourite);
            _tlv.RebuildColumns();

            _olvFavourite.IsVisible = UserSettings.ShowColumnFavourite;
            _olvFavourite.VisibilityChanged += (s, e) => UserSettings.ShowColumnFavourite = ((OLVColumn)s).IsVisible;
            
            return _olvFavourite;
        }

        private void OnCellClick(object sender, CellClickEventArgs cellClickEventArgs)
        {
            var col = cellClickEventArgs.Column;
            var o = cellClickEventArgs.Model as DatabaseEntity;


            if (col == _olvFavourite && o != null)
            {
                if (_activator.FavouritesProvider.IsFavourite(o))
                    _activator.FavouritesProvider.RemoveFavourite(this, o);
                else
                    _activator.FavouritesProvider.AddFavourite(this, o);
                
                try
                {
                    _tlv.RefreshObject(o);
                }
                catch (ArgumentException)
                {
                    
                }
            }
        }

        private object FavouriteImageGetter(object rowobject)
        {
            var o = rowobject as DatabaseEntity;

            if (o != null)
                return _activator.FavouritesProvider.IsFavourite(o) ? _starFull : _starHollow;
                    

            return null;
        }

    }
}
