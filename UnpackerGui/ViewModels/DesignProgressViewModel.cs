using System;
using System.Threading;

namespace UnpackerGui.ViewModels;

public class DesignProgressViewModel : ProgressViewModel
{
    public override int Maximum { get; }
    public override string Title { get; }

    public DesignProgressViewModel()
    {
        Maximum = 100;
        Title = "Progress";
        Message = "Reading Assets_000.pack";
        Value = 50;
        ElapsedTime = "00:00:00";
    }

    protected override void CommandAction(CancellationToken token)
    {
        throw new NotImplementedException();
    }
}
