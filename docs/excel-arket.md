# Prislisten som Excel-ark

Hver kunde får sin egen prisliste vedhæftet mailen. Arkene er **rigtige .xlsx-filer**, de læses og skrives med **Microsofts Open XML SDK**, og de vises i appen som et regneark.

Filer: `Model/Accounting.cs` (læs og skriv), `Components/MailView.razor` (visning og redigering), `wwwroot/sheets/` (arkene), `tools/make-sheets.py` (laver prøvearkene), `wwwroot/js/app.js` (gemmer filen).

---

## Pakken

```xml
<PackageReference Include="DocumentFormat.OpenXml" Version="3.5.1" />
```

Det er Microsofts eget SDK, MIT-licenseret, fra `github.com/dotnet/Open-XML-SDK`. Det er projektets eneste tredjepartspakke, og det kører i browseren i WebAssembly — også i en udgivet, trimmet Release-udgave.

**Hvad det gør:** læser og skriver .xlsx.
**Hvad det ikke gør:** det *regner ikke*, og det *tegner ikke*. En formelcelle indeholder kun det, Excel sidst gemte, og der findes ingen Microsoft-pakke, der tegner et regneark i din egen side. Det er derfor visningen er vores egen — se [Visningen](#visningen) — og hvorfor Office på nettet er et selvstændigt valg, se [Excel for the web](#excel-for-the-web).

**Hvad det koster i hentetid.** Målt på `dotnet publish -c Release`, komprimeret som en server sender det:

```
DocumentFormat.OpenXml     810 KB
  …Framework                70 KB
System.IO.Packaging         22 KB
System.IO.Compression       28 KB
────────────────────────────────
i alt                      930 KB   af 3,2 MB
```

Den udgivne udgave booter på ~1,2 s lokalt. Trimming fjerner ikke meget af SDK'et, fordi det bruger refleksion — men det virker efter trimning, hvilket er det, der plejer at knække.

## Arkene

Seks filer i `wwwroot/sheets/`, én pr. kunde, plus en oversigt:

```json
[
  { "file": "priser-vela-robotics.xlsx" },
  { "file": "priser-halden-co.xlsx" }
]
```

Appen spørger `index.json` først og henter kun de filer, der står der. Alternativet — at prøve hver kunde og lade den fejle med 404 — virker også, men skriver en fejl i konsollen for hver kunde, der endnu ikke er prissat. Solberg Media har med vilje ingen fil, så appen kan sige "Ingen prisliste i regnskabssystemet".

Filnavnet dannes af firmanavnet:

```csharp
public static string FileFor(string company) => $"priser-{Slug(company)}.xlsx";
```

`tools/make-sheets.py` laver prøvearkene med openpyxl: fed overskriftsrække, `=B2*C2` på hver linje, `=SUM(D2:D…)` nederst, `#,##0` på beløbene, arket hedder "Priser". Kør den fra repoets rod. Den bevarer eventuelle `url`-felter i `index.json`.

## Læsningen

Alt læses én gang, før første skærm, så resten af appen kan stille et almindeligt spørgsmål og få et svar uden at vente:

```csharp
var host = builder.Build();

using (var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) })
{
    await Accounting.LoadAll(http, MarketingGroup.All.Select(p => p.Company));
}

await host.RunAsync();
```

Selve læsningen er SDK'et:

```csharp
using var doc = SpreadsheetDocument.Open(file, false);
var workbook = doc.WorkbookPart ?? throw new InvalidDataException("No workbook part.");

var tabs = (workbook.Workbook?.Sheets?.Elements<XlSheet>() ?? [])
    .Select(entry => ReadTab(workbook, entry, strings, bold))
    .ToArray();
```

og et ark ad gangen:

```csharp
var part = (WorksheetPart)workbook.GetPartById(entry.Id!);

foreach (var row in part.Worksheet?.Descendants<Row>() ?? [])
{
    var at = (int)(row.RowIndex?.Value ?? (uint)(tab.Rows.Count + 1)) - 1;

    foreach (var cell in row.Elements<XlCell>())
    {
        tab.Put(at, Column(cell), new Cell(
            CellText(cell, strings),
            cell.CellFormula?.Text,
            bold.Contains(cell.StyleIndex?.Value ?? 0)));
    }
}
```

To ting, man skal vide om formatet:

- **Tekst ligger i en delt tabel**, ikke i cellen. Er `cell.DataType` `SharedString`, er celleværdien et *indeks* ind i `SharedStringTable`.
- **Cellen kender sin adresse**, ikke sin kolonne. `cell.CellReference` er `"B7"`; bogstaverne står forrest, så tallene afslutter dem:

```csharp
private static int Column(XlCell cell)
{
    var reference = cell.CellReference?.Value ?? "";
    var letters = reference.TakeWhile(char.IsLetter).Count();

    return reference[..letters].ToUpperInvariant()
        .Aggregate(0, (value, letter) => value * 26 + (letter - 'A' + 1)) - 1;
}
```

**Hele projektmappen læses**, ark for ark, og hvert ark bliver til en `Tab` med rækker af `Cell`. En celle husker både det, der står i den, og formlen den blev skrevet som — og om den er fed, så en overskrift stadig er en overskrift, når filen skrives tilbage.

En **linje** i mailens forstand er en række med et antal i B og en pris i C. Den regel springer både overskriften og sumrækken over, uden at nogen skal tælle rækker.

### Formlerne

SDK'et regner ikke, så appen gør det selv — i `Model/Sheet.cs`, og kun for de to formler, disse ark er skrevet med:

```csharp
// =SUM(D2:D7)
if (formula.StartsWith("SUM(", …)) { … }

// =B2*C2
var parts = formula.Split('*');
```

Alt andet vises, som det blev skrevet (`=NOGET(…)`), i stedet for at stå tomt. Det er ærligere end at lade som om, vi har en regnemaskine.

## Visningen

Fanen *Vedhæftning* viser arket som et regneark: kolonnebogstaver, rækketal, ruller i hver celle, fed overskriftslinje, sum, og ruller der fortsætter under tallene — fyrre tomme rækker, fordi et regneark ikke stopper, hvor tallene gør.

```razor
<tr class="xl-head" aria-hidden="true">
    <td class="xl-corner"></td><td>A</td><td>B</td><td>C</td><td>D</td>
</tr>
```

Bogstaverne og rækketallene er regnearkets møbler, ikke indhold, så de er `aria-hidden` — en skærmlæser får overskriftsrækken i stedet.

**Arkfanerne i bunden er filens egne ark.** En projektmappe er ikke ét ark: prøvefilerne har *Priser* og *Vilkår*, og fanerne kommer fra `Accounting.TabsFor(firma)` i den rækkefølge, de står i filen. Modtageren skiftes med pilene øverst; arket skiftes med fanerne nederst.

Den åbne fane er hvid med en streg under — bund og farve, aldrig vægt, så de andre faner holder sig i ro. Til højre står filstørrelsen.

**Overskriften står fast.** Kolonnebogstaverne og overskriftsrækken er frosne i toppen, og rækketallene i venstre side — ruller man, er det kun cellerne, der flytter sig:

```css
.xl-t .xl-head td { position: sticky; top: 0; z-index: 3; }
.xl-t tbody tr:first-child > * { position: sticky; top: 22px; z-index: 2; background: var(--surface); }
.xl-t .xl-n { position: sticky; left: 0; z-index: 1; }
```

De frosne celler streger sig selv op med en indre skygge i stedet for en kant: i en tabel med `border-collapse: collapse` hører kanten til tabellen og ruller væk med den. Og selektorerne skal være stærke nok — `.xl-t td { position: relative }` (som autofilteret bruger) står senere i filen og vinder ellers over `position: sticky`.

**Gitteret viser cellerne, ikke en fortolkning af dem.** Kolonnebogstaverne dannes af `Accounting.ColumnName(0) == "A"`, rækketallene af rækkens plads, og hver celle vises gennem `Formulas.Display`. Er der fed i filen, er der fed i gitteret.

**Kolonnerne er så brede, som filen siger.** Excel gemmer bredden i tegn — `A` er 34, `B` er 8 — og skriver dem som spænd ("kolonne 3 til 4 er 12 brede"), så de foldes ud én kolonne ad gangen og regnes om til pixels med den sædvanlige omregning, syv pr. tegn plus cellens egen luft:

```csharp
public static int WidthPx(Tab tab, int column)
{
    var width = column < tab.Widths.Length ? tab.Widths[column] : 0;
    return width <= 0 ? 64 : (int)Math.Round(width * 7 + 5);
}
```

Efter arkets egne kolonner følger otte tomme af Excels standardbredde og til sidst én, der tager resten af ruden. Så fortsætter gitteret til højre, ligesom det fortsætter nedad, og cellerne holder deres form, uanset hvor bred ruden er. Tabellen skal have `width: max-content`, ellers presser et `table-layout: fixed` kolonnerne sammen for at passe i ruden i stedet for at lade gitteret rulle. Autofilteret sidder kun i de kolonner, arket faktisk bruger.

### Sortering og filter i arket

Overskriftsrækken har Excels autofilter: en lille knap i hver celle, der åbner kolonnens egen sortering og filter.

- **Sortér** flytter rækkerne i arket. En flyttet række tager sine formler med, så referencerne i dem skrives om til den række, de lander på — ellers ville `=B2*C2` blive stående og pege på en anden linje. Sumrækken under området bliver, hvor den er, og peger stadig på det samme område.
- **Vis** er kolonnens egne værdier med flueben. Skjulte rækker forsvinder, og rækketallene springer — som i Excel, hvor de netop fortæller, at noget er filtreret fra.
- **Området**, sortering og filter arbejder på, er rækkerne under overskriften ned til den første tomme eller fede række. En sum er fed, og den bliver derfor liggende.

**Panelet er en søjle**, så arket får hele bredden og hele højden under modtagerens navn:

```css
.split-detail:has(.d-panel-file) { overflow-y: hidden; scrollbar-gutter: auto; }
.split-detail:has(.d-panel-file) .d { height: 100%; }
.d-panel-file { flex: 1; min-height: 0; display: flex; flex-direction: column; margin: 0 -22px -40px; }
.xl { flex: 1; min-height: 0; display: flex; flex-direction: column; }
.xl-grid { flex: 1; min-height: 0; overflow: auto; }
```

Bemærk `height: 100%` og ikke bare `min-height`. Rækkerne løber forbi bunden af ruden, og en søjle med kun et minimum vokser med dem og bærer fanestrimlen ud af syne.

## Redigering og gem

En **kladdes** ark kan rettes; et **sendt** ark kan kun læses — det, der gik ud, er det, der gik ud. Alle celler er felter, også de tomme, og det, man skriver, lander i cellen:

```csharp
public static void SetCell(string company, int tab, int row, int column, string typed)
{
    var was = book.Tabs[tab].At(row, column);
    var text = typed.Trim();

    book.Tabs[tab].Put(row, column, text.StartsWith('=')
        ? new Cell("", text[1..], was.Bold)      // en formel, som i Excel
        : new Cell(text, null, was.Bold));

    Books[company] = book with { Edited = true };
}
```

Skriver du `=B2*C2` i en celle, er det en formel — den regnes ud i gitteret og skrives som formel, når arket gemmes.

**Cellen viser sin værdi, ikke sin formel.** Dobbeltklikker du, bytter den om og viser formlen, som Excel gør, når man åbner en celle. Det er den samme formel, der ender i filen:

```razor
<input value="@(Editing(line, col) ? typedText : shownText)"
       @ondblclick="() => editing = (line, col)"
       @onchange="e => Typed(rec.Company, line, col, e.Value?.ToString() ?? string.Empty)"
       @onblur="() => editing = null" />
```

Rækkefølgen af browserens hændelser bærer det: `change` kommer før `blur`, så rettelsen er gemt, inden cellen falder tilbage til sin værdi.

Rettelsen lander i regnskabssystemets ark, og alt der læser derfra følger med ved næste tegning: gitterets linjetotal og sum, modtagerens beløb i *Modtagere*-fanen, udsendelsens total ude i listen. `SheetEdited` er en `EventCallback`, som `NewMail` bruger til at bygge kladden op igen — den komponeres af arkene, så det er nok at sige, at de har ændret sig.

Indtil arket gemmes, står der **"ikke gemt"** i strimlen, for filen på disken er stadig den gamle.

**Gem** skriver en rigtig .xlsx med det samme SDK, den anden vej — hele projektmappen, ark for ark:

```csharp
using (var doc = SpreadsheetDocument.Create(memory, SpreadsheetDocumentType.Workbook))
{
    var workbook = doc.AddWorkbookPart();
    workbook.Workbook = new Workbook();
    workbook.AddNewPart<WorkbookStylesPart>().Stylesheet = Styles();

    var sheetPart = workbook.AddNewPart<WorksheetPart>();
    var rows = new SheetData();
    sheetPart.Worksheet = new Worksheet(Widths(), rows);
    …
    rows.Append(new Row(
        Str($"A{index}", line.Description), Num($"B{index}", line.Qty),
        Num($"C{index}", line.Unit, Money), Formula($"D{index}", $"B{index}*C{index}", Money)) { RowIndex = index });
}
```

Regnestykkerne skrives som **formler**, ikke som tal, så Excel selv regner, når filen åbnes, og bliver ved med at virke, hvis nogen retter videre derinde. Stilarket skal have den `Normal`-stil og de to fills, Excel forventer at finde; uden dem tilbyder Excel at reparere filen på vej ind.

Filen rækkes til browseren af fire linjer JavaScript:

```js
saveFile(name, bytes, type) {
  const url = URL.createObjectURL(new Blob([bytes], { type }));
  const a = document.createElement('a');
  a.href = url; a.download = name; a.click();
  URL.revokeObjectURL(url);
}
```

Blazor sender `byte[]` over som en `Uint8Array`. Filen lander i Downloads. Skal den skrives tilbage på plads, er der tre veje: browserens File System Access API (Chrome og Edge, ingen server), et lille API hos jer, eller regnskabssystemet selv.

**Prøvet af:** ret 12 til 15 i appen, gem, læs filen igen — arket hedder stadig "Priser", værdierne er de nye, og `=B2*C2` og `=SUM(D2:D3)` står der stadig.

## Excel for the web

Har et ark en adresse, Microsoft kan nå, viser fanen **Office på nettet** i stedet for vores gitter — rigtig Excel, i en iframe, i fuld bredde og højde:

```json
{ "file": "priser-vela-robotics.xlsx",
  "url": "https://firma.sharepoint.com/:x:/g/…&action=embedview" }
```

```csharp
public static string? ViewerFor(string company)
{
    if (!Books.TryGetValue(company, out var book) || string.IsNullOrWhiteSpace(book.Url)) return null;

    var url = book.Url.Trim();
    return url.Contains("embed", StringComparison.OrdinalIgnoreCase)
        ? url
        : $"https://view.officeapps.live.com/op/embed.aspx?src={Uri.EscapeDataString(url)}";
}
```

- **Et SharePoint- eller OneDrive-embedlink** bruges som det er. Filen bliver i jeres eget M365, bag jeres login, og det er Microsoft, der tegner den. Det er vejen til rigtige kundepriser.
- **Enhver anden adresse** sendes til `view.officeapps.live.com`, som selv henter filen. Det kræver en offentlig URL, og prisdata forlader huset — kun til prøvetal.
- **Uden link** tegner appen arket selv. Det er også offline-udgaven.

En iframe kan ikke i sig selv vise en .xlsx; peger man den på filen, downloader browseren den bare. Det er kun Office på nettet, der gør en iframe til et regneark.

## Licenser

Ingen betalte pakker, ingen community-udgave med hale:

| | |
| --- | --- |
| DocumentFormat.OpenXml 3.5.1 | Microsoft, MIT |
| Blazor WebAssembly 8.0.30 | Microsoft, MIT |
| JS-biblioteker i `wwwroot` | ingen |

Fravalgt undervejs: **SheetJS** (community-udgaven lægger op til betaling), **Syncfusion** (kommerciel licens), **Luckysheet** (MIT, men arkiveret og 3 MB), **Univer** (import kræver deres backend og indeholder lukket kode).
