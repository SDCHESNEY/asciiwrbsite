---
title: Building the Blog Pipeline
slug: building-the-blog-pipeline
published: 2025-11-05
summary: A peek into the markdown parser, caching strategy, and validation rules powering the Phase 2 blog.
tags:
  - blog
  - dotnet
  - architecture
---
# Building the Blog Pipeline

Phase 2 introduces the content pipeline for markdown-driven posts. Every file ships with YAML frontmatter for the title, slug, publish date, summary, and tags. The server validates those fields, caches parsed results, and watches for file changes in development so folders stay hot reload friendly.

Blazor components render summaries and full posts directly from the provider, while the plaintext endpoint includes the latest highlights for terminal readers.
