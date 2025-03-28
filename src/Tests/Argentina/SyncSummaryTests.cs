using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Clarius.OpenLaw.Argentina;

public class SyncSummaryTests(ITestOutputHelper output)
{
    [Fact]
    public async Task DiffOmitsTimestampChanges()
    {
        var newDoc = await Document.ParseAsync(File.ReadAllText(@$"../../../Argentina/SaijSamples/Diff/123456789-0abc-095-0000-7991soterced-new.json"));
        var oldDoc = await Document.ParseAsync(File.ReadAllText(@$"../../../Argentina/SaijSamples/Diff/123456789-0abc-095-0000-7991soterced-old.json"));

        var result = new SyncActionResult(ContentAction.Updated, newDoc, oldDoc);
        var summary = new SyncSummary("Testing");
        summary.Add(result);

        var markdown = summary.ToMarkdown();

        Assert.DoesNotContain("<details>", markdown);
    }

    [Fact]
    public async Task DiffIncludesChangedDoc()
    {
        var newDoc = await Document.ParseAsync(File.ReadAllText(@$"../../../Argentina/SaijSamples/Diff/123456789-0abc-766-1000-2102soterced-new.json"));
        var oldDoc = await Document.ParseAsync(File.ReadAllText(@$"../../../Argentina/SaijSamples/Diff/123456789-0abc-766-1000-2102soterced-old.json"));

        var result = new SyncActionResult(ContentAction.Updated, newDoc, oldDoc);
        var summary = new SyncSummary("Testing");
        summary.Add(result);

        var markdown = summary.ToMarkdown();

        Assert.Contains("<details>", markdown);
    }
}
