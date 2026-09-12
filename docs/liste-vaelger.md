# Marketinglisten

Trin 1 spørger, hvem udsendelsen går til. Svaret er én marketingliste ud af **103**, og de tre, du bruger, står allerede på skærmen.

Alt ligger i to filer: `OnboardingChecklist/Components/Fields.razor` (markup og tilstand) og `wwwroot/css/app.css` (udseende), med to små hjælpere i `wwwroot/js/app.js`. Ingen pakker.

---

## Beslutningen

Afgjort ud fra fem retninger, bygget side om side på de rigtige 103 lister (prøvestanden er revet ned igen; se [Forkastet](#forkastet)).

**Valgt: kort.** Tre kort med de mest brugte lister, og de øvrige hundrede bag én knap.

- **De tre er på skærmen, ikke bag et klik.** Hver dag rammer valget en af dem, og så skal det ikke koste en åbning af noget.
- **Tallet står på kortet** — `244,200 · brugt 2 gange`. Det er dét, der giver listen pladsen; uden det ligner rækkefølgen et tilfælde.
- **Den valgte liste er altid et af de tre kort.** Vælger du en fra den lange hale, skubber den det tredje kort ud og bliver selv det første. Ellers ville det, du lige valgte, forsvinde i samme øjeblik.
- **De hundrede andre er undtagelsen** og ser sådan ud: én stiplet knap, `Alle 103 lister`, der åbner arket.

## Sådan opfører den sig

```
┌────────────────────┐ ┌────────────────────┐ ┌────────────────────┐
│ ✓ Rammeaftale 2027 │ │ Norden             │ │ Fragtkunder        │
│ 8 personer · 7 k.  │ │ 5 personer · 4 k.  │ │ 3 personer · 3 k.  │
│ 244,200 · brugt 2× │ │ 168,000 · brugt 1× │ │ 37,400 · brugt 1×  │
└────────────────────┘ └────────────────────┘ └────────────────────┘
┌──────────────────┐
│ 🔍 Alle 103 lister│
└──────────────────┘
      ↓ åbner
┌──────────────────────────────────────────────────────────────────┐
│ 🔍 Søg i lister, personer og kunder                              │
├──────────────────────────────────────────────────────────────────┤
│ Alle lister · 103                                                │
│▌Fragt Benelux              2 personer · 2 kunder        89,000   │ ← markøren
│ Fragt Danmark nord         6 personer · 6 kunder       159,200   │
├──────────────────────────────────────────────────────────────────┤
│ ↑↓ flyt   ↵ vælg   esc luk                                       │
└──────────────────────────────────────────────────────────────────┘
```

- **Kortene er én kontrol**, ikke tre: Tab rammer gruppen, piletasterne flytter inde i den. Det er det, `role="radiogroup"` lover, og det holder tabulatorvejen gennem trinnet kort.
- **Arket åbner med markøren på første række.** Piletasterne går ned gennem alle 103, `↵` tager den, markøren står på. At nå række tres med Tab er ikke en vej nogen går, og derfor står tasterne skrevet i bunden — kun på mus og tastatur, ikke på touch.
- **Søgningen rammer også personer og kunder**, ikke kun listenavne (`MailDraft.Search`), fordi man ofte husker kunden og ikke listen.
- **Escape og klik udenfor lukker**, og fokus går tilbage til knappen, der åbnede.
- Arket har ikke sin egen "Mest brugt"-sektion. **Kortene er den sektion.**

## Markup

Kortene, med den valgte som den eneste, der kan tabbes til:

```razor
<div class="grp-cards" role="radiogroup" aria-label="Mest brugte marketinglister">
    @foreach (var (g, i) in Cards.Select((g, i) => (g, i)))
    {
        var group = g;
        var on = S.List.Name == group.Name;

        <button type="button" class="grp-card" role="radio" id="grp-card-@i"
                aria-checked="@(on ? "true" : "false")" tabindex="@(on ? 0 : -1)"
                @onclick="() => Pick(group)" @onkeydown="e => CardKey(e, i)">
            <span class="grp-card-n">
                <span class="grp-card-tick" aria-hidden="true">@if (on) { @Icons.TickSm }</span>
                <span class="grp-card-name">@group.Name</span>
            </span>
            ...
        </button>
    }
</div>
```

Fluebenets plads er reserveret (`.grp-card-tick { width: 12px }`), så navnet ikke rykker sig, når valget flytter sig.

## Tilstanden bag

Hvilke tre kort:

```csharp
/// The three on screen. The chosen list is always one of them, even when it
/// came from the long tail — otherwise picking it would make it vanish.
private IEnumerable<Group> Cards =>
    MostUsed.Any(g => g.Name == S.List.Name)
        ? MostUsed
        : new[] { S.List }.Concat(MostUsed).Take(3);
```

`MostUsed` er frekvens, ikke rækkefølge — de tre lister med flest udsendelser bag sig, uafgjort brydes af den, der blev brugt sidst. Det kræver, at en udsendelse husker sin liste (`Mail.List`).

Markøren i arket:

```csharp
private async Task ListKey(KeyboardEventArgs e)
{
    if (e.Key == "Escape") { CloseLists(); return; }

    var rows = Rows();
    if (rows.Length == 0) return;

    switch (e.Key)
    {
        case "ArrowDown": cursor = Math.Min(cursor + 1, rows.Length - 1); showCursor = true; break;
        case "ArrowUp": cursor = Math.Max(cursor - 1, 0); showCursor = true; break;
        case "Home": cursor = 0; showCursor = true; break;
        case "End": cursor = rows.Length - 1; showCursor = true; break;
        case "Enter": await Pick(rows[cursor]); break;
    }
}
```

`showCursor` bliver til ét kald efter render — `app.showRow("grp-row-at")`, som ruller **listen** og ikke siden (`scrollIntoView({ block: 'nearest' })`). Og `FindChanged` sætter markøren tilbage på nul, fordi listen bliver en anden, når man taster.

## CSS der bærer det

```css
.grp-cards { display: grid; grid-template-columns: repeat(3, minmax(0, 1fr)); gap: 10px; }
@media (max-width: 760px) { .grp-cards { grid-template-columns: 1fr; } }

.grp-card[aria-checked="true"] { border-color: var(--accent); background: var(--accent-soft); }

/* Where the arrow keys are standing — not what is chosen, which is the tick. */
.grp-row[data-cursor] { background: var(--hover); box-shadow: inset 2px 0 0 var(--accent); }

@media (pointer: coarse) { .grp-keys { display: none; } }
```

**Markøren og valget er to ting.** Fluebenet siger, hvad listen er; den lodrette streg siger, hvor tastaturet står. De må ikke se ens ud, og ingen af dem må være fed skrift — vægt flytter teksten en pixel, hver gang man trykker på en pil.

## Forkastet

| Retning | Hvorfor ikke |
| --- | --- |
| **Nu** — ét felt med et panel under, søg + 103 rækker | Ingen kendte de tre lister uden at åbne noget. Det almindelige valg kostede lige så meget som det sjældne. |
| **Skriv** — kommandopalet, kun søgning | Hurtigst hvis man kender navnet. Men at *lede* blev andenrangs, og det straffer den, der ikke kan navnet udenad. |
| **Familier** — dialog med familier til venstre, lister til højre | Smuk til 103 navne med system i (10 temaer × 10 steder). Men en fuld dialog for et valg, der tager ét sekund — og den falder sammen den dag listerne ikke hedder noget systematisk. |
| **Tabel** — listerne som en sorterbar tabel i selve trinnet | Gør listerne sammenlignelige (sortér på I alt). Men to tabeller over hinanden i samme trin, og modtagerne blev skubbet under folden. |

Tastaturmodellen fra **Skriv** overlevede alligevel: det er den, arket bruger.

## Faldgruber, vi gik i

**`kunder@Used(group)`.** Razor læser et `@` lige efter et ord som en mailadresse og skriver det ud som tekst. Sæt parentes om: `@(Used(group))`.

**Bagtæppet dækkede kun trinkortet.** `animation: … both` lader en identitets-`transform` blive hængende, efter indgangen er slut, og et element med en transform er dét, `position: fixed` måler sig efter. Indgange skal have `backwards`.

**Enter lukkede og åbnede igen.** Panelet lukkede, fokus hoppede til knappen inde i samme tastetryk, og knappen fangede tastens aktivering. `app.focusId` venter nu en frame.
