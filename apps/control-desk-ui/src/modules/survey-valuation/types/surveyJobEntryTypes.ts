export type SurveyPaymentMode = "Unknown" | "Advance" | "Single" | "Master";

export type SurveyJobStatus =
  | "Draft"
  | "Received"
  | "Pending"
  | "Unsettled"
  | "Delivered"
  | "Settled"
  | "Cancelled";

export type SurveyDocumentType =
  | "ClaimForm"
  | "RegistrationBook"
  | "DrivingLicense"
  | "InsurancePolicy"
  | "Cnic"
  | "PoliceReport"
  | "Fir"
  | "DischargeSheet"
  | "FinalFir"
  | "PurchaseReceipt"
  | "TaxPaidReceipt"
  | "OwnerStatus"
  | "TransferLetter"
  | "VehicleKeys"
  | "OwnerCertificate";

export type SurveyDocumentStatus = "Unknown" | "Received" | "Missing" | "NotRequired";

export type SurveyInvoiceLineDescriptionType =
  | "Auto"
  | "Manual"
  | "Head1"
  | "Head2"
  | "SalesTax";

export type SurveyJobEntryFields = {
  surveyTypeCode: string;
  intimationDate: string;
  deliveredDate: string;
  reInspectionDate: string;
  invoiceDate: string;
  voucherDate: string;
  discountDate: string;
  purchaseOrderDate: string;
  clientCode: string;
  clientBranchCode: string;
  companyBranchCode: string;
  billingBranchCode: string;
  paymentMode: SurveyPaymentMode;
  isReInspection: boolean;
  insuredName: string;
  insuredPhone: string;
  insuredEmail: string;
  insuredAddress: string;
  insuredCnic: string;
  contactPerson: string;
  contactDesignationCode: string;
  referenceNumber: string;
  ccNumber: string;
  surveyorCode: string;
  supervisorCode: string;
  claimTypeCode: string;
  requestSourceCode: string;
  areaCode: string;
  agencyCode: string;
  vehicleMake: string;
  vehicleRegistrationNumber: string;
  vehicleChassisNumber: string;
  vehicleModel: string;
  vehicleEngineNumber: string;
  workshopCode: string;
  lossNumber: string;
  policyNumber: string;
  purchaseOrderNumber: string;
  remarks: string;
};

export type SurveyMoney = {
  amount: number;
  currencyCode: string;
};

export type SurveyJobEntry = {
  surveyJobId: string;
  surveyJobNumber: string;
  identity: {
    surveyTypeCode: string;
    clientCode: string | null;
    clientBranchCode: string | null;
    companyBranchCode: string | null;
    billingBranchCode: string | null;
    paymentMode: SurveyPaymentMode;
    status: SurveyJobStatus;
    isReInspection: boolean;
  };
  dates: {
    intimationDate: string;
    deliveredDate: string | null;
    reInspectionDate: string | null;
    invoiceDate: string | null;
    voucherDate: string | null;
    discountDate: string | null;
    purchaseOrderDate: string | null;
  };
  insuredParty: {
    name: string;
    phone: string | null;
    email: string | null;
    address: string | null;
    cnic: string | null;
    contactPerson: string | null;
    contactDesignationCode: string | null;
    referenceNumber: string | null;
    ccNumber: string | null;
  } | null;
  assignment: {
    surveyorCode: string | null;
    supervisorCode: string | null;
    claimTypeCode: string | null;
    requestSourceCode: string | null;
    areaCode: string | null;
    agencyCode: string | null;
  };
  vehicle: {
    make: string | null;
    registrationNumber: string | null;
    chassisNumber: string | null;
    model: string | null;
    engineNumber: string | null;
    workshopCode: string | null;
  };
  policyLoss: {
    lossNumber: string | null;
    policyNumber: string | null;
    purchaseOrderNumber: string | null;
  };
  invoiceLineTotal: SurveyMoney;
  documents: SurveyDocumentChecklistItem[];
  invoiceLines: SurveyJobInvoiceLine[];
  invoiceSummary: {
    invoiceNumber: string | null;
    netAmount: SurveyMoney | null;
    grossAmount: SurveyMoney | null;
    discountAmount: SurveyMoney | null;
    salesTaxPercent: number | null;
    salesTaxAmount: SurveyMoney | null;
    paymentMode: SurveyPaymentMode;
    workshopPaymentAmount: SurveyMoney | null;
    voucherNumber: string | null;
    journalCode: string | null;
    discountJournalCode: string | null;
  };
  remarks: string | null;
  createdAtUtc: string;
};

export type SurveyDocumentChecklistItem = {
  type: SurveyDocumentType;
  status: SurveyDocumentStatus;
  receivedOn: string | null;
};

export type SurveyJobInvoiceLine = {
  sequenceNumber: number;
  descriptionType: SurveyInvoiceLineDescriptionType;
  description: string;
  amount: SurveyMoney;
  billingHeadCode: string | null;
  taxCode: string | null;
  categoryCode: string | null;
};

export type EditableSurveyJobInvoiceLine = {
  sequenceNumber: number;
  descriptionType: SurveyInvoiceLineDescriptionType;
  description: string;
  amount: string;
  currencyCode: string;
  billingHeadCode: string;
  taxCode: string;
  categoryCode: string;
};

export type SurveyBillingDraftFields = {
  clientId: string;
  contractId: string;
  invoiceNumber: string;
  issueDate: string;
  dueDate: string;
  currencyCode: string;
};

export type SurveyBillingDraftResult = {
  invoiceId: string;
  invoiceNumber: string;
  status: string;
  totalAmount: number;
  balanceDue: number;
  currencyCode: string;
  lines: SurveyBillingDraftLine[];
  surveyJob: SurveyJobEntry;
};

export type SurveyBillingDraftLine = {
  chargeCodeId: string;
  chargeCode: string;
  description: string;
  amount: number;
  currencyCode: string;
};

export type ClientLookupOption = {
  clientId: string;
  code: string;
  legalName: string;
  displayName: string;
  status: string;
};

export type ChargeCodeLookupOption = {
  chargeCodeId: string;
  code: string;
  name: string;
  defaultUnitPriceAmount: number;
  currencyCode: string;
  revenueAccountId: string;
  taxAccountId: string | null;
  status: string;
};

export const emptySurveyJobEntryFields: SurveyJobEntryFields = {
  surveyTypeCode: "",
  intimationDate: new Date().toISOString().slice(0, 10),
  deliveredDate: "",
  reInspectionDate: "",
  invoiceDate: "",
  voucherDate: "",
  discountDate: "",
  purchaseOrderDate: "",
  clientCode: "",
  clientBranchCode: "",
  companyBranchCode: "",
  billingBranchCode: "",
  paymentMode: "Unknown",
  isReInspection: false,
  insuredName: "",
  insuredPhone: "",
  insuredEmail: "",
  insuredAddress: "",
  insuredCnic: "",
  contactPerson: "",
  contactDesignationCode: "",
  referenceNumber: "",
  ccNumber: "",
  surveyorCode: "",
  supervisorCode: "",
  claimTypeCode: "",
  requestSourceCode: "",
  areaCode: "",
  agencyCode: "",
  vehicleMake: "",
  vehicleRegistrationNumber: "",
  vehicleChassisNumber: "",
  vehicleModel: "",
  vehicleEngineNumber: "",
  workshopCode: "",
  lossNumber: "",
  policyNumber: "",
  purchaseOrderNumber: "",
  remarks: ""
};

export const surveyDocumentTypeLabels: Record<SurveyDocumentType, string> = {
  ClaimForm: "Claim Form",
  RegistrationBook: "Registration Book",
  DrivingLicense: "Driving License",
  InsurancePolicy: "Insurance Policy",
  Cnic: "CNIC",
  PoliceReport: "Police Report",
  Fir: "FIR",
  DischargeSheet: "Discharge Sheet",
  FinalFir: "Final FIR",
  PurchaseReceipt: "Purchase Receipt",
  TaxPaidReceipt: "Tax Paid Receipt",
  OwnerStatus: "Owner Status",
  TransferLetter: "Transfer Letter",
  VehicleKeys: "Vehicle Keys",
  OwnerCertificate: "Owner Certificate"
};

export const surveyDocumentTypes = Object.keys(
  surveyDocumentTypeLabels
) as SurveyDocumentType[];

export const surveyDocumentStatuses: SurveyDocumentStatus[] = [
  "Unknown",
  "Received",
  "Missing",
  "NotRequired"
];

export const defaultSurveyDocuments: SurveyDocumentChecklistItem[] = surveyDocumentTypes.map(
  (type) => ({
    type,
    status: "Unknown",
    receivedOn: null
  })
);

export const surveyInvoiceLineDescriptionTypes: SurveyInvoiceLineDescriptionType[] = [
  "Auto",
  "Manual",
  "Head1",
  "Head2",
  "SalesTax"
];

export const emptySurveyBillingDraftFields: SurveyBillingDraftFields = {
  clientId: "",
  contractId: "",
  invoiceNumber: "",
  issueDate: new Date().toISOString().slice(0, 10),
  dueDate: "",
  currencyCode: "PKR"
};

export function createEmptyInvoiceLine(sequenceNumber: number): EditableSurveyJobInvoiceLine {
  return {
    sequenceNumber,
    descriptionType: "Manual",
    description: "",
    amount: "0.00",
    currencyCode: "PKR",
    billingHeadCode: "",
    taxCode: "",
    categoryCode: ""
  };
}
