import type { MDXComponents } from "mdx/types";

// Global MDX component overrides. Add custom component mappings here as needed.
const components: MDXComponents = {};

export function useMDXComponents(): MDXComponents {
  return components;
}
