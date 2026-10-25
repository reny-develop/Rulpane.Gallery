# The policy

What a view, a theme or a paneset is held to before it is recorded in the Gallery, and what
happens after. This is what a submission is checked against, and it is written for the person
about to submit.

## Why there is anything to hold it to

What the Gallery holds is layout and colour and the words on a screen. Rulpane lets a link
carry any of it, and says so in its top bar: whatever a link carries, its author wrote. A
release in the Gallery differs in one way that matters — it has a **short name**, and a short
name reads as though somebody stood behind it.

Nobody does. **Nobody reviews a release here**, and nothing on these pages or in Rulpane says
otherwise. What the Gallery does instead is hold every submission to checks a machine can make,
and refuse what fails them, so that what a name stands for is exactly what those checks
establish and nothing more.

## What can be submitted

| Kind | What it is | Where a link names it |
| --- | --- | --- |
| view | a `rulealize/view/v1` document: where each value goes and which moves are offered. For one rule set or for any rules that have what it `expects` | `v` |
| theme | a `rulealize/theme/v1` document: colours, corners, typeface, spacing | `t` |
| paneset | a `rulpane/paneset/v1` document: rules, view, theme and where to start, each named or written in place, and `b` where the rules call what the view reads something else | `p` |

**Rule sets are not submitted here.** They are published to the
[Rulealize Registry](https://github.com/reny-develop/Rulealize.Registry), and a paneset names
one from there. Official releases are submitted the same way as anybody's and are held to the
same checks.

## The name

`<owner>/<name>`, in lowercase: `octocat/kanban`.

- **The owner is the GitHub account the repository belongs to**, and a release is submitted by
  that account
- **An organisation's repository** is submitted by any **public member** of the organisation,
  and its releases are named under the organisation: `acme/kanban`, from `acme/…`. GitHub lists
  an organisation's public members to anybody, which is how the Gallery checks; a member whose
  membership is private makes it public on the organisation's People page first. Every public
  member may submit a release of any of its names, so an organisation that wants fewer people
  to has fewer public members. Who submitted each release stays on its issue
- **The name is yours to choose** under the owner, as letters, digits and `-`
- **A name is one kind for good.** `octocat/kanban` is a view or a theme or a paneset, and every
  release of it is the same kind, so a link that names it never gets something else
- **A name is never handed to another account**

## A release

A release is one file, in one repository, at one commit. The Gallery numbers releases itself,
from 1, in the order they are recorded, and a link names one exactly: `octocat/kanban@2`.
**A link never moves to a newer release.** Submitting a commit where the file did not change is
refused: it would be the same release under a second number.

## What it is held to

Everything below is checked by [`tool/Admit`](../tool/Admit/), and the answer on the issue says
which one failed.

### It may be copied

The repository is under a licence, at that commit, that lets anybody keep and serve a copy:
`MIT`, `MIT-0`, `0BSD`, `ISC`, `BSD-2-Clause`, `BSD-3-Clause`, `Apache-2.0`, `Unlicense`,
`CC0-1.0` or `CC-BY-4.0`, as GitHub detects it. The Gallery keeps a copy of every release and
serves it, with the licence beside it, so that a link keeps working whatever happens to the
repository.

### Its words point nowhere

No string in it or in its sample — keys included — holds:

- an address (`://` or `www.`), or an e-mail address
- in a title, a text, a label or a name, what reads as a domain name (`help-desk.com`)
- markup (`<b>`)
- a character that is not shown: a direction mark, a zero-width space, a byte-order mark

A view has no block that shows a picture or a link, so this is about text alone, and it is the
one kind of release the Gallery exists to keep out: one that tells its reader where to go. A
document is at most 32 KiB.

### It runs

Each is read by Rulpane's own readers and refused with Rulpane's own message, and then run:

| Kind | Run in |
| --- | --- |
| paneset | itself |
| view | its **sample**: a paneset in the repository with everything but `v`, which the view fills |
| theme | its sample, if it names one — a paneset with everything but `t` — or not at all; its colours are held to their contrast either way |

The rules are compiled with Rulpane's runtime, the view is read against them with `b`, and from
the start one line of play is followed — the first move with nothing to type, its first
outcome — for up to 32 positions. At every one, each value a view expects has to be of the type
it said. That is the check Rulpane makes at every position a link opens, made before anybody
opens one. A sample may name anything bundled with Rulpane, published to the Registry, or
recorded here.

The sample is kept with the release, and its page opens the release in it.

## What is not checked

**Taste, usefulness and wording.** A release that passes is recorded, whatever it is for.

**Whether it is what it says it is.** A title saying "Bank login" is words, and the checks
above are about where words point, not what they claim. Rulpane's top bar says on every screen
that what is under it was not made by Rulpane, and that Rulpane never asks for a password or
payment. Nothing here can change the bar.

**Whether it works with rules other than its sample.** Rulpane checks that at every position,
for whatever rules a link names, and stops the screen with what failed.

## After it is recorded

The line is written into [`ledger/submitted.json`](../ledger/submitted.json), the copy under
`release/`, the issue is answered and closed, and the pages are rebuilt. Rulpane opens the
release once Rulpane is next deployed, which copies the Gallery beside it.

**A release can be withheld.** One that is reported as misleading, or as somebody else's work,
gets `"withheld"` on its line, with the reason. It stays on its page, marked; it is no longer
offered, and links to it stop opening at Rulpane's next deploy — and so does every paneset that
names it. Nothing else is ever removed, and a name is never reused.

**Every release is run again every week**, on that week's Rulpane. A change to Rulpane that
would stop one fails this repository's build before it reaches anybody.

## A pull request is not a submission

A submission is an issue opened from the [submit page](https://reny-develop.github.io/Rulpane.Gallery/submit/).
An issue is also where anything about the tools, the pages or this document belongs.
