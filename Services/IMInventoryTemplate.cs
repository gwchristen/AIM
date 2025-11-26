namespace AIM.Services;

/// <summary>
/// Template for generating inventory forms for I&M region.
/// </summary>
public class IMInventoryTemplate : BaseInventoryTemplate
{
    public override string TemplateName => "I&M";

    public IMInventoryTemplate(IPrintPaginationService paginationService)
    : base(paginationService)
    {
    }

    public IMInventoryTemplate() : base()
    {
    }
}