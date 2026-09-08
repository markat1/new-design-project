#!/usr/bin/env python3
"""Generate the Markdown guide from the HTML one.

The HTML is the source: it carries the print styling the PDF needs. Writing the
same content twice by hand would let the two drift, so this converts instead.

    python3 docs/guide-to-markdown.py
"""
import html
import re
from pathlib import Path

HERE = Path(__file__).parent
SRC = HERE / "blazor-checklist-guide.html"
OUT = HERE / "blazor-checklist-guide.md"


def strip_tags(s):
    s = re.sub(r"<span class=\"file\">(.*?)</span>", r"\1\n", s, flags=re.S)
    s = re.sub(r"<br\s*/?>", "\n", s)
    s = re.sub(r"<[^>]+>", "", s)
    return html.unescape(s)


def inline(s):
    """Inline markup that survives into Markdown."""
    # Code spans are parked behind placeholders first. Their content unescapes to
    # things like <PropertyGroup>, which the final tag strip would otherwise eat.
    parked = []

    def park(m):
        parked.append(strip_tags(m.group(1)).strip())
        return f"\x00{len(parked) - 1}\x00"

    s = re.sub(r"<code>(.*?)</code>", park, s, flags=re.S)
    s = re.sub(r"<kbd>(.*?)</kbd>", park, s, flags=re.S)

    # The number badge on a heading is a separate span; keep it as a prefix.
    s = re.sub(r"<span class=\"(?:n|step)\">(.*?)</span>", r"\1. ", s, flags=re.S)

    s = re.sub(r"<b>(.*?)</b>", r"**\1**", s, flags=re.S)
    s = re.sub(r"<i>(.*?)</i>", r"*\1*", s, flags=re.S)

    s = re.sub(r"\s+", " ", strip_tags(s)).strip()
    s = re.sub(r"\x00(\d+)\x00", lambda m: "`" + parked[int(m.group(1))] + "`", s)
    return s


def code_block(body):
    label = ""
    f = re.search(r"<span class=\"file\">(.*?)</span>", body, re.S)
    if f:
        label = strip_tags(f.group(1)).strip()
        body = body.replace(f.group(0), "")
    # diff spans become +/- prefixes so the intent survives without colour
    body = re.sub(r"<span class=\"del\">(.*?)</span>",
                  lambda x: "\n".join("- " + l for l in strip_tags(x.group(1)).split("\n")), body, flags=re.S)
    body = re.sub(r"<span class=\"add\">(.*?)</span>",
                  lambda x: "\n".join("+ " + l for l in strip_tags(x.group(1)).split("\n")), body, flags=re.S)
    text = strip_tags(body).strip("\n")
    head = f"*{label}*\n\n" if label else ""
    return f"{head}```\n{text}\n```\n"


def convert(src):
    body = src[src.index("<body>") + 6: src.index("</body>")]
    out = []

    cover = re.search(r"<section class=\"cover\">(.*?)</section>", body, re.S)
    if cover:
        c = cover.group(1)
        h1 = inline(re.search(r"<h1>(.*?)</h1>", c, re.S).group(1))
        out.append(f"# {h1}\n")
        for p in re.findall(r"<p[^>]*>(.*?)</p>", c, re.S):
            out.append(inline(p) + "\n")
        meta = re.search(r"<div class=\"meta\">(.*?)</div>", c, re.S)
        if meta:
            out.append(f"*{inline(meta.group(1))}*\n")
        out.append("---\n")
        body = body[cover.end():]

    # one regex walk, so document order is preserved
    pattern = re.compile(
        r"<h2[^>]*>(?P<h2>.*?)</h2>"
        r"|<h3[^>]*>(?P<h3>.*?)</h3>"
        r"|<pre><code>(?P<pre>.*?)</code></pre>"
        r"|<table>(?P<table>.*?)</table>"
        r"|<div class=\"callout(?P<warn> warn)?\">(?P<callout>.*?)</div>"
        r"|<p class=\"why\">(?P<why>.*?)</p>"
        r"|<p[^>]*>(?P<p>.*?)</p>"
        r"|<ul class=\"check\">(?P<check>.*?)</ul>"
        r"|<(?:ul|ol)[^>]*>(?P<list>.*?)</(?:ul|ol)>",
        re.S)

    for m in pattern.finditer(body):
        if m.group("h2"):
            out.append(f"\n## {inline(m.group('h2'))}\n")
        elif m.group("h3"):
            out.append(f"\n### {inline(m.group('h3'))}\n")
        elif m.group("pre"):
            out.append(code_block(m.group("pre")))
        elif m.group("table"):
            rows = re.findall(r"<tr>(.*?)</tr>", m.group("table"), re.S)
            lines = []
            for i, r in enumerate(rows):
                cells = [inline(c) for c in re.findall(r"<t[hd][^>]*>(.*?)</t[hd]>", r, re.S)]
                lines.append("| " + " | ".join(cells) + " |")
                if i == 0:
                    lines.append("|" + "---|" * len(cells))
            out.append("\n".join(lines) + "\n")
        elif m.group("callout"):
            mark = "> [!WARNING]\n" if m.group("warn") else "> [!NOTE]\n"
            out.append(mark + "> " + inline(m.group("callout")) + "\n")
        elif m.group("why"):
            out.append("> " + inline(m.group("why")) + "\n")
        elif m.group("check"):
            items = re.findall(r"<li>(.*?)</li>", m.group("check"), re.S)
            out.append("\n".join(f"- [ ] {inline(i)}" for i in items) + "\n")
        elif m.group("list"):
            items = re.findall(r"<li>(.*?)</li>", m.group("list"), re.S)
            out.append("\n".join(f"- {inline(i)}" for i in items) + "\n")
        elif m.group("p"):
            txt = inline(m.group("p"))
            if txt:
                out.append(txt + "\n")

    tree = re.search(r"<div class=\"tree\"><pre><code>(.*?)</code></pre></div>", src, re.S)
    if tree:
        out.append("```\n" + strip_tags(tree.group(1)).strip() + "\n```\n")

    return "\n".join(out)


if __name__ == "__main__":
    md = convert(SRC.read_text())
    md = re.sub(r"\n{3,}", "\n\n", md).strip() + "\n"
    OUT.write_text(md)
    print(f"{OUT.name}: {len(md.splitlines())} linjer")
