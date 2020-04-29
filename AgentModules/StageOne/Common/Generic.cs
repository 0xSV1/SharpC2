using System.Collections.Generic;

namespace Agent.Common
{
    internal sealed class DisableEtwSetting : SharpC2Result
    {
        internal bool Disabled { get; set; }
        public override IList<SharpC2ResultProperty> ResultProperties
        {
            get
            {
                return new List<SharpC2ResultProperty>
                    {
                        new SharpC2ResultProperty { Name = "Disabled", Value = Disabled }
                    };
            }
        }
    }

    internal sealed class BlockDllsSetting : SharpC2Result
    {
        internal bool Disabled { get; set; }
        public override IList<SharpC2ResultProperty> ResultProperties
        {
            get
            {
                return new List<SharpC2ResultProperty>
                    {
                        new SharpC2ResultProperty { Name = "Disabled", Value = Disabled }
                    };
            }
        }
    }

    internal sealed class PPIDSetting : SharpC2Result
    {
        internal int ProcessId { get; set; }
        internal string ProcessName { get; set; }
        public override IList<SharpC2ResultProperty> ResultProperties
        {
            get
            {
                return new List<SharpC2ResultProperty>
                    {
                        new SharpC2ResultProperty { Name = "Process Id", Value = ProcessId },
                        new SharpC2ResultProperty { Name = "Process Name", Value = ProcessName }
                    };
            }
        }
    }

    internal sealed class SpawnToSetting : SharpC2Result
    {
        internal string Path { get; set; }
        public override IList<SharpC2ResultProperty> ResultProperties
        {
            get
            {
                return new List<SharpC2ResultProperty>
                    {
                        new SharpC2ResultProperty { Name = "Path", Value = Path }
                    };
            }
        }
    }
}