using Microsoft.Extensions.Hosting;
using POC.Entities;

namespace POC.Harness.Data;

public static class SeedData
{
    public static List<Trial> TrialsData(this string providerName)
    {
        
        return new List<Trial>
        {
            new Trial
            {
                Name = $"Trial One - Db: {providerName}",
                StartDate = DateTime.SpecifyKind(DateTime.Parse("2026-04-01 09:00:00"), DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(DateTime.Parse("2026-04-01 10:00:00"), DateTimeKind.Utc),
                Criterion = new List<Criteria>
                {
                    new Criteria{ Description = $"Db: {providerName}, {nameof(Criteria)} => {nameof(Trial.Criterion)} {nameof(CriteriaTypes.MainEvent)}", Type = CriteriaTypes.MainEvent },
                    new Criteria{ Description = $"Db: {providerName}, {nameof(Criteria)} => {nameof(Trial.Criterion)} {nameof(CriteriaTypes.Inclusion)}", Type = CriteriaTypes.Inclusion },
                    new Criteria{ Description = $"Db: {providerName}, {nameof(Criteria)} => {nameof(Trial.Criterion)} {nameof(CriteriaTypes.Exclusion)}", Type = CriteriaTypes.Exclusion },
                }
            },
            new Trial
            {
                Name = $"Trial Two - Db: {providerName}",
                StartDate = DateTime.SpecifyKind(DateTime.Parse("2026-04-01 11:00:00"), DateTimeKind.Utc),
                EndDate = DateTime.SpecifyKind(DateTime.Parse("2026-04-02 11:30:00"), DateTimeKind.Utc),
                Criterion = new List<Criteria>
                {
                    new Criteria{ Description = $"Db: {providerName}, {nameof(Criteria)} => {nameof(Trial.Criterion)} {nameof(CriteriaTypes.MainEvent)}", Type = CriteriaTypes.MainEvent },
                    new Criteria{ Description = $"Db: {providerName}, {nameof(Criteria)} => {nameof(Trial.Criterion)} {nameof(CriteriaTypes.Inclusion)}", Type = CriteriaTypes.Inclusion }
                }
            }
        };
    }
}