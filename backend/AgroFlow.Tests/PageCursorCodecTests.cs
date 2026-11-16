using System.Text;
using AgroFlow.Api.Contracts.Conversations;
using AgroFlow.Application.Conversations;
using Xunit;

namespace AgroFlow.Tests;

public sealed class PageCursorCodecTests
{
    private static readonly Guid Id = Guid.Parse("abcdef01-2345-6789-abcd-ef0123456789");

    public static TheoryData<long, int> ValidTimestamps => new()
    {
        { DateTime.MinValue.Ticks, 46 },
        { DateTime.MinValue.Ticks + 1, 46 },
        { DateTime.UnixEpoch.Ticks, 68 },
        { new DateTime(2026, 11, 16, 12, 0, 0, DateTimeKind.Utc).Ticks, 68 },
        { DateTime.MaxValue.Ticks, 70 },
    };

    [Theory]
    [MemberData(nameof(ValidTimestamps))]
    public void Every_supported_timestamp_roundtrips_without_changing_the_wire_format(long ticks, int expectedLength)
    {
        var original = new PageCursor(new DateTime(ticks, DateTimeKind.Utc), Id);
        var encoded = PageCursorCodec.Encode(original);

        Assert.Equal(expectedLength, encoded.Length);
        Assert.DoesNotContain("=", encoded);
        Assert.True(PageCursorCodec.TryDecode(encoded, out var decoded));
        Assert.Equal(original, decoded);
        Assert.Equal(DateTimeKind.Utc, decoded!.TimestampUtc.Kind);
    }

    [Fact]
    public void Largest_timestamp_with_a_full_uuid_roundtrips()
    {
        var original = new PageCursor(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc),
            Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff"));
        var encoded = PageCursorCodec.Encode(original);

        Assert.Equal(70, encoded.Length);
        Assert.Equal(PageCursorCodec.MaxEncodedLength, encoded.Length);
        Assert.True(PageCursorCodec.TryDecode(encoded, out var decoded));
        Assert.Equal(original, decoded);
    }

    [Fact]
    public void Empty_uuid_is_preserved()
    {
        var original = new PageCursor(DateTime.UnixEpoch, Guid.Empty);
        Assert.True(PageCursorCodec.TryDecode(PageCursorCodec.Encode(original), out var decoded));
        Assert.Equal(original, decoded);
    }

    public static TheoryData<string?> InvalidCursors => new()
    {
        null,
        "",
        "not-a-cursor",
        "!",
        new string('a', 71),
        Raw($"-1.{Id:N}"),
        Raw($"+1.{Id:N}"),
        Raw($"01.{Id:N}"),
        Raw($"{DateTime.MaxValue.Ticks + 1}.{Id:N}"),
        Raw($"9223372036854775808.{Id:N}"),
        Raw($"1.{Id:D}"),
        Raw("1.zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz"),
        Raw($"1.{Id:N}.extra"),
        Raw($".{Id:N}"),
    };

    [Theory]
    [MemberData(nameof(InvalidCursors))]
    public void Malformed_noncanonical_or_out_of_range_values_remain_invalid(string? value)
    {
        Assert.False(PageCursorCodec.TryDecode(value, out _));
    }

    [Fact]
    public void Padding_and_whitespace_are_not_canonical()
    {
        var value = PageCursorCodec.Encode(new PageCursor(DateTime.UnixEpoch, Id));
        Assert.False(PageCursorCodec.TryDecode(value + "=", out _));
        Assert.False(PageCursorCodec.TryDecode(value.Insert(10, " "), out _));
        Assert.False(PageCursorCodec.TryDecode(value.Insert(10, "\n"), out _));
    }

    [Fact]
    public void Alternate_unused_base64_bits_are_not_canonical()
    {
        var value = PageCursorCodec.Encode(new PageCursor(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc), Id));
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
        var alternate = value[..^1] + alphabet[alphabet.IndexOf(value[^1]) | 1];
        Assert.NotEqual(value, alternate);
        Assert.False(PageCursorCodec.TryDecode(alternate, out _));
    }

    private static string Raw(string text) =>
        Convert.ToBase64String(Encoding.ASCII.GetBytes(text)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
