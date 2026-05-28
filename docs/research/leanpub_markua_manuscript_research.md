# Best Practices for Writing Leanpub Books in Markua with GitHub Integration

## TL;DR
- **Use Markua 0.30 (the default for new Leanpub books since 2021, per the Markua Spec at markua.com) with a `manuscript/` folder containing `Book.txt`, per-chapter `.txt` files, a `Subset.txt` for fast previews, and a `manuscript/resources/` folder for images and code** — `Sample.txt` no longer works in Markua and is replaced by the `{sample: true}` attribute on headings.
- **Master the four building blocks**: heading-driven structure (`# Chapter`, `# Part #`, `{mainmatter}`/`{backmatter}`), blurbs (`A>`, `B>`, `D>`, `E>`, `I>`, `Q>`, `T>`, `W>`, `X>`, `C>`), the resource+attribute-list syntax (`{title: …, format: ruby, line-numbers: true, id: foo}` then `![]()`), and document settings at the top of the first file (`{soft-breaks: break …}`).
- **The "professional polish" gap is mostly outside the manuscript**: pick the right Custom Theme page size + margins for your print target, use 300 DPI images sized for the trim, ship a real cover, avoid floating images and inline HTML, and reserve a Standard/Pro plan if you want any non-default layout.

## Key Findings

1. **There are two living versions of Markua.** Markua 0.10 (launched 2014, "evolved until 2021," and per the Markua Spec at markua.com "is currently the only way to write courses on Leanpub") is documented in *The Markua Manual*; Markua 0.30 (beta launched on Leanpub in 2021, "Currently … the default for new books on Leanpub") is the version you should start in. The 0.30 spec is at https://markua.com/, the 0.10 manual at https://leanpub.com/markua/read. They are mostly compatible, but several attribute names changed (notably `caption` → `title`).
2. **The GitHub repo layout is dictated by Leanpub, not by Markua.** Leanpub looks for a top-level `manuscript/` folder containing `Book.txt`. Markua itself does not specify how files are split; Leanpub still drives the build from `Book.txt`.
3. **Sample handling fundamentally changed in Markua.** In LFM you used `Sample.txt`; in Markua you put `{sample: true}` immediately above the heading you want included in the free sample. Leanpub explicitly says "the `Sample.txt` approach is not supported for books which use Markua."
4. **Blurb shortcuts are a fixed alphabet**: `A>` aside, `B>` generic blurb, `C>` centered blurb, `D>` discussion, `E>` error, `I>` information, `Q>` question, `T>` tip, `W>` warning, `X>` exercise.
5. **`line-numbers: true` (hyphenated), `format: ruby`, `crop-start`/`crop-end`, and `number-from`** are the canonical code-block attributes. The Leanpub-specific magic-comment pair is `markua-start-insert` / `markua-end-insert` (renamed from `leanpub-start-insert` in LFM).
6. **Images: 300 DPI at the trim size you target**, stored in `manuscript/resources/` (or a sub-folder), referenced by relative path. Leanpub's PDF rendering goes through LaTeX, which is why `float: left|right` is fragile — Leanpub's own docs recommend against it.
7. **Custom Theme is gated to the Standard/Pro plan** and is the only way to access serious layout knobs (page size, font size, margins, code font, ToC depth, paragraph indenting, section numbering, scene breaks).
8. **Previews fail loudly with line-numbered errors.** Per the Leanpub Help Center article *"Troubleshooting Book Preview Generation Failure with Line-Numbered Error Reports"* (help.leanpub.com/en/articles/8310155), which quotes Leanpub's AI Services Homepage Essay: "we've also greatly improved Markua 0.30 error handling, including providing line numbers of errors if they occur." The typical culprits are missing images, attribute typos, blank line between `{sample: true}` and the heading, and accidentally inline HTML.

## Details

### 1. Repository Structure

The canonical layout, drawn from Leanpub's `default-new-book-content` template repo and the Help Center article *"What should the folder structure be for my book?"*:

```
your-book-repo/
├── README.md                  # for collaborators on GitHub — not used by Leanpub
└── manuscript/                # REQUIRED. Leanpub only looks here.
    ├── Book.txt               # REQUIRED. Ordered manifest of files to compile.
    ├── Sample.txt             # OPTIONAL & LFM-only. In Markua, use {sample: true}.
    ├── Subset.txt             # OPTIONAL. Used for "Subset Preview" (faster PDF-only builds).
    ├── frontmatter.txt        # convention: contains only "{mainmatter}" directive markers
    ├── mainmatter.txt
    ├── backmatter.txt
    ├── preface.txt
    ├── chapter-01-intro.txt
    ├── chapter-02-…txt
    ├── appendix-a.txt
    ├── bibliography.txt
    ├── colophon.txt
    └── resources/             # REQUIRED for any local resource (image, code, etc.)
        ├── images/            # convention; subfolders are arbitrary
        │   ├── ch01-architecture.png
        │   └── ch02-flow.svg
        └── code/
            ├── ch01/hello.rb
            └── ch02/sample.py
```

**Required vs. optional:**

| File / folder | Required? | Purpose |
|---|---|---|
| `manuscript/` | Yes | Where Leanpub looks |
| `manuscript/Book.txt` | Yes | Ordered list of files to include |
| `manuscript/resources/` | Required *if* you use local images/code | Holds all non-text resources |
| `manuscript/Sample.txt` | LFM only — ignored under Markua | Free sample manifest |
| `manuscript/Subset.txt` | Optional | Speeds up previews of a few chapters |
| Front/main/back matter files | Optional but recommended | Hold the directives `{mainmatter}` and `{backmatter}` (and historically `{frontmatter}`, which Markua 0.30 dropped as redundant) |
| `README.md`, `.gitignore`, `LICENSE` | Optional | For your GitHub workflow, ignored by Leanpub |

**Branching with GitHub.** Leanpub's help article *"Recommended Repository Branch Structure for Translated Books and Managing Community Feedback with Git and GitHub"* (help.leanpub.com/en/articles/10471613) recommends *two* branches for a single-language book — a working `preview` branch (the trunk, not `main`) and a `published` (or release) branch — so you can iterate previews privately and only fast-forward the published branch when you want to ship. The same article cites Henrik Kniberg's *Generative AI in a Nutshell* (github.com/hkniberg/ainutshell) as "a great example of how to manage Leanpub book translations." This is also how you can keep a stable book branch and a "next edition" or "course" branch off the same repo.

**File naming.** One chapter per `.txt` file is the universal recommendation in the official LFM and Markua manuals: "we strongly recommend having one file per chapter (or one file per chapter section), since it makes creating sample books easier and keeps your book directory cleaner." Numbered prefixes (`01-intro.txt`, `02-…txt`) work well for sorting on disk but the order shown to readers is whatever order you list in `Book.txt` — the filename order on disk is irrelevant.

### 2. Book.txt, Sample.txt and Subset.txt

**`Book.txt`** is a plain list of filenames, one per line, in the order you want them compiled:

```
frontmatter.txt
preface.txt
mainmatter.txt
chapter-01.txt
chapter-02.txt
backmatter.txt
appendix-a.txt
bibliography.txt
```

Crucially, `{frontmatter}`/`{mainmatter}`/`{backmatter}` go *inside* a manuscript file, **not** in `Book.txt` itself. A common pattern is a one-line `mainmatter.txt` containing literally `{mainmatter}` and a one-line `backmatter.txt` containing `{backmatter}`, so the manifest reads cleanly.

**`Subset.txt`** is the same format as `Book.txt` but only used when you click "Subset preview" on the Versions page. It generates a PDF-only preview of just the listed files — typically the chapter you're actively editing — and is the single biggest win for iteration speed on a large book.

**`Sample.txt` is a trap in Markua.** Leanpub's help article *"I have a Sample.txt file but no sample book is being created"* explains: "The `Sample.txt` instructions are for when you are writing in Leanpub-Flavored Markdown. In Markua, you need to do this differently." In Markua you mark the *content* you want sampled by adding `{sample: true}` on the line directly above the heading:

```
{sample: true}
# Chapter 1: Why Markua

This whole chapter (and its subsections) appears in the free sample.
```

You cannot leave a blank line between the directive and the `#`. You can mark parts, chapters, or sections with `{sample: true}`.

### 3. Markua Element Best Practices

#### Headings, parts, chapters

- `#` = chapter, `##` = section, `###` = sub-section, down to `#####`.
- **Parts** in Markua 0.30 are top-level headings with the `# Part #` syntax (hash at start *and* end of the line) — distinct from LFM's `-#`. Example: `# Part I: Foundations #`.
- All headings can take an attribute list immediately above them:

```
{id: ch-intro, sample: true}
# Introduction
```

  Then cross-reference with a normal Markdown link: `[see the introduction](#ch-intro)`.

- **Frontmatter / mainmatter / backmatter** directives go in the manuscript, on their own line:

```
{mainmatter}

# Chapter 1
…

{backmatter}

# Appendix A
```

  With Leanpub's automatic chapter numbering enabled, frontmatter and backmatter chapters get no chapter numbers; mainmatter chapters do.

- In Markua 0.30, `{frontmatter}` is dropped: the spec states "there is no more `{frontmatter}` directive in Markua. It existed in Markua 0.10, but it is redundant: front matter is just the stuff which is comes before a `{mainmatter}` directive." Leanpub still auto-generates a title page and copyright page from the metadata you enter in the Leanpub web app — you do *not* hand-author them in Markua.

#### Asides, blurbs and the letter-prefix shortcuts

Asides are short side-discussions; blurbs are formatted callout boxes. Both use blockquote-like syntax:

```
A> This is an aside. Asides can span multiple paragraphs and
A> can contain other Markua blocks.

B> Generic blurb.
D> Discussion blurb.
E> Error blurb.
I> Information blurb.
Q> Question blurb.
T> Tip blurb.
W> Warning blurb.
X> Exercise blurb.
C> This is a centered blurb. This is the only way to center text in Markua.
```

The shortcuts are syntactic sugar for `{blurb, class: tip}` etc. You can write the long form when you need extra attributes:

```
{blurb, class: warning, icon: exclamation-triangle}
This is a warning blurb with a Font Awesome icon attribute.
Icon support is a Leanpub *extension attribute* — other Markua
processors will safely ignore it.
{/blurb}
```

Per the Markua spec: "Markua Processors must ignore any attributes which they do not understand." This is your safety net for extension attributes.

#### Code blocks — the precise attribute names

Fenced code with a language tag works as in CommonMark, but the *Markua* way to specify the language is the `format` attribute:

````
{format: ruby, line-numbers: true, number-from: 10}
```
puts "hello, world"
```
````

The canonical, verbatim-from-the-manual attribute names are:

| Attribute | Values | Purpose |
|---|---|---|
| `format` | `ruby`, `python`, `text`, `guess`, `console`, … | Programming language; `text` disables highlighting, `guess` auto-detects |
| `line-numbers` | `true` / `false` (default `false`) | Show line numbers (note the **hyphen** — not `lineNumbers`) |
| `number-from` | integer (default `1`) | Starting line number |
| `crop-start` | integer | First line included from an external file |
| `crop-end` | integer | Last line included (default: end of file) |
| `caption` (0.10) / `title` (0.30) | string | Figure caption / title for List of Listings |
| `id` | string | Cross-reference target (sugar: `{#my-id}`) |

To **include code from a file** in the resources folder, use the figure syntax:

```
{title: "Bubble sort", format: python, line-numbers: true,
 crop-start: 10, crop-end: 25}
![](code/ch04/sort.py)
```

To **mark inserted/deleted lines** for diff-style highlighting, Markua uses magic comments (renamed from LFM):

```
def sort(items):
    # markua-start-insert
    items.sort()
    # markua-end-insert
    return items
```

To get code blocks listed in a "List of Listings" rather than the generic "List of Figures", set the document setting `code-block-name: listing` once at the top of the first manuscript file.

#### Figures and images

Images are always inserted as figures. Syntax:

```
{width: "60%", id: fig-architecture}
![High-level architecture of the service](images/ch02-arch.png)
```

Supported attributes specific to images: `width`, `height`, `align` (`left|right|middle`), `float` (`left|right|inside|outside`), `fullbleed` (boolean). All figures additionally support `caption`/`title`, `class`, `format`, `type`, and `id`.

The square brackets `[ ]` after `!` are the **alt text and default caption**. Always include them, even if empty: `![](palm-trees.jpg)`. This isn't decorative — Markua's `alt-title` document setting (default `all`) uses alt text as the caption when no explicit `title:` is supplied.

**Avoid `float`.** Leanpub's own Markua Manual is unusually blunt about this: "our recommendation about using the float attribute is to save your time and not do it. Floating images and wrapping text occasionally do work nicely, but more often than not they leave something to be desired. … Our PDF rendering is done by producing LaTeX from Markua. LaTeX is great, but its support for floating images requires some manual tweaking at times, and this is not something we want to do for you." Use inline images at sensible widths instead.

#### Tables

GFM-style pipe tables with an attribute list above. The Help Center article *"Creating Tables in Markua"* (help.leanpub.com/en/articles/9119132) shows the canonical attributes:

```
{title: "Win/Loss Record", column-widths: "20% 80%", width: "100%"}
| Game | Outcome |
| ---  | ---     |
| 1    | Win     |
| 2    | Loss    |
```

To make tables appear in a List of Tables instead of the List of Figures, set `table-name: table` in the document settings.

Markua tables don't support rowspans, colspans, or multi-line cells. If you need that, you essentially can't have it — Markua removes raw HTML, and there is no other escape hatch. Restructure into multiple tables or use an image of the table.

#### Footnotes, links, cross-references and the index

- **Footnotes**: standard Markdown footnote syntax — `[^id]` inline and `[^id]: text` at the bottom. Leanpub PDF will turn them into proper footnotes, and `Settings > Theme > "Show links as footnotes in PDFs"` will also convert inline hyperlinks into footnotes in print.
- **Cross-references**: set an id with `{#foo}` or `{id: foo}` on a heading, figure, table, listing, or aside; reference with `[link text](#foo)`. Markua 0.30 introduces "smart crosslinks" (auto-numbered chapter/figure references) — supported on Leanpub's 0.30 implementation.
- **Reference-style links are not supported.** Use inline links only.
- **Index entries** (Markua 0.30 only): `{i: "Ishmael"}` placed inline next to the word generates an index entry pointing at that page. Leanpub auto-generates an "Index" section at the end of the book when any `{i:}` directive is present. See the official `leanpub/sample-book-with-index-entries` repo for examples of nested entries (`{i: "Whales!sperm"}`), see-also entries, ranges, and primary references.

#### Attribute syntax — the universal pattern

Every block element (heading, figure, blurb, aside, code, table) and every span (text, code span) can carry a `{key: value, key2: value2}` attribute list. Three placement rules from the Markua Spec:

1. **Above a block**: "Immediately above a block element (e.g. heading, figure, aside, blurb, quiz, etc.), with one newline (not a blank line) separating it from the block element."
2. **After a span**: "Immediately after a span element (e.g. a word, italicized phrase, etc.) in normal paragraphs and in similarly-simple contexts, with no spaces separating it from the span element." Example: `[span text]{#span-id}`.
3. **On a line by itself**: "with one blank line above and below it" — this creates a free-floating directive (e.g. `{mainmatter}`, `{pagebreak}`).

Syntactic sugar: `{#foo}` ≡ `{id: foo}`.

### 4. Front Matter and Back Matter

| Element | How to produce it |
|---|---|
| **Cover** | Uploaded as an image in the Leanpub web app under *Upload Book Cover*. Not a Markua concept. Requirements vary by theme; the Custom theme shows a page-size-specific requirement at upload time. |
| **Title page** | Auto-generated by Leanpub from metadata (title, subtitle, authors, copyright holder) you enter in the Leanpub web app. There is no `{title-page}` directive in Markua. |
| **Copyright page** | Auto-generated likewise. Leanpub un-branded export still includes title + copyright pages. |
| **Dedication / epigraph** | Write as a normal frontmatter chapter before `{mainmatter}`. Use `C>` for a centered single-line dedication. |
| **Table of Contents** | Auto-generated from headings. Depth controlled by *Settings > Theme > Table of Contents depth*. Insert the directive `{toc}` explicitly if you want to control placement. |
| **Preface / Foreword** | Frontmatter chapters (`# Preface` before `{mainmatter}`). |
| **List of Figures / Tables / Listings / Equations** | Markua 0.30 directives: `{figures}`, `{tables}`, `{listings}`, `{equations}`. The spec is explicit: "these directives **will not work** if they are inserted **after** the `{mainmatter}` directive." |
| **Appendices** | Normal `# Appendix A: …` chapters placed after `{backmatter}`. With auto-numbering enabled, they won't get chapter numbers. |
| **Bibliography** | No native BibTeX support in Markua. You write a chapter manually. There is no `book.bib` or `[@cite]` syntax. (The existence and framing of the Leanpub Help Center article *"Do you have a way to create a bibliography in a Leanpub book?"* — help.leanpub.com/en/articles/3601904 — confirms authors should format a bibliography manually.) |
| **Index** | Markua 0.30: just sprinkle `{i: "term"}` directives in the manuscript; Leanpub appends an Index section. |
| **Colophon** | A backmatter chapter you write yourself. |

### 5. Images and Resources

- **Folder**: put everything in `manuscript/resources/`. Subdirectories are arbitrary; Leanpub historically auto-organises by type (`resources/images/`, `resources/code/`, …) but you can name them anything.
- **Path in markup**: relative to `resources/`, so `![](images/ch01/diagram.png)`.
- **Formats**: GIF, PNG, JPEG, SVG, zipped SVG.
- **GIFs do not render in PDF**. Replace with a still PNG of the most important frame and link to the live GIF in the caption.
- **Resolution**: 300 DPI at the *physical size at which the image will print*. Leanpub's help article *"I'm confused about image sizes"* gives the calculation verbatim: "If I have an image which is 900 pixels wide, then if its resolution is 300 PPI, it will look like 3 inches (900 / 300) wide. This will fit in almost all of our books, based on the size chosen. HOWEVER, if its resolution is 72 PPI, then it will look like 12.5 inches wide (900 / 72), which will fit in no books we produce."
- **Color**: keep your originals in RGB for the Leanpub PDF; if you're going to a print-on-demand service that wants CMYK (Lulu does, KDP doesn't), do the conversion as a post-processing step on the downloaded print-ready PDF (Ghostscript or ImageMagick).
- **Take light-theme screenshots** for any code editor or terminal. Dark backgrounds look fine in PDF on-screen but are illegible in printed books.
- **File naming**: prefix with chapter or section number (`02-architecture-diagram.png`) so the asset list mirrors the manuscript order; this makes diffs cleaner in Git and orphan files easier to spot.

### 6. Common Pitfalls

1. **Trying to use raw HTML.** The Markua Spec is explicit: "in Markua **all raw HTML elements are removed**." Anything between `<` and `>` that isn't a Markdown auto-link disappears. If you migrated from a Markdown blog with `<br>`, `<sub>`, `<img>` tags, all of it has to be rewritten or replaced.
2. **`Sample.txt` with no sample produced.** You're on Markua; switch to `{sample: true}` directives. And: no blank line between the directive and the `#` heading.
3. **Mixing Markua 0.10 and 0.30 attribute names.** If you started in 0.10, do a global find-replace of `caption:` → `title:` when moving to 0.30, per the spec's migration guide: "Rename all `caption` attributes to `title`."
4. **`leanpub-start-insert` magic comments** silently doing nothing in Markua. In Markua 0.30 these are `markua-start-insert` / `markua-end-insert`.
5. **`-#` for parts.** That's LFM. In Markua, parts are `# Part I #` (with a trailing `#`).
6. **Reference-style links.** Not supported by Leanpub. Inline links only.
7. **Float-wrapped images.** Fragile in LaTeX → PDF; Leanpub itself tells you not to bother.
8. **Single newlines becoming line breaks (or not).** Markua 0.10 treated a single newline as a forced break; Markua 0.30 follows Markdown (joins them into a space) unless you set `{soft-breaks: break}` at the top of your first file.
9. **Long content in backticks overflowing the right margin.** The Leanpub Help Center calls this out by name: "Long content in backticks (making a code span) can lead to overflowing text in the right margin." Break the code span into shorter pieces or use a fenced code block, where wrapping is handled.
10. **Tables that need rowspans/colspans/multi-line cells.** Not supported; restructure or use an image.
11. **Index entries / `{i:}` directive in a 0.10 book.** Index entries exist only in Markua 0.30.
12. **Sample previews drift from full-book builds.** A sample preview only includes content with `{sample: true}` *and* its descendants; if a section that depends on definitions in an earlier non-sample chapter is included, broken cross-references appear in the sample. Audit sample previews before publishing.
13. **Books listed in `Book.txt` that don't exist.** Per the Help Center's line-numbered error-reporting article, modern Leanpub builds will fail with a `>`-marked line and an explanation; on older books, builds could silently skip them.
14. **Plain text encoding.** Save all manuscript files as UTF-8 (without BOM). Accent characters sometimes break otherwise.

### 7. Document Settings — The Top-of-File Block

Markua 0.30 introduces a document-settings block, placed *once* at the very top of the **first** manuscript file (i.e. the first file listed in `Book.txt`). The Markua Spec is explicit: "Make sure you add a blank line below the document settings, and also make sure you put the curly braces on lines by themselves!" Each setting is on its own line as `key: value`, with a blank line before the first heading:

```
{
soft-breaks: break
two-space-hack: false
default-code-language: typescript
code-block-name: listing
table-name: table
alt-title: text
}

# Chapter 1
```

Useful settings (verbatim names from the Markua Spec):

| Setting | Values | Effect |
|---|---|---|
| `soft-breaks` | `space` (default) / `break` | Treat single newline as a space (Markdown) or hard break (Markua 0.10 style) |
| `two-space-hack` | `true` / `false` | Old Markdown "two trailing spaces = line break" |
| `alt-title` | `all` / `text` / `none` | Whether `![alt]()` alt text is used as the figure title |
| `default-code-language` | language name | Default `format` for triple-backtick blocks (default is `guess`) |
| `code-block-name` | `figure` (default) / `listing` | Where code blocks appear in lists (List of Figures vs. List of Listings) |
| `table-name` | `figure` (default) / `table` | Tables in List of Figures vs. List of Tables |
| `lang` | language code | Default human language (e.g. `eng`, `jpn`) |

You can also set these globally via *Author > Books > [your book] > Settings > Generation Settings*; explicit per-book settings in the manifest win.

### 8. Professional Polish

These are the things that move a Leanpub book from "obviously a Leanpub default-theme book" to "feels like a real publication":

1. **Pay for a Standard or Pro plan and pick the Custom theme.** The Custom theme is the only way to set non-default page size, font size, line spacing, margins, code font, ToC depth, paragraph indenting, section numbering, scene-break style, and caption styling. Without it, your fiction prints at an unusable 8.5×11" with 1" margins.
2. **Match your print-ready PDF page size to your POD target.** Leanpub's *"Some Print-Ready PDF Guidance"* page recommends, for a sci-fi novel, "8.5 point font, 1.2 line spacing, digest page size, 1.25 inch inner margin, 0.75 inch outer margin, 0.5 inch top margin, 0.5 inch bottom margin, 1.25 line paragraph spacing." For technical books, 9pt body and the 7"×9.1" Technical page size are typical. The pertinent help article lists the exact print-PDF size matrix per ebook page size.
3. **Use `{listings}` and `{tables}` lists.** Setting `code-block-name: listing` and `table-name: table` and inserting `{listings}` and `{tables}` after the ToC gives the book a professional cross-referenceable apparatus that almost no default Leanpub book has.
4. **Number your figures, listings, and tables; reference them by id.** The id-and-cross-reference pattern (`{#fig-arch}` + `[Figure](#fig-arch)`) reads as a real technical book in the ToC and in the body text.
5. **Set captions (the `title:` attribute) on every non-decorative image and code block.** They appear under the figure, get into the lists of figures/listings, and signal craftsmanship.
6. **Show links as footnotes in PDF.** Settings > Theme > "Show links as footnotes in PDFs". Bare URLs in body text scream blog; footnoted links read like a book.
7. **Use blurbs (`T>`, `W>`, `I>`) for sidebars instead of bolded paragraphs.** They are visually distinct and semantically meaningful — they will be styled correctly in both PDF and EPUB.
8. **Subset preview while writing, full preview before publishing.** A `Subset.txt` containing only the chapter you're editing turns a multi-minute build into seconds. But always do a full-book preview before publishing — page breaks, ToC depth, cross-references, and sample contents can only be validated against the whole.
9. **Get a real cover.** Leanpub's cover requirements vary by theme and page size; use the dimensions shown on the *Upload Book Cover* page. A bookstore-style cover image is the single biggest visible signal of professionalism.
10. **Use the InDesign export for a print run that demands typographic precision.** Leanpub's stance is explicit in the Help Center article *"Can I make complex layouts for my ebook using Leanpub?"*: "No, you can't create a complex layout for your ebook when you're using one of our writing modes … However … when you're done writing, you can use our InDesign export feature to create a complex and beautiful book design yourself, or to provide the InDesign files to a professional book designer."
11. **Trigger previews from your editor or CI.** Leanpub exposes a simple JSON API (`POST https://leanpub.com/<book>/preview.json` with your API key) so you can wire previews to a Rakefile, git hook, or GitHub Actions job. This pairs naturally with `Subset.txt`.
12. **Public manuscript repo + community PRs.** Leanpub's help article on translation branch structure points to Henrik Kniberg's *Generative AI in a Nutshell* repo (github.com/hkniberg/ainutshell) as a model. The two-branch model (`preview` trunk, `published` release branch) keeps drive-by PRs from blowing up live builds.

## Recommendations

**Start here (week 1):**
1. Create your repo from `github.com/leanpub/default-new-book-content` ("Use this template"). Add Leanpub as a collaborator. Verify your existing preview pipeline still works on the cloned tree.
2. Lock in **Markua 0.30** under *Settings > Markdown Dialect*. Don't start a new book in Markua 0.10 unless you specifically need course features that aren't on 0.30 yet.
3. Add a document-settings block to the first file with at minimum `code-block-name: listing` and `table-name: table` if your book has code/tables; set `default-code-language` to your primary language.
4. Split into one chapter per file, named with numeric prefixes. Create `mainmatter.txt` and `backmatter.txt` as one-line directive files.

**Build out (week 2-3):**
5. Set up a `Subset.txt` workflow. Add a Rakefile or shell script that POSTs to Leanpub's preview API so previews come from your editor, not the web UI.
6. Move all images into `manuscript/resources/images/`, named `<chapter>-<slug>.png`, regenerated at 300 DPI at the print trim size you're targeting.
7. Pick your target page size **before** writing too many figures. Switching from US Letter to Technical mid-book means re-rendering every figure.

**Polish phase (before first publish):**
8. Switch to Custom theme; set page size, margins, code font, ToC depth. Pay for Standard plan if you haven't.
9. Add `{toc}`, `{figures}`, `{tables}`, `{listings}` in the front matter, in that order — and **before** `{mainmatter}`, since the spec says those directives don't work after it.
10. Audit every figure for a `title:` (caption) and an `id`. Replace bolded "Note:" paragraphs with `I>` or `T>` blurbs.
11. Set up the `{sample: true}` directives on 1 part + 1 chapter you want as the free sample. Preview the sample build separately; check cross-references don't dangle.
12. Turn on "Show links as footnotes in PDFs."
13. Run a full-book preview and a print-ready PDF export. Open the print PDF in two-page mode and check facing-page margins, widow/orphan lines, code blocks running off the page, and image resolution.

**Thresholds that should change your plan:**
- If your full-book preview takes more than ~3 minutes: invest in `Subset.txt` workflow immediately.
- If any image is below 300 DPI at its rendered size: redraw or re-source it before you commit.
- If you find yourself wanting raw HTML or rowspan tables: redesign the content; Markua won't bend.
- If you're going to print on KDP/IngramSpark/Lulu: do a proof print *before* you publish the ebook, not after.

## Caveats

- **Two Markua versions are in flight.** Most internet tutorials, GitHub repos and blog posts you'll find were written for Markua 0.10 or even LFM. When you see `-#` for parts, `caption:` for captions, `leanpub-start-insert` magic comments, or `Sample.txt`, you're reading old material. Cross-check against the live spec at https://markua.com/.
- **The Markua 0.30 manual is incomplete.** The Markua Spec at markua.com states verbatim: "The user manual for Markua 0.30 is not yet stable, so for now you're much better off reading this spec!" The spec at markua.com is more authoritative than the 0.30 beta manual on Leanpub.
- **Not every spec feature is implemented on Leanpub yet.** Per the markua.com spec, "Leanpub's Markua 0.30 implementation does not support courses yet" — courses still require Markua 0.10. Web code resources (URL-referenced source files) don't work yet under Leanpub's Markua either.
- **Layout knobs are deliberately limited.** Leanpub's stated philosophy in its *"Why are there so few layout options?"* Help Center article is that layout should be a global switch, not per-paragraph fiddling: "Layout should be done via global options which apply to your whole book." If you need fine layout control, the official escape hatch is the InDesign export, not a richer Markua.
- **Free-plan limitations bite quickly.** The Custom theme, custom margins, and several print-PDF page sizes are Standard/Pro only.
- **Print-ready PDF image handling is unclear.** At least one author (Davide Barranca, in his published write-up "Printing on demand a Leanpub book with Lulu.com") explicitly noted: "I'm not 100% sure to have converted to sRGB all the screenshots when taking them. It's not my mistake but it's not clear either (I've to check with Leanpub on that) whether or not the Print-Ready PDF export option downsamples images." If you're going to print, keep your original high-resolution source files outside the repo and treat the Leanpub PDF as a near-final, not a final.
- **Auto-generated title/copyright pages can't be customized in Markua.** They come from the Leanpub web UI metadata; if you want a hand-designed title page you'd typically do that in the InDesign export or by post-processing the PDF.
- **GitHub writing mode requires Leanpub be added as a repo collaborator.** Make sure your org's branch protection rules don't block the Leanpub bot account from reading branches you preview from.