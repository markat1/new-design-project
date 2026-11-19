# Indkøbslisten

En leverandørs linjer holdt op mod de åbne indkøbsordrer. Man ser linjerne igennem, vælger dem, der skal med, og sender dem videre.

Siden har to visninger af de samme linjer, og én knap skifter mellem dem:

- **Delt**: en smal tabel til venstre og den valgte linjes detaljer til højre, med en skillelinje, der kan trækkes.
- **Tabel**: alle kolonner i fuld bredde.

Det står som prototype i `SendChecklist/` (Blazor WASM, port 5179) på `/proto/indkob`. Den er ikke bygget ind i en rigtig side endnu. Alle data er opdigtede.

---

## Filerne

Fem filer, ingen pakker. De kan kopieres til et andet projekt, som de er:

| Fil | Indhold |
| --- | --- |
| `SendChecklist/Proto/IbProto.razor` | Siden: markup, tilstand og de to visninger |
| `SendChecklist/Proto/IbBox.razor` | Fluebenet: en rigtig checkbox under en tegnet |
| `SendChecklist/Proto/Ib.cs` | Modellen (`Line`, `Order`), eksempellinjerne og talformatering |
| `SendChecklist/wwwroot/css/proto-indkob.css` | Alt udseende, præfiks `ib-` |
| `SendChecklist/wwwroot/js/proto-indkob.js` | Skillelinjen og kolonnestregerne (ES-modul, hentes med `import`) |

Det, de låner udefra, skal findes i det nye projekt:

- **Farver og mål** fra `.v2` i `wwwroot/css/app.css`: `--surface`, `--raised`, `--field`, `--line`, `--line-2`, `--ink`, `--ink-2`, `--ink-3`, `--accent`, `--accent-2`, `--accent-soft`, `--bad`.
- **Knappen** `.ck-btn` med `.ck-primary`, og `.ck-vh` (skjult for øjet, læst af skærmlæsere) og `.num` (`font-variant-numeric: tabular-nums`).
- **Ikonerne** `Icons.Tick` og `Icons.Arrow` fra `Components/Icons.cs`.
- **Sidens ramme**: `.ck-root.v2.v2-solo` giver en side på `100dvh`, hvor kun tabellen og detaljerne ruller.

Siden sætter selv fem farver oven i:

```css
.ib {
  --ease-out: cubic-bezier(0.19, 1, 0.22, 1);
  --tick: oklch(0.52 0.13 155);  --tick-soft: oklch(0.96 0.03 155);   /* Klar */
  --warn: oklch(0.5 0.12 65);    --warn-soft: oklch(0.94 0.055 82);   /* prisafvigelse */
  --warn-row: oklch(0.982 0.022 85);                                  /* rækkens baggrund, afvigelse */
  --off-row: oklch(0.984 0.012 25);                                   /* rækkens baggrund, ingen ordre */
}
```

## Siden

```
┌───────────────────────────────────────────────────────────────────────────────┐
│ Indkøbsliste                                                  [▯ Delt visning] │
│ 25 linjer · 8 klar · 17 uden åben ordre                                       │
├──────────────────────────────────────────────┬────────────────────────────────┤
│ ☐  LP│VARENR.│VARENAVN          │ANTAL stk.│STATUS │ LINJE 5 AF 25              │
│ ☐  1  2204105 Pakningssæt, …          20      ○   │ Lejehus, 2/3-delt, venstre …   │
│ ☑  5  1180301 Lejehus … [Pris −0,65]  60      ●   │ Varenr. 1180301 · Lev. 5540    │
│ ☑  7  3050211 Afstandsring, stål      120     ●   │ ┌────────────────────────────┐ │
│ ☐  9  0471833 Koblingsflange …        20      ○   │ │ ● Klar            (●) Med  │ │
│   … (ruller for sig selv)                    ║   │ │   Fordeles på 2 ordrer     │ │
│                                              ║   │ └────────────────────────────┘ │
│                                 skillelinje ─╜   │ ┌ PRIS ──────────────────────┐ │
│                                                  │ │ Stykpris Aftalepris Forskel│ │
│                                                  │ │ 31,75    32,40      −0,65  │ │
│                                                  │ │ 60 stk. × 31,75   1.905,00 │ │
│                                                  │ └────────────────────────────┘ │
│                                                  │ ┌ ÅBNE ORDRER ───────────────┐ │
│                                                  │ │ Ordre 61207        45 stk. │ │
│                                                  │ │ Ordre 61388        15 stk. │ │
│                                                  │ │ Udestående i alt   90 stk. │ │
│                                                  │ └────────────────────────────┘ │
├──────────────────────────────────────────────┴────────────────────────────────┤
│ 8 af 25 linjer valgt │ Valgt for 7.606,60 EUR              [Send 8 linjer videre →] │
└───────────────────────────────────────────────────────────────────────────────┘
```

- **Top**: titel, og under den en linje med tal: "25 linjer · 8 klar · 17 uden åben ordre". Knappen **Delt visning** står til højre.
- **Bund**: "N af M linjer valgt", "Valgt for X EUR" og den ene primære knap, **Send N linjer videre**. Uden valgte linjer er den slået fra og hedder "Vælg linjer for at sende". Efter afsendelse står der "Sendt videre med N linjer", og fluebenene låses.

## Modellen

```csharp
public record Order(int No, int Qty);

public record Line(int Lp, string LevSku, string Sku, string RawName, int Qty,
                   decimal Price, decimal? Agreed, int? Outstanding = null, params Order[] Orders)
{
    public bool Ready => Orders.Length > 0;                       // "Klar"; ellers "Ingen åben ordre"
    public decimal? Diff => Agreed is null ? null : Price - Agreed;
    public bool Deviates => Diff is not null and not 0;
    public decimal Amount => Qty * Price;
    public string Name => /* RawName med kun første bogstav stort */;
}
```

- Kilden skriver varenavne med versaler. Siden viser dem med almindelige bogstaver, fordi det læses hurtigere.
- En linje uden varenavn (`"—"`) vises som "Intet varenavn".
- Linjer med en åben ordre er valgt fra start. Resten er ikke, men kan vælges.
- Tal skrives dansk, "1.014,00" og "−0,58" med et rigtigt minus. Appen kører med invariant kultur, så `Ib.Kr()` bytter selv tegnene om.

## Kolonnerne

| Kolonne | Delt | Tabel | Bredde | Bemærkning |
| --- | --- | --- | --- | --- |
| Flueben | ja | ja | 56px | "Tag linjen med" |
| Lp | ja | ja | 44px | grå |
| Lev. varenr. | – | ja | 108px | grå |
| Varenr. | ja | ja | 104px / 168px | |
| Varenavn | ja | ja | resten | 500 i vægt; i Delt en knap, der vælger linjen |
| Antal | ja | ja | 96px | højrestillet. Enheden står én gang i overskriften: "ANTAL stk." |
| Udestående | – | ja | 116px | højrestillet |
| Ordre | – | ja | 272px | ét mærke pr. ordre, se nedenfor |
| Stykpris | – | ja | 104px | højrestillet |
| Aftalepris | – | ja | 104px | højrestillet |
| Forskel | – | ja | 88px | grå ved 0,00; gul og fed ved afvigelse |
| Status | ja | ja | 84px | kun ikonet, se nedenfor |

- Tabellen har `table-layout: fixed`, 14px tekst og 44px høje rækker. Overskrifterne er 12px versaler med `letter-spacing: 0.04em` og står fast (`position: sticky`), mens rækkerne ruller.
- Lange navne afkortes med `…` og står fuldt ud i `title`.
- **Enheden i overskriften** forudsætter, at alle linjer på listen har samme enhed. Kan en liste blande enheder, skal den tilbage i cellen.

### Rækkens farve

| Tilstand | Baggrund | Venstre kant |
| --- | --- | --- |
| Klar, pris som aftalt | hvid | – |
| Prisen afviger, eller der er ingen aftalepris | `--warn-row` (svag gul) | 2px `--warn` 55 % |
| Ingen åben ordre | `--off-row` (svag rød) | 2px `--bad` 45 % |
| Valgt (kun Delt) | `--accent-soft` | 2px `--accent` |

Har en linje både ingen ordre og en prisfejl, er den rød: den manglende ordre er den tungeste fejl. Valgt ligger over alle. Rækker uden åben ordre har også roligere tekst (400 i vægt, `--ink-2`).

I Delt får en linje med prisafvigelse et lille gult mærke efter navnet: "Pris −0,65". I Tabel står tallet i Forskel-kolonnen i stedet.

### Status: kun ikonet

- **Klar**: fyldt grøn prik (`--tick`). **Ingen åben ordre**: tom rød ring (`inset 0 0 0 1.5px var(--bad)`).
- Ikonet bærer ordene som `aria-label` (med `role="img"`).
- Holder man musen over det i 250 ms, kommer en mørk boble med ordene. Den åbner **til venstre**: opad ville første rækkes boble blive skåret af overskriften, og til højre af tabellens kant. Den dækker rækkens Antal, så længe den er fremme.
- Boblen findes kun med mus (`@media (hover: hover) and (pointer: fine)`). På berøringsskærm står ordene i detaljerne.

### Ordre: ét mærke pr. ordre

`61207 (45), 61388 (15)` blev til to mærker, hver i to halvdele: ordrenummeret (500, hvid) og antallet med enhed (`--raised`, `--ink-2`) med en streg imellem. 24px høje, 6px radius, 1px kant `--line-2`. Cellen har hele listen som `title`: "Ordre 61207: 45 stk., Ordre 61388: 15 stk.". Uden ordre: en grå streg.

## Detaljerne (Delt)

Fra oven:

1. **Hoved**: "LINJE 5 AF 25" (12px versaler), varenavnet (20/28, 600, må brydes over flere linjer) og "Varenr. … · Lev. varenr. …".
2. **Status-bjælke**: prikken, "Klar" eller "Ingen åben ordre" i fed, og grunden under: "Føres på ordre 61388", "Fordeles på 2 ordrer" eller "Linjens 20 stk. har ikke noget at blive ført på". Til højre en kontakt, **Med i afsendelsen**, som er det samme valg som fluebenet i tabellen.
3. **Pris**: tre tal på række, Stykpris, Aftalepris og Forskel (22/28, 600). Forskel er grå med "Som aftalt" under, eller gul med beløbet for hele linjen: "−39,00 EUR for 60 stk.". Uden aftalepris: "Ingen" og "Ingen aftale at holde op imod". Nederst regnestykket: "60 stk. × 31,75" og linjens beløb.
4. **Åbne ordrer**: én linje pr. ordre med antal, og "Udestående på varen i alt". Uden ordre: "Der er ingen åben ordre på varenummeret."

Ruden er en container (`container-type: inline-size`). Er den **760px eller bredere**, står Pris og Åbne ordrer side om side (3fr / 2fr); ellers under hinanden. Indholdet stopper ved 1120px. De tre pristal bruger `repeat(auto-fit, minmax(118px, 1fr))` og falder selv til to pr. række, når ruden er smal.

## Knappen Delt visning

Én knap med `aria-pressed`. Trykket ind (blå, `--accent-soft`) betyder Delt; sluppet viser Tabel. Visningen ligger i adressen som `?vis=tabel`, så en genindlæsning beholder den. Fluebenene og den valgte linje bliver stående ved skift.

## Skillelinjen

- En 11px bred gribeflade, hvis synlige streg på 1px ligger præcis på rudens kant. Stregen bliver blå, når musen er over den, eller den trækkes.
- **Standardbredden** på detaljerne er `clamp(340px, max(29%, min(552px, 43.5%)), 720px)`: omkring 550px, nok til at de tre pristal står på én række. På store skærme vokser den til 29 % af siden; på små stopper den ved 43,5 %.

  | Skærm | Tabel | Detaljer |
  | --- | --- | --- |
  | 2000px | 1420 | 580 |
  | 1600px | 1048 | 552 |
  | 1270px | 718 | 552 |
  | 1000px | 565 | 435 |

- Trukket står bredden fast i pixels og huskes i `localStorage`. Indtil da følger den vinduet.
- Detaljerne kan ikke blive smallere end 340px, og tabellen ikke smallere end 520px.
- **Dobbeltklik** går tilbage til standarden. Med fokus på linjen flytter `←`/`→` den 16px, og `Home`/`End` går til yderpunkterne.
- Under 900px lægger detaljerne sig under tabellen, og skillelinjen er væk.

Trækket ligger i JavaScript og skriver direkte til `--detail-w` på elementet. En gengivelse i Blazor pr. musebevægelse ville være alt for tungt for noget, der kun er en layoutændring.

## Kolonnestregerne

Hver overskrift slutter med en lodret streg, der kan trækkes: kort (8px fra top og bund) i hvile, fuld højde og blå, når den peges på eller trækkes.

- Varenavn tager den plads, der er tilbage. Derfor styrer stregerne **til venstre** for Varenavn deres egen kolonne, og stregen på Varenavn og dem **til højre** for den styrer kolonnen efter sig. Så følger stregen altid musen.
- Bredderne huskes i `localStorage`, hver for sig i Delt og Tabel.
- En kolonne kan være mellem 44px og 520px. Tabellens `min-width` sættes til summen af de faste kolonner plus 200px til navnet, så den ruller vandret i stedet for at klemme navnet væk.
- Dobbeltklik giver kolonnen sin oprindelige bredde tilbage. `←`/`→` flytter stregen 16px.

## Bevægelse

| Hvad | Hvordan | Hvornår |
| --- | --- | --- |
| Flueben | Kassen farves på 120 ms; hakket tegnes på 180 ms med 60 ms forsinkelse (`stroke-dashoffset`) | Hver gang |
| Skift til Delt | Detaljeruden glider 24px ind fra højre og toner frem, 320 ms | Kun efter tryk på knappen |
| Skift til Tabel | De ekstra kolonner glider 8px ind fra venstre, 300 ms, forskudt 20 ms pr. kolonne | Kun efter tryk på knappen |
| Ny linje valgt med mus | Detaljernes indhold toner ind og kommer 8px nedefra eller oppefra, alt efter om linjen ligger længere nede eller oppe, 200 ms | Kun ved klik |
| Ny linje valgt med piletaster | Ingen bevægelse | – |
| Kontakten | Knoppen glider 14px, 180 ms | Hver gang |
| Statusboblen | Toner frem og vokser fra 0,96 på 140 ms efter 250 ms; forsvinder på 80 ms | Mus over ikonet |

Alt bruger `cubic-bezier(0.19, 1, 0.22, 1)` og flytter kun `transform` og `opacity`. Intet bevæger sig ved indlæsning. Med reduceret bevægelse slået til i systemet toner tingene kun frem uden at flytte sig.

## Tastatur og skærmlæsere

- I Delt er varenavnet en knap. `↑`/`↓` flytter den valgte linje, og `Home`/`End` går til første og sidste.
- Fluebenet er en rigtig `<input type="checkbox">` med en klikflade på 44px og navnet "Tag linje 5 med: …".
- Kontakten er en `<input type="checkbox" role="switch">`.
- Skillelinjen og kolonnestregerne har `role="separator"`, `tabindex="0"` og et navn ("Juster bredden på Varenr.").
- Status er aldrig farve alene: ikonets form skifter (fyldt prik over for tom ring), og ordene står i `aria-label` og i detaljerne.

## Faldgruber fundet undervejs

- **Samme `@key` i to grene.** Antal-overskriften stod først i både `@if` og `else` med samme nøgle. Ved skift fra Tabel til Delt mistede den sine attributter og dermed sin bredde. En overskrift, der findes i begge visninger, skal stå ét sted.
- **Stregen skal blive inde i sin egen overskrift.** Stak den 5px ud over kanten, blev den dækket af næste overskrift, og halvdelen af gribefladen virkede ikke.
- **`overflow: hidden` på cellen klipper klikfladen.** Flueben- og statuscellen har `overflow: visible`.
- **Skillelinjen skal kobles på igen**, hver gang Delt kommer tilbage, fordi elementet er nyt. Kolonnestregerne lytter på tabellen og kobles kun på én gang.

## Forkastet

- **Liste, Én ad gangen, Det der mangler**: tre andre retninger fra første runde. Delt blev valgt, fordi tabellen bevarer overblikket, mens detaljerne har plads til pris og ordrer.
- **To knapper, Delt og Tabel, ved siden af hinanden**: erstattet af én knap, der trykkes ind.
- **Status som farvede piller med tekst**: 17 røde piller i træk overdøvede alt andet.
- **Status som ikon og tekst i Delt**, og **ikonet foran varenavnet uden egen kolonne**: fravalgt for ikonet alene i en smal kolonne. Der er kun to statusser, rækken er farvet, og detaljerne skriver grunden ud.
- **Lyserød baggrund på hele rækken som eneste signal** (den oprindelige tabel): nu er farven svagere og følges af ikonet.
- **Grøn boks "Prisen er som aftalt"** i detaljerne: det almindelige tilfælde skal ikke råbe. Forskel er nu et tredje tal.
- **Ordrer som `61207 (45), 61388 (15)`**: parentesen krævede, at man vidste, hvad den betød.
- **Enheden gentaget i hver celle**: flyttet til overskriften.
- **Fast bredde på detaljerne (440px, senere 29 %)**: erstattet af reglen ovenfor, så små skærme også får plads til pristallene.

## Antagelser, der ikke er bekræftet

- Fluebenet betyder "linjen sendes med", og linjer uden åben ordre må gerne vælges.
- Stykprisen er i samme valuta som aftaleprisen.
- "Udestående" er det samlede åbne antal på varen på tværs af ordrer.
- Hvad "Send videre" gør, er ikke bestemt; knappen låser kun siden i prototypen.
- Sortering på kolonnerne er ikke lavet.
