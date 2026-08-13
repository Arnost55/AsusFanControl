using System.Collections.Generic;

namespace AsusFanControlGUI
{
    public sealed class FanStatusSnapshot
    {
        public bool IsReady { get; set; }

        public string StatusText { get; set; }

        public string ErrorMessage { get; set; }

        public List<int> FanSpeeds { get; set; } = new List<int>();

        public int? CpuTemperature { get; set; }

        public int FanCount
        {
            get { return FanSpeeds == null ? 0 : FanSpeeds.Count; }
        }
    }
}
