using SafarSuite.ControlCloud.Domain.Modules.ClientPortal;

namespace SafarSuite.ControlCloud.Application.Modules.ClientPortal.Ports;

public interface IControlCloudEntitlementBundleSigner
{
    ControlCloudSignedEntitlementBundle Sign(ControlCloudEntitlementBundlePayload payload);
}
