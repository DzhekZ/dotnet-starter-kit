import { readFileSync } from "node:fs";
import path from "node:path";
import { expect, test } from "@playwright/test";
import { IMPERSONATION_STATUS_KEYS, INVOICE_STATUS_KEYS, statusKey } from "../../src/lib/status-keys";

// Several labels build their catalog key from a value the API sends: `status.${status}`. Catalog
// parity cannot see those — both locales can be missing the same key and still match — so a status
// the backend emits and the catalog never learned about renders as the key itself. That is exactly
// what shipped for top-ups: approving one moves it to `Invoiced`, and the badge read
// "status.invoiced".
//
// The member list is read from the backend enum rather than copied here, so adding a member to the
// C# enum without translating it fails this test instead of reaching a screen. Where the app maps a
// status to its key (src/lib/status-keys.ts) the test reads that map, so a member the map lacks fails
// too; where it still builds the key from the value, the test applies the same transformation.

const repoRoot = path.resolve("../..");

function enumMembers(relativePath: string, enumName: string): string[] {
  const source = readFileSync(path.join(repoRoot, relativePath), "utf8");
  const declaration = new RegExp(`enum\\s+${enumName}\\s*\\{([^}]*)\\}`, "s").exec(source);
  expect(declaration, `${enumName} not found in ${relativePath}`).not.toBeNull();
  const members = [...declaration![1].matchAll(/^\s*([A-Z]\w*)\s*(?:=\s*-?\d+\s*)?,?\s*$/gm)].map(
    (m) => m[1],
  );
  expect(members.length, `${enumName} parsed as empty`).toBeGreaterThan(0);
  return members;
}

const readCatalog = (locale: string, ns: string): Record<string, string> =>
  JSON.parse(readFileSync(path.resolve("src/locales", locale, `${ns}.json`), "utf8"));

/** The transformation the top-up call sites apply: first letter lowered, rest kept. */
const camel = (member: string) => `status.${member.charAt(0).toLowerCase()}${member.slice(1)}`;

/** Looks a member up in the app's map; a member it lacks has no key. */
const mapped = (keys: Record<string, string>) => (member: string) =>
  Object.hasOwn(keys, member) ? keys[member] : `(no entry for ${member} in src/lib/status-keys.ts)`;

const CASES = [
  {
    label: "top-up requests",
    ns: "billing",
    file: "src/Modules/Billing/Modules.Billing.Contracts/BillingEnums.cs",
    enumName: "TopupRequestStatus",
    key: camel,
  },
  {
    label: "invoices",
    ns: "billing",
    file: "src/Modules/Billing/Modules.Billing.Contracts/BillingEnums.cs",
    enumName: "InvoiceStatus",
    key: mapped(INVOICE_STATUS_KEYS),
  },
  {
    label: "impersonation grants",
    ns: "impersonation",
    file: "src/Modules/Identity/Modules.Identity.Contracts/v1/Impersonation/ImpersonationGrantDto.cs",
    enumName: "ImpersonationGrantStatus",
    key: mapped(IMPERSONATION_STATUS_KEYS),
  },
];

test.describe("status keys built from backend enums", () => {
  for (const testCase of CASES) {
    test(`${testCase.label}: every member has a label in both locales`, () => {
      const members = enumMembers(testCase.file, testCase.enumName);
      const en = readCatalog("en-US", testCase.ns);
      const pt = readCatalog("pt-BR", testCase.ns);

      const missing = members
        .map((member) => testCase.key(member))
        .filter((key) => !(key in en) || !(key in pt));

      expect(missing).toEqual([]);
    });
  }
});

test("a status the server added after this build has no key, so the badge shows the raw value", () => {
  expect(statusKey(IMPERSONATION_STATUS_KEYS, "Suspended")).toBeUndefined();
  expect(statusKey(INVOICE_STATUS_KEYS, "constructor")).toBeUndefined();
  expect(statusKey(IMPERSONATION_STATUS_KEYS, "Active")).toBe("status.active");
});

// The permission matrix is the other place keys are built at run time: the role detail screen
// renders whatever GET /identity/permissions/catalog returns, looking each resource up as
// `perm.resource.<Resource>` and each entry as `perm.entry.<Permission>`, with the server's own text
// as the fallback. The catalog is no longer in the source, so the keys cannot be enumerated from it;
// what this guards is that every override one locale carries, the other carries too, so a
// translated row never degrades to English in a Portuguese table (or the reverse).
test.describe("permission catalog keys", () => {
  test("every permission label override exists in both locales", () => {
    const en = readCatalog("en-US", "roles");
    const pt = readCatalog("pt-BR", "roles");
    const permKeys = (catalog: Record<string, string>) =>
      Object.keys(catalog).filter((key) => key.startsWith("perm."));

    expect(permKeys(en).length).toBeGreaterThan(0);
    expect(permKeys(en).filter((key) => !(key in pt))).toEqual([]);
    expect(permKeys(pt).filter((key) => !(key in en))).toEqual([]);
  });
});
