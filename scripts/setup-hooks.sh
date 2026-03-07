#!/usr/bin/env bash
set -euo pipefail

# Configure this repository to use the included .githooks directory for Git hooks
git config core.hooksPath .githooks
echo "Configured core.hooksPath to .githooks"

echo "Make sure this is committed/pushed so other developers can run 'git config core.hooksPath .githooks' locally or run this script."
