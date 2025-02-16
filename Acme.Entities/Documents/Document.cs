using Acme.Common.Enums;

namespace Acme.Entities.Documents;

public class Document
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public File? File { get; set; }
    public ICollection<Page> Pages { get; private set; } = [];
    public string Name { get; set; } = string.Empty;
    public MediaType MediaType { get; set; } = MediaType.Unknown;

    public Document()
    {
    }

    public Page this[int pageNumber]
    {
        get
        {
            Page? page = Pages.FirstOrDefault(p => p.PageNumber == pageNumber);

            if (page is null)
            {
                page = new Page
                {
                    PageNumber = pageNumber,
                    Document = this
                };

                Pages.Add(page);
            }

            return page;
        }
    }
}
