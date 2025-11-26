using System.Collections.Generic;

namespace AIM.Services;

public class FormTemplateFactory
{
    private readonly IPrintPaginationService _paginationService;

    public FormTemplateFactory(IPrintPaginationService paginationService)
    {
        _paginationService = paginationService;
    }

    public IFormTemplate GetTemplate(string templateName)
    {
        return templateName switch
        {
            "Ohio" => new OhioInventoryTemplate(_paginationService),
            "I&M" => new IMInventoryTemplate(_paginationService),
            _ => new OhioInventoryTemplate(_paginationService)
        };
    }

    public IEnumerable<string> GetAvailableTemplates()
    {
        return new[] { "Ohio", "I&M" };
    }
}