#!/usr/bin/env bash
# Creates or updates a DNS-only CNAME in the Cloudflare zone named by $ZONE, with $CLOUDFLARE_API_TOKEN.
# Usage: cloudflare-cname.sh <name> <target>. Trailing dots, as ACM writes them, are dropped.
set -euo pipefail

NAME="${1%.}"
TARGET="${2%.}"
API=https://api.cloudflare.com/client/v4

call() {
  curl -sS --fail-with-body -H "Authorization: Bearer $CLOUDFLARE_API_TOKEN" -H "Content-Type: application/json" "$@"
}

ZONE_ID="$(call "$API/zones?name=$ZONE" | jq -r '.result[0].id // empty')"
if [ -z "$ZONE_ID" ]; then
  echo "::error::The Cloudflare token cannot see the zone $ZONE. It needs Zone:DNS:Edit on that zone."
  exit 1
fi

BODY="$(jq -cn --arg name "$NAME" --arg content "$TARGET" '{type: "CNAME", name: $name, content: $content, ttl: 1, proxied: false}')"
EXISTING="$(call "$API/zones/$ZONE_ID/dns_records?type=CNAME&name=$NAME" | jq -r '.result[0] // empty | "\(.id) \(.content)"')"

if [ -z "$EXISTING" ]; then
  call -X POST "$API/zones/$ZONE_ID/dns_records" --data "$BODY" >/dev/null
  echo "$NAME CNAME $TARGET created"
elif [ "${EXISTING#* }" = "$TARGET" ]; then
  echo "$NAME CNAME $TARGET unchanged"
else
  call -X PATCH "$API/zones/$ZONE_ID/dns_records/${EXISTING%% *}" --data "$BODY" >/dev/null
  echo "$NAME CNAME updated to $TARGET"
fi
