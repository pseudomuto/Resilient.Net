using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Resilient.Net.Tests
{
    public class TestTraceListener : TraceListener
    {
        private readonly StringBuilder _buffer = new StringBuilder();

        #region [TraceListener Implementation]

        public override void Write(string message)
        {
            _buffer.Append(message);
        }

        public override void WriteLine(string message)
        {
            _buffer.AppendLine(message);
        }

        #endregion

        public string[] Lines
        {
            get
            {
                Flush();

                return _buffer.ToString().Split(
                    new string[] { Environment.NewLine },
                    StringSplitOptions.RemoveEmptyEntries
                );
            }
        }

        public string LastLine { get { return Lines.Last(); } }
    }
}

