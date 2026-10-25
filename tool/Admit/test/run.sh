#!/usr/bin/env bash
#
# Runs tool/Admit on every submission under case/ against the repository beside this script,
# then records the ones that pass into an empty ledger, reads the cases that need a ledger, and
# checks it.
#
#   bash tool/Admit/test/run.sh [<registry>]      (from the top of the repository)
#
# The cases are written by cases.sh. <registry> is a copy of the Registry's rule sets, for the
# check to run what holds one; none of the cases needs it.

set -uo pipefail
here=$(cd "$(dirname "$0")" && pwd)
root=$(cd "$here/../../.." && pwd)
registry=${1:+--registry $(cd "$1" && pwd)}
work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT

dotnet build "$root/tool/Admit" -c Release --nologo -v quiet >/dev/null || exit 2
admit() { dotnet run --no-build -c Release --project "$root/tool/Admit" -- "$@"; }

failed=0
# Reads one case from the current directory, whose ledger it is held to.
read_case() {
    local name=$1
    { read -r code; read -r says; read -r license; read -r members; } < "$here/case/$name.expect"
    local out="$work/$name"
    admit read "$here/case/$name.md" --author octocat --out "$out" --checkout "$here/repository" --license "$license" --members "$members" >/dev/null 2>"$work/$name.err"
    local got=$?
    if [[ $got -ne $code ]] || ! grep -qF -- "$says" "$out/answer.md" 2>/dev/null; then
        echo "✕ $name: exit $got (expected $code)"
        cat "$out/answer.md" "$work/$name.err" 2>/dev/null | sed 's/^/    /'
        failed=1
    else
        echo "✓ $name"
    fi
}

# Every case but those that need a ledger, read against none.
mkdir -p "$work/gallery/ledger"
printf '{\n  "releases": [\n  ]\n}\n' > "$work/gallery/ledger/submitted.json"
cd "$work/gallery"
for body in "$here"/case/*.md; do
    name=$(basename "$body" .md)
    [[ $name == uses-gallery || $name == kind-taken ]] && continue
    read_case "$name"
done

# The ones that passed, recorded one after another.
for name in applied board plain theme paneset; do
    admit record "$work/$name" >/dev/null || { echo "✕ $name was not recorded"; failed=1; }
done

# The same document twice is one release: the second is refused, though it was read in another sample.
if admit record "$work/direct" 2>/dev/null; then
    echo "✕ the same document was recorded twice"
    failed=1
fi

# A paneset may name what the Gallery holds, and a name stays the kind it was recorded as.
read_case uses-gallery
read_case kind-taken
admit record "$work/uses-gallery" >/dev/null || { echo "✕ uses-gallery was not recorded"; failed=1; }

admit check $registry || failed=1
exit $failed
