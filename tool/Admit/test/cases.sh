#!/usr/bin/env bash
#
# Writes case/: one issue body (<name>.md) and one expectation (<name>.expect) per case. Run it
# again after changing a case here; run.sh reads what it wrote.
#
# An expectation is four lines: the exit code, a piece of text the answer has to contain, the
# licence GitHub would report for the repository, and the organisations GitHub would list the
# account that opened the issue, octocat, as a public member of.

set -euo pipefail
here=$(cd "$(dirname "$0")" && pwd)
rm -rf "$here/case"
mkdir -p "$here/case"

# name | exit | the answer says | licence | kind | name | path | sample | [repository] | [commit] | [organisations]
c() {
    local name=$1 code=$2 says=$3 license=$4 kind=$5 id=$6 path=$7 sample=$8
    printf '%s\n%s\n%s\n%s\n' "$code" "$says" "$license" "${11:-}" > "$here/case/$name.expect"
    cat > "$here/case/$name.md" <<EOF
### Kind

$kind

### Name

$id

### Repository

${9:-octocat/rulpane-panes}

### Commit

${10:-0123456789abcdef0123456789abcdef01234567}

### Path

$path

### Sample

${sample:-_No response_}
EOF
}

# Releases that pass.
c applied        0 '### ✓ Read — the view `octocat/stage-card`' MIT  View     octocat/stage-card views/stage-card.json panes/approval.json
c direct         0 'tried in `panes/request.json`'                 MIT  View     octocat/stage-card views/stage-card.json panes/request.json
c board          0 'rule set `tictactoe@1.0.0`'                     MIT  View     octocat/squares    views/squares.json    panes/tictactoe.json
c plain          0 'expects: not said'                              MIT  View     octocat/claim      views/claim.json      panes/approval-plain.json
c typed          0 'expects: `all: list<string>`'              MIT  View     octocat/ticks      views/ticks.json      panes/checklist.json
c theme          0 'its colours were checked on their own'          MIT  Theme    octocat/forest     themes/forest.json    ""
c theme-shown    0 'tried in `panes/claim-in-forest.json`'          MIT  Theme    octocat/forest     themes/forest.json    panes/claim-in-forest.json
c paneset        0 '### ✓ Read — the paneset `octocat/othello`'     MIT  Paneset  octocat/othello    panes/othello.json    ""

# Words that point somewhere.
c url            1 'holds an address'                    MIT  View  octocat/stage-card bad/url.json       panes/request.json
c domain         1 'reads as a domain name'              MIT  View  octocat/stage-card bad/domain.json    panes/request.json
c invisible      1 'a character that is not shown'       MIT  View  octocat/stage-card bad/invisible.json panes/request.json
c markup         1 'holds markup'                        MIT  View  octocat/stage-card bad/markup.json    panes/request.json

# What Rulpane would refuse.
c wrong-type     1 'the screen would stop'               MIT  View  octocat/stage-card bad/wrong-type.json    panes/request.json
c unknown-block  1 'there is no block called "image"'    MIT  View  octocat/stage-card bad/unknown-block.json panes/request.json
c no-fit         1 'the rules project no "stage"'        MIT  View  octocat/stage-card views/stage-card.json  panes/approval-plain.json
c misspelt-b     1 'b.stagee: the view expects no "stagee"' MIT View octocat/stage-card views/stage-card.json bad/misspelt-b.json
c no-such-rules  1 'cannot be read'                      MIT  View  octocat/stage-card views/stage-card.json  bad/nothing-rules.json
c faint          1 'is refused as a theme'               MIT  Theme octocat/faint      bad/faint.json         ""
c not-a-view     1 'is not a view'                       MIT  View  octocat/forest     themes/forest.json     panes/request.json
c not-a-paneset  1 'is not a paneset'                    MIT  Paneset octocat/stage-card views/stage-card.json ""
c no-rules       1 'names no rules'                      MIT  Paneset octocat/empty    bad/no-rules.json      ""

# The sample.
c sample-has-v   1 'leaves out `v`'                      MIT  View    octocat/stage-card views/stage-card.json bad/sample-has-v.json
c no-sample      1 'A view is tried in a **sample**'     MIT  View    octocat/stage-card views/stage-card.json ""
c paneset-sample 1 'takes no **sample**'                 MIT  Paneset octocat/othello    panes/othello.json    panes/request.json

# Who, where and what.
c licence        1 'under `GPL-3.0`'                     GPL-3.0 View octocat/stage-card views/stage-card.json panes/approval.json
c bad-kind       1 'The **kind** is View, Theme or Paneset' MIT Layout octocat/stage-card views/stage-card.json panes/approval.json
c someone-else   1 'opened by `octocat`'                 MIT  View  hubot/stage-card   views/stage-card.json panes/approval.json
c their-repo     1 'one the account submitting owns'     MIT  View  octocat/stage-card views/stage-card.json panes/approval.json hubot/panes
c organisation   0 '### ✓ Read — the view `acme/stage-card`' MIT View acme/stage-card views/stage-card.json panes/approval.json acme/panes "" acme
c not-a-member   1 'not a public member of `acme`'       MIT  View  acme/stage-card    views/stage-card.json panes/approval.json acme/panes "" other
c holder         1 'the account that owns its repository' MIT View  hubot/stage-card   views/stage-card.json panes/approval.json acme/panes "" acme
c short-commit   1 '40 lowercase hexadecimal'            MIT  View  octocat/stage-card views/stage-card.json panes/approval.json "" 0123abc
c missing        1 'has no `views/nothing.json`'         MIT  View  octocat/stage-card views/nothing.json   panes/approval.json
c climbing       1 'path of a `.json` file'              MIT  View  octocat/stage-card ../secret.json       panes/approval.json

# Read once the ledger holds octocat/stage-card@1 as a view (run.sh reads these after recording).
c uses-gallery   0 '### ✓ Read — the paneset `octocat/claims`' MIT Paneset octocat/claims    panes/uses-gallery.json ""
c kind-taken     1 'is a view in the Gallery'            MIT  Theme   octocat/stage-card themes/forest.json ""
