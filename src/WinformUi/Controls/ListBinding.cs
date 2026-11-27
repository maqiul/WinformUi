using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;

namespace WinformUi.Controls
{
    /// <summary>
    /// ListControl-style data binding shared by the owner-drawn items controls:
    /// resolves DataSource/DataMember through the binding context, tracks list
    /// and position changes, and extracts display/value members.
    /// </summary>
    internal sealed class ListBinding : IDisposable
    {
        private static readonly IList Empty = new List<object>();
        private readonly Control _owner;
        private CurrencyManager _manager;
        private bool _syncing;

        public ListBinding(Control owner)
        {
            _owner = owner;
            _owner.BindingContextChanged += (s, e) => Rebind();
            _owner.HandleCreated += (s, e) => Rebind();
            _owner.ParentChanged += (s, e) => Rebind();
            _owner.Disposed += (s, e) => Dispose();
        }

        public event EventHandler ListChanged;
        public event EventHandler PositionChanged;

        public object DataSource { get; set; }

        public string DataMember { get; set; } = string.Empty;

        public string DisplayMember { get; set; } = string.Empty;

        public string ValueMember { get; set; } = string.Empty;

        public IList UnboundItems { get; set; } = new List<object>();

        public IList BoundList { get; private set; }

        public IList CurrentItems => DataSource != null ? (BoundList ?? Empty) : UnboundItems;

        public int Position => _manager != null ? _manager.Position : -1;

        public void Rebind()
        {
            Detach();
            BoundList = null;

            if (DataSource != null)
            {
                bool memberFailed = false;
                if (_owner.BindingContext != null)
                {
                    try
                    {
                        _manager = _owner.BindingContext[DataSource, DataMember] as CurrencyManager;
                    }
                    catch (ArgumentException)
                    {
                        // Transient invalid DataSource/DataMember combinations
                        // (e.g. designer property assignment order) bind nothing.
                        memberFailed = true;
                    }
                }

                if (_manager != null)
                {
                    BoundList = _manager.List;
                    _manager.ListChanged += OnManagerListChanged;
                    _manager.PositionChanged += OnManagerPositionChanged;
                }
                else if (!memberFailed)
                {
                    BoundList = ResolveFallback();
                }
            }

            ListChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetPosition(int index)
        {
            if (_manager == null || _syncing)
            {
                return;
            }

            _syncing = true;
            try
            {
                _manager.Position = index;
            }
            finally
            {
                _syncing = false;
            }
        }

        public string GetItemText(object item)
        {
            if (item == null)
            {
                return string.Empty;
            }

            if (DisplayMember.Length == 0)
            {
                return item.ToString();
            }

            PropertyDescriptor pd = TypeDescriptor.GetProperties(item)[DisplayMember];
            return pd != null ? (pd.GetValue(item)?.ToString() ?? string.Empty) : item.ToString();
        }

        public object GetItemValue(object item)
        {
            if (item == null || ValueMember.Length == 0)
            {
                return item;
            }

            PropertyDescriptor pd = TypeDescriptor.GetProperties(item)[ValueMember];
            return pd != null ? pd.GetValue(item) : item;
        }

        private IList ResolveFallback()
        {
            object source = DataSource;
            if (source is IListSource listSource)
            {
                source = listSource.GetList();
            }

            if (source is IList list)
            {
                return list;
            }

            if (source is IEnumerable enumerable)
            {
                var snapshot = new List<object>();
                foreach (object item in enumerable)
                {
                    snapshot.Add(item);
                }

                return snapshot;
            }

            throw new ArgumentException("DataSource must implement IList, IListSource or IEnumerable.", nameof(DataSource));
        }

        private void OnManagerListChanged(object sender, ListChangedEventArgs e)
        {
            BoundList = _manager.List;
            ListChanged?.Invoke(this, EventArgs.Empty);
        }

        private void OnManagerPositionChanged(object sender, EventArgs e)
        {
            if (!_syncing)
            {
                PositionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private void Detach()
        {
            if (_manager != null)
            {
                _manager.ListChanged -= OnManagerListChanged;
                _manager.PositionChanged -= OnManagerPositionChanged;
                _manager = null;
            }
        }

        public void Dispose()
        {
            Detach();
        }
    }
}