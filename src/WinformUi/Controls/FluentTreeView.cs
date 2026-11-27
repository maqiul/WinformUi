using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using WinformUi.Drawing;
using WinformUi.Theming;

namespace WinformUi.Controls
{
    /// <summary>A node in <see cref="FluentTreeView"/>.</summary>
    public class FluentTreeNode
    {
        public string Text { get; set; }
        public SymbolRegular Icon { get; set; }
        public object Tag { get; set; }
        public bool IsExpanded { get; set; }
        public List<FluentTreeNode> Nodes { get; } = new List<FluentTreeNode>();

        public FluentTreeNode()
        {
        }

        public FluentTreeNode(string text)
        {
            Text = text;
        }

        public FluentTreeNode(string text, params FluentTreeNode[] children)
        {
            Text = text;
            Nodes.AddRange(children);
        }
    }

    /// <summary>
    /// Fluent tree view: indent + chevron expansion,
    /// subtle hover and accent selection pill.
    /// </summary>
    [System.ComponentModel.ToolboxItem(true)]
    public class FluentTreeView : FluentControlBase
    {
        private const int RowHeight = 30;
        private const int Indent = 18;

        private readonly List<FluentTreeNode> _rootNodes = new List<FluentTreeNode>();
        private readonly List<KeyValuePair<FluentTreeNode, int>> _visible = new List<KeyValuePair<FluentTreeNode, int>>();
        private FluentTreeNode _selectedNode;
        private int _hoverIndex = -1;
        private int _scrollOffset;

        public event EventHandler SelectedNodeChanged;
        public event EventHandler NodeExpandedChanged;

        public FluentTreeView()
        {
            Size = new Size(240, 200);
            Cursor = Cursors.Hand;
            BackColor = Color.Transparent;
        }

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public List<FluentTreeNode> RootNodes => _rootNodes;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public FluentTreeNode SelectedNode
        {
            get { return _selectedNode; }
            set
            {
                if (_selectedNode != value)
                {
                    _selectedNode = value;
                    OnSelectedNodeChanged(EventArgs.Empty);
                    Invalidate();
                }
            }
        }

        protected virtual void OnSelectedNodeChanged(EventArgs e)
        {
            SelectedNodeChanged?.Invoke(this, e);
        }

        protected virtual void OnNodeExpandedChanged(EventArgs e)
        {
            NodeExpandedChanged?.Invoke(this, e);
        }

        public void Rebuild()
        {
            _visible.Clear();
            foreach (var node in _rootNodes)
            {
                Flatten(node, 0);
            }

            Invalidate();
        }

        private void Flatten(FluentTreeNode node, int depth)
        {
            _visible.Add(new KeyValuePair<FluentTreeNode, int>(node, depth));
            if (node.IsExpanded)
            {
                foreach (var child in node.Nodes)
                {
                    Flatten(child, depth + 1);
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            int index = (e.Y + _scrollOffset) / RowHeight;
            if (index < 0 || index >= _visible.Count) index = -1;
            if (_hoverIndex != index)
            {
                _hoverIndex = index;
                Invalidate();
            }
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _hoverIndex = -1;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button != MouseButtons.Left) return;

            int index = (e.Y + _scrollOffset) / RowHeight;
            if (index < 0 || index >= _visible.Count) return;

            var entry = _visible[index];
            float chevronX = 4 + entry.Value * Indent;
            if (entry.Key.Nodes.Count > 0 && e.X >= chevronX && e.X <= chevronX + 18)
            {
                entry.Key.IsExpanded = !entry.Key.IsExpanded;
                Rebuild();
                OnNodeExpandedChanged(EventArgs.Empty);
                return;
            }

            SelectedNode = entry.Key;
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            base.OnMouseWheel(e);
            int maxOffset = Math.Max(0, _visible.Count * RowHeight - Height);
            _scrollOffset = Math.Max(0, Math.Min(maxOffset, _scrollOffset - e.Delta / 2));
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            switch (keyData & Keys.KeyCode)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Left:
                case Keys.Right:
                    return true;
                default:
                    return base.IsInputKey(keyData);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            int index = _selectedNode == null ? -1 : _visible.FindIndex(kv => kv.Key == _selectedNode);

            switch (e.KeyCode)
            {
                case Keys.Down:
                    if (index < _visible.Count - 1) SelectedNode = _visible[index + 1].Key;
                    e.Handled = true;
                    break;
                case Keys.Up:
                    if (index > 0) SelectedNode = _visible[index - 1].Key;
                    e.Handled = true;
                    break;
                case Keys.Right:
                    if (_selectedNode != null && _selectedNode.Nodes.Count > 0 && !_selectedNode.IsExpanded)
                    {
                        _selectedNode.IsExpanded = true;
                        Rebuild();
                        OnNodeExpandedChanged(EventArgs.Empty);
                    }

                    e.Handled = true;
                    break;
                case Keys.Left:
                    if (_selectedNode != null && _selectedNode.IsExpanded)
                    {
                        _selectedNode.IsExpanded = false;
                        Rebuild();
                        OnNodeExpandedChanged(EventArgs.Empty);
                    }

                    e.Handled = true;
                    break;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            ControlHelper.PaintFullBackground(this, e.Graphics);
            if (_visible.Count == 0 && _rootNodes.Count > 0)
            {
                Rebuild();
            }

            Graphics g = e.Graphics;
            FluentGraphics.SetupGraphics(g);
            FluentPalette p = Palette;
            Color background = ControlHelper.GetEffectiveBackground(this);

            int first = _scrollOffset / RowHeight;
            int last = Math.Min(_visible.Count - 1, (_scrollOffset + Height) / RowHeight);

            for (int i = first; i <= last; i++)
            {
                var entry = _visible[i];
                FluentTreeNode node = entry.Key;
                float y = i * RowHeight - _scrollOffset;
                var rect = new RectangleF(2, y + 2, Width - 4, RowHeight - 4);

                bool selected = node == _selectedNode;
                bool hovered = i == _hoverIndex;

                if (selected || hovered)
                {
                    using (var brush = new SolidBrush(AccentColor.Blend(p.SubtleFillSecondary, background)))
                    {
                        FluentGraphics.FillRoundedRect(g, brush, rect, FluentGraphics.ControlCornerRadius);
                    }
                }

                if (selected)
                {
                    using (var brush = new SolidBrush(p.SystemAccentColorPrimary))
                    {
                        FluentGraphics.FillRoundedRect(g, brush, new RectangleF(rect.X + 2, rect.Y + (rect.Height - 14) / 2f, 3, 14), 1.5f);
                    }
                }

                float x = 4 + entry.Value * Indent;

                if (node.Nodes.Count > 0)
                {
                    int chevron = node.IsExpanded ? 0xE70D : 0xE76C;
                    FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph(chevron), FluentTypography.Icon(7f), p.TextFillSecondary, new RectangleF(x, y + (RowHeight - 14) / 2f, 14, 14));
                }

                x += 18;

                if (node.Icon != SymbolRegular.Empty)
                {
                    FluentGraphics.DrawGlyph(g, FluentGraphics.Glyph((int)node.Icon), FluentTypography.IconRegular(11f), p.TextFillSecondary, new RectangleF(x, y + (RowHeight - 16) / 2f, 16, 16));
                    x += 22;
                }

                using (var format = new StringFormat { LineAlignment = StringAlignment.Center, FormatFlags = StringFormatFlags.NoWrap, Trimming = StringTrimming.EllipsisCharacter })
                {
                    FluentGraphics.DrawText(g, node.Text ?? string.Empty, Font, p.TextFillPrimary, new RectangleF(x, y, Width - x - 6, RowHeight), format);
                }
            }
        }
    }
}