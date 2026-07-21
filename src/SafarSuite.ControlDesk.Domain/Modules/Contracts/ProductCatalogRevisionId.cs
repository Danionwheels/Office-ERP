namespace SafarSuite.ControlDesk.Domain.Modules.Contracts;

public readonly record struct ProductCatalogRevisionId(Guid Value)
{
    public static ProductCatalogRevisionId Create(Guid value)
    {
        if (value == Guid.Empty)
        {
            throw new ArgumentException("Product catalog revision id cannot be empty.", nameof(value));
        }

        return new ProductCatalogRevisionId(value);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
