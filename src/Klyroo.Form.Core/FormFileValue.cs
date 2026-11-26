using System.Globalization;

namespace Klyroo.Form;

/// <summary>
/// A file chosen in a <see cref="FormFieldKind.File"/> field, without any UI type: the name, size and content type the user's device reported, and a way to open the content.
/// The value of a file field is a <see cref="FormFileValue"/> (or <c>null</c>), or a <c>List&lt;FormFileValue&gt;</c> with <see cref="FormFieldSchema.Multiple"/>. The controller checks
/// <see cref="FormFieldSchema.Accept"/>, <see cref="FormFieldSchema.MaxFileSize"/> and <see cref="FormFieldSchema.Multiple"/> on it, so a test can build one by hand with <see cref="FromBytes"/>.
/// </summary>
public sealed class FormFileValue : IEquatable<FormFileValue>
{
    private readonly Func<CancellationToken, ValueTask<Stream>> _open;

    /// <summary>Creates a file value.</summary>
    /// <param name="name">The file name, as reported by the device.</param>
    /// <param name="size">The size in bytes, as reported by the device.</param>
    /// <param name="contentType">The content type, or an empty string when the device did not report one.</param>
    /// <param name="open">Opens a new stream over the content each time it is called. The caller disposes the stream.</param>
    /// <param name="lastModified">When the file was last modified, if known.</param>
    /// <exception cref="ArgumentNullException"><paramref name="name"/> or <paramref name="open"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="size"/> is negative.</exception>
    public FormFileValue(string name, long size, string contentType, Func<CancellationToken, ValueTask<Stream>> open, DateTimeOffset? lastModified = null)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(open);
        ArgumentOutOfRangeException.ThrowIfNegative(size);
        Name = name;
        Size = size;
        ContentType = contentType ?? "";
        LastModified = lastModified;
        _open = open;
    }

    /// <summary>The file name, as reported by the device.</summary>
    public string Name { get; }

    /// <summary>The size in bytes, as reported by the device.</summary>
    public long Size { get; }

    /// <summary>The content type, or an empty string when the device did not report one.</summary>
    public string ContentType { get; }

    /// <summary>When the file was last modified, if the device reported it.</summary>
    public DateTimeOffset? LastModified { get; }

    /// <summary>Opens a new stream over the content. Dispose it when done.</summary>
    /// <param name="cancellationToken">Cancels the open.</param>
    /// <returns>A readable stream.</returns>
    public ValueTask<Stream> OpenReadAsync(CancellationToken cancellationToken = default) => _open(cancellationToken);

    /// <summary>Builds a file value over bytes in memory, for tests and for files your code produces.</summary>
    /// <param name="name">The file name.</param>
    /// <param name="content">The content. It is copied.</param>
    /// <param name="contentType">The content type.</param>
    /// <returns>The file value, whose <see cref="Size"/> is the length of <paramref name="content"/>.</returns>
    public static FormFileValue FromBytes(string name, ReadOnlySpan<byte> content, string contentType = "application/octet-stream")
    {
        var bytes = content.ToArray();
        return new FormFileValue(name, bytes.Length, contentType, _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
    }

    /// <summary>Tells whether the file matches an accept list in the syntax of the HTML <c>accept</c> attribute: comma-separated extensions (<c>.png</c>), content types (<c>image/png</c>) and wildcards (<c>image/*</c>), compared ignoring case.</summary>
    /// <param name="accept">The accept list. A <c>null</c> or blank list accepts every file.</param>
    /// <returns><c>true</c> if the list is empty or one of its entries matches the file.</returns>
    public bool Matches(string? accept)
    {
        if (string.IsNullOrWhiteSpace(accept)) return true;
        foreach (var raw in accept.Split(','))
        {
            var token = raw.Trim();
            if (token.Length == 0) continue;
            if (token[0] == '.')
            {
                if (Name.EndsWith(token, StringComparison.OrdinalIgnoreCase)) return true;
            }
            else if (token.EndsWith("/*", StringComparison.Ordinal))
            {
                if (ContentType.StartsWith(token[..^1], StringComparison.OrdinalIgnoreCase)) return true;
            }
            else if (string.Equals(ContentType, token, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>Formats a size for a message, in the invariant culture: <c>512 B</c>, <c>1.5 KB</c>, <c>5 MB</c>.</summary>
    /// <param name="bytes">The size in bytes.</param>
    /// <returns>The text, with the binary units B, KB, MB, GB and TB.</returns>
    public static string FormatSize(long bytes)
    {
        if (bytes < 1024) return string.Create(CultureInfo.InvariantCulture, $"{bytes} B");
        string[] units = ["KB", "MB", "GB", "TB"];
        var value = bytes / 1024d;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return string.Create(CultureInfo.InvariantCulture, $"{Math.Round(value, 1):0.#} {units[unit]}");
    }

    /// <summary>Two file values are equal when they have the same name, size, content type and modification time, which is how a field tells that the user chose the file that was already there.</summary>
    /// <param name="other">The other file value.</param>
    /// <returns><c>true</c> if they describe the same file.</returns>
    public bool Equals(FormFileValue? other) =>
        other is not null && Name == other.Name && Size == other.Size && ContentType == other.ContentType && LastModified == other.LastModified;

    /// <inheritdoc />
    public override bool Equals(object? obj) => obj is FormFileValue other && Equals(other);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Name, Size, ContentType, LastModified);

    /// <inheritdoc />
    public override string ToString() => $"{Name} ({FormatSize(Size)})";
}
