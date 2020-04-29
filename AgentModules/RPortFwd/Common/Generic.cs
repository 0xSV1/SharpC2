using System.Collections.Generic;

namespace Agent.Common
{
    internal sealed class ReversePortForwardResult : SharpC2Result
    {
        internal string BindAddress { get; set; }
        internal int BindPort { get; set; }
        internal string ForwardAddress { get; set; }
        internal int ForwardPort { get; set; }
        public override IList<SharpC2ResultProperty> ResultProperties
        {
            get
            {
                return new List<SharpC2ResultProperty>
                    {
                        new SharpC2ResultProperty { Name = "Bind Address", Value = BindAddress },
                        new SharpC2ResultProperty { Name = "Bind Port", Value = BindPort },
                        new SharpC2ResultProperty { Name = "Forward Address", Value = ForwardAddress },
                        new SharpC2ResultProperty { Name = "Forward Port", Value = ForwardPort }
                    };
            }
        }
    }
}