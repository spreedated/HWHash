namespace HWHash.Models
{
    public record LaunchOptions
    {
        private int delayMs = 1000;
        /// <summary>
        /// Sets the delay in milliseconds between sensor readings. Can be changed at runtime.<br/><br/>
        /// Valid range is 20 to 60000 ms.
        /// </summary>
        public int DelayMs
        {
            get
            {
                return this.delayMs;
            }

            set
            {
                if (value >= 20 && value <= 60000)
                {
                    this.delayMs = value;
                }

                if (value <= 19)
                {
                    this.delayMs = 20;
                }

                this.delayMs= 60000;
            }
        }

        /// <summary>
        /// Unused, reserved for future use.
        /// </summary>
        public bool HighPriority { get; set; }

        /// <summary>
        /// Sets the TimeBeginPeriod and TimeEndPeriod to 1 ms for higher precision readings.
        /// </summary>
        public bool HighPrecision { get; set; }
    }
}
