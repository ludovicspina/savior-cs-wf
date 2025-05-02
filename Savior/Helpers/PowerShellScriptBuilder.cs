using System.Text;

namespace Savior.Helpers
{
    public class PowerShellScriptBuilder
    {
        private readonly StringBuilder _builder = new();

        public PowerShellScriptBuilder AddLine(string line)
        {
            _builder.AppendLine(line);
            return this;
        }

        public PowerShellScriptBuilder AddEmptyLine()
        {
            _builder.AppendLine();
            return this;
        }

        public override string ToString()
        {
            return _builder.ToString();
        }

        public static implicit operator string(PowerShellScriptBuilder builder)
        {
            return builder.ToString();
        }
    }
}