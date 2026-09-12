"""Writes the sample price sheets the app reads.

The app never authors a workbook — the accounting system does. These files
stand in for that system, so they are real .xlsx: a bold header row, a line
per item with =B*C, and =SUM() underneath. The app reads them with Microsoft's
Open XML SDK, and that SDK does not recalculate, so it takes the quantity and
the unit price and does the arithmetic itself.

    python3 tools/make-sheets.py

Needs openpyxl. Run it from the repository root; it writes into
OnboardingChecklist/wwwroot/sheets/ and rewrites index.json, which is the list
the app asks for first — a customer missing from it has no sheet.

An entry can also carry a "url", and then the attachment tab frames Office for
the web on that address instead of drawing the sheet itself:

    { "file": "priser-vela-robotics.xlsx",
      "url": "https://firma.sharepoint.com/:x:/g/…&action=embedview" }

A SharePoint or OneDrive embed link is used as it is, and stays behind the
tenant's sign-in. Any other address is handed to view.officeapps.live.com,
which means Microsoft's servers fetch the file themselves — fine for sample
prices on a public host, not for a customer's. Existing urls survive a rerun.
"""

import glob
import json
import os
import re

from openpyxl import Workbook
from openpyxl.styles import Font

OUT = "OnboardingChecklist/wwwroot/sheets"

SHEETS = {
    "Featherstonehaugh-Villanueva International Systems": [
        ("Implementering, fase 1", 1, 28000),
        ("Licens pr. bruger", 40, 380),
        ("Support, årligt", 1, 5000),
    ],
    "Vela Robotics": [
        ("Servicebesøg", 12, 850),
        ("Reservedelslager", 1, 2400),
    ],
    "Halden & Co.": [("Konsulenttime", 8, 1175)],
    "Ferrous Manufacturing Group": [("Wartung, monatlich", 12, 1000)],
    "Kestrel Analytics": [
        ("Platform, årligt", 1, 61000),
        ("Onboarding", 1, 12000),
    ],
    "Bright Harbour Logistics": [
        ("Palleplads pr. måned", 40, 320),
        ("Håndtering", 1, 3200),
    ],
    # Solberg Media on purpose has none: the app has to say so.
}


def slug(name):
    """Same rule as Accounting.Slug in C#, including the collapsed runs."""
    return re.sub("-{2,}", "-", "".join(c if c.isalnum() else "-" for c in name.lower())).strip("-")


def write(company, lines):
    book = Workbook()
    sheet = book.active
    sheet.title = "Priser"

    sheet.append(["Beskrivelse", "Antal", "Stykpris", "I alt"])
    for cell in sheet[1]:
        cell.font = Font(bold=True)

    for row, (description, qty, unit) in enumerate(lines, start=2):
        sheet.append([description, qty, unit, f"=B{row}*C{row}"])
        sheet[f"C{row}"].number_format = "#,##0"
        sheet[f"D{row}"].number_format = "#,##0"

    last = len(lines) + 1
    total = last + 1
    sheet.append(["I alt", None, None, f"=SUM(D2:D{last})"])
    for cell in sheet[total]:
        cell.font = Font(bold=True)
    sheet[f"D{total}"].number_format = "#,##0"

    for column, width in (("A", 34), ("B", 8), ("C", 12), ("D", 12)):
        sheet.column_dimensions[column].width = width

    # A workbook is not one sheet. The terms live on their own tab, the way an
    # accountant keeps them, so the app has more than one to show.
    terms = book.create_sheet("Vilkår")
    terms.append(["Vilkår", "Værdi"])
    for cell in terms[1]:
        cell.font = Font(bold=True)
    terms.append(["Betaling", "30 dage netto"])
    terms.append(["Priserne gælder til", "31-12-2027"])
    terms.append(["Valuta", "DKK"])
    terms.column_dimensions["A"].width = 24
    terms.column_dimensions["B"].width = 20

    path = f"{OUT}/priser-{slug(company)}.xlsx"
    book.save(path)
    return path


os.makedirs(OUT, exist_ok=True)
for company, lines in SHEETS.items():
    path = write(company, lines)
    print(f"{os.path.getsize(path):>6} B  {path}")

# Keep whatever links are already in the index; only the file list is rebuilt.
links = {}
if os.path.exists(f"{OUT}/index.json"):
    for entry in json.load(open(f"{OUT}/index.json")):
        if isinstance(entry, dict) and entry.get("url"):
            links[entry["file"]] = entry["url"]

made = sorted(os.path.basename(p) for p in glob.glob(f"{OUT}/*.xlsx"))
listed = [{"file": f, **({"url": links[f]} if f in links else {})} for f in made]
with open(f"{OUT}/index.json", "w") as index:
    json.dump(listed, index, indent=2, ensure_ascii=False)
print(f"{len(made)} in index.json, {len(links)} with a viewer link")
