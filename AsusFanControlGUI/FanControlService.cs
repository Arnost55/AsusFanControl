using AsusFanControl;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace AsusFanControlGUI
{
    internal sealed class FanControlService : IDisposable
    {
        private readonly SemaphoreSlim hardwareGate = new SemaphoreSlim(1, 1);
        private AsusControl control;
        private bool disposed;
        private string lastErrorMessage;

        public string LastErrorMessage
        {
            get { return lastErrorMessage; }
        }

        public Task<FanStatusSnapshot> ReadSnapshotAsync()
        {
            return Task.Run(() =>
            {
                hardwareGate.Wait();
                try
                {
                    return ReadSnapshotLocked();
                }
                finally
                {
                    hardwareGate.Release();
                }
            });
        }

        public Task ApplySpeedAsync(int percent)
        {
            return Task.Run(() =>
            {
                hardwareGate.Wait();
                try
                {
                    var activeControl = EnsureControlLocked();
                    activeControl.SetFanSpeeds(percent);
                }
                catch (Exception ex)
                {
                    lastErrorMessage = DescribeException(ex);
                    throw;
                }
                finally
                {
                    hardwareGate.Release();
                }
            });
        }

        public void DisableControl()
        {
            hardwareGate.Wait();
            try
            {
                var activeControl = EnsureControlLocked();
                activeControl.SetFanSpeeds(0);
            }
            catch (Exception ex)
            {
                lastErrorMessage = DescribeException(ex);
            }
            finally
            {
                hardwareGate.Release();
            }
        }

        private FanStatusSnapshot ReadSnapshotLocked()
        {
            var snapshot = new FanStatusSnapshot
            {
                FanSpeeds = new List<int>(),
                StatusText = "ASUS interface unavailable",
                ErrorMessage = lastErrorMessage
            };

            AsusControl activeControl;
            if (!TryEnsureControlLocked(out activeControl))
            {
                snapshot.IsReady = false;
                snapshot.StatusText = "ASUS interface unavailable";
                snapshot.ErrorMessage = lastErrorMessage;
                return snapshot;
            }

            var errors = new List<string>();

            try
            {
                snapshot.FanSpeeds = activeControl.GetFanSpeeds();
            }
            catch (Exception ex)
            {
                errors.Add("RPM: " + DescribeException(ex));
            }

            try
            {
                var cpuTemperature = activeControl.Thermal_Read_Cpu_Temperature();
                if (cpuTemperature > 0 && cpuTemperature <= 150)
                {
                    snapshot.CpuTemperature = (int)cpuTemperature;
                }
            }
            catch (Exception ex)
            {
                errors.Add("CPU: " + DescribeException(ex));
            }

            snapshot.IsReady = errors.Count == 0;
            snapshot.StatusText = snapshot.IsReady ? "ASUS interface ready" : "ASUS interface issue";
            snapshot.ErrorMessage = errors.Count == 0 ? null : string.Join(" ", errors);

            if (!snapshot.IsReady)
            {
                lastErrorMessage = snapshot.ErrorMessage;
            }
            else
            {
                lastErrorMessage = null;
            }

            return snapshot;
        }

        private AsusControl EnsureControlLocked()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(FanControlService));
            }

            if (control != null)
            {
                return control;
            }

            try
            {
                control = new AsusControl();
                lastErrorMessage = null;
                return control;
            }
            catch (Exception ex)
            {
                lastErrorMessage = DescribeException(ex);
                throw new InvalidOperationException("ASUS hardware is unavailable.", ex);
            }
        }

        private bool TryEnsureControlLocked(out AsusControl activeControl)
        {
            activeControl = null;

            if (disposed)
            {
                throw new ObjectDisposedException(nameof(FanControlService));
            }

            if (control != null)
            {
                activeControl = control;
                return true;
            }

            try
            {
                control = new AsusControl();
                lastErrorMessage = null;
                activeControl = control;
                return true;
            }
            catch (Exception ex)
            {
                lastErrorMessage = DescribeException(ex);
                return false;
            }
        }

        private static string DescribeException(Exception ex)
        {
            if (ex == null)
            {
                return "Unknown ASUS hardware error.";
            }

            var message = ex.Message;
            if (string.IsNullOrWhiteSpace(message))
            {
                message = ex.GetType().Name;
            }

            return message;
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            hardwareGate.Wait();
            try
            {
                if (control != null)
                {
                    control.Dispose();
                    control = null;
                }
            }
            catch
            {
                // Best-effort cleanup only.
            }
            finally
            {
                hardwareGate.Release();
                hardwareGate.Dispose();
            }
        }
    }
}
