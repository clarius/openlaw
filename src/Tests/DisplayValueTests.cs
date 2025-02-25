using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Clarius.OpenLaw;

public class DisplayValueTests
{
    [Fact]
    public void CanParseFromDisplay()
    {
        var foo = DisplayValue.Parse<Foo>("The Bar");

        Assert.Equal(Foo.Bar, foo);
    }

    [Fact]
    public void CanParseFromSecondDisplay()
    {
        var foo = DisplayValue.Parse<Foo>("Second");

        Assert.Equal(Foo.Baz, foo);
    }

    [Fact]
    public void CanParseCaseInsensitive()
    {
        var foo = DisplayValue.Parse<Foo>("the bar", ignoreCase: true);
        Assert.Equal(Foo.Bar, foo);
    }

    [Fact]
    public void CanParseDirectEnum()
    {
        var foo = DisplayValue.Parse<Foo>(nameof(Foo.Bar));
        Assert.Equal(Foo.Bar, foo);
    }

    public enum Foo
    {
        [DisplayValue("The Bar")]
        [DisplayValue("First")]
        Bar,
        [DisplayValue("The Baz")]
        [DisplayValue("Second")]
        Baz
    }
}
