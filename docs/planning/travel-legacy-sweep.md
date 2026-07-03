# Travel Legacy Sweep

Date reviewed: 2026-07-03

## Sources Reviewed

| File | Evidence gathered |
| --- | --- |
| `E:/travel tour/travel/TRV.mdb` | Access travel database/app file. Direct table read was blocked because ACE/Jet OLE DB providers are not registered on this machine. Binary string scan still exposed form, report, class, module, and field names. |
| `E:/travel tour/travel/TRV.mde` | Compiled Access app. Binary string scan exposed travel UI/workflow names, reports, PNR reader classes, ticketing, visa, supplier, invoice, voucher, client, and accounting surfaces. |
| `C:/Users/Daniyal/Downloads/Data.sql` | SQL Server script for `Champion`, containing 122 tables plus views/procedures. This is accounting/back-office evidence for Control Desk's GL direction, while also showing what travel workflows post into or align with. |

## Control Desk Accounting Direction

The purpose of this sweep is not only to preserve a future Travel/Tour module reference. Its immediate value is to guide the accounting-specific direction inside SafarSuite Control Desk.

Use the Travel/Champion evidence for Control Desk accounting in these ways:

- Control Desk billing must sit on a controlled GL spine, not free-form account fields.
- Client/customer setup should create or select a controlled receivable ledger identity, following the legacy 9-digit party account plus 5-digit summary/control account pattern.
- Supplier-style payable accounts are not a first Control Desk screen, but the same account-role mapping model should support them later.
- Invoices, receipts, refunds, credit notes, debit notes, and adjustments should all produce balanced voucher/journal rows through backend transactions.
- Voucher/journal numbering, branch/company context, status, audit user/date fields, currency/rate, tax, discount, and file/reference numbers are part of the accounting model, not just print/report details.
- Ledger balances should come from opening balances plus posted voucher/journal lines, with branch-aware reporting and adjusted-date support as later accounting requirements.

## Travel App Evidence

The Travel Access app is not a small side form. It looks like a full module with these surfaces:

| Area | Legacy evidence |
| --- | --- |
| Client/customer | `Form_Client`, `Form_Client_List`, `Form_Client_Aging`, `Client_Code`, `Client_Name`, `Client_Sum_Code`, `Client_Account`, `Client_Category`, `Client_SP` |
| Airline/airport setup | `Form_AirLine`, `Form_Airline_List`, `Form_Airports`, `AIR_CODE`, `AIR_NAME`, `AIR_ABB`, `AirlineTaxes` |
| PNR import and parsing | `Form_PNR`, `Import_PNR_Data`, `clsPNRReader`, `clsPNRReaderAmadeus`, `clsPNRReaderGalileo`, `clsPNRReaderWorldspan`, `clsPNRDataLoader`, `modPNRImport`, `modPNRReader` |
| Ticketing | `AddTicket`, `AddConjunctiveTicket`, `Add_Sectors_to_Ticket`, `Form_TKTQ`, `Form_L_TKT`, `Form_Ticket_Move`, `Form_Tkt_Class`, `Form_Ref_Tkt`, `Form_Refund_List`, `Report_TKT_Q_R`, `Report_TKT_Q_S` |
| Passenger/sector data | `clsPassenger`, `clsPassengerCollection`, `clsSectorCollection`, `PassengerName`, `PassportNumber`, `Passport_Date`, `PNR_FLT_NUM`, `PNR_FLT_FM`, `PNR_FLT_TO`, `PNR_DEP_DATE`, `PNR_ARR_DATE` |
| Visa | `Form_MVISA`, `Form_DVISA`, `Form_Visa_List`, `Form_Visa_Rpt`, `cmdAddVisa`, `DL_VISA_NUM`, `DL_VISA_DATE`, `DN_VISA_TYPE`, `DN_VISA_STATUS`, `VS_PKG`, `VS_TYPE` |
| Tour/hotel | `BIN_TOURS`, `MTOUR`, `LTour_code`, `HOTEL`, `HOTEL_AMT`, `lblTourFinalBalance` |
| Invoice output | `Form_Auto_INV_GEN`, `Form_Invoice_List`, `Form_INV_CUS`, many invoice reports such as `Report_INV_LST`, `Report_Inv_PSF`, `Report_INV_VI`, `Report_Inv_SB`, `Report_INV_CN_LST` |
| Supplier side | `Form_SUP_Invoice`, `Form_SUP_VOUCHER`, `Form_SUP_Refund`, `Form_SUP_DebitNote`, `Form_SUP_MINV`, `Form_SUP_MVOU`, `Form_SUP_DVOU` |
| Accounting/vouchers | `Form_COA`, `Form_GL`, `Form_MVOU`, `Form_DVOU`, `Form_CVOU`, `Form_Rec_Vou`, `Form_Voucher_List`, `Form_Voucher_Copy`, `Form_Voucher_Contra` |
| Security | `Form_M_ROLES`, `Form_D_ROLES`, `Form_USEROP`, `Form_login`, `Form_SET_BYPASS` |

## SQL Accounting Evidence

`Data.sql` is not strongly travel-specific by table names, but it gives us useful shared accounting rules:

| Legacy object | Meaning for modern model |
| --- | --- |
| `ACT_SD_COA_LEVEL3` | Ledger account table: company code, 9-digit account code, description, nature, 5-digit summary code, type, active flag, currency |
| `ACT_SO_CUSTOMER` | Customer master is also a ledger identity: company, branch, 9-digit customer/account code, 5-digit summary/control code, name, short name, address/contact, category/group, currency, credit limit/terms |
| `ACT_SO_SUPPLIER` | Supplier master mirrors customer behavior with a 9-digit supplier/account code and 5-digit summary/control code |
| `ACT_TM_VOUCHER` / `ACT_TD_VOUCHER` | Voucher header/detail pattern. Detail carries account code, debit/credit amounts, foreign/base currency amounts, currency/rate, file number, tax, and remarks |
| `ACT_TM_SI` / `ACT_TD_SI` | Customer-side sales invoice master/detail pattern with company, branch, invoice number, customer code, gross/discount/tax/receipt totals, line amount, currency/rate, file number |
| `ACT_TM_PI` / `ACT_TD_PI` | Supplier-side purchase invoice master/detail pattern with supplier code, totals, item/line amount, currency/rate, expense fields |
| `ACT_TM_PR`, `ACT_TM_SR`, `ACT_TM_DC` families | Return/challan/related document families. Names need validation with sample data before modern naming is finalized |
| `Tax`, `Client_Discount`, `Credit_terms` | Tax percentages/categories/sections, client discount setup, and credit-term day limits |
| `MYK_XO_USERS`, `MYK_XM_SECURITY`, `MYK_XD_SECURITY` | Old user, security group, and per-program permission model; useful for capability mapping only, not for password/security implementation |
| `VUCustomer` | Customer read model joins customer, category, group, sales executive, and COA summary code |
| `VUActBalance` and `spActBalance` | Ledger balance is calculated from vouchers plus opening balances, with branch merge and adjusted-date options |

## Implementation Implications

- Travel/Tour operational screens are future SafarSuite app module work, not Control Desk screens. Control Desk should bill, license, and enable them through module entitlements.
- The accounting behavior behind those screens is Control Desk evidence now: controlled COA, party ledger identities, voucher/journal posting, invoice/receipt/refund chains, tax/discount setup, branch/currency context, and ledger reporting.
- When we enter the SafarSuite app workspace, the Travel module should be designed around the legacy work centers: client, airline/airport setup, PNR import, ticketing, visa, supplier voucher/invoice, invoice printout, refunds, and accounting posting.
- Customer and supplier masters should remain tied to controlled GL accounts. The 9-digit party account plus 5-digit summary/control account pattern reinforces the controlled COA direction from `GL_Working.xlsx`.
- Ticket/visa/travel invoices should not post free-form money changes. They should call shared accounting posting services that create balanced voucher/journal rows inside a transaction.
- The old security tables should become modern roles/permissions. Do not copy the old password storage or bypass mechanics.
- The old Access report catalog is valuable for later print/report parity, especially invoice, ticket, PNR, receipt, GL, and client aging reports.

## Follow-Up Needed

- Install or use an Access-capable export path later to read exact table/link/query definitions from `TRV.mdb`; current machine has no registered `Microsoft.ACE.OLEDB` or `Microsoft.Jet.OLEDB` provider.
- If the user can export Access object lists or table schemas from Access, add them here before implementing the Travel module.
- Before naming modern sales return/receipt/challan concepts, validate `ACT_TM_SR`, `ACT_TM_PR`, and `ACT_TM_DC` with sample rows or Access forms.
- Keep Travel operational screens reference-only for now, but use the accounting patterns immediately for Control Desk GL/accounting design.
