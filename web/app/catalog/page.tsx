import { redirect } from "next/navigation";

/**
 * `/catalog` root — redirects to `/catalog/dashboard` (DEC-A16, TECH-1614).
 */
export default function CatalogRootPage(): never {
  redirect("/catalog/dashboard");
}
