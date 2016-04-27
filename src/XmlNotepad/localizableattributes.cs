using System;
using System.Diagnostics;
using System.Text;
using System.ComponentModel;

namespace XmlNotepad {
    [AttributeUsage(AttributeTargets.All)]
    internal sealed class SRDescriptionAttribute : DescriptionAttribute {

        private bool replaced = false;

        public SRDescriptionAttribute(string description)
            : base(description) {
        }

        public override string Description {
            get {
                if (!replaced) {
                    replaced = true;
                    DescriptionValue = SR.ResourceManager.GetString(base.Description);
                }
                return base.Description;
            }
        }
    }

    [AttributeUsage(AttributeTargets.All)]
    internal sealed class SRCategoryAttribute : CategoryAttribute {

        public SRCategoryAttribute(string category)
            : base(category) {
        }

        protected override string GetLocalizedString(string value) {
            return SR.ResourceManager.GetString(value);
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    internal sealed class LocDisplayNameAttribute : DisplayNameAttribute {
        string name;

        /// <include file='doc\PropertyPages.uex' path='docs/doc[@for="LocDisplayNameAttribute.DisplayNameAttribute"]/*' />
        public LocDisplayNameAttribute(string name) {
            this.name = name;
        }

        /// <include file='doc\PropertyPages.uex' path='docs/doc[@for="LocDisplayNameAttribute.DisplayName"]/*' />
        public override string DisplayName {
            get {
                string result = SR.ResourceManager.GetString(this.name);
                if (result == null) {
                    Debug.Assert(false, "String resource '" + this.name + "' is missing");
                    result = this.name;
                }
                return result;
            }
        }
    }
}
