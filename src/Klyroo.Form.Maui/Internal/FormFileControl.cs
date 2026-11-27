namespace Klyroo.Form.Maui;

/// <summary>
/// A file field: the file upload of Klyroo.UI.Maui. A chosen file is read into memory right away, up to <see cref="FieldNode.MaxFileSize"/>, so the value holds the bytes and does not depend on the
/// platform file that may go away; a file above the limit is not read, stays in the value so that validation names it, and opening it fails.
/// </summary>
internal sealed class FormFileControl : FormControl
{
    private readonly KlyFileUpload _upload = new();
    private int _reading;

    public FormFileControl(FormSlot slot)
        : base(slot)
    {
        var field = slot.Field;
        var texts = FormTexts.Current;
        _upload.Multiple = field.Multiple;
        _upload.Accept = field.Accept;
        _upload.MaxSize = field.MaxFileSize;
        _upload.Label = field.Multiple ? texts.ChooseFiles : texts.ChooseFile;
        _upload.FilesChanged += (_, e) =>
        {
            if (!Busy) _ = ReadAsync(e.NewValue ?? []);
        };
        Init(_upload);
    }

    protected override bool CanClear => false;

    public KlyFileUpload Upload => _upload;

    /// <summary>Opens the content of a picked file. Tests replace it: the plain framework target has no file system service behind <see cref="FileResult"/>.</summary>
    internal Func<FileResult, Task<Stream>> Open { get; set; } = file => file.OpenReadAsync();

    /// <summary>True while a pick is being read.</summary>
    public bool IsReading => _reading > 0;

    protected override void Show()
    {
        // The form is emptied (a reset, a draft): the list of files goes too. Files that are not in the control cannot be drawn: it lists what the user picked.
        if (_reading == 0 && _upload.Files.Count > 0 && FilesOf(Slot.Value).Count == 0) _upload.ClearFiles();
    }

    /// <summary>Reads the files of a pick and commits them to the form. Returns once the form holds them.</summary>
    internal async Task ReadAsync(IReadOnlyList<FileResult> picked)
    {
        _reading++;
        try
        {
            if (picked.Count == 0)
            {
                Slot.Commit(null);
                return;
            }
            var limit = Slot.Field.MaxFileSize ?? FormFileValue.DefaultMaxSize;
            var values = new List<FormFileValue>();
            foreach (var file in picked) values.Add(await ReadFileAsync(file, limit));
            Slot.Commit(Slot.Field.Multiple ? values : values[0]);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            System.Diagnostics.Trace.TraceError($"Klyroo.Form: reading a file chosen for '{Slot.Field.Key}' failed: {ex}");
            Slot.Engine.SetError(Slot.Field.Key, FormTexts.Current.SomethingWentWrong);
        }
        finally
        {
            _reading--;
        }
    }

    private async Task<FormFileValue> ReadFileAsync(FileResult file, long limit)
    {
        await using var stream = await Open(file);
        var name = file.FileName;
        var size = stream.CanSeek ? stream.Length : -1;
        if (size > limit)
        {
            return new FormFileValue(name, size, file.ContentType ?? "", _ => throw new InvalidOperationException($"'{name}' is larger than the limit of {limit} bytes and was not read."));
        }
        using var buffer = new MemoryStream();
        await stream.CopyToAsync(buffer);
        var bytes = buffer.ToArray();
        return new FormFileValue(name, bytes.Length, file.ContentType ?? "", _ => ValueTask.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
    }

    private static IReadOnlyList<FormFileValue> FilesOf(object? value) => value switch
    {
        FormFileValue single => [single],
        IEnumerable<FormFileValue> many => many.ToList(),
        _ => [],
    };
}
