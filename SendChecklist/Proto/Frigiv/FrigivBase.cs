using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace SendChecklist.Proto.Frigiv;

// PROTO frigiv: four ways to ask about, send and confirm a release to the
// warehouse system. Each variant is one .razor file here; this is what they
// share: the lines chosen, and the stages ask -> busy -> done.
/// One line's share of an order: how many of its pieces go there, and for how much.
public record OrderRow(int Lp, string Sku, string Name, int Qty, decimal Amount, decimal? Diff);

public record OrderSum(int No, int Lines, int Qty, decimal Amount, IReadOnlyList<OrderRow> Rows);

public record Batch(int N, int Total, decimal Sum, IReadOnlyList<OrderSum> Orders, int OffPrice, int Unplaced, int Qty, decimal DiffSum)
{
    public string Linjer => N == 1 ? "linje" : "linjer";
    public string Ordrer => Orders.Count == 1 ? "ordre" : "ordrer";
}

public abstract class FrigivBase : ComponentBase, IDisposable
{
    [Inject] protected IJSRuntime JS { get; set; } = default!;
    [Parameter] public Batch B { get; set; } = default!;
    [Parameter] public EventCallback OnDone { get; set; }
    /// Open at once when mounted: the picker switched while one was open.
    [Parameter] public bool AutoOpen { get; set; }

    protected string stage = "ask";
    protected bool open, failed;
    protected ElementReference dlgEl, doneBtn, firstBtn;
    private bool shown, focusDone, focusFirst, gone;
    private IJSObjectReference? js;

    /// A native <dialog>, opened with showModal(). The variant without a
    /// modal turns this off.
    protected virtual bool Modal => true;
    public bool IsOpen => open;
    protected bool Busy => stage == "busy";
    protected bool Done => stage == "done";

    public void Show() { stage = "ask"; failed = false; open = true; focusFirst = !Modal; StateHasChanged(); }

    protected void Close() { if (!Busy) open = false; }

    /// PROTO: the warehouse system's answer is a wait.
    protected virtual Task Work() => Task.Delay(1400);

    /// `fail` shows the release failing: hold Shift while pressing.
    protected async Task Release(bool fail)
    {
        if (stage != "ask") return;
        stage = "busy"; failed = false;
        StateHasChanged();
        await Work();
        if (gone) return;
        if (fail) { stage = "ask"; failed = true; StateHasChanged(); return; }
        stage = "done"; focusDone = true;
        await OnDone.InvokeAsync();
        StateHasChanged();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && AutoOpen) { Show(); return; }
        if (Modal && open != shown)
        {
            shown = open;
            js ??= await JS.InvokeAsync<IJSObjectReference>("import", "./js/proto-indkob.js");
            await js.InvokeVoidAsync("dialog", dlgEl, open);
        }
        if (focusFirst && firstBtn.Id is not null) { focusFirst = false; await firstBtn.FocusAsync(); }
        if (focusDone && doneBtn.Id is not null) { focusDone = false; await doneBtn.FocusAsync(); }
    }

    public void Dispose() => gone = true;
}
