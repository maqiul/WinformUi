using System;
using System.ComponentModel;
#if NETFRAMEWORK
using System.ComponentModel.Design;
using System.Drawing.Design;
using System.Reflection;
using System.Windows.Forms.Design;
#else
using Microsoft.DotNet.DesignTools.Designers;
using Microsoft.DotNet.DesignTools.Designers.Actions;
#endif

namespace WinformUi.Design
{
    /// <summary>
    /// Gives the owner-drawn list controls the same design-time data source
    /// entry point as the native DataGridView: a "choose data source" smart tag.
    /// </summary>
    public class FluentListControlDesigner : ControlDesigner
    {
        private ListBindingActionList _actions;

        public override DesignerActionListCollection ActionLists
        {
            get
            {
                var lists = new DesignerActionListCollection();
                lists.AddRange(base.ActionLists);
                if (_actions == null)
                {
                    _actions = new ListBindingActionList(this, Component);
                }

                lists.Add(_actions);
                return lists;
            }
        }
    }

    public class ListBindingActionList : DesignerActionList
    {
        private bool _boundMode;
        private readonly FluentListControlDesigner _designer;

        public ListBindingActionList(FluentListControlDesigner designer, IComponent component) : base(component)
        {
            _designer = designer;
            _boundMode = GetPropertyValue("DataSource") != null;
        }

        public bool BoundMode
        {
            get { return _boundMode || GetPropertyValue("DataSource") != null; }
            set
            {
                _boundMode = value;
                if (!value)
                {
                    SetPropertyValue("DataSource", null);
                }
            }
        }

        [AttributeProvider(typeof(IListSource))]
        [TypeConverter(typeof(DataSourceReferenceConverter))]
        public object DataSource
        {
            get { return GetPropertyValue("DataSource"); }
            set { SetPropertyValue("DataSource", value); }
        }

        public string DataMember
        {
            get { return (string)GetPropertyValue("DataMember"); }
            set { SetPropertyValue("DataMember", value); }
        }

        public string DisplayMember
        {
            get { return (string)GetPropertyValue("DisplayMember"); }
            set { SetPropertyValue("DisplayMember", value); }
        }

        public string ValueMember
        {
            get { return (string)GetPropertyValue("ValueMember"); }
            set { SetPropertyValue("ValueMember", value); }
        }

        public void InvokeItemsDialog()
        {
#if NETFRAMEWORK
            PropertyDescriptor pd = TypeDescriptor.GetProperties(Component)["Items"];
            if (pd.GetEditor(typeof(UITypeEditor)) is UITypeEditor editor)
            {
                editor.EditValue(new ItemsEditorContext(Component, pd), Component.Site, pd.GetValue(Component));
            }
#else
            // The server cannot instantiate System.Design editors; ask the client
            // to open the property's editor, same as the property grid "..." button.
            _designer.InvokePropertyEditor("Items");
#endif
        }

        public override DesignerActionItemCollection GetSortedActionItems()
        {
            var items = new DesignerActionItemCollection();
            items.Add(new DesignerActionPropertyItem("BoundMode", L("Use Data Bound Items", "使用数据绑定项"), string.Empty, L("Switch between bound and unbound mode.", "在绑定模式与未绑定模式之间切换。")));
            if (BoundMode)
            {
                string cat = L("Bound Mode", "绑定模式");
                items.Add(new DesignerActionHeaderItem(cat));
                items.Add(new DesignerActionPropertyItem("DataSource", L("Choose Data Source", "选择数据源"), cat, L("A data component on this form (BindingSource, DataSet…).", "本窗体上的数据组件(BindingSource、DataSet 等)。")));
                items.Add(new DesignerActionPropertyItem("DataMember", L("Data Member", "数据成员"), cat, L("Table name when binding a DataSet.", "绑定 DataSet 时的表名。")));
                items.Add(new DesignerActionPropertyItem("DisplayMember", L("Display Member", "显示成员"), cat, L("Item property shown as text.", "用作显示文本的项属性名。")));
                items.Add(new DesignerActionPropertyItem("ValueMember", L("Value Member", "值成员"), cat, L("Item property returned by SelectedValue.", "用作 SelectedValue 的项属性名。")));
#if NETFRAMEWORK
                items.Add(new DesignerActionMethodItem(this, "ShowDataSources", L("Add Project Data Source...", "添加项目数据源..."), cat, L("Opens the Data Sources window where new data sources can be created.", "打开数据源窗口以新建数据源。"), true));
#endif
            }
            else
            {
                string cat = L("Unbound Mode", "未绑定模式");
                items.Add(new DesignerActionHeaderItem(cat));
                items.Add(new DesignerActionMethodItem(this, "InvokeItemsDialog", L("Edit Items...", "编辑项..."), cat, L("Opens the items collection editor.", "打开项集合编辑器。"), true));
            }

            return items;
        }

#if NETFRAMEWORK
        public void ShowDataSources()
        {
            // EnvDTE is late-bound so the library keeps zero compile-time VS dependencies.
            try
            {
                Type dteType = null;
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    dteType = asm.GetType("EnvDTE.DTE");
                    if (dteType != null)
                    {
                        break;
                    }
                }

                if (dteType == null)
                {
                    return;
                }

                object dte = GetService(dteType);
                if (dte != null)
                {
                    dteType.InvokeMember("ExecuteCommand", BindingFlags.InvokeMethod, null, dte, new object[] { "Data.ShowDataSourcesWindow" });
                }
            }
            catch (Exception)
            {
            }
        }
#endif

        private object GetPropertyValue(string name)
        {
            PropertyDescriptor pd = TypeDescriptor.GetProperties(Component)[name];
            return pd != null ? pd.GetValue(Component) : null;
        }

        private void SetPropertyValue(string name, object value)
        {
            PropertyDescriptor pd = TypeDescriptor.GetProperties(Component)[name];
            if (pd != null)
            {
                pd.SetValue(Component, value);
            }
        }

        private static string L(string en, string zh)
        {
            return System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "zh" ? zh : en;
        }

#if NETFRAMEWORK
        private sealed class ItemsEditorContext : ITypeDescriptorContext
        {
            private readonly IComponent _component;
            private readonly PropertyDescriptor _pd;

            public ItemsEditorContext(IComponent component, PropertyDescriptor pd)
            {
                _component = component;
                _pd = pd;
            }

            public IContainer Container
            {
                get { return _component.Site != null ? _component.Site.Container : null; }
            }

            public object Instance
            {
                get { return _component; }
            }

            public PropertyDescriptor PropertyDescriptor
            {
                get { return _pd; }
            }

            public object GetService(Type serviceType)
            {
                return _component.Site != null ? _component.Site.GetService(serviceType) : null;
            }

            public bool OnComponentChanging()
            {
                return true;
            }

            public void OnComponentChanged()
            {
            }
        }
#endif
    }

    /// <summary>
    /// Built-in reference converter scoped to IListSource: the property grid and
    /// the smart tag show the form's BindingSource/DataSet/DataTable components as
    /// a drop-down, in both the in-process and the out-of-process designer.
    /// </summary>
    public sealed class DataSourceReferenceConverter : ReferenceConverter
    {
        public DataSourceReferenceConverter() : base(typeof(IListSource))
        {
        }
    }
}