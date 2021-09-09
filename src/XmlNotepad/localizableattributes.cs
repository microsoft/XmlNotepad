using System;
using System.Diagnostics;
using System.ComponentModel;
using SR = XmlNotepad.StringResources;

namespace XmlNotepad
{
    [AttributeUsage(AttributeTargets.All)]
    public sealed class SRDescriptionAttribute : DescriptionAttribute
    {
        private bool _replaced = false;

        public SRDescriptionAttribute(string description)
            : base(description)
        {
        }

        public override string Description
        {
            get
            {
                if (!_replaced)
                {
                    _replaced = true;
                    DescriptionValue = SR.ResourceManager.GetString(base.Description);
                }
                return base.Description;
            }
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    public sealed class SRCategoryAttribute : CategoryAttribute
    {

        public SRCategoryAttribute(string category)
            : base(category)
        {
        }

        protected override string GetLocalizedString(string value)
        {
            return SR.ResourceManager.GetString(value);
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public sealed class LocDisplayNameAttribute : DisplayNameAttribute
    {
        private string _name;

        /// <include file='doc\PropertyPages.uex' path='docs/doc[@for="LocDisplayNameAttribute.DisplayNameAttribute"]/*' />
        public LocDisplayNameAttribute(string name)
        {
            this._name = name;
        }

        /// <include file='doc\PropertyPages.uex' path='docs/doc[@for="LocDisplayNameAttribute.DisplayName"]/*' />
        public override string DisplayName
        {
            get
            {
                string result = SR.ResourceManager.GetString(this._name);
                if (result == null)
                {
                    Debug.Assert(false, "String resource '" + this._name + "' is missing");
                    result = this._name;
                }
                return result;
            }
        }
    }
}
