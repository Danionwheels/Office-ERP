using SafarSuite.ControlCloud.Application.Modules.ClientPortal;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.CreatePortalPaymentClaim;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.GetPortalPaymentClaim;
using SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;
using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Identity.Tests;

public sealed class ClientPortalPaymentClaimHandlerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 14, 10, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateDoesNotFindAnInvoiceOwnedByAnotherClient()
    {
        var clientId = Guid.NewGuid();
        var otherClientId = Guid.NewGuid();
        var invoice = CreateInvoice();
        var harness = CreateHarness((otherClientId, invoice));

        var result = await harness.Handler.HandleAsync(CreateCommand(clientId, invoice.InvoiceId));

        Assert.False(result.IsSuccess);
        Assert.Equal("PortalInvoiceNotFound", result.FailureCode);
        Assert.Equal((clientId, invoice.InvoiceId), harness.Projections.LastInvoiceLookup);
        Assert.Empty(harness.Claims.Added);
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public async Task CreateRejectsProofNotOwnedByTheSubmittingPortalUser(
        bool sameClient,
        bool sameUser)
    {
        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invoice = CreateInvoice();
        var proof = new ControlCloudClientPortalAttachment
        {
            AttachmentId = Guid.NewGuid(),
            ClientId = sameClient ? clientId : Guid.NewGuid(),
            UploadedByUserId = sameUser ? userId : Guid.NewGuid()
        };
        var harness = CreateHarness((clientId, invoice), proof: proof);

        var result = await harness.Handler.HandleAsync(
            CreateCommand(clientId, invoice.InvoiceId, userId, proof.AttachmentId));

        Assert.False(result.IsSuccess);
        Assert.Equal("PaymentProofNotFound", result.FailureCode);
        Assert.Empty(harness.Claims.Added);
    }

    [Fact]
    public async Task CreateRejectsADuplicateReferenceWithinTheClient()
    {
        var clientId = Guid.NewGuid();
        var invoice = CreateInvoice();
        var duplicate = CreateClaim(clientId, invoice, 10m, "BANK-REFERENCE-1");
        var harness = CreateHarness((clientId, invoice), claims: [duplicate]);

        var result = await harness.Handler.HandleAsync(
            CreateCommand(
                clientId,
                invoice.InvoiceId,
                transferReferenceNumber: "  bank-reference-1  "));

        Assert.False(result.IsSuccess);
        Assert.Equal("PaymentClaimDuplicate", result.FailureCode);
        Assert.Empty(harness.Claims.Added);
    }

    [Fact]
    public async Task CreateSubtractsPendingClaimsFromTheAvailableInvoiceBalance()
    {
        var clientId = Guid.NewGuid();
        var invoice = CreateInvoice(balanceDue: 100m);
        var pending = CreateClaim(clientId, invoice, 70m, "PENDING-1");
        var harness = CreateHarness((clientId, invoice), claims: [pending]);

        var result = await harness.Handler.HandleAsync(
            CreateCommand(
                clientId,
                invoice.InvoiceId,
                amount: 30.01m,
                transferReferenceNumber: "PENDING-2"));

        Assert.False(result.IsSuccess);
        Assert.Equal("PaymentClaimAmountInvalid", result.FailureCode);
        Assert.Contains("after pending claims", result.Detail, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(harness.Claims.Added);
    }

    [Fact]
    public async Task CreatePersistsAValidClaimAsPendingVerification()
    {
        var clientId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var invoice = CreateInvoice(balanceDue: 100m);
        var harness = CreateHarness((clientId, invoice));

        var result = await harness.Handler.HandleAsync(
            CreateCommand(
                clientId,
                invoice.InvoiceId,
                userId,
                amount: 42.50m,
                transferReferenceNumber: " transfer-42 "));

        Assert.True(result.IsSuccess);
        var claim = Assert.Single(harness.Claims.Added);
        Assert.Same(claim, result.Value!.Claim);
        Assert.Equal(clientId, claim.ClientId);
        Assert.Equal(userId, claim.SubmittedByUserId);
        Assert.Equal(invoice.InvoiceId, claim.InvoiceId);
        Assert.Equal(42.50m, claim.Amount);
        Assert.Equal("transfer-42", claim.TransferReferenceNumber);
        Assert.Equal("TRANSFER-42", claim.NormalizedTransferReferenceNumber);
        Assert.Equal(ControlCloudClientPortalPaymentClaimStatus.PendingVerification, claim.Status);
        Assert.Equal(Now, claim.SubmittedAtUtc);
        Assert.Single(harness.Audit.Records);
    }

    [Fact]
    public async Task GetDoesNotReturnAClaimForADifferentRequiredClient()
    {
        var clientId = Guid.NewGuid();
        var otherClientId = Guid.NewGuid();
        var invoice = CreateInvoice();
        var claim = CreateClaim(clientId, invoice, 10m, "CLAIM-1", Guid.NewGuid());
        var claims = new PaymentClaimRepositoryFake([claim]);
        var attachments = new AttachmentRepositoryFake();
        var handler = new GetClientPortalPaymentClaimHandler(claims, attachments);

        var result = await handler.HandleAsync(claim.ClaimId, otherClientId);

        Assert.False(result.IsSuccess);
        Assert.Equal("PaymentClaimNotFound", result.FailureCode);
        Assert.Equal(0, attachments.GetByIdCount);
    }

    private static Harness CreateHarness(
        (Guid ClientId, ControlCloudInvoiceProjection Invoice) projectedInvoice,
        IReadOnlyCollection<ControlCloudClientPortalPaymentClaim>? claims = null,
        ControlCloudClientPortalAttachment? proof = null)
    {
        var projections = new CommercialProjectionRepositoryFake([projectedInvoice]);
        var claimRepository = new PaymentClaimRepositoryFake(claims ?? []);
        var attachments = new AttachmentRepositoryFake(proof is null ? [] : [proof]);
        var audit = new AuditRecorderFake();
        var handler = new CreateClientPortalPaymentClaimHandler(
            projections,
            claimRepository,
            attachments,
            audit,
            new IdentityTestUnitOfWork(),
            new IdentityTestClock(Now));

        return new Harness(handler, projections, claimRepository, audit);
    }

    private static CreateClientPortalPaymentClaimCommand CreateCommand(
        Guid clientId,
        Guid invoiceId,
        Guid? userId = null,
        Guid? proofAttachmentId = null,
        decimal amount = 10m,
        string transferReferenceNumber = "REFERENCE-1") =>
        new(
            clientId,
            userId ?? Guid.NewGuid(),
            invoiceId,
            amount,
            transferReferenceNumber,
            proofAttachmentId);

    private static ControlCloudInvoiceProjection CreateInvoice(decimal balanceDue = 100m) =>
        new(
            Guid.NewGuid(),
            "INV-1001",
            Guid.NewGuid(),
            "Issued",
            new DateOnly(2026, 7, 1),
            new DateOnly(2026, 7, 31),
            100m,
            balanceDue,
            "PKR");

    private static ControlCloudClientPortalPaymentClaim CreateClaim(
        Guid clientId,
        ControlCloudInvoiceProjection invoice,
        decimal amount,
        string reference,
        Guid? proofAttachmentId = null) =>
        ControlCloudClientPortalPaymentClaim.Create(
            Guid.NewGuid(),
            clientId,
            Guid.NewGuid(),
            invoice.InvoiceId,
            invoice.InvoiceNumber,
            amount,
            invoice.CurrencyCode,
            reference,
            proofAttachmentId,
            Now.AddMinutes(-5));

    private sealed record Harness(
        CreateClientPortalPaymentClaimHandler Handler,
        CommercialProjectionRepositoryFake Projections,
        PaymentClaimRepositoryFake Claims,
        AuditRecorderFake Audit);

    private sealed class CommercialProjectionRepositoryFake(
        IEnumerable<(Guid ClientId, ControlCloudInvoiceProjection Invoice)> invoices)
        : IControlCloudClientCommercialProjectionRepository
    {
        private readonly Dictionary<(Guid ClientId, Guid InvoiceId), ControlCloudInvoiceProjection> _invoices =
            invoices.ToDictionary(item => (item.ClientId, item.Invoice.InvoiceId), item => item.Invoice);

        public (Guid ClientId, Guid InvoiceId)? LastInvoiceLookup { get; private set; }

        public Task<ControlCloudInvoiceProjection?> GetInvoiceAsync(
            Guid clientId,
            Guid invoiceId,
            CancellationToken cancellationToken = default)
        {
            LastInvoiceLookup = (clientId, invoiceId);
            return Task.FromResult(_invoices.GetValueOrDefault((clientId, invoiceId)));
        }

        public Task<ControlCloudClientCommercialProjection?> GetByClientIdAsync(
            Guid clientId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<ControlCloudClientCommercialProjection?>(null);

        public Task SaveAsync(
            ControlCloudClientCommercialProjection projection,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task ApplyChangeAsync(
            ControlCloudCommercialProjectionChange change,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<IReadOnlyCollection<ControlCloudCommercialDocumentProjection>> ListDocumentsAsync(
            Guid clientId,
            string documentType,
            DateOnly? beforeDate,
            Guid? beforeDocumentId,
            int take,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<ControlCloudCommercialDocumentProjection>>([]);

        public Task<IReadOnlyCollection<ControlCloudInvoiceProjection>> ListInvoicesAsync(
            Guid clientId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<ControlCloudInvoiceProjection>>(
                _invoices
                    .Where(pair => pair.Key.ClientId == clientId)
                    .Select(pair => pair.Value)
                    .ToArray());

        public Task<IReadOnlyCollection<ControlCloudPaymentProjection>> ListPaymentsAsync(
            Guid clientId,
            Guid? invoiceId = null,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<ControlCloudPaymentProjection>>([]);
    }

    private sealed class PaymentClaimRepositoryFake(
        IEnumerable<ControlCloudClientPortalPaymentClaim> claims)
        : IClientPortalPaymentClaimRepository
    {
        private readonly List<ControlCloudClientPortalPaymentClaim> _claims = [.. claims];

        public List<ControlCloudClientPortalPaymentClaim> Added { get; } = [];

        public Task<ControlCloudClientPortalPaymentClaim?> GetByIdAsync(
            Guid claimId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_claims.SingleOrDefault(claim => claim.ClaimId == claimId));

        public Task<ControlCloudClientPortalPaymentClaim?> GetByClientAndReferenceAsync(
            Guid clientId,
            string normalizedTransferReferenceNumber,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_claims.SingleOrDefault(claim =>
                claim.ClientId == clientId
                && claim.NormalizedTransferReferenceNumber == normalizedTransferReferenceNumber));

        public Task<IReadOnlyCollection<ControlCloudClientPortalPaymentClaim>> ListAsync(
            Guid? clientId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<ControlCloudClientPortalPaymentClaim>>(
                _claims.Where(claim => clientId is null || claim.ClientId == clientId).ToArray());

        public Task AddAsync(
            ControlCloudClientPortalPaymentClaim claim,
            CancellationToken cancellationToken = default)
        {
            Added.Add(claim);
            _claims.Add(claim);
            return Task.CompletedTask;
        }

        public Task SaveAsync(
            ControlCloudClientPortalPaymentClaim claim,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class AttachmentRepositoryFake(
        IEnumerable<ControlCloudClientPortalAttachment>? attachments = null)
        : IClientPortalAttachmentRepository
    {
        private readonly IReadOnlyCollection<ControlCloudClientPortalAttachment> _attachments =
            attachments?.ToArray() ?? [];

        public int GetByIdCount { get; private set; }

        public Task<ControlCloudClientPortalAttachment?> GetByIdAsync(
            Guid attachmentId,
            CancellationToken cancellationToken = default)
        {
            GetByIdCount++;
            return Task.FromResult(_attachments.SingleOrDefault(item => item.AttachmentId == attachmentId));
        }

        public Task AddAsync(
            ControlCloudClientPortalAttachment attachment,
            CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }

    private sealed class AuditRecorderFake : IClientPortalAuditRecorder
    {
        public List<ClientPortalAuditRecord> Records { get; } = [];

        public Task RecordAsync(
            ClientPortalAuditRecord audit,
            CancellationToken cancellationToken = default)
        {
            Records.Add(audit);
            return Task.CompletedTask;
        }
    }
}
