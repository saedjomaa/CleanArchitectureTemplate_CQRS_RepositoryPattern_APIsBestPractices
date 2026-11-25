using Application.Common.Abstractions;
using Infrastructure.Persistence.Idempotency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

internal sealed class IdempotencyRecordConfiguration : IEntityTypeConfiguration<IdempotencyRecord>
{
    public void Configure(EntityTypeBuilder<IdempotencyRecord> builder)
    {
        builder.ToTable("IdempotencyRecords");

        // The primary key is what makes claiming a key atomic: a second insert with the same key fails.
        builder.HasKey(record => record.Key);
        builder.Property(record => record.Key).HasMaxLength(IIdempotencyStore.MaximumKeyLength);

        builder.Property(record => record.RequestHash).HasMaxLength(IdempotencyRecord.RequestHashLength).IsRequired();
        builder.Property(record => record.ContentType).HasMaxLength(IdempotencyRecord.MaximumContentTypeLength);
        builder.Property(record => record.Location).HasMaxLength(IdempotencyRecord.MaximumLocationLength);
        builder.HasIndex(record => record.ExpiresAtUtc);
    }
}
