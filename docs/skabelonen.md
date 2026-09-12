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

Derfor findes der ingen "vælg skabelon"-kontrol. Der er en fane pr. sprog, **der faktisk er i spil** — er hele udsendelsen dansk, er der én fane:

```
┌ Dansk 2 ┬ Svensk 2 ┬ Engelsk 4 ┐
│ Emne   [Era priser för 2027            ]
│ Tekst  [Hej {navn}                     ]
│        [                               ]
│ {navn} og {firma} sættes ind pr. modtager.
```

Tallet på fanen er modtagere, ikke sprog. Fire engelske betyder fire mennesker, ikke fire breve.

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

- **Klik en sprogfane** → vinduet stiller sig på en, der taler det sprog. Et brev uden en læser på den anden side af ruden er bare tekst.
- **Klik en person i vinduet** → formularens fane flytter sig til det brev, personen rent faktisk får. Klik en svensker, og du redigerer den svenske skabelon.
- **Bladreren `‹ ›`** går gennem alle modtagere, og sproget skifter med dem.

Teknisk er det én besked hver vej. `MailView` råber op, hvem den viser (`Showed`), siden giver det videre til udkastet, og `Fields` retter sin fane ind efter det:

```csharp
protected override void OnParametersSet()
{
    if (S.Show is not { } ask || ask.Nonce == seenShow) return;

    seenShow = ask.Nonce;

    if (S.Recipients.FirstOrDefault(r => r.Email == ask.Email) is { } who) langShown = who.Lang;
}
```

**Hvilken rude et klik åbner, er trinnets sag, ikke klikkets.** På trin 1 vælger du modtagere, så et klik på en person viser deres prisliste. På trin 2 skriver du brevet, så det samme klik viser deres mail. Det er `LetterFirst`, som siden sætter ud fra `S.StepKey`.

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
