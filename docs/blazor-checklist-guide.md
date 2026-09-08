# Pristilbud i Blazor — fra tom mappe til hostet app

Sådan bygger du et Blazor WebAssembly-projekt fra prototypen: først checklisten alene, så et app-skelet med sidemenu, en tabel over sendte pristilbud og trin-flowet der laver et nyt — og til sidst en hurtig Release-version du kan dele.

Hvert trin i denne guide er udført og verificeret på den kode der ligger i `proto/BlazorProto` (commit `fdbb995`). Følger du dem i rækkefølge, ender du med det samme resultat — og det færdige projekt ligger som reference i `OnboardingChecklist/` i repoet.

*new-design-project · september 2026 · .NET 8*

---

## 0. Før du starter

- **.NET 8 SDK.** Tjek med `dotnet --version` — du skal se `8.x`. Guiden er testet på 8.0.130.
- **Kildekoden.** Vi kopierer filer fra den eksisterende prototype. I kommandoerne nedenfor står `$SRC` for stien til den: `SRC=~/dev/emil-kowalski/new-design-project/proto/BlazorProto`
- **En terminal.** Alt her er kommandolinje + en teksteditor. Ingen Visual Studio nødvendig.

> Hvorfor et nyt projekt og ikke bare slette de to andre varianter? Fordi prototype-harnesset (picker, tastaturnavigation, variant-skift, `?v=N` i URL'en) er filtret ind i `Proto.razor`, `Picker.razor`, `picker.js` og en tredjedel af CSS'en. Det er hurtigere og renere at starte fra nul og hente præcis de filer checklisten bruger.

## 1. Byg projektet

### 1. Opret et tomt Blazor WASM-projekt

```
dotnet new blazorwasm -e -o OnboardingChecklist
cd OnboardingChecklist
```

`-e` betyder *empty*: ingen Bootstrap, ingen eksempelsider. Templaten laver stadig en router, et layout og en Home-side, som vi ikke skal bruge — slet dem sammen med dens CSS og index.html:

```
rm -rf App.razor Pages Layout wwwroot/css wwwroot/index.html
mkdir -p Model Components wwwroot/css wwwroot/js
```

> Appen er én side uden navigation, så en `Router` og et `MainLayout` er kun støj. Root-komponenten monteres direkte i `Program.cs` (trin 3).

### 2. Slå kulturdata fra i `.csproj`

Åbn `OnboardingChecklist.csproj` og tilføj to linjer i `<PropertyGroup>`, lige efter `<ImplicitUsings>`:

*OnboardingChecklist.csproj*

```
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
  <Nullable>enable</Nullable>
  <ImplicitUsings>enable</ImplicitUsings>
+   true
+   false
</PropertyGroup>
```

> Appen formaterer ingen datoer eller tal efter kultur. Uden de to linjer sender Blazor ~2,5 MB ICU- og tidszonedata med i hvert load, som aldrig bliver brugt.

### 3. Erstat `Program.cs`

*Program.cs*

```
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using OnboardingChecklist;
using OnboardingChecklist.Model;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Single page, no router: Onboarding is the root component.
builder.RootComponents.Add<Onboarding>("#app");

// Singleton so answers survive re-renders.
builder.Services.AddSingleton<OnboardingState>();

await builder.Build().RunAsync();
```

### 4. Erstat `_Imports.razor`

*_Imports.razor*

```
@using Microsoft.AspNetCore.Components.Web
@using Microsoft.JSInterop
@using OnboardingChecklist
@using OnboardingChecklist.Model
@using OnboardingChecklist.Components
```

> Filen gælder for alle `.razor`-filer i projektet, så komponenterne kan referere til `OnboardingState`, `Content`, `Icons` osv. uden egne `@using`-linjer.

### 5. Kopiér datamodellen

```
cp $SRC/Model/Content.cs $SRC/Model/OnboardingState.cs Model/
```

I begge filer: skift første linje fra `namespace BlazorProto.Model;` til `namespace OnboardingChecklist.Model;`.

I `Model/OnboardingState.cs` skal de felter der kun fandtes for pickeren og de to andre varianter væk. Find blokken *picker + per-variant navigation* og slet de markerede linjer:

*Model/OnboardingState.cs*

```
-     public int ActiveVariant { get; set; }
-     public int MountCounter { get; set; }
- 
-     public int StepperIndex { get; set; }
-     public bool StepperDone { get; set; }
- 
-     public int ConvoIndex { get; set; }
-     public bool ConvoDone { get; set; }
- 
    public string ChecklistKey { get; set; } = "name";
    public bool ChecklistDone { get; set; }
```

Og nederst i `ResetFlow()`:

```
    public void ResetFlow()
    {
        Errors.Clear();
-         StepperIndex = 0; StepperDone = false;
-         ConvoIndex = 0; ConvoDone = false;
        ChecklistKey = "name"; ChecklistDone = false;
    }
```

> `Content.cs` er alt indholdet — de fire opgaver, rollelisten, notifikationsvalgene, workspace-navnet. `OnboardingState.cs` er svarene, valideringen og de afledte værdier (hvor mange er færdige, hvad mangler). Begge er ren C# uden Blazor-afhængigheder, så de flytter med uændret.

### 6. Kopiér de delte komponenter

```
cp $SRC/Components/Icons.cs $SRC/Components/Recap.razor $SRC/Components/Fields.razor Components/
```

- `Icons.cs`: skift namespace til `OnboardingChecklist.Components`.
- `Recap.razor`: ingen ændringer.
- `Fields.razor`: én ændring. Feltfokus går gennem et lille JS-kald, og funktionen skifter navn fra picker-objektet til vores eget:

*Components/Fields.razor*

```
-     public ValueTask FocusFirstAsync() => JS.InvokeVoidAsync("protoPicker.focusFirst", root);
+     public ValueTask FocusFirstAsync() => JS.InvokeVoidAsync("app.focusFirst", root);
```

> `Fields` renderer felterne for én opgave ad gangen (navn, rolle, invitationer, notifikationer) og var delt mellem Stepper og Checklist. Kommentaren øverst i filen om Stepper kan du slette — den passer ikke længere.

### 7. Kopiér checklisten som root-komponent

```
cp $SRC/Variants/Checklist.razor Onboarding.razor
```

Ingen ændringer i indholdet. Filnavnet bliver klassenavnet, så `Onboarding.razor` giver komponenten `Onboarding` — den som `Program.cs` monterer i `#app`.

### 8. Lav `wwwroot`

**JavaScript.** Én funktion, otte linjer — resten af picker.js var harness:

*wwwroot/js/app.js*

```
// Blazor's FocusAsync() needs an ElementReference per field; one helper that
// finds the first input in a container is far less ceremony.
window.app = {
  focusFirst(container) {
    if (!container) return;
    const el = container.querySelector('input:not([type=hidden]), textarea');
    (el || container).focus();
  },
};
```

**index.html.** Skriv filen sådan (boot-baren udfyldes af Blazor via `--blazor-load-percentage`, og fejl-elementet er skjult indtil der sker en rigtig fejl — begge dele styles i CSS'en):

*wwwroot/index.html*

```
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Onboarding</title>
    <base href="/" />
    <link rel="stylesheet" href="css/app.css" />
    <link rel="icon" type="image/png" href="favicon.png" />
</head>
<body>
    <div id="app">
        <div class="boot" role="status" aria-label="Loading">
            <div class="boot-bar"><span class="boot-fill"></span></div>
        </div>
    </div>

    <div id="blazor-error-ui">
        An unhandled error has occurred.
        <a href="" class="reload">Reload</a>
        <a class="dismiss">🗙</a>
    </div>

    <script src="js/app.js"></script>
    <script src="_framework/blazor.webassembly.js"></script>
</body>
</html>
```

Kopiér også `favicon.png` fra `$SRC/wwwroot/`.

**CSS.** `$SRC/wwwroot/css/app.css` er delt op i fem sektioner med tydelige bannere (`/* ==== NAVN ==== */`). Lav en ny `wwwroot/css/app.css` med disse tre dele, i denne rækkefølge:

| Tag med | Fra … til | Hvorfor |
|---|---|---|
| **MATERIALS** | Banneret `MATERIALS` til og med `@media (forced-colors: active) { … }` | Tokens, base, knapper, felter, boot-bar, fejl-bjælke |
| **Tick + recap** | Fra `.v1-tick {` til og med `.recap span { … }` (inkl. `@keyframes pop`) | Bruges på "You're all set"-skærmen |
| **VARIANT 3 — CHECKLIST** | Banneret til filens slutning | Selve checklisten |

**Udelad:** `PICKER` (øverst), resten af `VARIANT 1 — STEPPER` og hele `VARIANT 2 — CONVERSATIONAL`. Resultatet er ca. 530 linjer mod 934.

Klassenavnene `v3-*` og `v1-tick` stammer fra prototypen. De virker som de er; omdøb dem først når appen kører.

### 9. Kør den

```
dotnet run
```

Terminalen skriver `Now listening on: http://localhost:XXXX` — åbn den adresse. Du skal se:

- [ ] En tynd lilla bar i midten under load, som fylder op (ikke tekst)
- [ ] "Finish setting up" med rail til venstre (2 of 4 complete, 50 %) og panelet "What's your name?" til højre
- [ ] Klik **Save and continue** → panelet skifter til "What do you do?"
- [ ] Vælg en rolle, gem, klik **Finish setup** → "You're all set, Bart." med recap
- [ ] Ingen tekst nederst på siden, ingen scrollbar

Får du gammel visning efter en ændring: hard reload (`Ctrl`+`Shift`+`R`). Blazor cacher runtime-filerne aggressivt.

### 10. Hvis noget fejler

| Symptom | Årsag | Løsning |
|---|---|---|
| `The type or namespace name 'BlazorProto' could not be found` | Et namespace blev ikke skiftet | Søg efter `BlazorProto` i alle filer; skift til `OnboardingChecklist` |
| `'OnboardingState' does not contain a definition for 'StepperIndex'` | Du slettede et felt der stadig bruges | Du har kopieret en anden variant end `Checklist.razor` |
| Fejl i konsollen: `Could not find 'protoPicker.focusFirst'` | Trin 6, JS-navnet | Ret til `app.focusFirst` i Fields.razor |
| Appen loader men er ustylet | index.html peger på forkert CSS, eller CSS'en mangler MATERIALS | Tjek `<link href="css/app.css">` og at `:root {` findes i filen |
| "An unhandled error has occurred" nederst, men appen virker | CSS-reglen for `#blazor-error-ui` mangler | Den ligger i MATERIALS — sektion ikke kopieret helt |

## 2. Gør den til en to-sides app

Del 1 giver en app med én skærm. Herfra bliver den et rigtigt app-skelet: en sidemenu i venstre kant med to punkter — listen over sendte pristilbud, og trin-flowet der laver et nyt.

> Hvert trin herunder svarer til én commit i repoet, så du kan læse den præcise ændring med `git show <hash>` hvis noget er uklart.

### 1. Back og Next i panelets fod — `e33c0ed`

Railen lader dig åbne trinnene i vilkårlig rækkefølge. Foden går dem igennem lineært. I `Onboarding.razor` erstattes fodens indhold:

*Onboarding.razor — .v3-panel-f*

```
<div class="v3-panel-f">
    @if (Prev is { } prev)
    {
        <button type="button" class="btn btn-ghost" @onclick="() => Open(prev.Key)">@Icons.ArrowL Back</button>
    }
    <div class="v3-panel-f-end">
        @* Skip og den primære knap ankres til højre, så primæren aldrig flytter sig *@
    </div>
</div>
```

Og i `@code`-blokken erstattes `NextIncomplete` med lineære naboer:

```
private int Index => Content.IndexOf(S.ChecklistKey);
private TaskDef? Prev => Index > 0 ? Content.Tasks[Index - 1] : null;
private TaskDef? Next => Index < Content.Tasks.Length - 1 ? Content.Tasks[Index + 1] : null;
```

> Back er *skjult* på første trin, ikke deaktiveret. En knap der aldrig kan gøre noget hører ikke til på skærmen. På sidste trin bliver primæren "Save and finish" og kalder `Finish()`, som stadig går tilbage til et manglende krævet trin.

### 2. App-skelettet — `39cb774`

Fire nye filer og en omrokering:

```
mkdir -p Pages
git mv Onboarding.razor Pages/NewQuote.razor
```

`Model/AppState.cs` — hvilken af de to sider der vises:

*Model/AppState.cs*

```
namespace OnboardingChecklist.Model;

public enum AppPage { Sent, Quote }

public class AppState
{
    public AppPage Page { get; set; } = AppPage.Sent;
}
```

`Model/Quote.cs` holder de sendte pristilbud. Bemærk indholdet: et meget langt klientnavn, en kladde uden beløb og dato, og en afvist række — tabellen skal overleve mere end det pæne tilfælde.

*Model/Quote.cs*

```
public enum QuoteStatus { Draft, Sent, Viewed, Accepted, Declined }

public record Quote(string Ref, string Client, QuoteStatus Status, decimal Amount, string Sent)
{
    public string Label => Status.ToString();

    public static readonly Quote[] All =
    [
        new("Q-2418", "Featherstonehaugh-Villanueva International Systems", QuoteStatus.Viewed, 48_200m, "2026-09-02"),
        new("Q-2414", "Solberg Media", QuoteStatus.Draft, 0m, "—"),
        // … seks til
    ];
}
```

`Shell.razor` i projektets rod bliver den nye rodkomponent: sidemenu til venstre, én side ad gangen til højre.

> Ingen `Router`. To sider tjener ikke URL-routing hjem, og at indføre den ville betyde at hente `App.razor` og routeren tilbage — dem vi bevidst slettede i del 1, trin 1.

Endelig i `Program.cs`:

```
- builder.RootComponents.Add("#app");
+ builder.RootComponents.Add("#app");
builder.Services.AddSingleton<OnboardingState>();
+ builder.Services.AddSingleton();
```

Og `@using OnboardingChecklist.Pages` tilføjes i `_Imports.razor`.

> [!NOTE]
> **Navnekollision:** kald ikke siden `Quote.razor`. `Quote` er allerede datamodellen, og `<Quote />` ville være tvetydig. Derfor `NewQuote.razor`.

`Pages/Sent.razor` er tabellen. Sortering og filtrering er LINQ — det som en tilsvarende vanilla-prototype bruger 90 linjer JavaScript på:

```
Func<Quote, IComparable> key = sort switch
{
    "ref" => r => r.Ref,
    "client" => r => r.Client,
    "amount" => r => r.Amount,
    _ => r => r.Sent,
};

var ordered = asc ? rows.OrderBy(key) : rows.OrderByDescending(key);
// En kladde har ingen dato. "—" sorterer som tekst over enhver rigtig dato
// faldende, så udaterede rækker skubbes bagest i begge retninger.
return ordered.OrderBy(r => r.Sent == "—" ? 1 : 0).ToArray();
```

### 3. Fælles indholdsbredde — `9d5146f`

Tabellen fyldte alt hvad sidemenuen levnede: 2246 px ved et 2560 px vindue, med 1582 px mellem en rækkes reference og dens dato. Begge sider kappes nu ved samme token, så indholdets venstrekant ikke hopper når man skifter side.

*wwwroot/css/app.css*

```
:root {
  --page-w: 940px;
}

.v3-shell { max-width: var(--page-w); margin: 0 auto; }
/* border-box er global, så max-width dækker polstringen */
.page { max-width: calc(var(--page-w) + 64px); margin: 0 auto; padding: 44px 32px 64px; }
```

Tabellen får faste kolonner, så klientnavnet afkortes ved kolonnens kant i stedet for ved et vilkårligt `ch`-loft med tom plads ved siden af:

```
.t { table-layout: fixed; min-width: 660px; }
.t th:nth-child(1) { width: 118px; }   /* Reference */
.t th:nth-child(3) { width: 116px; }   /* Status */
.t th:nth-child(4) { width: 116px; }   /* Amount */
.t th:nth-child(5) { width: 128px; }   /* Sent */
- .t-client { max-width: 34ch; }
+ .t-client { max-width: 100%; }
```

### 4. Trinnene ind i sidemenuen — `11f15d9`

Da skallen gav siden en rigtig sidemenu, stod der pludselig to lodrette navigationssøjler 8 px fra hinanden: app-navigationen, og rail-kortet der efterlignede den. Railen flytter ind som indrykkede trin under sit eget menupunkt.

Opret `Components/StepRail.razor` med trinnene, og render den i `Shell.razor`:

*Shell.razor*

```
@if (App.Page == AppPage.Quote && !S.ChecklistDone)
{
    <StepRail />
}
```

Trinnene og felterne er nu ikke længere forælder og barn, så de kan ikke gentegne hinanden med `StateHasChanged`. Navigationen flytter op på den tilstand de deler:

*Model/OnboardingState.cs*

```
public event Action? Changed;
public void NotifyChanged() => Changed?.Invoke();

public string? Nudge { get; set; }
public bool FocusNext { get; set; }

public void Open(string key) { ChecklistKey = key; Errors.Clear(); FocusNext = true; NotifyChanged(); }
public void Finish() { /* går til første manglende krævede trin, ellers ChecklistDone */ }
```

Begge komponenter abonnerer:

```
@implements IDisposable

protected override void OnInitialized() => S.Changed += StateHasChanged;
public void Dispose() => S.Changed -= StateHasChanged;
```

> [!WARNING]
> **Fælden:** `Fields` sender sin `Changed` videre til forælderen. Lader du den stå som `Changed="StateHasChanged"`, gentegnes kun panelet, og sidemenuens måler bliver hængende på det gamle tal. Den skal være `Changed="S.NotifyChanged"`.

Med railen væk fra indholdet kapper panelet sig selv, i stedet for at strække et navnefelt ud over 900 px:

```
.v3-single .v3-panel { max-width: 620px; }
```

### 5. Tilgængelighedsgulvet — `91f3bba`

To målte fejl, begge blokerende.

**Kontrast.** `--ink-3` var `#a1a1aa` — **2,56:1** på hvid, langt under gulvet på 4,5:1 for brødtekst. Tokenet bruges udelukkende som tekstfarve, 19 steder, så det rettes ét sted:

```
- --ink-3: #a1a1aa;
+ --ink-3: #67676f;
```

| Baggrund | Før | Efter |
|---|---|---|
| hvid | 2,56:1 | 5,61:1 |
| `--accent-soft` | 2,56:1 | 5,01:1 |
| pillens `#f0f0f1` | 2,56:1 | 4,92:1 |

> Prisen er at den neutrale skala trykkes sammen mod `--ink-2` (7,7:1). Det gamle spring var købt ved at falde under gulvet.

**Tap targets.** Gulvet er 44×44 px. Sidemenu, sorteringshoveder og felter havde plads til at vokse; knapperne beholder deres udseende og får gulvet gennem et hit-areal:

```
.side-nav-item { min-height: 44px; }
.f-input       { min-height: 44px; }
.t-th          { min-height: 44px; }

/* 38px er rigtigt for en desktop-knap; 38px hit-areal er ikke. */
.btn { position: relative; }
.btn::after {
  content: "";
  position: absolute;
  left: 0; right: 0; top: 50%;
  height: 100%;
  min-height: 44px;
  transform: translateY(-50%);
}
```

### 6. Bevægelse, navn og dødt CSS — `c78be7d`

- **Varigheder.** Målerens fyld 480 ms → **260 ms**, nudge 480 ms → **300 ms**. Gulvet er under 300 ms medmindre afstand eller kurve retfærdiggør mere.
- **Reduced motion.** Blokken var et fladt `0.01ms` på alt, hvilket også dræber de opacity- og farveovergange man skal *beholde*. Nu falder bevægelsen væk og fades bliver.
- **Gruppenavn.** Trinlisten mistede sit navn da den forlod sin `<aside aria-label="Setup checklist">`. Den er nu `role="group" aria-label="Quote steps"`.
- **Dødt CSS.** Flytningen efterlod 11 forældreløse klasser — `.v3-rail`, `.v3-grid`, `.v3-task` og resten, 43 linjer.

```
@media (prefers-reduced-motion: reduce) {
  *, *::before, *::after {
    animation-duration: 0.01ms !important;
    animation-iteration-count: 1 !important;
    transition-property: opacity, color, background-color, border-color, box-shadow !important;
    transition-duration: 120ms !important;
  }
  .side-step[data-nudge] { animation: none !important; }
  .v3-meter-bar i { transition: none !important; }
}
```

### 7. Progressen på kortet — `4557547`, `3bcebb7`

Sidemenuen sagde den samme kendsgerning fire gange i én 224 px kolonne: to af fire udfyldte prikker, teksten "2 of 4 done", tallet "50%", og baren. Det numeriske resumé flytter ud; prikkerne bliver og bærer trin-for-trin-tilstanden.

Første forsøg lagde det øverst til højre i headeren. Det var forkert — det flugtede med ingenting. Det ender direkte over kortet, i kortets egen bredde:

*Pages/NewQuote.razor — inde i .v3-single, før panelet*

```
<div class="v3-progress">
    <div class="v3-progress-top">
        <span class="v3-progress-n">@S.DoneCount of @Content.Tasks.Length done</span>
        <span class="v3-progress-pct">@Pct%</span>
    </div>
    <div class="v3-meter-bar" role="progressbar" aria-label="Setup progress"
         aria-valuemin="0" aria-valuemax="@Content.Tasks.Length" aria-valuenow="@S.DoneCount">
        <i style="transform:scaleX(@Fraction)"></i>
    </div>
</div>
```

```
.v3-progress { max-width: 620px; margin-bottom: 14px; }
```

> Den ligger *uden for* panelet, ikke inde i det. Panelet er nøglet på det aktive trin og kører sin entré-animation forfra ved hvert skift — indeni ville en stabil størrelse glide ind på ny hver gang du klikker et trin.

**Finish setup** var samtidig det eneste indrammede felt i en søjle af rammeløse rækker. Den deler nu én erklæring med menupunkterne:

```
.side-nav-item,
.side-finish-btn {
  display: flex;
  align-items: center;
  gap: 9px;
  width: 100%;
  min-height: 44px;
  padding: 8px 10px;
  border: 0;
  border-radius: 8px;
  background: transparent;
  /* … */
}
```

> Den beholder sin lave vægt. Panelets Save-knap er den eneste fyldte primærknap på skærmen — én primær handling pr. skærmbillede.

### 8. Kør den

```
dotnet run
```

- [ ] Sidemenu i venstre kant med **Sent quotes** (med tallet 8) og **New quote**
- [ ] Sent quotes: tabel med fem sorterbare kolonner, søgning, og en tom tilstand med "Clear search"
- [ ] New quote: trinnene indrykket under menupunktet, progressbar over kortet i kortets bredde
- [ ] Klik et trin i menuen → panelet skifter; klik **Back** i panelet → menuen følger med
- [ ] Vælg en rolle → måleren går fra 2 of 4 til 3 of 4 *med det samme*
- [ ] **Finish setup** med manglende rolle → springer til "What do you do?" med rød besked

> [!WARNING]
> **Ser du gammel styling?** Blazors dev-server sender kun `ETag` og `Last-Modified` på `app.css` — ingen `Cache-Control`. Browseren genbruger sin kopi uden at spørge. Hard reload med `Ctrl`+`Shift`+`R`, eller slå **Disable cache** til i DevTools' Network-fane.

## 3. Listen som master-detail

Sent quotes bliver en delt visning: tabellen løber helt ud til sidemenuen, og ruden til højre viser den mail der faktisk blev sendt — modtager, emne, den vedhæftede `.xlsx`, og priserne inde i den.

### 1. Mailen ved siden af listen — `63fd3ea`

Først skal datamodellen kunne bære det. En pris uden sine linjer kan ikke vises:

*Model/Quote.cs*

```
public record QuoteLine(string Description, int Qty, decimal Unit)
{
    public decimal Total => Qty * Unit;
}

public record Quote(string Ref, string Client, string To, string Subject,
                    QuoteStatus Status, string Sent, QuoteLine[] Lines)
{
    /// Udledt, aldrig gemt — en total der kan komme ud af trit med sine linjer
    /// er en fejl der venter på at ske.
    public decimal Amount => Lines.Sum(l => l.Total);

    public string Attachment => $"priser-{Ref}.xlsx";
}
```

> Bemærk at `Amount` ikke længere er et felt. Tallene i listen ændrer sig derfor: `Q-2416` går fra 940 til 9.400, fordi den er otte konsulenttimer à 1.175. Det er linjerne der har ret.

Layoutet fylder skærmen i stedet for at kappe ved `--page-w`:

*wwwroot/css/app.css*

```
.split-page { height: 100vh; display: flex; flex-direction: column; }
.split { --detail-w: 400px; flex: 1; display: flex; min-height: 0; }
.split-list { flex: 1 1 0; min-width: 0; display: flex; flex-direction: column; min-height: 0; }
.split-scroll { flex: 1; min-height: 0; overflow: auto; }
.split-detail { flex: 0 0 var(--detail-w); overflow-y: auto; border-left: 1px solid var(--line); }
```

> [!NOTE]
> **`min-height: 0` to steder.** Uden den kan en flex-boks ikke krympe under sit indhold, og så scroller hele siden i stedet for ruderne hver for sig.

> Det er en bevidst afvigelse: New quote beholder sin kappede, centrerede bredde, mens denne side fylder alt. En delt visning *er* en anden layoutklasse. De to sider flugter ikke længere.

Selve valget er én variabel — det er hele forskellen på Blazor og prototypens 90 linjer JavaScript:

*Pages/Sent.razor*

```
<tr @key="row.Ref" tabindex="0"
    data-picked="@(picked == row.Ref ? "" : null)"
    aria-selected="@(picked == row.Ref ? "true" : "false")"
    @onclick="() => picked = row.Ref"
    @onkeydown="e => RowKey(e, row.Ref)">
```

> [!WARNING]
> **En bar `<tr onclick>` kan hverken nås med tastatur eller annonceres.** Rækkerne skal have `tabindex`, `aria-selected` og reagere på Enter og mellemrum, ellers er listen kun brugbar med mus.

Og filtrerer man det valgte væk, skal markeringen ryddes — ellers viser ruden en mail listen ikke længere tilbyder:

```
private void KeepSelectionVisible()
{
    if (picked is not null && !Rows.Any(r => r.Ref == picked)) picked = null;
}
```

### 2. Stregen der kan trækkes — `71ec6ef`

Grebet er 11 px bredt, men den synlige streg er 1 px og lander præcis på kanten mellem ruderne — `margin: 0 -5px` gør forskellen:

```
.splitter {
  flex: none; position: relative; z-index: 1;
  width: 11px; margin: 0 -5px;
  border: 0; background: transparent;
  cursor: col-resize; touch-action: none;
}
.splitter::before {
  content: ""; position: absolute; top: 0; bottom: 0; left: 50%;
  width: 1px; transform: translateX(-50%);
  background: var(--line);
  transition: background-color 150ms ease;
}
.splitter:hover::before,
.splitter[data-dragging]::before { background: var(--accent); }
```

> Trækket ligger i `app.js`, ikke i Blazor. En `pointermove` gennem Blazor ville betyde en gentegning pr. billede for noget der kun er en layoutændring. JS skriver `--detail-w`, og CSS gør resten.

*wwwroot/js/app.js*

```
const MIN = 280;        // detaljeruden aldrig smallere
const LIST_MIN = 660;   // svarer til .t's min-width, så tabellen aldrig klippes

const write = w => {
  const c = Math.round(clamp(w));
  split.style.setProperty('--detail-w', c + 'px');
  handle.setAttribute('aria-valuenow', String(c));
  try { localStorage.setItem(KEY, String(c)); } catch { /* private mode */ }
};
```

> [!NOTE]
> **Bind grænsen til noget virkeligt.** Først satte jeg listens minimum til 420 px — et tal jeg fandt på. Trak man langt nok, blev "Sent"-kolonnen klippet af midt i datoen. Bundet til tabellens egen `min-width` kan det ikke ske.

Piletaster flytter 16 px, Home og End går til yderpunkterne, og elementet er `role="separator"` med `aria-valuenow`. At trække med mus er ikke en mulighed for alle.

### 3. Cachen der lignede ødelagt kode — `ba070d7`

Dev-serveren sender hverken `Cache-Control` på `css/app.css` eller `js/app.js`. Browseren genbruger derfor sine gamle kopier efter en genopbygning. Det kostede tre sessioner:

- Gammel CSS ligner et ødelagt layout — man leder efter fejlen i regler der er i orden.
- Gammel JS *styrter appen*: den cachede `app.js` havde ingen `app.splitter`, så interop-kaldet kastede, og Blazor viste sin fejlbjælke på et sundt build.

*wwwroot/index.html — i <head>*

```
<script>
    (function () {
        var v = Date.now();

        var css = document.createElement('link');
        css.rel = 'stylesheet';
        css.href = 'css/app.css?v=' + v;
        document.head.appendChild(css);

        var js = document.createElement('script');
        js.src = 'js/app.js?v=' + v;
        js.async = false;
        document.head.appendChild(js);
    })();
</script>
```

Læg boot-skærmens CSS inline i samme `<head>`, ellers når det dynamisk indsatte stylesheet at vise et ustylet glimt.

> Prisen er at de to filer aldrig caches — 20 KB pr. indlæsning. Det er intet mod at fejlsøge et layout der i virkeligheden var i orden.

Til sidst: lad ikke en forbedring tage siden med sig, når den fejler.

*Pages/Sent.razor*

```
try
{
    await JS.InvokeVoidAsync("app.splitter.attach", handle, split);
}
catch (JSException)
{
    // En træk-bar streg er en forbedring — uden den er ruden bare fastbredde.
    // At miste hele siden over den er værre.
}
```

### 4. Kør den

- [ ] Tabellen starter præcis hvor sidemenuen slutter, og detaljeruden går ud til skærmkanten
- [ ] Siden scroller ikke selv — listen og detaljeruden scroller hver for sig, og tabelhovedet bliver stående
- [ ] Klik en række → modtager, emne, `.xlsx` og priserne med sum
- [ ] `Q-2414` er en kladde: ingen vedhæftning, ingen linjer, og den siger det
- [ ] Træk stregen — bredden huskes efter reload
- [ ] Søg "vela" med en anden række valgt → ruden går tom i stedet for at lyve

## 4. Fra to sider til ét værktøj

Indtil her er de to sider naboer. Nu bliver de ét flow: trinnene laver et tilbud, tilbuddet lander i listen, og man skriver ved siden af en levende forhåndsvisning af mailen.

### 1. Søgningen finder sin plads — `ef0be59`, `79b4bbe`, `332b530`

Tre commits på samme problem, fordi de to første kun flyttede symptomet.

**Den stod over den forkerte rude.** I et fuldbredde-sidehoved bliver et højrestillet felt skubbet ud over *detaljeruden* — den filtrerede listen til venstre fra en position over den til højre:

| Vindue | Søgefelt | Detaljerude starter |
|---|---|---|
| 1440px | 1156–1416px | 1040px |
| 2000px | 1716–1976px | 1600px |

Den flyttede ind i listeruden, med venstrekanten præcis på Reference-kolonnens tekst (262 px).

**Den manglede lup og en vej ud.** Uden lup var det eneste signal en pladsholder, der forsvinder når man skriver. Og havde man søgt sig ned til ét resultat, var markér-alt-og-slet den eneste udvej — `Clear search` findes kun i den *tomme* tilstand.

> [!NOTE]
> **Ryd-knappen skal give fokus tilbage.** Den forsvinder sammen med den tekst der kaldte den frem, så uden en `FocusAsync()` lander fokus på `<body>` og næste Tab starter forfra.

**Til sidst foldede den sig sammen.** Et helt bånd brugt på et tomt felt tjener ingenting:

*Pages/Sent.razor*

```
@if (searchOpen || query.Length > 0)
{
    <div class="search">…</div>
}
else
{
    <button class="search-open" aria-label="Søg i tilbud" @ref="searchOpener"
            @onclick="OpenSearch">@Icons.Search</button>
}
```

> Den folder sig *aldrig* sammen mens der er en søgning i gang. En filtreret liste uden synlig grund til at være kort er værre end et felt der fylder. Escape rydder og folder sammen i én bevægelse — sikkert netop fordi rydningen også fjerner filtret. Ikon-knappen har samme 44 px kasse som feltet, så båndet er 69 px højt i begge tilstande.

### 2. Flowet laver et tilbud — `601abbd`

De to sider kunne ikke forbindes, fordi flowet ikke lavede noget at forbinde. Trinnene spurgte stadig om navn, rolle, invitationer og notifikationer. De fire trin bliver de fire felter et tilbud består af — som `Quote`-recorden allerede havde:

*Model/Content.cs*

```
new("client", "Kunde",      true,  "Hvem er tilbuddet til?", …),
new("mail",   "Modtager",   true,  "Hvor skal det sendes hen?", …),
new("lines",  "Prislinjer", true,  "Hvad koster det?", …),
new("note",   "Besked",     false, "Vil du skrive noget med?", …),
```

Forbindelsen er én delt liste. Begge sider læser den, flowet skriver til den:

*Model/QuoteStore.cs*

```
public class QuoteStore
{
    private readonly List<Quote> quotes = [.. Quote.Seed];
    public IReadOnlyList<Quote> All => quotes;
    public event Action? Changed;

    public Quote Send(QuoteDraft draft)
    {
        var quote = draft.ToQuote(NextRef(), DateTime.Now.ToString("yyyy-MM-dd"));
        Add(quote);
        draft.Reset();
        return quote;
    }
}
```

*Components/StepRail.razor — Send()*

```
if (!S.ReadyToSend()) return;

var quote = Store.Send(S);
App.SelectOnArrival = quote.Ref;
App.Page = AppPage.Sent;
S.NotifyChanged();
```

> Afsendelse fører hen til listen med den nye række allerede åben. Derfor er "You're all set"-skærmen væk: når kvitteringen er at *se* tilbuddet, er en skærm der påstår det overflødig. `Recap.razor` gik med.

> [!NOTE]
> **`OnboardingState` hedder nu `QuoteDraft`.** Det gamle navn beskrev den app det her voksede ud af, ikke den det blev — og hver linje der rørte den blev alligevel skrevet om.

### 3. Beskeden der forsvandt — `94a8a0a`

Spørgsmålet var hvor vedhæftningen skulle ligge. Svaret var at mailen ingen krop havde at ligge under:

```
Quote-recorden:            Ref, Client, To, Subject, Status, Sent, Lines
ToQuote sender med:        (ikke Note)
Sent.razor nævner Note:    0 gange
```

> [!WARNING]
> **Et trin der stiltiende kasserer det man skriver er værre end et trin der ikke findes.** "Besked" samlede tekst op i udkastet, og den forsvandt i det øjeblik man trykkede send.

Med `Note` på recorden bliver rækkefølgen den man læser en mail i: hoved → emne → modtager → besked → vedhæftning → priser. `white-space: pre-wrap`, så skribentens linjeskift overlever. En mail sendt uden besked siger det i dæmpet kursiv; en kladde viser ingen krop, fordi den aldrig blev sendt.

### 4. Skriv ved siden af mailen — `2b1cd39`

Formularen sad som et 620 px kort i en ellers tom side. Den deler nu den delte skal med listesiden: formular til venstre, mailen som den bliver til højre.

*Pages/NewQuote.razor*

```
<aside class="split-detail" aria-label="Sådan bliver mailen">
    <MailView Q="S.Preview(Store.NextRef())" Preview="true" />
</aside>
```

> Det er *den samme* `MailView` som listen bruger, trukket ud af `Sent.razor` frem for skrevet to gange. Man komponerer i den visning man får bagefter, og to kopier ville drive fra hinanden.

- Tomme felter siger hvad der mangler — "Ingen modtager endnu" — i stedet for at efterlade huller der ligner noget der ikke blev tegnet.
- Pillen siger **Udkast**. En forhåndsvisning der påstår *Sent* på en mail ingen har sendt er det ene den aldrig må gøre.
- Stregen husker sin bredde pr. side, så en bred forhåndsvisning under skrivning ikke gør listen smal bagefter — 566 px mod 400 px hen over et sideskift.

*wwwroot/js/app.js*

```
attach(handle, split, key) {
  const KEY = key || 'sent.detailWidth';   // hver side sin nøgle
```

### 5. Bredden, og en fod der holder formen — `607e39c`, `aef95c4`

Kortet stod på 620 px i en 951 px rude. Det fylder nu ruden — men felterne følger ikke med:

```
.v3-panel .f-group .f-input    { max-width: 460px; }
.v3-panel .f-group .f-textarea { max-width: 620px; }
```

> Et firmanavn tastet ind i en 850 px boks er en dårligere formular end et kort med luft til overs. Trinnet der faktisk havde brug for bredden er prislinjerne: rækken bruger 822 px til fire kolonner og en fjern-knap.

> [!NOTE]
> **Specificitet:** `.f-textarea`-reglen skal ligge på samme dybde som `.f-group .f-input` ovenover, ellers taber den og tekstfeltet bliver hængende på 460 px.

Til sidst går formularen helt ud til begge sider — og det afslørede foden. Den skiftede form tre gange:

```
1: [                        Gem og fortsæt]
2: [Tilbage                 Gem og fortsæt]
4: [Tilbage   Spring over   Send tilbud   ]
```

> [!WARNING]
> **En knap der ikke er der, siger ingenting.** *Tilbage* var skjult på første trin, med den begrundelse at en kontrol der aldrig kan handle ikke hører til på skærmen. Her var det forkert: man kan ikke se forskel på "du er ved begyndelsen" og "knappen blev ikke tegnet".

Den bliver stående og bliver slørret — ægte `disabled`, så den ikke kan tabbes til, og farvet med et token frem for opacity, som klarer kontrastkravet på én baggrund og falder på en anden. Et **Trin x af 4** fylder midten foden lod stå tom; det er ikke fremdriftsbjælken gentaget, for den tæller hvad der er *udfyldt*, ikke hvor man *står*.

### 6. Kør den

- [ ] Søgeikonet folder sig ud til feltet og tilbage igen med Escape — men aldrig mens der er søgt
- [ ] Udfyld de fire trin: forhåndsvisningen til højre bygger sig op mens du skriver
- [ ] *Send tilbud* → du lander på Sent quotes med den nye række valgt, og tallet i menuen tæller op
- [ ] Beskeden står i detaljeruden med sine linjeskift, over vedhæftningen
- [ ] Foden har samme venstrekant på alle fire trin; *Tilbage* er slørret på det første

> [!WARNING]
> **Brug ikke `pkill -f "blazor-devserver"` til at stoppe serveren.** Mønsteret matcher sin egen kommandolinje og slår processen ihjel med kode 144. Stop på porten i stedet: `lsof -ti:5210 -sTCP:LISTEN | xargs -r kill`

## 5. En række er en udsendelse

> [!WARNING]
> **Denne del erstatter datamodellen fra del 2–4.** Følger du guiden forfra for at genskabe appen som den er nu, bygger du prislinje-editoren i del 4 og sletter den her. Vil du springe over, så byg del 1, og hop derefter til appendiks B som viser sluttilstanden direkte.

### 1. Jagten på et hop — `71e1056`, `97e27c7`, `c6058a3`

Tre commits på ét symptom: den lodrette streg mellem ruderne så ud til at hoppe ved hvert trinskift. To af dem var forkerte. De står her, fordi fejlslutningerne er en del af oplysningen.

**Første gæt: scrollbaren.** Trinnene har forskellig højde, så en klassisk scrollbar dukker op og forsvinder ved siden af stregen. `scrollbar-gutter: stable` på de rullende ruder reserverer pladsen. Det er en rigtig rettelse — bare ikke på det her.

> [!NOTE]
> **Headless Chromium skjuler den slags.** Den bruger overlay-scrollbars med bredde 0. Vil du reproducere hvad Edge på Windows viser, så start browseren med `--disable-features=OverlayScrollbar`.

**Andet gæt: procenttallet.** Ved at måle hvert eneste elements kasse før og efter et trinskift dukkede det op:

```
.v3-progress-pct   venstre [958, 951]   højre [975, 975]
```

Højrestillet tekst vokser mod venstre, så `0%` → `25%` → `100%` flytter sin egen venstrekant. `tabular-nums` gør cifrene lige brede; det der manglede var plads til at *antallet* af cifre ændrer sig:

```
.v3-progress-pct { min-width: 4ch; text-align: right; font-variant-numeric: tabular-nums; }
```

> Også en rigtig rettelse, og værd at beholde. Stadig ikke årsagen.

**Årsagen: strukturen.** Svaret lå i spørgsmålet "hvorfor gør vi ikke bare som den første side?"

```
Sent quotes:  .list-bar     | .split-scroll ← scroller | .t-foot
New quote:    .v3-progress  | .v3-panel     ← scroller | .v3-panel-f ← sticky INDE i den
```

> [!WARNING]
> **En sticky fod inde i en scrollende container tegnes om mod scrollbaren.** Med `margin: 0 -24px` rækker den oven i købet ud over scrollbarens plads. Listesiden hakkede aldrig, fordi dens fod altid har ligget udenfor.

Foden bliver et søskende-bånd, og kun trinnets indhold scroller:

*Pages/NewMail.razor*

```
<div class="v3-scroll">
    <section class="v3-panel" @key="S.StepKey">…</section>
</div>

<div class="v3-panel-f">…</div>
```

```
.v3-scroll        { flex: 1; min-height: 0; overflow-y: auto; scrollbar-gutter: stable; }
.split-form .v3-panel-f { flex: none; border-top: 1px solid var(--line); }
```

> Foden ligger nu også uden for den nøglede sektion, så den ikke kører entré-animation om ved hvert trinskift.

### 2. Modellen vendes om — `296606d`

En række er ikke én mail. Den er én udsendelse til en hel marketinggruppe — mennesker fra forskellige firmaer — og **hver kunde får sin egen prisliste**.

```
Mail (én udsendelse)
  reference, emne, besked, dato, status
  Recipients[]
    person + firma + status
    QuoteLine[]        ← kundens ark
```

**Priserne laves i regnskabssystemet**, ikke her. Prislinje-editoren slettes: et trin der lod dig taste priser skrev på noget appen ikke ejer. Trin 1 bliver marketinggruppen, og vælger du en person, trækker hun sit firmas ark med sig.

*Model/Accounting.cs*

```
/// Stands in for the accounting system. One class to swap for a call.
public static class Accounting
{
    private static readonly Dictionary<string, QuoteLine[]> Sheets = new()
    {
        ["Vela Robotics"] = [new("Servicebesøg", 12, 850m), …],
    };

    public static QuoteLine[] SheetFor(string company) =>
        Sheets.TryGetValue(company, out var lines) ? lines : [];
}
```

> [!NOTE]
> **Arkene kopieres ned på mailen når den sendes.** Det der blev sendt forbliver det der blev sendt, også når regnskabssystemet ændrer priser bagefter. `Recipient` bærer sine egne `Lines` — den slår ikke op.

**Status flytter ned på modtageren.** Fire mennesker svarer fire forskellige ting, så én status på udsendelsen ville lyve. Rækken viser sammendraget, detaljen viser hvem der gjorde hvad:

*Model/Mail.cs*

```
public string Responses
{
    get
    {
        if (Status == MailStatus.Draft) return "Ikke sendt";
        // "2 set · 1 accepteret", ellers "Ingen svar endnu"
    }
}
```

> En kladde siger *"Ikke sendt"*, ikke *"Ingen svar endnu"* — den venter ikke på noget, den har ikke spurgt nogen.

Trinnene bliver tre: **Modtagere**, **Emne**, **Besked** (valgfri). Og navnene skifter: `Quote` → `Mail`, *Sent quotes* → **Mails**, *New quote* → **Ny mail**.

> [!WARNING]
> **Ikke "Inbox".** En indbakke er det man modtager. Det her er det du har sendt.

### 3. Tre faner i detaljeruden — `71e1056`, `3f869ec`

Mailen, modtagerne og det enkelte ark er tre ting. Først blev vedhæftningen skilt fra mailen; siden fik den enkelte modtagers ark sin egen fane, fordi det lå klemt under den række det hørte til.

```
[ Mail ]  [ Modtagere ④ ]  [ Vedhæftning ]
```

- Klik på en modtager åbner *hendes* ark i tredje fane — chevronen peger ind i det i stedet for at rotere, så rækken læses som en vej videre.
- Pilene i arket træder mellem modtagerne med positionen imellem (`1 / 4`), slørret i hver ende.
- Fanen starter på den første modtager, så den aldrig står tom når der er noget at vise.
- Piletaster går gennem alle tre faner; hver fane har 44 px hit-areal gennem et pseudo-element.

### 4. Hvad review'et fangede — `c043afe`

| Fund | Årsag | Rettelse |
|---|---|---|
| Beløbet stod 49 px fra kanten i udkast, 28 px når sendt | Pillen blev udeladt i preview, men gitteret havde stadig fem spor | Reservér sporet med et tomt element frem for at fjerne barnet |
| Kladden sagde "Ingen svar endnu" | Antyder at den gik ud og ingen svarede | "Ikke sendt" |
| Checkboksen var 11×13 px i browserens blå | Det eneste kontrolelement uden projektets farver | `16px` og `accent-color: var(--accent)` |
| `priser-halden--co.xlsx` | "Halden & Co." har tre ikke-bogstaver i træk; ét `Replace("--","-")` fjerner kun det første par | Kollaps hele løbet med `-{2,}` |

### 5. Kør den

- [ ] Listen viser udsendelser: `M-2418 · 4 · 4 kunder · 82.200` med "2 set · 1 accepteret" under emnet
- [ ] Fanen **Modtagere** viser de fire med hver sin status og sit beløb
- [ ] Klik en modtager → **Vedhæftning** åbner med netop hendes ark, og pilene bladrer
- [ ] Under **Ny mail**: vælg to personer hos samme kunde — de får det samme ark
- [ ] Send → du lander på listen med udsendelsen valgt, og tallet i menuen tæller op
- [ ] Skift trin: intet flytter sig vandret

## 6. Publish og host

`dotnet run` er et Debug-build: 193 separate `.wasm`-filer, 8,5 MB, ingen trimming. Det er fint til udvikling og forkert til alt andet. En Release-publish trimmer klassebiblioteket ned til det der faktisk bruges og komprimerer det.

### 1. Publicér

```
dotnet publish -c Release -o publish
```

Tager 10–20 sekunder. Det der skal på nettet ligger i **`publish/wwwroot/`** — alt andet i `publish/` kan ignoreres.

|  | Filer | Requests | Overført | Load (localhost) |
|---|---|---|---|---|
| Debug (`dotnet run`) | 193 wasm | 202 | 8,5 MB | 1,1 s |
| Release publish, brotli | 30 wasm | 38 | 1,8 MB | 0,5 s |

Målt på prototypen med de to csproj-flag fra trin 2. Ved 4 Mbit tager Debug-versionen 19 s og publish-versionen ca. 4 s — forskellen er størst for dem der ikke sidder på din maskine.

Ved siden af hver `.wasm` ligger en `.br` (brotli) og en `.gz`. En server der kender dem, sender den komprimerede udgave og sparer 70 %. En server der ikke gør, sender `.wasm` ukomprimeret (ca. 5,5 MB) — det virker stadig.

### 2. Test det lokalt

```
python3 -m http.server 8080 -d publish/wwwroot
```

Åbn `http://localhost:8080`. Python-serveren sender ukomprimeret, men det er 38 requests i stedet for 202, så det er stadig markant hurtigere end `dotnet run`. Stop med `Ctrl`+`C`.

### 3. Læg det online

**Netlify Drop** — hurtigst når du bare vil vise det til nogen:

- Gå til `app.netlify.com/drop`
- Træk mappen `publish/wwwroot` ind i browservinduet
- Du får en URL med det samme. Netlify komprimerer selv, så du får brotli-hastigheden uden opsætning.

**GitHub Pages** — hvis det skal ligge ved siden af koden:

> [!WARNING]
> **Bemærk:** GitHub Pages er kun tilgængeligt for private repos med GitHub Pro. Med en gratis konto skal repoet være offentligt.

- Lav en tom fil `publish/wwwroot/.nojekyll`. Uden den ignorerer Pages mappen `_framework/`, fordi den starter med underscore.
- Sitet bliver serveret fra `https://<bruger>.github.io/<repo>/`, så ret `<base href="/" />` i `publish/wwwroot/index.html` til `<base href="/<repo>/" />`.
- Push indholdet af `publish/wwwroot` til en `gh-pages`-branch (eller en `docs/`-mappe på main), og peg repoets *Settings → Pages* på den.

GitHub Pages sender ikke `.br`-filerne selv, så load er ~5,5 MB ukomprimeret. Stadig 38 requests og trimmet — helt brugbart.

### 4. Valgfrit: mindre runtime

```
sudo dotnet workload install wasm-tools
```

Med denne workload relinker publish selve .NET-runtimen (`dotnet.native.wasm`, den største enkelte fil på 2,8 MB) og fjerner det appen ikke bruger. Du behøver ikke ændre noget i projektet — næste `dotnet publish` bruger den automatisk. Kræver sudo, fordi SDK'en ligger i `/usr/lib/dotnet`.

## A. Appendiks A: filerne i det færdige projekt

```
OnboardingChecklist/
├── OnboardingChecklist.csproj   ← D1 trin 2
├── Program.cs                   ← D1 trin 3, D2 trin 2
├── _Imports.razor               ← D1 trin 4, D2 trin 2
├── Shell.razor                  ← D2 trin 2, D5 trin 2
├── Model/
│   ├── Accounting.cs            ← D5 trin 2
│   ├── AppState.cs              ← D2 trin 2
│   ├── Content.cs               ← D1 trin 5, D5 trin 2
│   ├── Mail.cs                  ← D5 trin 2
│   ├── MailDraft.cs             ← D5 trin 2
│   ├── MailStore.cs             ← D4 trin 2, D5 trin 2
│   └── MarketingGroup.cs        ← D5 trin 2
├── Pages/
│   ├── Mails.razor              ← D2–D3, D5 trin 2
│   └── NewMail.razor            ← D1 trin 7, D4–D5
├── Components/
│   ├── Fields.razor             ← D1 trin 6, D5 trin 2
│   ├── Icons.cs                 ← D1 trin 6
│   ├── MailView.razor           ← D4 trin 4, D5 trin 3
│   └── StepRail.razor           ← D2 trin 4, D5 trin 2
├── Properties/
│   └── launchSettings.json      (fra templaten)
└── wwwroot/
    ├── index.html               ← D1 trin 8, D3 trin 3
    ├── favicon.png
    ├── css/app.css              ← D1 trin 8, så D2–D5
    └── js/app.js                ← D1 trin 8, D3 trin 2, D4 trin 4
```

CSS'en har fire sektioner når du er færdig: `MATERIALS`, `VARIANT 3 — CHECKLIST`, `APP SHELL` og `SENT QUOTES — full-bleed master–detail`. `Recap.razor`, `Quote.cs` og `QuoteStore.cs` findes ikke længere — de blev afløst i del 4 og 5. Filer fra prototypen der *ikke* skal med: `Proto.razor`, `Components/Picker.razor`, `Variants/Stepper.razor`, `Variants/Conversational.razor`, `wwwroot/js/picker.js`.

## B. Appendiks B: modellen som den ser ud nu

Sluttilstanden, samlet ét sted, så den kan bygges direkte uden at spille historien igennem.

*Model/Mail.cs*

```
public enum MailStatus { Draft, Sent }
public enum RecipientStatus { Sent, Viewed, Accepted, Declined }

public record QuoteLine(string Description, int Qty, decimal Unit)
{
    public decimal Total => Qty * Unit;
}

public partial record Recipient(string Name, string Email, string Company,
                               QuoteLine[] Lines, RecipientStatus Status)
{
    public decimal Total => Lines.Sum(l => l.Total);
    public string Attachment => $"priser-{Slug(Company)}.xlsx";
    public string Initials => …;
}

public record Mail(string Ref, string Subject, string Note,
                   MailStatus Status, string Sent, Recipient[] Recipients)
{
    public decimal Total => Recipients.Sum(r => r.Total);
    public int Companies => Recipients.Select(r => r.Company).Distinct().Count();
    public string Responses => …;   // "Ikke sendt" | "2 set · 1 accepteret"
}
```

| Fil | Ansvar |
|---|---|
| `Model/Mail.cs` | Udsendelsen, modtageren og linjen |
| `Model/Accounting.cs` | Stedfortræder for regnskabssystemet — firma → prisliste |
| `Model/MarketingGroup.cs` | De personer en mail kan gå til |
| `Model/MailStore.cs` | Den ene liste begge sider læser; `Send()` lægger udkastet i den |
| `Model/MailDraft.cs` | Mailen der skrives — valgte modtagere, emne, besked, trin-navigation |
| `Model/AppState.cs` | Hvilken side der vises, og hvilken række listen skal åbne på |
| `Model/Content.cs` | De tre trin |
| `Components/MailView.razor` | Mail / Modtagere / Vedhæftning — delt af begge sider |
| `Components/Fields.razor` | Ét trins felter: modtager-vælger, emne, besked |
| `Components/StepRail.razor` | Trinnene i sidemenuen, og Send-knappen |
| `Pages/Mails.razor` | Listen over udsendelser, delt visning |
| `Pages/NewMail.razor` | Flowet, med levende forhåndsvisning |

> De tre singletons i `Program.cs` er `MailDraft`, `AppState` og `MailStore`. Alt andet er komponenter uden egen tilstand ud over den fane eller række de har åben.

```
OnboardingChecklist/
├── OnboardingChecklist.csproj   ← D1 trin 2
├── Program.cs                   ← D1 trin 3, D2 trin 2
├── _Imports.razor               ← D1 trin 4, D2 trin 2
├── Shell.razor                  ← D2 trin 2, D5 trin 2
├── Model/
│   ├── Accounting.cs            ← D5 trin 2
│   ├── AppState.cs              ← D2 trin 2
│   ├── Content.cs               ← D1 trin 5, D5 trin 2
│   ├── Mail.cs                  ← D5 trin 2
│   ├── MailDraft.cs             ← D5 trin 2
│   ├── MailStore.cs             ← D4 trin 2, D5 trin 2
│   └── MarketingGroup.cs        ← D5 trin 2
├── Pages/
│   ├── Mails.razor              ← D2–D3, D5 trin 2
│   └── NewMail.razor            ← D1 trin 7, D4–D5
├── Components/
│   ├── Fields.razor             ← D1 trin 6, D5 trin 2
│   ├── Icons.cs                 ← D1 trin 6
│   ├── MailView.razor           ← D4 trin 4, D5 trin 3
│   └── StepRail.razor           ← D2 trin 4, D5 trin 2
├── Properties/
│   └── launchSettings.json      (fra templaten)
└── wwwroot/
    ├── index.html               ← D1 trin 8, D3 trin 3
    ├── favicon.png
    ├── css/app.css              ← D1 trin 8, så D2–D5
    └── js/app.js                ← D1 trin 8, D3 trin 2, D4 trin 4
```
