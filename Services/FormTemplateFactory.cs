using System;
using System.Collections.Generic;
using System.Linq;

namespace AIM.Services;

/// <summary>
/// Factory for creating and managing form templates.
/// </summary>
public class FormTemplateFactory
{
    private readonly Dictionary<string, Func<IFormTemplate>> _templates;

    public FormTemplateFactory()
    {
        _templates = new Dictionary<string, Func<IFormTemplate>>
        {
            { "Ohio", () => new OhioInventoryTemplate() },
            { "I&M", () => new IMInventoryTemplate() }
        };
    }

    /// <summary>
    /// Gets a template by name.
    /// </summary>
    public IFormTemplate GetTemplate(string templateName)
    {
        if (_templates.TryGetValue(templateName, out var factory))
        {
            return factory();
        }

        throw new ArgumentException($"Template '{templateName}' not found.");
    }

    /// <summary>
    /// Gets all available template names.
    /// </summary>
    public IEnumerable<string> GetAvailableTemplates()
    {
        return _templates.Keys;
    }

    /// <summary>
    /// Registers a new template.
    /// </summary>
    public void RegisterTemplate(string name, Func<IFormTemplate> factory)
    {
        _templates[name] = factory;
    }
}