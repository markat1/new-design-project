# Skabelonen og afsendelsen

En udsendelse er tre beslutninger, ikke fem:

| Trin | Spørgsmålet | Svaret |
| --- | --- | --- |
| 1 · Modtagere | Hvem skal have den? | Én eller to marketinglister — se [`liste-vaelger.md`](liste-vaelger.md) |
| 2 · Skabelon | Hvad skal der stå? | Ét brev pr. sprog, valgt af modtagerens land |
| 3 · Afsendelse | Hvordan skal den ud? | CRM, lokal kopi, kopi til dig selv |

*Emne* og *Besked* var to trin før. De er ikke to beslutninger — de er felterne i den ene beslutning, skabelonen allerede har taget.

Filerne: `Model/Template.cs` (de tre breve), `Model/MailDraft.cs` (udkastets tekster og afsendelsesvalg), `Components/Fields.razor` (begge trins formularer) og `Components/MailView.razor` (det høje vindue).

---

## Sproget er ikke et valg

Ingen vælger sprog i en dropdown. **Kundens eget land afgør det**, og modtagerlisten kender landet:

```csharp
public static string For(string country) => country switch
{
    "DK" => Da,
    "SE" => Sv,
    _ => En,
};
```

Det er ikke en vurdering af, hvem der taler hvad. Det er, hvilke breve virksomheden har skrevet, og der er tre af dem.

Derfor findes der ingen "vælg skabelon"-kontrol. Der er et kort pr. sprog, **der faktisk er i spil** — er hele udsendelsen dansk, er der ét kort.

## Beslutningen: sprogkort

Afgjort ud fra to runder prototyper (13. september 2026). Den sidste blev bygget på et aftryk af appens egen ramme, så valget blev truffet mod den rigtige side og ikke mod en opfundet.

```
┌───────────────────┐ ┌───────────────────┐ ┌───────────────────┐
│ ✓ Dansk           │ │   Svensk          │ │   Engelsk         │
│ Jeres priser 2027 │ │ Era priser 2027   │ │ Your prices 2027  │
│ (AS)(BH) 2 modt.  │ │ (CN)(JV) 2 modt.  │ │ (BF)(KR)(MB)(IS) 4│
└───────────────────┘ └───────────────────┘ └───────────────────┘
Den danske skabelon gælder Anna Sørensen og Bo Halden.
Emne   [Jeres priser for 2027         ]
Tekst  [Hej {navn} …                  ]
```

- **Et kort pr. sprog, i samme form som listekortene i trin 1.** Ét sprog, du allerede har lært, frem for et nyt.
- **Alle emnelinjer er synlige på én gang.** Et tomt emne på svensk ses uden at åbne svensk, og kortet får rød kant, når valideringen stopper dig.
- **Modtagerne står på kortet, som deres egne initialer.** De er information, ikke knapper. Den, forhåndsvisningen viser, har en blå ring, og ringen flytter sig, når du bladrer i ruden.
- **Linjen under kortene siger, hvem en rettelse rammer**, i ord: *"Den svenske skabelon gælder Cecilie Nord og Jonas Vik."*
- **Trinnet starter på det sprog, forhåndsvisningen faktisk viser.** Fanerne startede på dansk, mens ruden viste en englænder — to ruder, der sagde hver sit.
- **Ét kort, ét klik, én betydning.** Klik hvor som helst på kortet, og skabelonen vises. Første udgave gjorde noget andet, når man ramte et ansigt — et kort, der skifter mening efter 24 pixels, er en fælde.

### Forkastet

| Retning | Hvorfor ikke |
| --- | --- |
| **Faner** (det, der var) | Tallet på fanen sagde *hvor mange*, ikke *hvem*, og personen fandtes kun i ruden til højre. Kompakt, men ingen kunne se konsekvensen af en rettelse. |
| **Personer i fanen** | Tættest på fanerne, men initialer i en fane er små, og ved mange modtagere bliver de til "+12" — et tal igen. |
| **Vis som** | Mindst plads og skalerer til hundrede modtagere, men sprogene er usynlige, indtil man åbner, og man vælger en person for at komme til en skabelon. |
| **Modtagere** (liste) | Konsekvensen var tydelig, men en lodret liste ved siden af felterne fyldte og lignede en anden side. |
| **Brevet** | Pladsholdere som objekter kan ikke staves forkert, men forhåndsvisningen viste det samme igen, og redigering med låste objekter er skrøbelig: en linje, der slutter med en pladsholder, slugte tekst uden en lyd. |
| **Side om side** | Godt til at holde sprogene ens, men tre smalle kolonner bliver et tæt gitter ved et langt brev. |
| **Grundtekst** | "Dansk ændret siden" lover en oversætterproces, der ikke findes — uden automatisk oversættelse betyder "følger dansk" kun "set efter". |

## Pladsholderne

`{navn}` og `{firma}` er værktøjets, ikke læserens — de forlader aldrig editoren:

```csharp
public static string Fill(string text, string name, string company) =>
    text.Replace("{navn}", name).Replace("{firma}", company);
```

Ét sæt på tværs af alle tre sprog, så en tekst kan kopieres fra dansk til svensk uden at skrive pladsholderne om. At der står `{navn}` i et engelsk brev, ser kun du.

## Vinduet viser kladden, som én person får den

Det høje vindue viser ikke et gennemsnit af udsendelsen. Det viser **ét brev med et navn på**:

```
Som Cecilie Nord får den · svensk            ‹ 5 / 8 ›
Era priser för 2027
Hej Cecilie Nord
Här är era priser för 2027. Arket är bifogat och gäller endast Kestrel Analytics.
```

Og de to ruder følges ad **begge veje**:

- **Klik et sprogkort** → vinduet stiller sig på en, der taler det sprog. Et brev uden en læser på den anden side af ruden er bare tekst.
- **Klik en person i vinduet** → kortet flytter sig til det brev, personen rent faktisk får. Klik en svensker, og du redigerer den svenske skabelon.
- **Bladreren `‹ ›`** går gennem alle modtagere, og sproget skifter med dem.

Teknisk er det én besked hver vej. `MailView` råber op, hvem den viser (`Showed`), siden giver det videre til udkastet, og `Fields` retter sit kort ind efter det:

```csharp
protected override void OnParametersSet()
{
    if (S.Show is not { } ask || ask.Nonce == seenShow) return;

    seenShow = ask.Nonce;

    if (S.Recipients.FirstOrDefault(r => r.Email == ask.Email) is { } who) langShown = who.Lang;
}
```

**Fanen er din.** Et klik i venstre side ændrer *hvem* vinduet viser, aldrig *hvilken fane* det står på. Læser du prislister og klikker det svenske kort, ser du en svenskers prisliste — ikke pludselig mailen.

| Du står på | Du klikker | Vinduet |
| --- | --- | --- |
| Mail | et sprogkort, en række i trin 1, bladreren | bliver på Mail, ny person |
| Prislister | det samme | bliver på Prislister, ny person |
| Modtagere | en person | går tilbage til den fane, du kom fra (Mail eller Prislister) |

Modtagere er den eneste undtagelse, fordi den ikke er noget at læse — den er en måde at vælge en person på. Hvem de andre faner viser, har en blå ring om ansigtet, den samme ring som på sprogkortene.

Første udgave lod trinnet bestemme fanen (`LetterFirst`: prisliste på trin 1, mail på trin 2). Det lød logisk, men den, der sad og tjekkede prislister på trin 2, blev revet over på Mail ved hvert klik.

## Trinnet starter udfyldt

```csharp
"template" => Langs.Length > 0 && Langs.All(l => LetterFor(l).Subject.Trim().Length > 0),
```

Skabelonerne kommer skrevet, så trinnet er færdigt, i det øjeblik der er modtagere. Det er sandheden: der *er* et brev til alle. Det går først i stykker, hvis du tømmer et emnefelt — og så siger fejlen hvilket sprog.

## Afsendelsen

Tre kontakter, og en linje der siger hvad der sker:

```
8 mails med 7 prislister, skrevet på 3 sprog.

[◉] Læg den i CRM          Hver mail havner på kundens kort.
[ ] Gem en kopi lokalt     Mailen og arkene i udsendelsesmappen.
[ ] Send en kopi til mig selv
```

**Kopi til mig selv tæller ikke som et sted.** Slår man både CRM og lokal kopi fra, er der ingen udsendelse — og det siger trinnet, når man prøver at gå videre: *"Vælg mindst ét sted, udsendelsen skal ende."*

## Brevet, der blev sendt, er det brev, der blev sendt

`Letter` kopieres ind i udsendelsen ved afsendelsen, på samme måde som prislistens linjer:

```csharp
public record Letter(string Lang, string Subject, string Body);
```

Retter nogen skabelonen næste måned, ændrer det ikke, hvad der står i en udsendelse fra i går. `Mail.LetterFor(person)` finder personens sprog og sætter navn og firma ind; findes der intet brev — en udsendelse fra før skabelonerne — falder den tilbage på udsendelsens overskrift.

## Faldgruber

**Klassenavne, der ligner hinanden.** Bladreren fandtes allerede som `.sheet-nav` / `.sheet-count` / `.sheet-next`; jeg skrev `.sheet-pager` og `.sheet-pos`, som ikke er stylet nogen steder, og de to pile stablede sig oven på hinanden. Kig efter kontrollen, før du navngiver den igen.

**Et `@` lige efter et ord** læser Razor som en mailadresse. `kunder@(Used(group))`, ikke `kunder@Used(group)`.

**"Den dansk skabelon".** Fejlteksten byggede adjektivet af sprognavnet uden bøjning. Efter *den* skal det hedde *danske*, *svenske*, *engelske* — et `e` bag på navnet.

**Klikbare ansigter på et klikbart kort.** Første udgave lod navnet være kortets knap og ansigterne være knapper ovenpå. Det virkede, men kortet betød to ting afhængigt af, hvor man ramte. Nu er hele kortet én `<button>`, og ansigterne er `aria-hidden`.
