import {
  AlertCircle,
  Banknote,
  BarChart3,
  Building2,
  CheckCircle2,
  CircleDot,
  Cloud,
  FileCog,
  Gauge,
  KeyRound,
  LayoutDashboard,
  ListChecks,
  LogIn,
  LogOut,
  ShieldCheck,
  SlidersHorizontal
} from "lucide-react";
import type { LucideIcon } from "lucide-react";
import { useEffect, useState, type FormEvent } from "react";
import { ApiError } from "../shared/api/apiError";
import {
  clearControlDeskSession,
  controlDeskSessionInvalidatedEvent,
  getControlDeskSession,
  saveControlDeskSession,
  type StoredControlDeskSession
} from "../shared/api/controlDeskSession";
import { createLocalOperatorSession } from "../shared/api/localOperatorAuth";
import { ClientDeskPage } from "../modules/clients/pages/ClientDeskPage";
import {
  Client360Page,
  type Client360LaunchTarget,
  type Client360Tab
} from "../modules/client-360/pages/Client360Page";
import { CommandCenterPage } from "../modules/command-center/pages/CommandCenterPage";
import { SetupWorkspacePage } from "../modules/setup/pages/SetupWorkspacePage";
import { ReportsAuditPage } from "../modules/reports";

type WorkSectionKey =
  | "command-center"
  | "setup"
  | "client-360"
  | "commercial"
  | "accounting"
  | "deployment-cloud"
  | "access-security"
  | "reports-audit"
  | "legacy-desk";

type WorkSection = {
  key: WorkSectionKey;
  label: string;
  shortLabel: string;
  description: string;
  status: string;
  Icon: LucideIcon;
};

type WorkSectionPanel = {
  title: string;
  kicker: string;
  items: string[];
};

type LoginForm = {
  email: string;
  password: string;
  expiresInMinutes: number;
};

type Client360OpenTarget = {
  clientId: string;
  tab: Client360Tab;
  reason: string;
};

const sectionOrder: WorkSection[] = [
  {
    key: "command-center",
    label: "Command Center",
    shortLabel: "Command",
    description: "The daily starting point for client priority work, setup gaps, money work, and cloud health.",
    status: "1 Daily queue",
    Icon: LayoutDashboard
  },
  {
    key: "client-360",
    label: "Client 360",
    shortLabel: "Client",
    description: "One selected client view across profile, setup, contract, vouchers, access, cloud, and support.",
    status: "2 Client home",
    Icon: Building2
  },
  {
    key: "commercial",
    label: "Client Money",
    shortLabel: "Money",
    description: "Client contract, charges, invoices, receipts, credits, refunds, and payment review.",
    status: "3 Bill/receive",
    Icon: Banknote
  },
  {
    key: "accounting",
    label: "Voucher Register",
    shortLabel: "Vouchers",
    description: "Simple client voucher proof for invoices, receipts, credits, refunds, and adjustments.",
    status: "4 Proof",
    Icon: ListChecks
  },
  {
    key: "deployment-cloud",
    label: "Cloud & Installation",
    shortLabel: "Cloud",
    description: "Cloud publishing, local server setup, package handoff, heartbeat, diagnostics, and commands.",
    status: "5 Local/cloud",
    Icon: Cloud
  },
  {
    key: "setup",
    label: "Setup",
    shortLabel: "Setup",
    description: "Reusable client, product, billing, ledger, and deployment defaults created once.",
    status: "Defaults",
    Icon: SlidersHorizontal
  },
  {
    key: "access-security",
    label: "Access & Security",
    shortLabel: "Security",
    description: "Provider operators, scopes, MFA, local devices, and pairing security events.",
    status: "Trust",
    Icon: ShieldCheck
  },
  {
    key: "reports-audit",
    label: "Reports & Audit",
    shortLabel: "Audit",
    description: "Client statements, voucher proof, cloud receipts, diagnostics history, and audit evidence.",
    status: "Evidence",
    Icon: BarChart3
  },
  {
    key: "legacy-desk",
    label: "Admin Desk",
    shortLabel: "Admin",
    description: "Advanced accounting and fallback tools that should not be part of normal client work.",
    status: "Advanced",
    Icon: Gauge
  }
];

const sectionPanels: Record<Exclude<WorkSectionKey, "legacy-desk">, WorkSectionPanel[]> = {
  "command-center": [
    {
      kicker: "Today",
      title: "Work Queue",
      items: [
        "Setup gaps",
        "Invoice and payment work",
        "Cloud install warnings",
        "Client lifecycle issues"
      ]
    },
    {
      kicker: "Flow",
      title: "Operating Path",
      items: [
        "Client 360",
        "Client money",
        "Voucher proof",
        "Cloud and local server",
        "Setup defaults"
      ]
    },
    {
      kicker: "Signal",
      title: "Health Snapshot",
      items: [
        "Active clients",
        "Open receivables",
        "Access renewals",
        "Local heartbeat"
      ]
    }
  ],
  setup: [
    {
      kicker: "Client",
      title: "Client Defaults",
      items: [
        "Client profile rules",
        "Billing/support contacts",
        "Portal invitation defaults",
        "Deployment profile defaults"
      ]
    },
    {
      kicker: "Commercial",
      title: "Product & Billing Defaults",
      items: [
        "Product modules",
        "Charge templates",
        "Contract defaults",
        "Billing cycle defaults"
      ]
    },
    {
      kicker: "Proof",
      title: "Voucher & Ledger Defaults",
      items: [
        "Receivable control account",
        "Revenue defaults",
        "Voucher numbering",
        "Opening balance profile"
      ]
    }
  ],
  "client-360": [
    {
      kicker: "Identity",
      title: "Client Record",
      items: [
        "Profile",
        "Contacts",
        "Support notes",
        "Portal invitations"
      ]
    },
    {
      kicker: "State",
      title: "Client Position",
      items: [
        "Active contract",
        "Statement balance",
        "Latest access renewal",
        "Deployment health"
      ]
    },
    {
      kicker: "Action",
      title: "Next Best Work",
      items: [
        "Complete setup",
        "Draft invoice",
        "Record receipt",
        "Send to Cloud"
      ]
    }
  ],
  commercial: [
    {
      kicker: "Agreement",
      title: "Contract & Pricing",
      items: [
        "Client contract",
        "Module allowances",
        "Billing cycle",
        "Paid add-on rules"
      ]
    },
    {
      kicker: "Billing",
      title: "Invoice Voucher Flow",
      items: [
        "Generate draft",
        "Issue invoice",
        "Void unpaid invoice",
        "Issue credit note"
      ]
    },
    {
      kicker: "Cash",
      title: "Receipt Flow",
      items: [
        "Record payment",
        "Approve/reject review",
        "Reverse receipt",
        "Apply credit",
        "Issue refund"
      ]
    }
  ],
  accounting: [
    {
      kicker: "Register",
      title: "Client Voucher Proof",
      items: [
        "Invoice vouchers",
        "Receipt vouchers",
        "Credit note vouchers",
        "Refund vouchers"
      ]
    },
    {
      kicker: "Status",
      title: "Voucher State",
      items: [
        "Draft or issued",
        "Paid or credited",
        "Cloud pending/sent",
        "Approval trail"
      ]
    },
    {
      kicker: "Proof",
      title: "Accounting Proof On Demand",
      items: [
        "Source document lookup",
        "Expandable posting lines",
        "Created/approved by",
        "Audit evidence"
      ]
    }
  ],
  "deployment-cloud": [
    {
      kicker: "Install",
      title: "Client Installation",
      items: [
        "Local server profile",
        "Setup token",
        "Bootstrap package",
        "Package handoff"
      ]
    },
    {
      kicker: "Runtime",
      title: "Local Server Status",
      items: [
        "Registration",
        "Heartbeat",
        "Access pull",
        "Diagnostics"
      ]
    },
    {
      kicker: "Support",
      title: "Operator Actions",
      items: [
        "Queue diagnostics",
        "Refresh entitlement",
        "Issue app activation",
        "Revoke activation"
      ]
    }
  ],
  "access-security": [
    {
      kicker: "Provider",
      title: "Operator Access",
      items: [
        "Operator users",
        "Scopes",
        "Password reset",
        "MFA and recovery"
      ]
    },
    {
      kicker: "Local",
      title: "Device Trust",
      items: [
        "Pending devices",
        "Approve device",
        "Suspend device",
        "Revoke device"
      ]
    },
    {
      kicker: "Defense",
      title: "Pairing Security",
      items: [
        "Security events",
        "Abuse summary",
        "Quarantine source",
        "Deny/release source"
      ]
    }
  ],
  "reports-audit": [
    {
      kicker: "Client",
      title: "Statement Evidence",
      items: [
        "Invoices",
        "Payments",
        "Credits/refunds",
        "Voucher proof"
      ]
    },
    {
      kicker: "Cloud",
      title: "Communication Evidence",
      items: [
        "Outbox messages",
        "Cloud receipts",
        "Diagnostics history",
        "Package handoff proof"
      ]
    },
    {
      kicker: "Admin",
      title: "Advanced Reports",
      items: [
        "Trial balance",
        "Profit and loss",
        "Balance sheet",
        "Ledger activity"
      ]
    }
  ]
};

const defaultLoginForm: LoginForm = {
  email: "",
  password: "",
  expiresInMinutes: 480
};

export function App() {
  const [session, setSession] = useState<StoredControlDeskSession | null>(
    () => getControlDeskSession()
  );

  useEffect(() => {
    const handleSessionInvalidated = () => setSession(null);
    window.addEventListener(controlDeskSessionInvalidatedEvent, handleSessionInvalidated);

    return () => window.removeEventListener(
      controlDeskSessionInvalidatedEvent,
      handleSessionInvalidated
    );
  }, []);
  const [activeSection, setActiveSection] = useState<WorkSectionKey>(() => getInitialSection());
  const [client360LaunchTarget, setClient360LaunchTarget] =
    useState<Client360LaunchTarget | null>(null);

  useEffect(() => {
    const handleHashChange = () => {
      const nextSection = getInitialSection();

      if (nextSection !== "client-360") {
        setClient360LaunchTarget(null);
      }

      setActiveSection(nextSection);
    };

    window.addEventListener("hashchange", handleHashChange);

    return () => window.removeEventListener("hashchange", handleHashChange);
  }, []);

  function handleSectionChange(section: WorkSectionKey) {
    if (section !== "client-360") {
      setClient360LaunchTarget(null);
    }

    setActiveSection(section);
    window.location.hash = section;
  }

  function handleOpenClient360(target: Client360OpenTarget) {
    setClient360LaunchTarget({
      clientId: target.clientId,
      tab: target.tab,
      sequence: Date.now()
    });
    setActiveSection("client-360");
    window.location.hash = "client-360";
  }

  function handleSignedIn(nextSession: StoredControlDeskSession) {
    saveControlDeskSession(nextSession);
    setSession(nextSession);
  }

  function handleSignOut() {
    clearControlDeskSession();
    setSession(null);
  }

  if (session === null) {
    return <LoginPage onSignedIn={handleSignedIn} />;
  }

  return (
    <AuthenticatedShell
      activeSection={activeSection}
      client360LaunchTarget={client360LaunchTarget}
      session={session}
      onOpenClient360={handleOpenClient360}
      onSectionChange={handleSectionChange}
      onSignOut={handleSignOut}
    />
  );
}

function LoginPage({
  onSignedIn
}: {
  onSignedIn: (session: StoredControlDeskSession) => void;
}) {
  const [form, setForm] = useState<LoginForm>(defaultLoginForm);
  const [isBusy, setIsBusy] = useState(false);
  const [error, setError] = useState("");

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setIsBusy(true);
    setError("");

    try {
      const createdSession = await createLocalOperatorSession({
        email: form.email,
        password: form.password,
        expiresInMinutes: form.expiresInMinutes
      });

      onSignedIn({
        ...createdSession,
        email: createdSession.email ?? form.email.trim()
      });
    } catch (caughtError) {
      setError(formatError(caughtError));
    } finally {
      setIsBusy(false);
    }
  }

  return (
    <main className="app-shell overhaul-login-shell">
      <section className="overhaul-login-panel">
        <div className="overhaul-login-brand">
          <span>SafarSuite</span>
          <h1>Control Desk</h1>
          <p>Local operator workspace</p>
        </div>

        <form className="overhaul-login-form" onSubmit={handleSubmit}>
          <div className="overhaul-login-heading">
            <LogIn size={18} />
            <strong>Sign in</strong>
          </div>

          {error !== "" && (
            <div className="overhaul-login-error" role="alert">
              <AlertCircle size={16} />
              <span>{error}</span>
            </div>
          )}

          <label className="form-field">
            <span>Email</span>
            <input
              autoComplete="username"
              disabled={isBusy}
              maxLength={320}
              type="email"
              value={form.email}
              onChange={(event) => setForm({ ...form, email: event.target.value })}
            />
          </label>

          <label className="form-field">
            <span>Password</span>
            <input
              autoComplete="current-password"
              disabled={isBusy}
              type="password"
              value={form.password}
              onChange={(event) => setForm({ ...form, password: event.target.value })}
            />
          </label>

          <label className="form-field">
            <span>Session minutes</span>
            <input
              disabled={isBusy}
              max={1440}
              min={5}
              type="number"
              value={form.expiresInMinutes}
              onChange={(event) =>
                setForm({
                  ...form,
                  expiresInMinutes: Number(event.target.value)
                })
              }
            />
          </label>

          <button
            className="icon-button primary overhaul-login-submit"
            disabled={
              isBusy
              || form.email.trim() === ""
              || form.password.trim() === ""
              || form.expiresInMinutes < 5
              || form.expiresInMinutes > 1440
            }
            type="submit"
          >
            <KeyRound size={16} />
            Sign in
          </button>
        </form>
      </section>
    </main>
  );
}

function AuthenticatedShell({
  activeSection,
  client360LaunchTarget,
  session,
  onOpenClient360,
  onSectionChange,
  onSignOut
}: {
  activeSection: WorkSectionKey;
  client360LaunchTarget: Client360LaunchTarget | null;
  session: StoredControlDeskSession;
  onOpenClient360: (target: Client360OpenTarget) => void;
  onSectionChange: (section: WorkSectionKey) => void;
  onSignOut: () => void;
}) {
  const section = getSection(activeSection);

  return (
    <main className="app-shell overhaul-app-shell">
      <aside className="overhaul-sidebar" aria-label="Control Desk work areas">
        <div className="overhaul-sidebar-brand">
          <span>SafarSuite</span>
          <strong>Control Desk</strong>
        </div>

        <nav className="overhaul-nav" aria-label="Primary work areas">
          {sectionOrder.map((item) => (
            <button
              aria-current={activeSection === item.key ? "page" : undefined}
              className={activeSection === item.key ? "active" : ""}
              key={item.key}
              onClick={() => onSectionChange(item.key)}
              title={item.label}
              type="button"
            >
              <item.Icon size={18} />
              <span>
                <strong>{item.shortLabel}</strong>
                <small>{item.status}</small>
              </span>
            </button>
          ))}
        </nav>
      </aside>

      <section className="overhaul-main">
        <header className="overhaul-topbar">
          <div>
            <span>{section.status}</span>
            <h1>{section.label}</h1>
          </div>
          <div className="overhaul-session">
            <span>{session.actor}</span>
            <small>{formatSessionExpiry(session.expiresAtUtc)}</small>
            <button
              className="icon-button"
              onClick={onSignOut}
              title="Sign out"
              type="button"
            >
              <LogOut size={16} />
              Sign out
            </button>
          </div>
        </header>

        {activeSection === "legacy-desk" ? (
          <div className="overhaul-legacy-frame">
            <ClientDeskPage />
          </div>
        ) : (
          <WorkSectionPage
            client360LaunchTarget={client360LaunchTarget}
            section={section}
            onOpenClient360={onOpenClient360}
            onSectionChange={onSectionChange}
          />
        )}
      </section>
    </main>
  );
}

function WorkSectionPage({
  client360LaunchTarget,
  section,
  onOpenClient360,
  onSectionChange
}: {
  client360LaunchTarget: Client360LaunchTarget | null;
  section: WorkSection;
  onOpenClient360: (target: Client360OpenTarget) => void;
  onSectionChange: (section: WorkSectionKey) => void;
}) {
  if (section.key === "setup") {
    return <SetupWorkspacePage onOpenLegacyDesk={() => onSectionChange("legacy-desk")} />;
  }

  if (section.key === "command-center") {
    return (
      <CommandCenterPage
        onOpenClientAction={onOpenClient360}
        onOpenSection={onSectionChange}
      />
    );
  }

  if (section.key === "client-360") {
    return <Client360Page launchTarget={client360LaunchTarget} />;
  }

  if (section.key === "reports-audit") {
    return <ReportsAuditPage />;
  }

  const panels = getSectionPanels(section.key);
  const sectionIndex = sectionOrder.findIndex((item) => item.key === section.key);
  const nextSection = sectionOrder[sectionIndex + 1] ?? sectionOrder[0];

  return (
    <section className="overhaul-workspace">
      <div className="overhaul-workspace-intro">
        <div>
          <span>UX overhaul foundation</span>
          <h2>{section.label}</h2>
          <p>{section.description}</p>
        </div>
        <div className="overhaul-stage-badge">
          <CircleDot size={16} />
          <span>Slice 1</span>
        </div>
      </div>

      <div className="overhaul-flow-strip" aria-label="Workspace flow">
        {sectionOrder
          .filter((item) => item.key !== "legacy-desk")
          .slice(0, 8)
          .map((item) => (
            <span
              className={item.key === section.key ? "active" : ""}
              key={item.key}
            >
              <item.Icon size={14} />
              {item.shortLabel}
            </span>
          ))}
      </div>

      <div className="overhaul-panel-grid">
        {panels.map((panel) => (
          <section className="overhaul-work-panel" key={panel.title}>
            <span>{panel.kicker}</span>
            <h3>{panel.title}</h3>
            <ul>
              {panel.items.map((item) => (
                <li key={item}>
                  <CheckCircle2 size={14} />
                  {item}
                </li>
              ))}
            </ul>
          </section>
        ))}
      </div>

      <div className="overhaul-next-step">
        <FileCog size={18} />
        <div>
          <span>Next migration target</span>
          <strong>{nextSection.label}</strong>
        </div>
      </div>
    </section>
  );
}

function getSectionPanels(key: WorkSectionKey): WorkSectionPanel[] {
  if (key === "legacy-desk") {
    return [];
  }

  return sectionPanels[key];
}

function getInitialSection(): WorkSectionKey {
  if (typeof window === "undefined") {
    return "command-center";
  }

  const hashValue = window.location.hash.replace(/^#/, "");
  const section = sectionOrder.find((item) => item.key === hashValue);

  return section?.key ?? "command-center";
}

function getSection(key: WorkSectionKey): WorkSection {
  return sectionOrder.find((section) => section.key === key) ?? sectionOrder[0];
}

function formatSessionExpiry(value: string): string {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "Session active";
  }

  return `Expires ${new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short"
  }).format(date)}`;
}

function formatError(caughtError: unknown): string {
  if (caughtError instanceof ApiError) {
    return caughtError.errors[0]?.message ?? caughtError.message;
  }

  if (caughtError instanceof Error) {
    return caughtError.message;
  }

  return "Sign-in failed.";
}
