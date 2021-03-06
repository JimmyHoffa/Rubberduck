using System.Runtime.InteropServices;
using Microsoft.Vbe.Interop;
using Rubberduck.UI.UnitTesting;
using Rubberduck.UnitTesting;

namespace Rubberduck.UI.Command
{
    /// <summary>
    /// A command that adds a new test method stub to the active code pane.
    /// </summary>
    [ComVisible(false)]
    public class AddTestMethodCommand : CommandBase
    {
        private readonly VBE _vbe;
        private readonly TestExplorerModelBase _model;

        public AddTestMethodCommand(VBE vbe, TestExplorerModelBase model)
        {
            _vbe = vbe;
            _model = model;
        }

        public override void Execute(object parameter)
        {
            // legacy static class...
            var test = NewTestMethodCommand.NewTestMethod(_vbe);
            if (test != null)
            {
                _model.Tests.Add(test);
            }
        }
    }
}