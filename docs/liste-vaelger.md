# Marketinglisten

Trin 1 spørger, hvem udsendelsen går til. Svaret er **én eller to** marketinglister ud af 103, og de tre, du bruger, står allerede på skærmen.

Alt ligger i to filer: `OnboardingChecklist/Components/Fields.razor` (markup og tilstand) og `wwwroot/css/app.css` (udseende), med to små hjælpere i `wwwroot/js/app.js`. Ingen pakker.

---

## Beslutningen

Afgjort ud fra fem retninger, bygget side om side på de rigtige 103 lister (prøvestanden er revet ned igen; se [Forkastet](#forkastet)).

**Valgt: kort.** Tre kort med de mest brugte lister, og et søgefelt til de øvrige hundrede.

- **De tre er på skærmen, ikke bag et klik.** Hver dag rammer valget en af dem, og så skal det ikke koste en åbning af noget.
- **Tallet står på kortet** — `244,200 · brugt 2 gange`. Det er dét, der giver listen pladsen; uden det ligner rækkefølgen et tilfælde.
- **De valgte lister er altid blandt de tre kort.** Vælger du en fra den lange hale, skubber den et kort ud og bliver selv det første. Ellers ville det, du lige valgte, forsvinde i samme øjeblik.
- **Én liste er reglen, to er undtagelsen, tre er ingenting.** Forbi to kan ingen holde i hovedet, hvem der er ved at få en mail.
- **Nul er tilladt, mens man vælger.** Første forsøg nægtede at slippe den sidste liste, og så var det ikke det andet valg, der føltes låst — det var det første: at bytte én liste ud med en anden blev *tilføj og fjern igen*, med et øjeblik i midten hvor udsendelsen gik til begge. En kontrol, der ikke vil give slip på sin egen standardværdi, er en kontrol, man skal slås med. Kravet om mindst én hører hjemme ved afsendelsen, ikke i klikket.
- **De hundrede andre er undtagelsen**, og feltet under kortene er vejen til dem. Ikke en knap foran et felt: det klik, en knap ville koste, er alligevel det klik, du var på vej til at lave i feltet.

## Sådan opfører den sig

```
┌────────────────────┐ ┌────────────────────┐ ┌────────────────────┐
│ ✓ Rammeaftale 2027 │ │ Norden             │ │ Fragtkunder        │
│ 8 personer · 7 k.  │ │ 5 personer · 4 k.  │ │ 3 personer · 3 k.  │
│ 244,200 · brugt 2× │ │ 168,000 · brugt 1× │ │ 37,400 · brugt 1×  │
└────────────────────┘ └────────────────────┘ └────────────────────┘
┌──────────────────────────────────────────────────────────────────┐
│ 🔍 Søg i alle 103 lister, personer og kunder                     │
└──────────────────────────────────────────────────────────────────┘
      ↓ klik i feltet
┌──────────────────────────────────────────────────────────────────┐
│ Alle lister · 103                                                │
│▌Fragt Benelux              2 personer · 2 kunder        89,000   │ ← markøren
│ Fragt Danmark nord         6 personer · 6 kunder       159,200   │
├──────────────────────────────────────────────────────────────────┤
│ ↑↓ flyt   ↵ vælg   esc luk                                       │
└──────────────────────────────────────────────────────────────────┘
```

- **Kortene er tre kontakter**, ikke ét valg: klik tænder, klik igen slukker (`aria-pressed`). Piletasterne flytter fokus mellem dem, men vælger ikke undervejs — med to lister tilladt ville man ellers tænde lister bag sig.
- **Når to er valgt, kan de øvrige ikke tage imod** (`aria-disabled`, dæmpet kant og tekst). De bliver stående og kan stadig nås med tastatur — linjen under kortene siger hvad man gør i stedet. Det samme gælder rækkerne i arket.
- **Et klik i feltet åbner listen**, og markøren står på første række. At *få* fokus gør det ikke: tabulerer man forbi på vej til næste trin, skal der ikke folde sig hundrede rækker ud. Pil ned åbner den også.
- **Piletasterne går ned gennem alle 103**, og `↵` tager den, markøren står på. At nå række tres med Tab er ikke en vej nogen går, og derfor står tasterne skrevet i bunden af arket — kun på mus og tastatur, ikke på touch.
- **Søgningen rammer også personer og kunder**, ikke kun listenavne (`MailDraft.Search`), fordi man ofte husker kunden og ikke listen.
- **Escape og klik udenfor lukker**, rydder ordet og lader fokus blive i feltet. Feltet selv står over bagtæppet, så man kan sætte markøren tilbage og skrive videre, mens listen er åben.
- **Enter i feltet må ikke springe trinnet over.** Trinnets egen "Enter = næste" lytter oppe på `.f-fields`, så feltet stopper tastens vandring (`@onkeydown:stopPropagation`).
- Arket har ikke sin egen "Mest brugt"-sektion. **Kortene er den sektion.**
- **Står en person på begge lister, får hun én mail.** Personlisten viser hende én gang, kolonnen *Liste* siger "Begge", og fjerner man fluebenet, ryger hun af begge på én gang. Linjen under kortene tæller dem: *"3 personer står på begge lister og får én mail."*
- **Kolonnen *Liste* findes kun, når der er to lister.** Med én er svaret det samme i hver eneste række.
- **Ingen liste valgt er en tilstand, ikke en fejl.** Personlisten bliver til en rolig kasse, der siger hvad der kommer til at stå der, og linjen over kortene bliver til spørgsmålet. Fejlen kommer først, når man prøver at gå videre: *"Vælg den liste, udsendelsen skal gå til."*

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
private IEnumerable<Group> Cards =>
    S.Lists.Where(l => !MostUsed.Any(m => m.Name == l.Name))
        .Concat(MostUsed)
        .Take(3);

/// A card or row that cannot take a click: the second list is chosen, and
/// this is not one of them. Told, not hidden — the rule line says why.
private bool Blocked(Group group) => !S.Chosen(group) && !S.RoomForMore;
```

To lister, én person:

```csharp
/// Everybody the chosen lists hold, each person once. Somebody on both
/// lists is one mail with one sheet, not two of each.
public Person[] People => [.. lists.SelectMany(l => l.People).DistinctBy(p => p.Email)];
```

Fluebenerne bliver ved med at ligge pr. liste (`picks[listName]`), så en liste, man tager af og på igen, husker sine fravalg. `Picked` er foreningsmængden, og `Toggle(email)` rammer alle de valgte lister, personen står på — ellers ville hun blive siddende på den anden.

`MostUsed` er frekvens, ikke rækkefølge — de tre lister med flest **sendte** udsendelser bag sig, uafgjort brydes af den, der blev sendt sidst. Kladder tæller ikke: de er ikke gået nogen steder, og deres "—" som dato sorterer oven over alle rigtige. Det kræver, at en udsendelse husker sine lister (`Mail.Lists`, som nu er flertal).

Markøren i arket:

```csharp
private async Task ListKey(KeyboardEventArgs e)
{
    if (e.Key == "Escape") { CloseLists(); return; }

    if (!listOpen)
    {
        if (e.Key is "ArrowDown" or "Enter") OpenLists();
        return;
    }

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

/* Above the backdrop (5), under the sheet (7): the field stays a field while
   its list is open — you can put the caret back, select a word, keep typing. */
.grp-find { position: relative; z-index: 6; }

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

**Escape virkede kun fra feltet.** Klikker man en række i arket, står fokus på en knap inde i panelet, og tastaturhåndteringen sad kun på inputtet. Panelet lytter nu selv med.

**Enter lukkede og åbnede igen.** Panelet lukkede, fokus hoppede til knappen inde i samme tastetryk, og knappen fangede tastens aktivering. `app.focusId` venter nu en frame.
