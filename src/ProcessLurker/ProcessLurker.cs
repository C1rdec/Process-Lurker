﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ProcessLurker
{
    public class ProcessLurker : IDisposable
    {
        #region Fields

        private static readonly int WaitingTime = 888;
        private IEnumerable<string> _processNames;
        private CancellationTokenSource _tokenSource;
        private Process _activeProcess;
        private int _processId;

        #endregion

        #region Constructors

        public ProcessLurker(string processName)
            : this(new string[] { processName })
        {
        }

        public ProcessLurker(IEnumerable<string> processNames)
        {
            _processNames = processNames;
            _tokenSource = new CancellationTokenSource();
        }

        #endregion

        #region Events

        public event EventHandler ProcessClosed;

        #endregion

        #region Methods

        public static Process GetProcessById(int processId)
        {
            try
            {
                return Process.GetProcessById(processId);
            }
            catch
            {
                return null;
            }
        }

        public static int CurrentProcessId => Environment.ProcessId;

        public virtual async Task<int> WaitForProcess(bool waitForExit)
        {
            var process = GetProcess();

            while (process == null)
            {
                await Task.Delay(WaitingTime);
                process = GetProcess();
            }

            if (waitForExit)
            {
                WaitForExit();
            }

            return WaitForWindowHandle();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            Dispose(true);
        }

        protected virtual void OnExit()
        {
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tokenSource.Cancel();

                if (_activeProcess != null)
                {
                    _activeProcess.Dispose();
                }
            }
        }

        public Process GetProcess()
        {
            if (_activeProcess != null)
            {
                _activeProcess.Dispose();
            }

            foreach (var processName in _processNames)
            {
                var process = Process.GetProcessesByName(processName).FirstOrDefault();
                if (process != null)
                {
                    _activeProcess = process;
                    return process;
                }
            }

            return null;
        }

        private async void WaitForExit()
        {
            await Task.Run(() =>
            {
                try
                {
                    var token = _tokenSource.Token;
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }

                    var process = GetProcess();
                    while (process != null)
                    {
                        process.WaitForExit(WaitingTime);
                        process = GetProcess();
                    }
                }
                catch
                {
                }

                OnExit();
                ProcessClosed?.Invoke(this, EventArgs.Empty);
            });
        }

        private int WaitForWindowHandle()
        {
            Process currentProcess;

            try
            {
                do
                {
                    var process = this.GetProcess();
                    Thread.Sleep(200);
                    currentProcess = process ?? throw new InvalidOperationException();
                }
                while (currentProcess.MainWindowHandle == IntPtr.Zero);

                _processId = currentProcess.Id;
            }
            catch
            {
                _processId = this.WaitForWindowHandle();
            }

            return _processId;
        }

        #endregion
    }
}