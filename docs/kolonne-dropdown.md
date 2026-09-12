# Kolonnens dropdown

Sortering og filter på listen over udsendelser, samlet i ét panel pr. kolonne, tegnet efter Outlooks *Af Dato*-menu.

Alt ligger i tre filer: `OnboardingChecklist/Pages/Mails.razor` (markup og tilstand), `wwwroot/css/app.css` (udseende) og `Components/Icons.cs` (de tre mærker). Der er ingen pakker involveret.

---

## Sådan opfører den sig

Du klikker på en kolonneoverskrift, og kolonnens eget panel falder ned under den:

```
┌──────────────────────────────┐
│ Sorteringsrækkefølge         │
│    Ældste øverst             │
│  ✓ Nyeste øverst             │
│ ────────────────────────────-│
│ Vis                          │   ← "Gik til" på Modtagere
│ 🔍 Søg                       │
│ ────────────────────────────-│
│ ☑ M-2418                     │
│ ☐ M-2417                     │
│ ────────────────────────────-│
│ Ryd filter                   │
└──────────────────────────────┘
```

- **Rækkefølgen står i feltets egne ord**, laveste først, som Outlook lister dem: *Ældste/Nyeste øverst* for en dato, *Mindst/Størst øverst* for en sum, *Færrest/Flest øverst* for et antal, *A til Å / Å til A* for tekst. "Stigende" og "Faldende" skal oversættes i hovedet og betyder ikke det samme for en dato som for et navn.
- **Et valg sorterer og lukker**, som i Outlook, og fokus går tilbage til overskriften.
- **Filteret er kolonnens egne værdier** med flueben — ikke en fritekstsøgning. Søgefeltet ovenover gør kun *listen* kortere; det er fluebenene, tabellen retter sig efter. Det er regnearkets model, og den fjerner gætteriet om stavemåder.
- **Overskriften bærer ét mærke**: pilen når kolonnen er sorteret, filterstregerne når den er filtreret, og ét samlet tegn når den er begge dele.

## Markup

Overskriften er selve knappen, og panelet ligger inde i `<th>`, som er `position: sticky` og dermed panelets udgangspunkt:

```razor
<th scope="col" class="@(col.Num ? "num" : null)" data-open="@(isOpen ? "" : null)"
    aria-sort="@(sorted ? (asc ? "ascending" : "descending") : "none")">
    <button type="button" class="t-th" id="th-@col.Key"
            aria-haspopup="dialog" aria-expanded="@(isOpen ? "true" : "false")"
            @onclick="() => Toggle(col.Key)">
        @if (sorted && filtered)
        {
            <span class="t-mark" role="img" aria-label="filtreret">@(asc ? Icons.SortFilterUp : Icons.SortFilterDown)</span>
        }
        else if (sorted)
        {
            <span class="t-mark t-sort" data-asc="@(asc ? "" : null)">@Icons.ArrowUp</span>
        }
        else if (filtered)
        {
            <span class="t-mark" role="img" aria-label="filtreret">@Icons.Filter</span>
        }
        else
        {
            <span class="t-mark"></span>
        }
        @col.Label
    </button>

    @if (isOpen)
    {
        <div class="t-menu" role="dialog" tabindex="-1" aria-label="Sortér og filtrér @col.Label"
             @ref="menu" @onkeydown="MenuKey">
            ...
        </div>
    }
</th>
```

Den tomme `<span class="t-mark"></span>` er ikke et uheld: pladsen skal være der, også når der intet mærke er, ellers rykker etiketten sig, hver gang kolonnen bliver sorteret.

Selve panelet er to afsnit — rækkefølgen og filteret:

```razor
<span class="t-menu-head">Sorteringsrækkefølge</span>
<button type="button" class="t-menu-item" aria-pressed="@(sorted && asc ? "true" : "false")"
        @onclick="() => SetSort(col.Key, true)">
    <span class="t-menu-ico">@if (sorted && asc) { @Icons.TickSm }</span>
    @order.Asc
</button>
```

Filterafsnittet er søgefeltet og værdilisten:

```razor
<div class="t-find">
    <span class="t-find-ico">@Icons.Search</span>
    <input class="t-find-input" id="find-@col.Key" type="search" placeholder="Søg"
           autocomplete="off" spellcheck="false" data-1p-ignore data-lpignore="true"
           value="@Find(col.Key)"
           @oninput="e => SetFind(col.Key, e.Value?.ToString() ?? string.Empty)" />
    @if (Find(col.Key).Length > 0)
    {
        <button type="button" class="t-find-x" aria-label="Ryd søgning"
                @onclick="() => ClearFind(col.Key)">@Icons.X</button>
    }
</div>
```

## Tilstanden bag

En kolonne er en post, og strengen i `Filter` er både flag og overskrift over dens værdier:

```csharp
private record Col(string Key, string Label, bool Num = false, string? Filter = null);

private static readonly Col[] Columns =
[
    new("ref", "Reference", Filter: "Vis"),
    new("subject", "Emne", Filter: "Vis"),
    new("recipients", "Modtagere", true, "Gik til"),
    new("total", "I alt", true),
    new("sent", "Sendt", true),
];
```

Filtertilstanden er to ordbøger: de valgte værdier pr. kolonne, og det ord der indsnævrer listen i dens dropdown. Ordet når aldrig ud i tabellen.

```csharp
private readonly Dictionary<string, HashSet<string>> picks = [];
private readonly Dictionary<string, string> finds = [];

private HashSet<string> Chosen(string key) =>
    picks.TryGetValue(key, out var set) ? set : picks[key] = [];

private IEnumerable<string> Values(string key) => key switch
{
    "recipients" => Store.All.SelectMany(m => m.Recipients).Select(r => r.Company),
    "subject" => Store.All.Select(m => m.Subject),
    _ => Store.All.Select(m => m.Ref),
};

private IEnumerable<string> Shown(string key)
{
    var all = Values(key).Distinct().Order();
    var word = Find(key).Trim();
    return word.Length == 0 ? all : all.Where(v => v.Contains(word, StringComparison.OrdinalIgnoreCase));
}
```

`Modtagere` filtrerer på firmaer, fordi det er dét, cellen tæller: en udsendelse rammer flere kunder, og værdien ligger inde i rækken, ikke i cellen.

Retningsordene og valget:

```csharp
private static (string Asc, string Desc) OrderLabels(string key) => key switch
{
    "sent" => ("Ældste øverst", "Nyeste øverst"),
    "total" => ("Mindst øverst", "Størst øverst"),
    "recipients" => ("Færrest øverst", "Flest øverst"),
    _ => ("A til Å", "Å til A"),
};

private void SetSort(string key, bool ascending)
{
    sort = key;
    asc = ascending;
    Close();
}
```

Og filtreringen selv er almindelig LINQ i `Rows`:

```csharp
var refs = Chosen("ref");
if (refs.Count > 0) rows = rows.Where(r => refs.Contains(r.Ref));

var companies = Chosen("recipients");
if (companies.Count > 0) rows = rows.Where(r => r.Recipients.Any(x => companies.Contains(x.Company)));
```

## Fokus

Panelet er en dialog, og fokus skal kunne findes hele vejen rundt:

```csharp
private void Toggle(string key)
{
    openCol = openCol == key ? null : key;
    focusMenu = openCol is not null;
}

private void Close()
{
    focusAfter = $"th-{openCol}";
    openCol = null;
}
```

`OnAfterRenderAsync` udfører de to ønsker via `app.focusIn(menu)` (første kontrol i panelet) og `app.focusId(id)` (tilbage til overskriften). `ClearFind` sætter `focusAfter = $"find-{key}"`, fordi ryd-knappen forsvinder i samme åndedrag som den bruges.

## CSS der bærer det

```css
:root { --radius-menu: 0; }          /* firkantet, som Outlook læses */

.t-th {                              /* overskriften er knappen */
  display: flex;
  align-items: baseline;             /* mærket står på tekstens grundlinje */
  gap: 5px;
  width: 100%;
  padding: 12px 14px;
  line-height: 16px;                 /* 16 + 24 = 40px høj */
}
@media (pointer: coarse) { .t-th { padding-block: 14px; } }   /* 44px */

.t th.num .t-th { justify-content: flex-end; }
.t-mark { display: flex; justify-content: flex-end; flex: none; width: 15px; color: var(--accent); }

.t th[data-open] { z-index: 6; }     /* over bagtæppet, som er z-index 5 */
.t th[data-open] .t-th { color: var(--ink-1); background: #f0f0f1; }

.t-menu { position: absolute; top: calc(100% + 4px); left: 0; width: 240px; padding: 6px; }
.t th.num .t-menu { left: auto; right: 0; }   /* talkolonner åbner indad */
```

Alt i panelet starter på den samme 16px-kant: overskrifterne, fluebenspladsen, lupen og checkboksene. Søgefeltet er en **række** i menuen, ikke en kasse inde i den — fuld bredde forbi menuens polstring, 40px høj, en streg under, og 13px tekst på mus mod 16px på touch, hvor iOS ellers zoomer.

## Mærkerne

Tre tegn, alle på pixelgitteret med 1px streg og i højde med de store bogstaver (9px ved 12px tekst):

| Tegn | Fil | Størrelse |
| --- | --- | --- |
| Pil | `Icons.ArrowUp` | 7 × 9 |
| Filterstreger | `Icons.Filter` | 11 × 9 (11/7/3 på 4px-trin, som Fluent) |
| Begge dele | `Icons.SortFilterUp` / `…Down` | 15 × 9 |

```csharp
public static readonly MarkupString ArrowUp = new(
    """<svg width="7" height="9" viewBox="0 0 7 9" fill="none" stroke="currentColor" stroke-width="1" aria-hidden="true"><path d="M3.5 9V1"/><path d="M.5 3.5l3-3 3 3" stroke-linecap="round" stroke-linejoin="round"/></svg>""");
```

Den flade ende står på grundlinjen, spidsen når toppen af de store bogstaver. Pladsen er altid 15px bred — det bredeste tegn — så intet flytter sig, når en kolonne skifter tilstand.

## Fem fælder, vi gik i

**Razor og anførselstegn.** `@oninput="e => SetText("ref", …)"` er en parsefejl: de indre anførselstegn lukker attributten. Brug enkelte anførselstegn om attributten, eller beregn strengen i `@code` og sæt variablen ind.

**Tabellen forsvandt, mens man skrev.** Den tomme tilstand erstattede hele tabellen, og feltet, man skrev i, sad i en overskrift — så det forsvandt midt i ordet. Tabellen bliver nu stående, og "Ingen udsendelser matcher" vises under overskrifterne.

**Escape holdt op med at virke.** Klikkede man på panelets egen baggrund, røg fokus over på `body`, og panelet lytter på sig selv. `tabindex="-1"` på dialogen fanger klikket, som en dialog skal.

**Etiketten hoppede.** Mærket efter en højrestillet etiket skubber den til siden, hver gang sorteringen lander på kolonnen. Den faste 15px-plads løser det, uanset hvilket af de tre tegn der står der.

**Fluebenspladsen ligner en indrykning.** Den er med vilje: pladsen er reserveret, så teksten ikke rykker sig, når valget flytter sig. Outlook gør præcis det samme — kun den valgte linje har et flueben.
