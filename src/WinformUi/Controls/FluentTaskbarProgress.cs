using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WinformUi.Interop;

namespace WinformUi.Controls
{
    /// <summary>Specifies the progress state shown on a Windows taskbar button.
    /// The numeric values match the shell's TBPFLAG so they can be passed straight through.</summary>
    public enum TaskbarProgressState
    {
        /// <summary>Shows no progress.</summary>
        None = 0,
        /// <summary>Shows an indeterminate progress animation.</summary>
        Indeterminate = 1,
        /// <summary>Shows determinate progress in the normal color.</summary>
        Normal = 2,
        /// <summary>Shows determinate progress in the error color.</summary>
        Error = 4,
        /// <summary>Shows determinate progress in the paused color.</summary>
        Paused = 8
    }

    /// <summary>
    /// Shows indeterminate, determinate, paused or error progress on a form's taskbar button.
    /// Add it to a form's component tray and assign <see cref="Form"/>; the value updates live.
    /// </summary>
    public sealed class FluentTaskbarProgress : Component
    {
        private Form _form;
        private TaskbarProgressState _state;
        private int _value;
        private int _total = 100;
        private bool _subscribed;

        /// <summary>Initializes a new instance of the <see cref="FluentTaskbarProgress"/> class.</summary>
        public FluentTaskbarProgress()
        {
        }

        /// <summary>Initializes a new instance associated with the given form.</summary>
        public FluentTaskbarProgress(IContainer container)
            : this()
        {
            if (container != null)
            {
                container.Add(this);
            }
        }

        /// <summary>Gets or sets the form whose taskbar button displays progress.</summary>
        [Category("Behavior")]
        [DefaultValue(null)]
        public Form Form
        {
            get { return _form; }
            set
            {
                if (ReferenceEquals(_form, value))
                {
                    return;
                }

                if (_form != null)
                {
                    Unsubscribe();
                    ApplyCore(_form, TaskbarProgressState.None, 0, 100);
                }

                _form = value;
                if (_form == null)
                {
                    return;
                }

                _form.HandleCreated += OnFormHandleCreated;
                _form.FormClosed += OnFormClosed;
                _subscribed = true;
                Apply();
            }
        }

        /// <summary>Gets or sets the taskbar progress state.</summary>
        [Category("Behavior")]
        [DefaultValue(TaskbarProgressState.None)]
        public TaskbarProgressState State
        {
            get { return _state; }
            set
            {
                if (_state == value)
                {
                    return;
                }

                _state = value;
                Apply();
            }
        }

        /// <summary>Gets or sets the completed progress out of <see cref="Total"/>.</summary>
        [Category("Behavior")]
        [DefaultValue(0)]
        public int Value
        {
            get { return _value; }
            set
            {
                int clamped = Math.Max(0, value);
                if (clamped == _value)
                {
                    return;
                }

                _value = clamped;
                Apply();
            }
        }

        /// <summary>Gets or sets the total progress; nonpositive values are treated as 100.</summary>
        [Category("Behavior")]
        [DefaultValue(100)]
        public int Total
        {
            get { return _total; }
            set
            {
                int normalized = value <= 0 ? 100 : value;
                if (normalized == _total)
                {
                    return;
                }

                _total = normalized;
                Apply();
            }
        }

        /// <summary>Gets whether this operating system exposes taskbar progress.</summary>
        public static bool IsSupported
        {
            get
            {
                Version version = NativeMethods.OsVersion;
                return version.Major > 6 || (version.Major == 6 && version.Minor >= 1);
            }
        }

        /// <summary>Reapplies the current state and value, for example after the handle was recreated.</summary>
        public void Apply()
        {
            ApplyCore(_form, _state, _value, _total);
        }

        /// <summary>Clears the taskbar button and returns <see cref="State"/> to <see cref="TaskbarProgressState.None"/>.</summary>
        public void Reset()
        {
            _state = TaskbarProgressState.None;
            Apply();
        }

        // The shell interface belongs to the thread that owns the window, so a progress update
        // raised on a worker thread is marshalled instead of building a second shell object there.
        private static void ApplyCore(Form form, TaskbarProgressState state, int value, int total)
        {
            if (form == null || !form.IsHandleCreated)
            {
                return;
            }

            if (form.InvokeRequired)
            {
                form.BeginInvoke((Action)delegate { ApplyCore(form, state, value, total); });
                return;
            }

            ApplyToForm(form, state, value, total);
        }

        private static void ApplyToForm(Form form, TaskbarProgressState state, int value, int total)
        {
            if (!IsSupported || form == null || !form.IsHandleCreated)
            {
                return;
            }

            ITaskbarList3 taskbar = GetTaskbar();
            if (taskbar == null)
            {
                return;
            }

            IntPtr handle = form.Handle;
            try
            {
                if (taskbar.SetProgressState(handle, (int)state) < 0)
                {
                    return;
                }

                if (state == TaskbarProgressState.Indeterminate || state == TaskbarProgressState.None)
                {
                    return;
                }

                int normalizedTotal = total <= 0 ? 100 : total;
                ulong completed = (ulong)Math.Max(0, Math.Min(value, normalizedTotal));
                taskbar.SetProgressValue(handle, completed, (ulong)normalizedTotal);
            }
            catch (Exception)
            {
                // Shell interface unavailable (older OS, group policy, non-STA caller): progress stays hidden.
            }
        }

        // One shell object per UI thread, kept for the process lifetime: releasing it makes the
        // taskbar button drop the progress bar it was showing.
        [ThreadStatic]
        private static ITaskbarList3 _taskbar;

        private static ITaskbarList3 GetTaskbar()
        {
            if (_taskbar != null)
            {
                return _taskbar;
            }

            try
            {
                ITaskbarList3 taskbar = (ITaskbarList3)new TaskbarList();
                if (taskbar.HrInit() == 0)
                {
                    _taskbar = taskbar;
                }
            }
            catch (Exception)
            {
            }

            return _taskbar;
        }

        private void OnFormHandleCreated(object sender, EventArgs e)
        {
            Apply();
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            _state = TaskbarProgressState.None;
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
            {
                return;
            }

            _form.HandleCreated -= OnFormHandleCreated;
            _form.FormClosed -= OnFormClosed;
            _subscribed = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Unsubscribe();
            }

            base.Dispose(disposing);
        }

        [ComImport]
        [Guid("56FDF344-FD6D-11D0-958A-006097C9A090")]
        [ClassInterface(ClassInterfaceType.None)]
        private class TaskbarList
        {
        }

        // ITaskbarList3 redeclares the inherited members in vtable order; skipping any of
        // them would shift SetProgressState onto the wrong slot.
        [ComImport]
        [Guid("ea1afb91-9e28-4b86-90e9-9e9f8a5eefaf")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface ITaskbarList3
        {
            [PreserveSig]
            int HrInit();

            [PreserveSig]
            int AddTab(IntPtr hwnd);

            [PreserveSig]
            int DeleteTab(IntPtr hwnd);

            [PreserveSig]
            int ActivateTab(IntPtr hwnd);

            [PreserveSig]
            int SetActiveAlt(IntPtr hwnd);

            [PreserveSig]
            int MarkFullscreenWindow(IntPtr hwnd, [MarshalAs(UnmanagedType.Bool)] bool fullscreen);

            [PreserveSig]
            int SetProgressValue(IntPtr hwnd, ulong completed, ulong total);

            [PreserveSig]
            int SetProgressState(IntPtr hwnd, int flags);
        }
    }
}
