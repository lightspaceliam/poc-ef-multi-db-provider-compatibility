using POC.Entities;

namespace POC.Harness.Data;

public static class SeedData
{
    public static List<Patient> PatientsData(this string providerName)
    {
        
        return new List<Patient>
        {
            new Patient
            {
                Name = $"Luke Skywalker - Db: {providerName}",
                BirthDate = new DateTime(1972, 1, 2, 0, 0, 0, DateTimeKind.Utc),
                Identifiers = new List<Identifier>
                {
                    new Identifier{ Code = "1", Description = $"Db: {providerName}, {nameof(Identifier)} => {nameof(Patient.Identifiers)} {nameof(Use.Official)}", Use = Use.Official },
                    new Identifier{ Code = "2", Description = $"Db: {providerName}, {nameof(Identifier)} => {nameof(Patient.Identifiers)} {nameof(Use.Secondary)}", Use = Use.Secondary },
                    new Identifier{ Code = "3", Description = $"Db: {providerName}, {nameof(Identifier)} => {nameof(Patient.Identifiers)} {nameof(Use.Official)}", Use = Use.Official },
                }
            },
            new Patient
            {
                Name = $"Han Solo - Db: {providerName}",
                BirthDate = new DateTime(1968, 2, 12, 0, 0, 0, DateTimeKind.Utc),
                Identifiers = new List<Identifier>
                {
                    new Identifier{ Code = "1", Description = $"Db: {providerName}, {nameof(Identifier)} => {nameof(Patient.Identifiers)} {nameof(Use.Official)}", Use = Use.Official },
                    new Identifier{ Code = "2", Description = $"Db: {providerName}, {nameof(Identifier)} => {nameof(Patient.Identifiers)} {nameof(Use.Official)}", Use = Use.Official }
                }
            }
        };
    }
}