namespace Brontide.Reference.Studio;

/// <summary>
/// Replaces a durable record with the temporary file its store has just written and flushed. The
/// rename is what commits a write, and on Windows it fails while any other handle holds either file
/// without sharing deletion — a scanner or indexer does exactly that for a few milliseconds after a
/// file is written, and every store maps the resulting exception to its write-failed code. That
/// code is a permanent verdict, so a transient hold is waited out here, over a bounded budget, before
/// the exception reaches the store.
/// </summary>
internal static class DurableRecordReplacement
{
    // Doubling from one millisecond to just over half a second in all: a hold that outlasts the
    // budget is not transient, and a permanent condition — a directory where the record should be —
    // costs the budget once and is then reported exactly as it was before this existed.
    private static readonly TimeSpan[] Delays =
        [.. new[] { 1, 2, 4, 8, 16, 32, 64, 128, 256 }.Select(milliseconds => TimeSpan.FromMilliseconds(milliseconds))];

    public static void Replace(string temporary, string path)
    {
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                File.Move(temporary, path, overwrite: true);
                return;
            }
            catch (Exception exception) when (attempt < Delays.Length
                && exception is IOException or UnauthorizedAccessException)
            {
                Thread.Sleep(Delays[attempt]);
            }
        }
    }
}
