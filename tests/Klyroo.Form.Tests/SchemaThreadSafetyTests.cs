using Xunit;

namespace Klyroo.Form.Tests;

public sealed class SchemaThreadSafetyTests
{
    [Fact]
    public async Task Asking_a_shared_schema_for_handles_from_many_threads_is_safe_and_gives_one_handle_per_field()
    {
        for (var round = 0; round < 200; round++)
        {
            var schema = FormSchema.For<Person>(s => s.AllMembers());
            var barrier = new Barrier(8);
            var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() =>
            {
                barrier.SignalAndWait();
                return new object[]
                {
                    schema.Field(p => p.Name), schema.Field(p => p.Age), schema.Field(p => p.Children), schema.Field(p => p.Salary), schema.Field(p => p.Height),
                    schema.Field(p => p.Active), schema.Field(p => p.Role), schema.Field(p => p.Backup), schema.Field(p => p.Birthday), schema.Field(p => p.Appointment),
                    schema.Field(p => p.Created), schema.Field(p => p.Alarm), schema.Field(p => p.Id), schema.Field(p => p.Tags), schema.Field(p => p.Nickname),
                };
            }, TestContext.Current.CancellationToken)));
            for (var i = 0; i < results[0].Length; i++)
            {
                Assert.All(results, r => Assert.Same(results[0][i], r[i]));
            }
        }
    }
}
