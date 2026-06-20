# Redis TTL Audit Tool

Create a small CLI tool that scans Redis keys and reports keys that have no TTL.

The tool should group keys by prefix, estimate how many keys never expire, and generate a Markdown report.

The tool must be read-only and must not delete or modify Redis keys.
