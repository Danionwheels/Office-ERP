using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using SafarSuite.ControlDesk.Domain.Modules.Auth;
using SafarSuite.ControlDesk.Infrastructure.Persistence.EntityFramework;

namespace SafarSuite.ControlDesk.Api.Tests;

public sealed class LocalOperatorPersistenceModelTests
{
    [Fact]
    public void Model_matches_the_authoritative_three_table_operator_contract()
    {
        var options = new DbContextOptionsBuilder<ControlDeskDbContext>()
            .UseNpgsql("Host=localhost;Database=model_only;Username=model;Password=model")
            .Options;
        using var context = new ControlDeskDbContext(options);
        var model = context.GetService<IDesignTimeModel>().Model;

        var operatorType = model.FindEntityType(typeof(LocalOperator));
        Assert.NotNull(operatorType);
        var operatorTable = StoreObjectIdentifier.Table("local_operators", "auth");

        Assert.Equal("auth", operatorType!.GetSchema());
        Assert.Equal("local_operators", operatorType.GetTableName());
        Assert.Equal(
            "operator_id",
            operatorType.FindProperty(nameof(LocalOperator.Id))!.GetColumnName(operatorTable));
        Assert.Equal(
            "password_hash",
            operatorType.FindProperty(nameof(LocalOperator.PasswordHash))!.GetColumnName(operatorTable));

        var normalizedEmailIndex = Assert.Single(operatorType.GetIndexes(), index =>
            index.GetDatabaseName() == "ux_local_operators_normalized_email");
        Assert.True(normalizedEmailIndex.IsUnique);
        Assert.Equal(
            nameof(LocalOperator.NormalizedEmail),
            Assert.Single(normalizedEmailIndex.Properties).Name);

        Assert.Equal(
            [
                "ck_local_operators_normalized_email",
                "ck_local_operators_security_version",
                "ck_local_operators_status"
            ],
            operatorType.GetCheckConstraints()
                .Select(constraint => constraint.Name)
                .OrderBy(name => name, StringComparer.Ordinal));

        AssertOwnedGrantTable<LocalOperatorRoleGrant>(
            model,
            "local_operator_roles",
            "role",
            "ck_local_operator_roles_role");
        AssertOwnedGrantTable<LocalOperatorScopeGrant>(
            model,
            "local_operator_scopes",
            "scope",
            "ck_local_operator_scopes_scope");
    }

    private static void AssertOwnedGrantTable<TGrant>(
        IModel model,
        string tableName,
        string valueColumnName,
        string checkConstraintName)
    {
        var grantType = Assert.Single(model.GetEntityTypes(), entityType =>
            entityType.ClrType == typeof(TGrant));
        var table = StoreObjectIdentifier.Table(tableName, "auth");

        Assert.True(grantType.IsOwned());
        Assert.Equal("auth", grantType.GetSchema());
        Assert.Equal(tableName, grantType.GetTableName());
        Assert.Equal(DeleteBehavior.Cascade, Assert.Single(grantType.GetForeignKeys()).DeleteBehavior);

        var primaryKeyColumns = grantType.FindPrimaryKey()!.Properties
            .Select(property => property.GetColumnName(table)!)
            .ToArray();
        Assert.Equal(["operator_id", valueColumnName], primaryKeyColumns);
        Assert.Equal(checkConstraintName, Assert.Single(grantType.GetCheckConstraints()).Name);
    }
}
