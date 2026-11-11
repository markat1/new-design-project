using Microsoft.AspNetCore.Components;
using SendChecklist.Model;

namespace SendChecklist.Components;

/// Every piece reads the one checklist and redraws when it changes.
public abstract class CkBase : ComponentBase, IDisposable
{
    [Inject] protected Checklist C { get; set; } = default!;

    protected override void OnInitialized() => C.Changed += Redraw;

    private void Redraw() => InvokeAsync(StateHasChanged);

    public void Dispose() => C.Changed -= Redraw;
}
