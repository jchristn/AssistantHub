#!/usr/bin/env python3
"""Build the Meridian benchmark dataset.

Renders every source document in src/<department>/ to the format its manifest entry asks for (md, txt, html, pdf,
docx) under files/, and merges the three manifests and question files into ../meridian.json (one corpus).

Standard library only, so the build is reproducible anywhere:
  - pdf:  a minimal PDF 1.4 writer using the built-in Helvetica fonts (WinAnsi encoding), text wrapped per line;
          tables are rendered one row per line with " | " between cells.
  - docx: minimal WordprocessingML (document, styles, content types) with real tables.
  - txt:  Markdown with syntax removed.

Usage: python benchmarks/datasets/meridian/build.py
"""

import json
import os
import re
import sys
import zipfile
from xml.sax.saxutils import escape

HERE = os.path.dirname(os.path.abspath(__file__))
SRC = os.path.join(HERE, "src")
FILES = os.path.join(HERE, "files")
OUTPUT = os.path.join(HERE, "..", "meridian.json")
DEPARTMENTS = ["hr", "products", "engineering"]
CONTENT_TYPES = {
    "md": "text/markdown",
    "txt": "text/plain",
    "html": "text/html",
    "pdf": "application/pdf",
    "docx": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
}


# --- Markdown parsing -------------------------------------------------------------------------------------------------

def inline(text):
    """Strip inline Markdown: bold/italic markers, code ticks, links."""
    text = re.sub(r"\[([^\]]+)\]\([^)]+\)", r"\1", text)
    text = re.sub(r"\*\*([^*]+)\*\*", r"\1", text)
    text = re.sub(r"__([^_]+)__", r"\1", text)
    text = re.sub(r"(?<![\w*])\*([^*\n]+)\*(?!\w)", r"\1", text)
    text = text.replace("`", "")
    return text


def blocks(markdown):
    """Parse restricted Markdown into (kind, payload) blocks: heading, para, bullet, number, table."""
    result = []
    paragraph = []
    table = []

    def flush():
        if paragraph:
            result.append(("para", inline(" ".join(paragraph))))
            paragraph.clear()
        if table:
            rows = [r for r in table if not re.match(r"^\s*\|?\s*:?-{2,}", r)]
            parsed = [[inline(c.strip()) for c in r.strip().strip("|").split("|")] for r in rows]
            result.append(("table", parsed))
            table.clear()

    for raw in markdown.splitlines():
        line = raw.rstrip()
        stripped = line.strip()
        if not stripped:
            flush()
            continue
        if stripped.startswith("|"):
            if paragraph:
                flush()
            table.append(stripped)
            continue
        if table:
            flush()
        heading = re.match(r"^(#{1,6})\s+(.*)$", stripped)
        if heading:
            flush()
            result.append(("heading", (len(heading.group(1)), inline(heading.group(2)))))
            continue
        bullet = re.match(r"^[-*]\s+(.*)$", stripped)
        if bullet:
            flush()
            result.append(("bullet", inline(bullet.group(1))))
            continue
        number = re.match(r"^(\d+)[.)]\s+(.*)$", stripped)
        if number:
            flush()
            result.append(("number", (number.group(1), inline(number.group(2)))))
            continue
        paragraph.append(stripped)
    flush()
    return result


def to_text(markdown):
    lines = []
    for kind, payload in blocks(markdown):
        if kind == "heading":
            lines.append(payload[1])
        elif kind == "para":
            lines.append(payload)
        elif kind == "bullet":
            lines.append("- " + payload)
        elif kind == "number":
            lines.append(payload[0] + ". " + payload[1])
        elif kind == "table":
            lines.extend(" | ".join(row) for row in payload)
        lines.append("")
    return "\n".join(lines).strip() + "\n"


# --- PDF --------------------------------------------------------------------------------------------------------------

def wrap(text, width):
    words = text.split()
    lines, current = [], ""
    for word in words:
        if len(current) + len(word) + (1 if current else 0) > width:
            if current:
                lines.append(current)
            current = word
        else:
            current = current + " " + word if current else word
    if current:
        lines.append(current)
    return lines or [""]


def pdf_escape(text):
    data = text.encode("cp1252", errors="replace")
    out = []
    for b in data:
        c = chr(b)
        if c in "()\\":
            out.append("\\" + c)
        elif b < 32 or b > 126:
            out.append("\\%03o" % b)
        else:
            out.append(c)
    return "".join(out)


def to_pdf(markdown, path):
    # (font, size, text) lines; F1 Helvetica, F2 Helvetica-Bold.
    lines = []
    for kind, payload in blocks(markdown):
        if kind == "heading":
            size = {1: 16, 2: 13, 3: 11}.get(payload[0], 11)
            for chunk in wrap(payload[1], int(95 * 10 / size)):
                lines.append(("F2", size, chunk))
            lines.append(("F1", 6, ""))
        elif kind == "para":
            for chunk in wrap(payload, 95):
                lines.append(("F1", 10, chunk))
            lines.append(("F1", 6, ""))
        elif kind in ("bullet", "number"):
            prefix = "- " if kind == "bullet" else payload[0] + ". "
            text = payload if kind == "bullet" else payload[1]
            wrapped = wrap(text, 90)
            lines.append(("F1", 10, prefix + wrapped[0]))
            for chunk in wrapped[1:]:
                lines.append(("F1", 10, "   " + chunk))
        elif kind == "table":
            for i, row in enumerate(payload):
                for chunk in wrap(" | ".join(row), 95):
                    lines.append(("F2" if i == 0 else "F1", 9, chunk))
            lines.append(("F1", 6, ""))

    pages, page, y = [], [], 742
    for font, size, text in lines:
        leading = size + 3
        if y - leading < 54:
            pages.append(page)
            page, y = [], 742
        y -= leading
        if text:
            page.append("BT /%s %d Tf 54 %d Td (%s) Tj ET" % (font, size, y, pdf_escape(text)))
    pages.append(page)

    objects = []

    def add(body):
        objects.append(body)
        return len(objects)

    font1 = add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>")
    font2 = add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>")
    pages_id = len(objects) + 1
    objects.append(None)  # placeholder for the pages tree
    kids = []
    for content in pages:
        stream = "\n".join(content)
        content_id = add("<< /Length %d >>\nstream\n%s\nendstream" % (len(stream.encode("latin-1")), stream))
        page_id = add("<< /Type /Page /Parent %d 0 R /MediaBox [0 0 612 792] /Contents %d 0 R /Resources << /Font << /F1 %d 0 R /F2 %d 0 R >> >> >>"
                      % (pages_id, content_id, font1, font2))
        kids.append(page_id)
    objects[pages_id - 1] = "<< /Type /Pages /Kids [%s] /Count %d >>" % (" ".join("%d 0 R" % k for k in kids), len(kids))
    catalog = add("<< /Type /Catalog /Pages %d 0 R >>" % pages_id)

    out = bytearray(b"%PDF-1.4\n")
    offsets = []
    for i, body in enumerate(objects, start=1):
        offsets.append(len(out))
        out += ("%d 0 obj\n%s\nendobj\n" % (i, body)).encode("latin-1")
    xref = len(out)
    out += ("xref\n0 %d\n0000000000 65535 f \n" % (len(objects) + 1)).encode("latin-1")
    for offset in offsets:
        out += ("%010d 00000 n \n" % offset).encode("latin-1")
    out += ("trailer\n<< /Size %d /Root %d 0 R >>\nstartxref\n%d\n%%%%EOF\n" % (len(objects) + 1, catalog, xref)).encode("latin-1")
    with open(path, "wb") as f:
        f.write(out)


# --- DOCX -------------------------------------------------------------------------------------------------------------

W = 'xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"'


def run(text, bold=False, size=None):
    props = ""
    if bold or size:
        props = "<w:rPr>%s%s</w:rPr>" % ("<w:b/>" if bold else "", '<w:sz w:val="%d"/>' % (size * 2) if size else "")
    return '<w:r>%s<w:t xml:space="preserve">%s</w:t></w:r>' % (props, escape(text))


def para(text, style=None, bold=False, size=None):
    ppr = '<w:pPr><w:pStyle w:val="%s"/></w:pPr>' % style if style else ""
    return "<w:p>%s%s</w:p>" % (ppr, run(text, bold, size))


def to_docx(markdown, path, title):
    body = []
    for kind, payload in blocks(markdown):
        if kind == "heading":
            body.append(para(payload[1], "Heading%d" % min(payload[0], 3), True, {1: 16, 2: 13, 3: 11}.get(payload[0], 11)))
        elif kind == "para":
            body.append(para(payload))
        elif kind == "bullet":
            body.append(para("• " + payload, "ListParagraph"))
        elif kind == "number":
            body.append(para(payload[0] + ". " + payload[1], "ListParagraph"))
        elif kind == "table":
            rows = []
            for i, row in enumerate(payload):
                cells = "".join('<w:tc><w:tcPr><w:tcW w:w="0" w:type="auto"/></w:tcPr>%s</w:tc>' % para(cell, bold=(i == 0)) for cell in row)
                rows.append("<w:tr>%s</w:tr>" % cells)
            body.append('<w:tbl><w:tblPr><w:tblStyle w:val="TableGrid"/><w:tblW w:w="0" w:type="auto"/></w:tblPr>%s</w:tbl>' % "".join(rows))
            body.append("<w:p/>")
    document = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:document %s><w:body>%s<w:sectPr/></w:body></w:document>' % (W, "".join(body))
    styles = ('<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:styles %s>'
              '<w:style w:type="paragraph" w:default="1" w:styleId="Normal"><w:name w:val="Normal"/></w:style>'
              '<w:style w:type="paragraph" w:styleId="Heading1"><w:name w:val="heading 1"/><w:basedOn w:val="Normal"/></w:style>'
              '<w:style w:type="paragraph" w:styleId="Heading2"><w:name w:val="heading 2"/><w:basedOn w:val="Normal"/></w:style>'
              '<w:style w:type="paragraph" w:styleId="Heading3"><w:name w:val="heading 3"/><w:basedOn w:val="Normal"/></w:style>'
              '<w:style w:type="paragraph" w:styleId="ListParagraph"><w:name w:val="List Paragraph"/><w:basedOn w:val="Normal"/></w:style>'
              '<w:style w:type="table" w:styleId="TableGrid"><w:name w:val="Table Grid"/></w:style></w:styles>') % W
    content_types = ('<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
                     '<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">'
                     '<Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>'
                     '<Default Extension="xml" ContentType="application/xml"/>'
                     '<Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>'
                     '<Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>'
                     '<Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>'
                     '</Types>')
    rels = ('<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
            '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
            '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>'
            '<Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>'
            '</Relationships>')
    document_rels = ('<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
                     '<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">'
                     '<Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>'
                     '</Relationships>')
    core = ('<?xml version="1.0" encoding="UTF-8" standalone="yes"?>'
            '<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" '
            'xmlns:dc="http://purl.org/dc/elements/1.1/"><dc:title>%s</dc:title><dc:creator>Meridian Instruments</dc:creator></cp:coreProperties>') % escape(title)
    with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as z:
        for name, data in (("[Content_Types].xml", content_types), ("_rels/.rels", rels), ("word/document.xml", document),
                           ("word/_rels/document.xml.rels", document_rels), ("word/styles.xml", styles), ("docProps/core.xml", core)):
            info = zipfile.ZipInfo(name, date_time=(2026, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            z.writestr(info, data)


# --- Build ------------------------------------------------------------------------------------------------------------

def main():
    os.makedirs(FILES, exist_ok=True)
    documents, queries = [], []
    counts = {}
    for department in DEPARTMENTS:
        with open(os.path.join(SRC, "manifest-%s.json" % department), encoding="utf-8") as f:
            manifest = json.load(f)
        for entry in manifest:
            fmt = entry["format"]
            source = os.path.join(SRC, entry["source"])
            with open(source, encoding="utf-8") as f:
                text = f.read()
            name = entry["id"] + "." + fmt
            target = os.path.join(FILES, name)
            if fmt == "md":
                with open(target, "w", encoding="utf-8", newline="\n") as f:
                    f.write(text)
            elif fmt == "html":
                with open(target, "w", encoding="utf-8", newline="\n") as f:
                    f.write(text)
            elif fmt == "txt":
                with open(target, "w", encoding="utf-8", newline="\n") as f:
                    f.write(to_text(text))
            elif fmt == "pdf":
                to_pdf(text, target)
            elif fmt == "docx":
                to_docx(text, target, entry["title"])
            else:
                sys.exit("unknown format %s for %s" % (fmt, entry["id"]))
            counts[fmt] = counts.get(fmt, 0) + 1

            document = {
                "id": entry["id"],
                "title": entry["title"],
                "file": "meridian/files/" + name,
                "contentType": CONTENT_TYPES[fmt],
                "summary": entry.get("summary"),
                "labels": entry.get("labels"),
                "tags": entry.get("tags"),
                "date": entry.get("date"),
                "version": entry.get("version"),
                "supersedes": entry.get("supersedes"),
            }
            documents.append({k: v for k, v in document.items() if v is not None})

        with open(os.path.join(SRC, "questions-%s.json" % department), encoding="utf-8") as f:
            queries.extend(json.load(f))

    dataset = {
        "name": "meridian",
        "description": ("Synthetic enterprise knowledge base for Meridian Instruments (fictional): %d documents across HR/finance, products/support "
                        "and engineering/IT, in md, txt, html, pdf and docx, with superseded versions, regional and model near-duplicates, tables, long "
                        "documents, labels/tags for filters, and %d questions written by a separate pass from the corpus.") % (len(documents), len(queries)),
        "corpora": [{"id": "meridian", "documents": documents, "queries": queries}],
    }
    with open(OUTPUT, "w", encoding="utf-8", newline="\n") as f:
        json.dump(dataset, f, indent=2, ensure_ascii=False)
        f.write("\n")
    print("wrote %s: %d documents (%s), %d queries" % (os.path.normpath(OUTPUT), len(documents), ", ".join("%s %d" % kv for kv in sorted(counts.items())), len(queries)))


if __name__ == "__main__":
    main()
