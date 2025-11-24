---
title: Launching ASCII Site
slug: launching-ascii-site
published: 2025-11-01
summary: Why this project exists and how ASCII-first storytelling keeps terminals and browsers in sync.
tags:
  - ascii
  - roadmap
---
# Launching ASCII Site

ASCII Site started as a tiny experiment: could we build a storytelling surface that felt equally at home in curl and in a browser? Phase 0 proved the baseline—security headers, health checks, and shared analyzers. Phase 1 brought the hero banner and markdown-powered About page to life.

The next increments focus on content velocity. Markdown files live in `content/blog`, hot reload in development, and stay friendly to diffs in pull requests. The same files hydrate Blazor components and curl/plaintext output, so there is always a single source of truth.
