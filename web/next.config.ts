import createMDX from "@next/mdx";
import type { NextConfig } from "next";

// Plugin names as strings — required for Turbopack compatibility (Next 16+).
// JavaScript function references cannot be serialized to Rust (Turbopack constraint).
// See: node_modules/next/dist/docs/01-app/02-guides/mdx.md §Using Plugins with Turbopack
const withMDX = createMDX({
  extension: /\.mdx?$/,
  options: {
    remarkPlugins: ["remark-frontmatter", "remark-gfm"],
    rehypePlugins: ["rehype-slug", "rehype-autolink-headings"],
  },
});

const nextConfig: NextConfig = {
  pageExtensions: ["ts", "tsx", "md", "mdx"],
};

export default withMDX(nextConfig);
