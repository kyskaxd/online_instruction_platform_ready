#!/usr/bin/env bash
set -e
cd "$(dirname "$0")/../frontend/instruction-platform-client"
npm install
npm run dev
