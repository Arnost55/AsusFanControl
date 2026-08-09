using AsusSystemAnalysis;
using System;
using System.Collections.Generic;
using System.Threading;

namespace AsusFanControl
{
    public class AsusControl : IDisposable
    {
        private bool disposed;

        public AsusControl()
        {
            AsusWinIO64.InitializeWinIo();
        }

        ~AsusControl()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            try
            {
                AsusWinIO64.ShutdownWinIo();
            }
            catch
            {
                // Best-effort cleanup only.
            }

            disposed = true;
        }

        private void EnsureNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(AsusControl));
            }
        }

        private static int ClampPercent(int percent)
        {
            if (percent < 0)
            {
                return 0;
            }

            if (percent > 100)
            {
                return 100;
            }

            return percent;
        }

        public void SetFanSpeed(byte value, byte fanIndex = 0)
        {
            EnsureNotDisposed();
            AsusWinIO64.HealthyTable_SetFanIndex(fanIndex);
            AsusWinIO64.HealthyTable_SetFanTestMode((char)(value > 0 ? 0x01 : 0x00));
            AsusWinIO64.HealthyTable_SetFanPwmDuty(value);
        }

        public void SetFanSpeed(int percent, byte fanIndex = 0)
        {
            EnsureNotDisposed();
            percent = ClampPercent(percent);
            var value = (byte)(percent / 100.0f * 255);
            SetFanSpeed(value, fanIndex);
        }

        public void SetFanSpeeds(byte value)
        {
            EnsureNotDisposed();

            var fanCount = AsusWinIO64.HealthyTable_FanCounts();
            for (byte fanIndex = 0; fanIndex < fanCount; fanIndex++)
            {
                SetFanSpeed(value, fanIndex);
                Thread.Sleep(20);
            }
        }

        public void SetFanSpeeds(int percent)
        {
            EnsureNotDisposed();
            percent = ClampPercent(percent);
            var value = (byte)(percent / 100.0f * 255);
            SetFanSpeeds(value);
        }

        public int GetFanSpeed(byte fanIndex = 0)
        {
            EnsureNotDisposed();
            AsusWinIO64.HealthyTable_SetFanIndex(fanIndex);
            var fanSpeed = AsusWinIO64.HealthyTable_FanRPM();
            return fanSpeed;
        }

        public List<int> GetFanSpeeds()
        {
            EnsureNotDisposed();
            var fanSpeeds = new List<int>();

            var fanCount = AsusWinIO64.HealthyTable_FanCounts();
            for (byte fanIndex = 0; fanIndex < fanCount; fanIndex++)
            {
                var fanSpeed = GetFanSpeed(fanIndex);
                fanSpeeds.Add(fanSpeed);
            }

            return fanSpeeds;
        }

        public int HealthyTable_FanCounts()
        {
            EnsureNotDisposed();
            return AsusWinIO64.HealthyTable_FanCounts();
        }

        public ulong Thermal_Read_Cpu_Temperature()
        {
            EnsureNotDisposed();
            return AsusWinIO64.Thermal_Read_Cpu_Temperature();
        }
    }
}
