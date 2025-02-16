using Aspose.Pdf;
using Aspose.Pdf.Text;

namespace Acme.Services;

public class PdfExtractor
{
    public static async Task<Dictionary<int, string>> ExtractPaginatedTextAsync(
        Document pdf,
        IEnumerable<int> pagesNumbers)
    {
        pagesNumbers = pagesNumbers
            .Where(p => p > 0 && p <= pdf.Pages.Count)
            .OrderBy(p => p);
        Dictionary<int, string> pagesTexts = [];

        await Task.Run(() =>
        {
            foreach (int pageNumber in pagesNumbers)
            {
                try
                {
                    var absorber = new TextAbsorber();
                    absorber.TextSearchOptions.LimitToPageBounds = true;

                    pdf.Pages[pageNumber].Accept(absorber);

                    pagesTexts[pageNumber] = absorber.Text;
                }
                catch (Exception)
                {
                    pagesTexts[pageNumber] = $"Error extracting text from page {pageNumber}";
                }
            }
        }).ConfigureAwait(false);

        return pagesTexts;
    }
}
