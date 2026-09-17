# Farver og detaljer: kort, bobler, ring, grå knap, dialog

Fem ting, som ikke kan læses ud af et skærmbillede: **sprogkortenes farver**, **de runde bobler med initialer**, **den blå ring om én af dem**, **knappen, der er slået fra**, og **dialogen med den røde knap**. Alle værdier her står i koden; filerne er nævnt under hvert afsnit.

Kopiér CSS-blokkene, som de er. De bruger kun tokens fra §1, så de virker, så snart tokens findes.

---

## 1 · Tokens og font

Alt i dette dokument bygger på disse. De er defineret i `:root` i `wwwroot/css/app.css`.

| Token | Værdi | Hex | Bruges til |
| --- | --- | --- | --- |
| `--accent` | `oklch(0.461 0.120 259)` | #2B579A | Outlooks blå: valgt, flueben, ring, links |
| `--accent-2` | `oklch(0.380 0.100 259)` | #1F4177 | Knappen, mens den holdes nede |
| `--accent-soft` | `oklch(0.962 0.014 258)` | #EDF3FC | Fladen bag noget valgt |
| `--surface` | `#ffffff` | #FFFFFF | Kort, felter, paneler |
| `--canvas` | `oklch(0.983 0.002 68)` | #FAF9F8 | Siden bag indholdet |
| `--sunk` / `--hover` | `oklch(0.962 0.002 68)` | #F3F2F1 | Hover, nedsænkede striber |
| `--press` | `oklch(0.941 0.003 68)` | #EDEBE9 | Trykket, eller en åben menu |
| `--line` | `oklch(0.905 0.003 68)` | #E1DFDD | Hårstreger mellem ting |
| `--line-2` | `oklch(0.859 0.004 68)` | #D2D0CE | Kant om felter og kort, **og teksten i en slået fra knap** |
| `--ink-1` | `oklch(0.240 0.002 68)` | #201F1E | Brødtekst og overskrifter (16,5:1) |
| `--ink-2` | `oklch(0.483 0.004 68)` | #605E5C | Underordnet tekst (6,5:1) |
| `--ink-3` | `oklch(0.539 0.004 68)` | #706E6C | Mindst vigtige tekst (5,1:1 på hvid) |
| `--danger` | `oklch(0.475 0.162 24)` | #A4262C | Fejl |

**Farverne er skrevet i oklch, ikke hex.** I oklch er første tal lysheden, og den er den samme for alle ti bobletoner. To farver med samme L opleves lige lyse, uanset kulør. Det gælder ikke i hex eller hsl, hvor gul virker lys og blå mørk ved samme tal — og derfor bliver kontrasten tilfældig.

**De grå toner er varme**: alle har kulør 68°. Det er Office-grå. Kølige grå (blå-violette) får et Windows-værktøj til at ligne en web-app ved siden af Outlook og Excel.

### Fonten

```css
:root {
  --font: "Segoe UI Variable Text", "Segoe UI", system-ui, -apple-system,
          BlinkMacSystemFont, Roboto, "Helvetica Neue", Arial, sans-serif;
}

body {
  font-family: var(--font);
  color: var(--ink-1);
  background: var(--canvas);
  /* Kun macOS tynder stregerne her. Windows beholder sin egen subpixel-
     rendering og ignorerer begge. */
  -webkit-font-smoothing: antialiased;
  -moz-osx-font-smoothing: grayscale;
}
```

- **Segoe UI Variable Text** er Windows 11's systemfont, tegnet til små størrelser. Windows 10 falder tilbage til **Segoe UI**, alt andet til systemets egen font. Ingen webfont hentes, så der er intet at vente på og intet layoutspring.
- **Vægte:** 400 normal, 500 til labels og knapper, 600 til overskrifter og initialer. **Klassisk Segoe UI har ingen 500** — den falder tilbage til 400 på Windows 10. Byg aldrig noget, der kun kan ses på forskellen mellem 400 og 500.
- **Hele pixels.** Hver skriftstørrelse er sat sammen med en linjehøjde i hele pixels (13/18, 14/20, 12/16). Falder en grundlinje mellem to pixels, bliver teksten blød.
- **Ændr aldrig vægt på hover eller valgt.** Teksten skifter bredde, og alt ved siden af rykker sig. Brug farve.

---

## 2 · Sprogkortene (de to skabeloner)

Filer: `Components/Fields.razor` (markup), `wwwroot/css/app.css` (`.tpl-*`).

Hele kortet er **én knap**. Ansigterne på det er information, ikke knapper: et kort, der gør noget andet, når man rammer 24 pixels af det, er en fælde.

```html
<button type="button" class="tpl-card" id="tpl-en" data-on aria-pressed="true">
  <span class="tpl-card-n">
    <span class="tpl-card-tick" aria-hidden="true"><!-- flueben, kun når valgt --></span>
    Engelsk
  </span>
  <span class="tpl-card-s" title="Your prices for 2027">Your prices for 2027</span>
  <span class="tpl-card-ppl">
    <span class="tpl-faces" aria-hidden="true">
      <span class="rcp-av" data-tone="6" data-shown title="Mia Brandt">MB</span>
      <span class="rcp-av" data-tone="7" title="Klaus Richter">KR</span>
    </span>
    <span class="tpl-card-count">2 modtagere</span>
  </span>
</button>
```

```css
.tpl-cards {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(170px, 1fr));
  gap: 10px;
  margin-bottom: 12px;
}

.tpl-card {
  display: grid;
  gap: 4px;
  padding: 11px 14px;
  border: 1px solid var(--line-2);
  border-radius: 11px;
  background: var(--surface);
  font: inherit;
  text-align: left;
  cursor: pointer;
  transition: border-color 130ms ease, background-color 130ms ease;
}
.tpl-card:hover           { background: var(--sunk); }
.tpl-card[data-on]        { border-color: var(--accent); background: var(--accent-soft); }
.tpl-card[data-bad]       { border-color: var(--danger); }
.tpl-card:focus-visible   { outline: 2px solid var(--accent); outline-offset: 2px; }

.tpl-card-n    { display: flex; align-items: center; gap: 6px; font-size: 14px; line-height: 20px; font-weight: 500; color: var(--ink-1); }
.tpl-card-tick { display: flex; flex: none; width: 12px; color: var(--accent); }
.tpl-card-s    { overflow: hidden; font-size: 12px; line-height: 16px; color: var(--ink-3); text-overflow: ellipsis; white-space: nowrap; }
.tpl-card-ppl  { display: flex; align-items: center; margin-top: 4px; }
.tpl-card-count{ margin-left: 8px; font-size: 12px; line-height: 16px; color: var(--ink-3); white-space: nowrap; }
```

| Tilstand | Kant | Flade | Tekst |
| --- | --- | --- | --- |
| Hvile | `--line-2` | `--surface` (hvid) | Navn `--ink-1`, emne `--ink-3` |
| Hover | `--line-2` | `--sunk` | Uændret |
| **Valgt** (`data-on`) | `--accent` | `--accent-soft` | Uændret, plus blåt flueben |
| Fejl (`data-bad`) | `--danger` | som den var | Ordene står ved feltet nedenfor, ikke på kortet |
| Fokus (tastatur) | `outline: 2px solid var(--accent)` med `outline-offset: 2px` | | |

**Hvorfor ikke kraftigere farve på det valgte kort?** Fordi der kan være tre kort ved siden af hinanden. Kanten og den lyse flade er nok til at pege det ud, og teksten beholder sin kontrast. Et fyldt blåt kort ville også gøre fluebenet og ansigterne ulæselige.

**Fluebenet har sin egen plads på 12px, også når det ikke er der** (`.tpl-card-tick` er altid i markup, tom når kortet ikke er valgt). Ellers rykker navnet sig, når man vælger.

---

## 3 · De runde bobler med initialer

Filer: `Model/Avatar.cs` (farvevalg), `Model/Mail.cs` (`Initials`, `Tone`), `wwwroot/css/app.css` (`.rcp-av`).

Grundformen er 28px. På sprogkortene er den 24px.

```css
.rcp-av {
  display: grid;
  place-items: center;
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: var(--accent-soft);
  color: var(--accent);
  font-size: 11px;
  font-weight: 600;
}
```

### De ti toner

```css
.rcp-av[data-tone="0"] { background: oklch(0.935 0.035 259); color: oklch(0.470 0.150 259); }
.rcp-av[data-tone="1"] { background: oklch(0.935 0.035 292); color: oklch(0.470 0.150 292); }
.rcp-av[data-tone="2"] { background: oklch(0.935 0.035 325); color: oklch(0.470 0.150 325); }
.rcp-av[data-tone="3"] { background: oklch(0.935 0.035 8);   color: oklch(0.470 0.150 8); }
.rcp-av[data-tone="4"] { background: oklch(0.935 0.035 44);  color: oklch(0.470 0.127 44); }
.rcp-av[data-tone="5"] { background: oklch(0.935 0.035 78);  color: oklch(0.470 0.082 78); }
.rcp-av[data-tone="6"] { background: oklch(0.935 0.035 128); color: oklch(0.470 0.101 128); }
.rcp-av[data-tone="7"] { background: oklch(0.935 0.035 163); color: oklch(0.470 0.086 163); }
.rcp-av[data-tone="8"] { background: oklch(0.935 0.035 196); color: oklch(0.470 0.067 196); }
.rcp-av[data-tone="9"] { background: oklch(0.935 0.035 228); color: oklch(0.470 0.079 228); }
```

Læs kolonnerne:

- **Lysheden er fast.** Cirklen er altid `L 0.935`, initialerne altid `L 0.470`. Derfor er kontrasten den samme for alle ti: mellem **5,4:1 og 6,0:1**. Tilføjer du en ellevte kulør med de samme to L-værdier, passer den også.
- **Cirklens mætning er fast (0.035) for alle.** Den almindelige regel — "giv hver kulør samme *andel* af sit eget loft" — blev prøvet og forkastet: ved denne lyshed går loftet fra 0.038 (blå) til 0.199 (lime), så de grønne blev fem gange så kraftige som de blå og faldt fra hinanden som familie.
- **Initialerne bruger en andel, med loft.** Lille tekst skal have mere mætning for overhovedet at se farvet ud. Derfor 0.150 på blå/lilla/pink/rød, og lavere tal, hvor kuløren ikke kan bære mere (0.067 på cyan).

### Hvem får hvilken farve

```csharp
public static class Avatar
{
    public const int Tones = 10;

    /// FNV-1a, og bevidst ikke string.GetHashCode(): .NET randomiserer
    /// string-hashing pr. proces, så farverne ville blive delt ud på ny ved
    /// hver genindlæsning og ikke betyde noget.
    public static int ToneOf(string key)
    {
        var hash = 2166136261u;

        foreach (var c in key.Trim().ToLowerInvariant())
        {
            hash ^= c;
            hash *= 16777619u;
        }

        return (int)(hash % Tones);
    }
}
```

```csharp
public string Initials =>
    string.Concat(Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                      .Take(2).Select(w => char.ToUpperInvariant(w[0])));

/// Adressen, ikke navnet: to personer kan hedde det samme, og en, der skifter
/// navn, skal beholde sin farve.
public int Tone => Avatar.ToneOf(Email);
```

- **Nøglen er mailadressen**, og den trimmes og sænkes til små bogstaver først, så `Anna@X.dk` og `anna@x.dk` er samme person.
- **Højst to initialer**, taget fra de to første ord i navnet.
- Boblen er `aria-hidden="true"` overalt, hvor navnet står ved siden af. To initialer læst højt er støj, når navnet står lige der.

---

## 4 · Den blå ring

Ringen betyder **én ting i hele appen: det er den person, forhåndsvisningen viser lige nu.** Den er en tilstand, ikke et mål man klikker på. Bladrer man i ruden til højre, flytter ringen sig.

Den er tegnet med `box-shadow`, aldrig `border`: en kant ville gøre boblen større og skubbe naboerne.

```css
/* På sprogkortet: 24px, og de overlapper hinanden */
.tpl-faces { display: flex; }
.tpl-faces .rcp-av { width: 24px; height: 24px; font-size: 10px; box-shadow: 0 0 0 2px var(--surface); }
.tpl-faces .rcp-av + .rcp-av { margin-left: -2px; }

/* Ringen skal have samme farve som fladen bagved, ellers ses den som en streg */
.tpl-card[data-on] .tpl-faces .rcp-av { box-shadow: 0 0 0 2px var(--accent-soft); }

/* Den, forhåndsvisningen viser */
.tpl-card .tpl-faces .rcp-av[data-shown] { box-shadow: 0 0 0 2px var(--accent); }

/* Samme ring i Modtagere-fanen, så reglen holder alle steder */
.rcp-head[aria-current] .rcp-av { box-shadow: 0 0 0 2px var(--accent); }
```

Tre ting at bemærke:

1. **Alle bobler har en ring** i en stak, også dem uden markering. Den er bare i fladens egen farve (`--surface`, eller `--accent-soft` på et valgt kort). Det er den, der klipper naboen fri, når de ligger 2px ind over hinanden. Den blå ring erstatter altså kun en farve, den tilføjer ikke en form, så intet flytter sig.
2. **Overlappet er kun 2px.** Mere, og den næste cirkel dækker det andet bogstav i den forrige.
3. **Ringen er 2px, aldrig mere.** Den skal kunne ses på 24px uden at æde cirklen.

I markup:

```razor
<span class="rcp-av" data-tone="@r.Tone"
      data-shown="@(r.Email == Shown?.Email ? "" : null)"
      title="@r.Name">@r.Initials</span>
```

`data-shown="@(… ? "" : null)"` er Blazors måde at få attributten til at **forsvinde helt**, når den er falsk. `data-shown="false"` ville stadig ramme `[data-shown]` i CSS.

---

## 5 · "Tilbage", når den er slået fra

Fil: `wwwroot/css/app.css` (`.btn:disabled`), `Pages/NewMail.razor` (footeren).

```css
/* En dedikeret dæmpet farve, ikke opacity: opacity består kontrasten på én
   baggrund og fejler på en anden. */
.btn:disabled {
  color: var(--line-2);
  cursor: default;
  pointer-events: none;
}
.btn:disabled svg { opacity: 0.55; }
```

```razor
<button type="button" class="btn btn-ghost" data-act="back"
        disabled="@(Prev is null)"
        @onclick="() => S.Open(Prev!.Key)">@Icons.ArrowL Tilbage</button>
```

- **Knappen bliver stående**, også på første trin. En knap, der forsvinder, siger ingenting; en dæmpet siger "du er i begyndelsen". Og intet rykker sig, når man går videre til trin 2.
- **Farven er `--line-2` (#D2D0CE)**, altså samme tone som kanten om felterne. Ikke `opacity: 0.4` på hele knappen: opacity blander sig med baggrunden, så den samme knap får forskellig kontrast på hvid og på grå. En token gør den forudsigelig.
- **Ikonet får `opacity: 0.55`** i stedet for en farve, fordi det tegnes med `currentColor` og ellers ville se hårdere ud end teksten ved siden af.
- **`pointer-events: none`** gør, at markøren ikke skifter, og at hover aldrig kan udløses. `disabled` på selve `<button>` er det, der holder den ude af tastaturets rækkefølge — begge dele skal være der.
- **Gør den ikke grå med `background`.** Den er en `btn-ghost` uden flade; kun teksten skifter.

---

## 6 · Dialogen med den røde knap

Fil: `Components/StartOverDialog.razor`, `wwwroot/css/app.css` (`.dlg*`, `.btn-danger`, `.btn-outline`).

Det er en **native `<dialog>`, åbnet med `showModal()`**. Så følger fokusfælden, Escape, det øverste lag og `::backdrop` med gratis, og fokus vender af sig selv tilbage til knappen, der åbnede den, når den lukkes.

```html
<dialog class="dlg" aria-labelledby="dlg-new-h" aria-describedby="dlg-new-b">
  <h2 class="dlg-h" id="dlg-new-h">Start en ny udsendelse?</h2>
  <p class="dlg-b" id="dlg-new-b">
    Der er kun plads til én kladde ad gangen. Starter du en ny, bliver
    <b>»Jeres priser for 2027«</b> til 3 mails slettet.
  </p>
  <div class="dlg-acts">
    <button type="button" class="btn btn-danger">🗑 Slet kladden og start ny</button>
    <button type="button" class="btn btn-outline" autofocus>Fortsæt kladden</button>
  </div>
</dialog>
```

```css
.dlg {
  width: min(400px, calc(100vw - 32px));
  padding: 20px 24px;
  border: 0;
  border-radius: var(--radius-menu);   /* 0 — menuer og dialoger er firkantede */
  background: var(--surface);
  box-shadow: var(--shadow-menu);
  color: var(--ink-1);
}
/* Blækket ved 28 %, ikke sort: sort giver et hul i siden, den varme grå lægger
   et skær. */
.dlg::backdrop { background: oklch(0.24 0.002 68 / 0.28); }
.dlg[open] { animation: t-menu-in 160ms var(--ease) backwards; }
@media (prefers-reduced-motion: reduce) { .dlg[open] { animation: none; } }

.dlg-h     { margin: 0 0 8px; font-size: 18px; line-height: 24px; font-weight: 600; }
.dlg-b     { margin: 0 0 20px; font-size: 14px; line-height: 20px; color: var(--ink-2); }
.dlg-b b   { font-weight: 600; color: var(--ink-1); }
.dlg-acts  { display: flex; flex-wrap: wrap; justify-content: flex-end; gap: 8px; }
```

```css
/* Kun til det ene valg, der smider arbejde væk, og altid navngivet efter det,
   den smider væk. Hover er den samme farve blandet mørkere, ikke en anden rød. */
.btn-danger { background: var(--danger); color: #fff; box-shadow: var(--lift); }
.btn-danger:hover  { background: color-mix(in oklch, var(--danger) 85%, black); box-shadow: var(--lift-hover); }
.btn-danger:active { box-shadow: var(--lift-down); }

/* Den samme historie i neutralt: en kant, der løfter sig lidt, frem for en
   streg tegnet om en flad form. */
.btn-outline { background: var(--surface); border-color: var(--line-2); color: var(--ink-1); box-shadow: 0 1px 2px oklch(0.24 0.002 68 / 0.08); }
.btn-outline:hover  { background: var(--surface); border-color: var(--ink-3); box-shadow: 0 2px 5px -1px oklch(0.24 0.002 68 / 0.12); }
.btn-outline:active { box-shadow: none; }
```

Skyggerne er tokens, fordi de skal være de samme overalt:

```css
--shadow-menu:
  0 0 0 1px rgb(0 0 0 / 0.08),      /* hårstregen, som en kant ville tegne */
  0 2px 4px rgb(0 0 0 / 0.06),      /* det tætte lag lige under */
  0 10px 24px -6px rgb(0 0 0 / 0.14); /* det brede, bløde lys */

/* Den løftede knap. Skyggen er i den blå kulør, ikke neutral: en grå skygge
   under en mættet flade bliver mudret i stedet for mørk. */
--lift:       0 1px 2px oklch(0.38 0.10 259 / 0.26), 0 2px 6px -1px oklch(0.38 0.10 259 / 0.22);
--lift-hover: 0 2px 4px oklch(0.38 0.10 259 / 0.28), 0 6px 14px -3px oklch(0.38 0.10 259 / 0.28);
--lift-down:  0 1px 1px oklch(0.38 0.10 259 / 0.24);
```

Reglerne bag billedet:

- **Rød bruges kun her.** Rød er fejl og tab i resten af appen. En rød knap betyder derfor "det her sletter noget", ikke "det her er den vigtigste knap".
- **Knapperne hedder det, de gør** — "Slet kladden og start ny", "Fortsæt kladden" — ikke "Ja" og "Nej". Man skal kunne læse knappen alene og vide, hvad der sker.
- **Fokus starter på den sikre** (`autofocus` på "Fortsæt kladden"). Enter trykket i blinde må aldrig slette noget.
- **Escape og klik uden for boksen fortsætter kladden.** Den farlige handling kræver et rigtigt klik.
- **Brødteksten siger prisen** med navn og antal: *"bliver »Jeres priser for 2027« til 3 mails slettet"*. Et tal og et navn gør forskellen på "ja ja" og "vent lidt".
- **Dialogen er firkantet** (`--radius-menu: 0`), som Outlooks egne flyouts. Knapperne inde i den beholder deres egen radius på 9px.

**To fælder, jeg gik i:**

1. Et klik på dialogens egen **padding** er også et klik på `<dialog>`. Vil du lukke ved klik på bagtæppet, skal du måle: luk kun, hvis klikket ligger uden for `getBoundingClientRect()`.
2. En menu inde i en `position: sticky` sidebjælke ligger **under** et bagtæppe uden for den, uanset z-index. `<dialog>` + `showModal()` har ikke problemet, fordi den ligger i browserens øverste lag.

## 7 · Tjekliste, når det skal bygges et andet sted

- [ ] Tokens fra §1 findes i `:root`, i oklch
- [ ] `--font` sat på `body`, ingen webfont hentes
- [ ] Ingen skriftstørrelse eller linjehøjde med halve pixels
- [ ] Kortet er én `<button>`; ansigterne på det er `aria-hidden`
- [ ] Valgt kort = `--accent` kant + `--accent-soft` flade, ikke fyldt blå
- [ ] Fluebenets plads er der, også når fluebenet ikke er
- [ ] Boblernes ti toner har **samme** L (0.935 / 0.470) og samme chroma på cirklen (0.035)
- [ ] Farven vælges af en stabil hash (FNV-1a) af mailadressen, ikke `GetHashCode()`
- [ ] Alle bobler i en stak har en ring i baggrundens farve; den blå erstatter kun farven
- [ ] Ringen er `box-shadow`, 2px, aldrig `border`
- [ ] Slået fra = dæmpet tokenfarve + `pointer-events: none` + `disabled`, aldrig `opacity` på hele knappen
- [ ] Dialogen er en native `<dialog>` med `showModal()`, ikke en div i en overlay
- [ ] Rød knap kun til det, der sletter; fokus starter på den sikre knap
- [ ] Knapperne hedder det, de gør, og brødteksten siger, hvad der går tabt
