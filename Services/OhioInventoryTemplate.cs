namespace AIM.Services;

public class OhioInventoryTemplate : BaseInventoryTemplate
{
    public override string TemplateName => "Ohio";

    public OhioInventoryTemplate(IPrintPaginationService paginationService)
        : base(paginationService)
    {
    }

    public OhioInventoryTemplate() : base()
    {
    }
}