using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimaticClientService
{
    public class Tag
    {
        private string _itemName;
        public string ItemName
        {
            get { return _itemName; }
            set { _itemName = value; }
        }

        private object _itemValue;
        public object ItemValue
        {
            get { return _itemValue; }
            set { _itemValue = value; }
        }
        private object _itemQValue;
        public object ItemQValue
        {
            get { return _itemQValue; }
            set { _itemQValue = value; }
        }
        private string _itemType;
        public string ItemType
        {
            get { return _itemType; }
            set { _itemType = value; }
        }
        private string _itemQType;
        public string ItemQType
        {
            get { return _itemQType; }
            set { _itemQType = value; }
        }
        private string _itemAddr;
        public string ItemAddr
        {
            get { return _itemAddr; }
            set { _itemAddr = value; }
        }
        private bool _itemQUse;
        public bool ItemQUse
        {
            get { return _itemQUse; }
            set { _itemQUse = value; }
        }

        private int _itemDBAddr;
        public int ItemDBAddr
        {
            get { return _itemDBAddr; }
            set { _itemDBAddr = value; }
        }
        private int _itemAddrInDB;
        public int ItemAddrInDB
        {
            get { return _itemAddrInDB; }
            set { _itemAddrInDB = value; }
        }
        private int _itemQDBAddr;
        public int ItemQDBAddr
        {
            get { return _itemQDBAddr; }
            set { _itemQDBAddr = value; }
        }
        private int _itemQAddrInDB;
        public int ItemQAddrInDB
        {
            get { return _itemQAddrInDB; }
            set { _itemQAddrInDB = value; }
        }

        private string _item1CDestination;
        public string Item1CDestination
        {
            get { return _item1CDestination; }
            set { _item1CDestination = value; }
        }
        private string _item1CSource;
        public string Item1CSource
        {
            get { return _item1CSource; }
            set { _item1CSource = value; }
        }
        private string _itemFunc;
        public string ItemFunc
        {
            get { return _itemFunc; }
            set { _itemFunc = value; }
        }

        private byte _itemFuncParNum;
        public byte ItemFuncParNum
        {
            get { return _itemFuncParNum; }
            set { _itemFuncParNum = value; }
        }
        private string _itemParamRule;
        public string ItemParamRule
        {
            get { return _itemParamRule; }
            set { _itemParamRule = value; }
        }

        private string _itemTagLink;
        public string ItemTagLink
        {
            get { return _itemTagLink; }
            set { _itemTagLink = value; }
        }
        private List<Tag> _itemTagRef;
        public List<Tag> ItemTagRef
        {
            get { return _itemTagRef; }
            set { _itemTagRef = value; }
        }

        #region Costructors
        public Tag()
        {

        }

        public Tag(string itemName)
        {
            this.ItemName = itemName;
        }

        public Tag(string itemName, object itemValue)
        {
            this.ItemName = itemName;
            this.ItemValue = itemValue;
        }

        public Tag(string itemName, string itemAddr, string itemType, object itemValue)
        {
            this.ItemName = itemName;
            this.ItemAddr = itemAddr;
            this.ItemType = itemType;
            this.ItemValue = itemValue;
        }
        public Tag(string itemName, int itemDBAddr, int itemAddrInDB, string itemType, object itemValue)
        {
            this.ItemName = itemName;
            this.ItemDBAddr = itemDBAddr;
            this.ItemAddrInDB = itemAddrInDB;
            this.ItemType = itemType;
            this.ItemValue = itemValue;
        }
        //16
        public Tag(string itemName, int itemDBAddr, int itemAddrInDB, string itemType, object itemValue, bool itemQUse, int itemQDBAddr, int itemQAddrInDB, string itemQType, object itemQValue, string item1CDestination, string item1CSource, string itemFunc, byte itemFuncParNum, string itemParamRule, string itemTagLink, List<Tag> itemRef)
        {
            this.ItemName = itemName;
            this.ItemDBAddr = itemDBAddr;
            this.ItemAddrInDB = itemAddrInDB;
            this.ItemType = itemType;
            this.ItemValue = itemValue;
            this.ItemQUse = itemQUse;
            this.ItemQDBAddr = itemQDBAddr;
            this.ItemQAddrInDB = itemQAddrInDB;
            this.ItemQType = itemQType;
            this.ItemQValue = itemQValue;
            this.Item1CDestination = item1CDestination;
            this.Item1CSource = item1CSource;
            this.ItemFunc = itemFunc;
            this.ItemFuncParNum = itemFuncParNum;
            this.ItemParamRule = itemParamRule;
            this.ItemTagLink = itemTagLink;
            this.ItemTagRef = itemRef;
        }
        #endregion

        public /*override*/ void Change(string itemName, int itemDBAddr, int itemAddrInDB, string itemType)
        {
            this.ItemName = itemName;
            this.ItemDBAddr = itemDBAddr;
            this.ItemAddrInDB = itemAddrInDB;
            this.ItemType = itemType;

        }
        public /*override*/ void Change(string itemName, int itemDBAddr, int itemAddrInDB, string itemType, bool itemQUse, int itemQDBAddr, int itemQAddrInDB, string itemQType, string item1CDestination, string item1CSource, string itemfunc)
        {
            this.ItemName = itemName;
            this.ItemDBAddr = itemDBAddr;
            this.ItemAddrInDB = itemAddrInDB;
            this.ItemType = itemType;
            this.ItemQUse = ItemQUse;
            this.ItemQDBAddr = itemQDBAddr;
            this.ItemQAddrInDB = itemQAddrInDB;
            this.ItemQType = itemQType;
            this.Item1CDestination = item1CDestination;
            this.Item1CSource = item1CSource;
            this.ItemFunc = itemfunc;
        }
    }
}
