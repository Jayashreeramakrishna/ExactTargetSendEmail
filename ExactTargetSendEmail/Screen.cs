using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ExactTargetSendEmail
{
    #region PagerDate
    public abstract class PagerData
    {
        private int _pageIndex = 0;
        private int _noOfRecordsPerPage = 0;
        private string _sortColumn = string.Empty;
        private int _totalRecordCount = 0;
        private string _sortDirection = "ASC";
        private string _ModifyBy = "";
        private DateTime _ModifyDate;
        private bool _enablePaging = false;

        public PagerData()
        {
        }

        public int PageIndex { get { return this._pageIndex; } set { this._pageIndex = value; } }
        public int NoOfRecordsPerPage { get { return this._noOfRecordsPerPage; } set { this._noOfRecordsPerPage = value; } }
        public string SortingColumn { get { return this._sortColumn; } set { this._sortColumn = value; } }
        public string SortingDirection { get { return this._sortDirection; } set { this._sortDirection = value; } }
        public int TotalRecordsCount { get { return this._totalRecordCount; } set { this._totalRecordCount = value; } }
        public string ModifyBy { get { return this._ModifyBy; } set { this._ModifyBy = value; } }
        public DateTime ModifyDate { get { return this._ModifyDate; } set { this._ModifyDate = value; } }
        public bool EnablePaging { get { return this._enablePaging; } set { this._enablePaging = value; } }
    }
    #endregion

    #region Screen class
    public class Screen : PagerData
    {
        private int _id;
        private string _screenName;
        private int _customerID;
        private string _modifiedBy;
        private System.DateTime _modifiedOn;
        private List<ScreenDetails> _screenDetailsCollection = new List<ScreenDetails>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Screen"/> class.
        /// </summary>
        public Screen()
        {
            // EMPTY
        }

        
        public int ID
        {
            get { return _id; }
            set { _id = value; }
        }

        
        public string ScreenName
        {
            get { return _screenName; }
            set { _screenName = value; }
        }

        
        public int CustomerID
        {
            get { return _customerID; }
            set { _customerID = value; }
        }

        
        public string ModifiedBy
        {
            get { return _modifiedBy; }
            set { _modifiedBy = value; }
        }

        
        public System.DateTime ModifiedOn
        {
            get
            {
                return _modifiedOn;
            }
            set
            {
                _modifiedOn = value;
            }
        }

        
        public List<ScreenDetails> ScreenDetailsCollection
        {
            set
            {
                if (value != null)
                {
                    this._screenDetailsCollection = value;
                }
            }
            get { return this._screenDetailsCollection; }
        }

        
        public override string ToString()
        {
            System.Text.StringBuilder dynStr = new System.Text.StringBuilder(GetType().Name);
            dynStr.Append(':');
            dynStr.Append("  ID=");
            dynStr.Append(ID);
            dynStr.Append("  ScreenName=");
            dynStr.Append(ScreenName);
            dynStr.Append("  CustomerID=");
            dynStr.Append(CustomerID.ToString());
            dynStr.Append("  ModifiedBy=");
            dynStr.Append(ModifiedBy);
            dynStr.Append("  ModifiedOn=");
            dynStr.Append(ModifiedOn);
            return dynStr.ToString();
        }
    }
    #endregion

    #region ScreenDetails class
    public class ScreenDetails : PagerData
    {
        private int _id;
        private int _screenID;
        private int _fieldID;
        private string _fieldName = "";
        private bool _isVisible = false;
        private bool _isEnable = false;
        private bool _isRequired = false;
        private bool _showInSearchCriteria = false;
        private bool _showInSearchResults = false;
        private bool _isMultiSelect = false;
        private bool _isRestricted = false;
        private bool _filter = false;
        private bool _isMultiline = false;

       
        public ScreenDetails()
        {
            // EMPTY
        }

        
        public int ID
        {
            get { return _id; }
            set { _id = value; }
        }

        
        public int ScreenID
        {
            get { return _screenID; }
            set { _screenID = value; }
        }

        
        public int FieldID
        {
            get { return _fieldID; }
            set { _fieldID = value; }
        }

        
        public string FieldName
        {
            get { return _fieldName; }
            set { _fieldName = value; }
        }

        
        public bool IsVisible
        {
            get
            {
                return _isVisible;
            }
            set
            {
                _isVisible = value;
            }
        }


        
        public bool IsEnable
        {
            get
            {
                return _isEnable;
            }
            set
            {
                _isEnable = value;
            }
        }


        
        public bool IsRequired
        {
            get
            {
                return _isRequired;
            }
            set
            {
                _isRequired = value;
            }
        }

        
        public bool ShowInSearchCriteria
        {
            get
            {
                return _showInSearchCriteria;
            }
            set
            {
                _showInSearchCriteria = value;
            }
        }

        
        public bool ShowInSearchResults
        {
            get
            {
                return _showInSearchResults;
            }
            set
            {
                _showInSearchResults = value;
            }
        }

        
        public bool IsMultiSelect
        {
            get
            {
                return _isMultiSelect;
            }
            set
            {
                _isMultiSelect = value;
            }
        }

        
        public bool IsRestricted
        {
            get
            {
                return _isRestricted;
            }
            set
            {
                _isRestricted = value;
            }
        }

       
        public bool Filter
        {
            get
            {
                return _filter;
            }
            set
            {
                _filter = value;
            }
        }

        
        public bool IsMultiline
        {
            get
            {
                return _isMultiline;
            }
            set
            {
                _isMultiline = value;
            }
        }

        
        public override string ToString()
        {
            System.Text.StringBuilder dynStr = new System.Text.StringBuilder(GetType().Name);
            dynStr.Append(':');
            dynStr.Append("  ID=");
            dynStr.Append(ID);
            dynStr.Append("  ScreenID=");
            dynStr.Append(ScreenID);
            dynStr.Append("  FieldID=");
            dynStr.Append(FieldID);
            dynStr.Append("  IsVisible=");
            dynStr.Append(IsVisible);
            dynStr.Append("  IsEnable=");
            dynStr.Append(IsEnable);
            dynStr.Append("  IsRequired=");
            dynStr.Append(IsRequired);
            dynStr.Append("  ShowInSearchCriteria=");
            dynStr.Append(ShowInSearchCriteria);
            dynStr.Append("  ShowInSearchResults=");
            dynStr.Append(ShowInSearchResults);
            dynStr.Append("  IsMultiSelect=");
            dynStr.Append(IsMultiSelect);
            dynStr.Append("  IsRestricted=");
            dynStr.Append(IsRestricted);
            dynStr.Append("  Filter=");
            dynStr.Append(Filter);
            dynStr.Append("  IsMultiline=");
            dynStr.Append(IsMultiline);
            return dynStr.ToString();
        }
    }
    #endregion
}
