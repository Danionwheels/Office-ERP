import type {
  AccountingControlSettings,
  AccountingControlSettingsInput,
  LedgerAccountSummary,
  VoucherNumberingRule,
  VoucherNumberingRuleInput
} from "../types/accountingTypes";

export type AccountingControlTone = "ready" | "warning" | "neutral";

export type AccountingControlFact = {
  label: string;
  value: string;
  tone: AccountingControlTone;
  title?: string;
};

export function getActivePostingAccounts(accounts: LedgerAccountSummary[]): LedgerAccountSummary[] {
  return accounts
    .filter((account) => account.status === "Active" && account.isPostingAccount)
    .sort((left, right) => left.code.localeCompare(right.code));
}

export function getEquityPostingAccounts(accounts: LedgerAccountSummary[]): LedgerAccountSummary[] {
  return accounts.filter((account) => account.type === "Equity");
}

export function findAccountById(
  accounts: LedgerAccountSummary[],
  accountId: string
): LedgerAccountSummary | null {
  return accounts.find((account) => account.ledgerAccountId === accountId) ?? null;
}

export function canSaveAccountingControls(value: AccountingControlSettingsInput): boolean {
  return value.baseCurrencyCode.trim().length === 3 && hasDistinctSelectedAccounts(value);
}

export function hasDistinctSelectedAccounts(value: AccountingControlSettingsInput): boolean {
  const selected = [
    value.retainedEarningsAccountId,
    value.incomeSummaryAccountId,
    value.roundingAccountId
  ].filter((accountId) => accountId.trim() !== "");

  return new Set(selected).size === selected.length;
}

export function getAccountingControlReadinessFacts({
  settings,
  value,
  activePostingAccountCount,
  retainedEarningsAccount,
  incomeSummaryAccount,
  roundingAccount,
  voucherRules,
  voucherRuleForms
}: {
  settings: AccountingControlSettings | null;
  value: AccountingControlSettingsInput;
  activePostingAccountCount: number;
  retainedEarningsAccount?: LedgerAccountSummary | null;
  incomeSummaryAccount?: LedgerAccountSummary | null;
  roundingAccount?: LedgerAccountSummary | null;
  voucherRules: VoucherNumberingRule[];
  voucherRuleForms: Record<string, VoucherNumberingRuleInput>;
}): AccountingControlFact[] {
  const selectedControlAccounts = [
    retainedEarningsAccount,
    incomeSummaryAccount,
    roundingAccount
  ];
  const missingAccountCount = [
    value.retainedEarningsAccountId,
    value.incomeSummaryAccountId,
    value.roundingAccountId
  ].filter((accountId) => accountId.trim() === "").length;
  const invalidSelectedAccountCount = selectedControlAccounts.filter(
    (account) => account !== null
      && account !== undefined
      && (account.status !== "Active" || !account.isPostingAccount)
  ).length;
  const voucherInvalidCount = voucherRules.filter((rule) => {
    const form = voucherRuleForms[rule.sourceType] ?? toVoucherRuleInput(rule);
    return !isVoucherRuleFormValid(form);
  }).length;
  const voucherInactiveCount = voucherRules.filter((rule) => {
    const form = voucherRuleForms[rule.sourceType] ?? toVoucherRuleInput(rule);
    return !form.isActive;
  }).length;
  const setupReady =
    value.baseCurrencyCode.trim().length === 3
    && missingAccountCount === 0
    && invalidSelectedAccountCount === 0
    && hasDistinctSelectedAccounts(value);

  return [
    {
      label: "Setup state",
      value: settings?.isConfigured && setupReady ? "Configured" : "Needs review",
      tone: settings?.isConfigured && setupReady ? "ready" : "warning"
    },
    {
      label: "Base currency",
      value: value.baseCurrencyCode.trim().toUpperCase() || "-",
      tone: value.baseCurrencyCode.trim().length === 3 ? "ready" : "warning"
    },
    {
      label: "Control accounts",
      value: missingAccountCount === 0 ? "3 selected" : `${missingAccountCount} missing`,
      tone: missingAccountCount === 0 && invalidSelectedAccountCount === 0 ? "ready" : "warning",
      title: invalidSelectedAccountCount === 0
        ? undefined
        : `${invalidSelectedAccountCount} selected accounts need review`
    },
    {
      label: "Distinct accounts",
      value: hasDistinctSelectedAccounts(value) ? "No overlap" : "Overlap found",
      tone: hasDistinctSelectedAccounts(value) ? "ready" : "warning"
    },
    {
      label: "Posting accounts",
      value: String(activePostingAccountCount),
      tone: activePostingAccountCount > 0 ? "ready" : "warning"
    },
    {
      label: "Voucher rules",
      value: voucherInvalidCount === 0
        ? `${voucherRules.length - voucherInactiveCount}/${voucherRules.length} active`
        : `${voucherInvalidCount} invalid`,
      tone: voucherInvalidCount === 0 ? "ready" : "warning"
    }
  ];
}

export function getAccountingControlSummaryFacts(
  settings: AccountingControlSettings | null
): AccountingControlFact[] {
  return [
    {
      label: "Retained earnings",
      value: formatControlAccount(settings?.retainedEarningsAccount),
      tone: settings?.retainedEarningsAccount ? "ready" : "warning"
    },
    {
      label: "Income summary",
      value: formatControlAccount(settings?.incomeSummaryAccount),
      tone: settings?.incomeSummaryAccount ? "ready" : "warning"
    },
    {
      label: "Rounding",
      value: formatControlAccount(settings?.roundingAccount),
      tone: settings?.roundingAccount ? "ready" : "warning"
    },
    {
      label: "Updated",
      value: settings?.updatedAtUtc ? settings.updatedAtUtc.slice(0, 10) : "-",
      tone: settings?.updatedAtUtc ? "neutral" : "warning"
    }
  ];
}

export function getVoucherNumberingFacts(
  voucherRules: VoucherNumberingRule[],
  voucherRuleForms: Record<string, VoucherNumberingRuleInput>
): AccountingControlFact[] {
  const activeCount = voucherRules.filter((rule) => {
    const form = voucherRuleForms[rule.sourceType] ?? toVoucherRuleInput(rule);
    return form.isActive;
  }).length;
  const customCount = voucherRules.filter((rule) => rule.isConfigured).length;
  const invalidCount = voucherRules.filter((rule) => {
    const form = voucherRuleForms[rule.sourceType] ?? toVoucherRuleInput(rule);
    return !isVoucherRuleFormValid(form);
  }).length;
  const changedCount = voucherRules.filter((rule) => {
    const form = voucherRuleForms[rule.sourceType] ?? toVoucherRuleInput(rule);
    return isVoucherRuleFormDirty(rule, form);
  }).length;

  return [
    {
      label: "Active rules",
      value: `${activeCount}/${voucherRules.length}`,
      tone: activeCount === voucherRules.length ? "ready" : "warning"
    },
    {
      label: "Custom rules",
      value: String(customCount),
      tone: customCount > 0 ? "ready" : "neutral"
    },
    {
      label: "Unsaved edits",
      value: changedCount === 0 ? "None" : String(changedCount),
      tone: changedCount === 0 ? "ready" : "warning"
    },
    {
      label: "Invalid patterns",
      value: invalidCount === 0 ? "None" : String(invalidCount),
      tone: invalidCount === 0 ? "ready" : "warning"
    }
  ];
}

export function getVoucherRuleState(
  rule: VoucherNumberingRule,
  form: VoucherNumberingRuleInput
): AccountingControlFact {
  if (!isVoucherRuleFormValid(form)) {
    return {
      label: "Invalid",
      value: "Invalid",
      tone: "warning"
    };
  }

  if (!form.isActive) {
    return {
      label: "Inactive",
      value: "Inactive",
      tone: "warning"
    };
  }

  if (isVoucherRuleFormDirty(rule, form)) {
    return {
      label: "Unsaved",
      value: "Unsaved",
      tone: "warning"
    };
  }

  return {
    label: "Ready",
    value: "Ready",
    tone: "ready"
  };
}

export function getControlAccountTone(
  account: LedgerAccountSummary | null | undefined,
  expectedType?: string
): AccountingControlTone {
  if (!account) {
    return "warning";
  }

  if (account.status !== "Active" || !account.isPostingAccount) {
    return "warning";
  }

  if (expectedType && account.type !== expectedType) {
    return "warning";
  }

  return "ready";
}

export function formatControlAccount(
  account: AccountingControlSettings["retainedEarningsAccount"]
): string {
  return account === null || account === undefined
    ? "-"
    : `${account.code} ${account.name}`;
}

export function formatControlAccountContext(
  account: LedgerAccountSummary | null | undefined,
  expectedType?: string
): string {
  if (!account) {
    return expectedType ? `Select an active ${expectedType} posting account` : "Select an active posting account";
  }

  const postingLabel = account.isPostingAccount ? "Posting" : "Non-posting";
  const expectedLabel = expectedType && account.type !== expectedType
    ? ` / Expected ${expectedType}`
    : "";

  return `${account.displayCode} / ${account.type} / ${account.normalBalance} / ${postingLabel} / ${account.status}${expectedLabel}`;
}

export function toVoucherRuleInput(rule: VoucherNumberingRule): VoucherNumberingRuleInput {
  return {
    prefix: rule.prefix,
    numberPaddingWidth: rule.numberPaddingWidth.toString(),
    isActive: rule.isActive
  };
}

export function isVoucherRuleFormValid(form: VoucherNumberingRuleInput): boolean {
  const width = Number(form.numberPaddingWidth);
  return form.prefix.trim() !== "" && Number.isInteger(width) && width >= 1 && width <= 10;
}

export function isVoucherRuleFormDirty(
  rule: VoucherNumberingRule,
  form: VoucherNumberingRuleInput
): boolean {
  return form.prefix !== rule.prefix
    || Number(form.numberPaddingWidth) !== rule.numberPaddingWidth
    || form.isActive !== rule.isActive;
}

export function formatVoucherPattern(form: VoucherNumberingRuleInput): string {
  const width = Number(form.numberPaddingWidth);

  if (form.prefix.trim() === "" || !Number.isInteger(width) || width < 1 || width > 10) {
    return "-";
  }

  return `${form.prefix.trim().toUpperCase()}${"0".repeat(Math.max(width - 1, 0))}1`;
}

export function formatSourceType(sourceType: string): string {
  return sourceType.replace(/([a-z])([A-Z])/g, "$1 $2");
}
