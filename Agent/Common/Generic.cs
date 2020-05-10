using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

namespace Agent.Common
{
    public sealed class GenericObjectResult : SharpC2Result
    {
        public object Result { get; }
        protected internal override IList<SharpC2ResultProperty> ResultProperties
        {
            get
            {
                return new List<SharpC2ResultProperty>
                    {
                        new SharpC2ResultProperty
                        {
                            Name = this.Result.GetType().Name,
                            Value = this.Result
                        }
                    };
            }
        }

        public GenericObjectResult(object Result)
        {
            this.Result = Result;
        }
    }

    public class SharpC2ResultList<T> : IList<T> where T : SharpC2Result
    {
        private List<T> Results { get; } = new List<T>();

        public int Count => Results.Count;
        public bool IsReadOnly => ((IList<T>)Results).IsReadOnly;


        private const int PROPERTY_SPACE = 3;

        public string FormatList()
        {
            return this.ToString();
        }

        private string FormatTable()
        {
            return "";
        }

        public override string ToString()
        {
            if (this.Results.Count > 0)
            {
                StringBuilder labels = new StringBuilder();
                StringBuilder underlines = new StringBuilder();
                List<StringBuilder> rows = new List<StringBuilder>();
                for (int i = 0; i < this.Results.Count; i++)
                {
                    rows.Add(new StringBuilder());
                }
                for (int i = 0; i < this.Results[0].ResultProperties.Count; i++)
                {
                    labels.Append(this.Results[0].ResultProperties[i].Name);
                    underlines.Append(new string('-', this.Results[0].ResultProperties[i].Name.Length));
                    int maxproplen = 0;
                    for (int j = 0; j < rows.Count; j++)
                    {
                        SharpC2ResultProperty property = this.Results[j].ResultProperties[i];
                        string ValueString = property.Value.ToString();
                        rows[j].Append(ValueString);
                        if (maxproplen < ValueString.Length)
                        {
                            maxproplen = ValueString.Length;
                        }
                    }
                    if (i != this.Results[0].ResultProperties.Count - 1)
                    {
                        labels.Append(new string(' ', Math.Max(2, maxproplen + 2 - this.Results[0].ResultProperties[i].Name.Length)));
                        underlines.Append(new string(' ', Math.Max(2, maxproplen + 2 - this.Results[0].ResultProperties[i].Name.Length)));
                        for (int j = 0; j < rows.Count; j++)
                        {
                            SharpC2ResultProperty property = this.Results[j].ResultProperties[i];
                            string ValueString = property.Value.ToString();
                            rows[j].Append(new string(' ', Math.Max(this.Results[0].ResultProperties[i].Name.Length - ValueString.Length + 2, maxproplen - ValueString.Length + 2)));
                        }
                    }
                }
                labels.AppendLine();
                labels.Append(underlines.ToString());
                foreach (StringBuilder row in rows)
                {
                    labels.AppendLine();
                    labels.Append(row.ToString());
                }
                return labels.ToString();
            }
            return "";
        }

        public T this[int index] { get => Results[index]; set => Results[index] = value; }

        public IEnumerator<T> GetEnumerator()
        {
            return Results.Cast<T>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Results.Cast<T>().GetEnumerator();
        }

        public int IndexOf(T item)
        {
            return Results.IndexOf(item);
        }

        public void Add(T t)
        {
            Results.Add(t);
        }

        public void AddRange(IEnumerable<T> range)
        {
            Results.AddRange(range);
        }

        public void Insert(int index, T item)
        {
            Results.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            Results.RemoveAt(index);
        }

        public void Clear()
        {
            Results.Clear();
        }

        public bool Contains(T item)
        {
            return Results.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            Results.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return Results.Remove(item);
        }
    }

    public abstract class SharpC2Result
    {
        protected internal abstract IList<SharpC2ResultProperty> ResultProperties { get; }
    }

    public class SharpC2ResultProperty
    {
        public string Name { get; set; }
        public object Value { get; set; }
    }

    //

    public sealed class FileSystemEntryResult : SharpC2Result
    {
        public string Name { get; set; }
        public long Length { get; set; }
        public DateTime CreationTimeUtc { get; set; }
        public DateTime LastAccessTimeUtc { get; set; }
        public DateTime LastWriteTimeUtc { get; set; }
        protected internal override IList<SharpC2ResultProperty> ResultProperties
        {
            get
            {
                return new List<SharpC2ResultProperty>
                    {
                        new SharpC2ResultProperty { Name = "Name", Value = Name },
                        new SharpC2ResultProperty { Name = "Length", Value = Length },
                        new SharpC2ResultProperty { Name = "CreationTimeUtc", Value = CreationTimeUtc },
                        new SharpC2ResultProperty { Name = "LastAccessTimeUtc", Value = LastAccessTimeUtc },
                        new SharpC2ResultProperty { Name = "LastWriteTimeUtc", Value = LastWriteTimeUtc }
                    };
            }
        }
    }

    public sealed class DisableEtwSetting : SharpC2Result
    {
        public bool Disabled { get; set; }
        protected internal override IList<SharpC2ResultProperty> ResultProperties
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

    public sealed class BlockDllsSetting : SharpC2Result
    {
        public bool Disabled { get; set; }
        protected internal override IList<SharpC2ResultProperty> ResultProperties
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

    public sealed class PPIDSetting : SharpC2Result
    {
        public int ProcessId { get; set; }
        public string ProcessName { get; set; }
        protected internal override IList<SharpC2ResultProperty> ResultProperties
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

    public sealed class SpawnToSetting : SharpC2Result
    {
        public string Path { get; set; }
        protected internal override IList<SharpC2ResultProperty> ResultProperties
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

    public sealed class ReversePortForwardResult : SharpC2Result
    {
        public string BindAddress { get; set; }
        public int BindPort { get; set; }
        public string ForwardAddress { get; set; }
        public int ForwardPort { get; set; }
        protected internal override IList<SharpC2ResultProperty> ResultProperties
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

    public sealed class AgentHelpResult : SharpC2Result
    {
        public string Module { get; set; }
        public string Command { get; set; }
        public string Description { get; set; }
        public string HelpText { get; set; }
        protected internal override IList<SharpC2ResultProperty> ResultProperties
        {
            get
            {
                return new List<SharpC2ResultProperty>
                    {
                        new SharpC2ResultProperty { Name = "Module", Value = Module },
                        new SharpC2ResultProperty { Name = "Command", Value = Command },
                        new SharpC2ResultProperty { Name = "Description", Value = Description },
                        new SharpC2ResultProperty { Name = "HelpText", Value = HelpText }
                    };
            }
        }
    }
}