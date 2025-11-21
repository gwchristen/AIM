# Contributing to AIM

Thank you for your interest in contributing to AIM (Asset Inventory Management)! This document provides guidelines and instructions for contributing to the project.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Development Environment Setup](#development-environment-setup)
- [Code Style and Standards](#code-style-and-standards)
- [Commit Message Conventions](#commit-message-conventions)
- [Pull Request Process](#pull-request-process)
- [Testing Requirements](#testing-requirements)
- [Security Considerations](#security-considerations)
- [Documentation Requirements](#documentation-requirements)
- [Issue Reporting](#issue-reporting)
- [Questions and Support](#questions-and-support)

---

## Code of Conduct

### Our Pledge

We are committed to providing a welcoming and inclusive environment for all contributors, regardless of experience level, background, or identity.

### Expected Behavior

- Be respectful and considerate in all communications
- Provide constructive feedback
- Focus on what is best for the project and community
- Show empathy towards other contributors
- Be patient with newcomers

### Unacceptable Behavior

- Harassment, discrimination, or offensive comments
- Trolling or insulting remarks
- Publishing others' private information
- Any conduct that could be considered unprofessional

---

## Getting Started

### Prerequisites

Before you begin, ensure you have the following installed:

- **Windows 10** (Build 19041) or **Windows 11**
- **Visual Studio 2022** (version 17.8 or later)
  - Workload: **.NET Desktop Development**
  - Workload: **Windows App SDK (WinUI 3)**
- **.NET 8.0 SDK** or later
- **Git** for version control

### Finding an Issue to Work On

1. Check the [Issues](https://github.com/gwchristen/AIM/issues) page
2. Look for issues labeled:
   - `good first issue` - Great for newcomers
   - `help wanted` - Community contributions welcome
   - `bug` - Bug fixes needed
   - `enhancement` - Feature requests
3. Comment on the issue to express your interest
4. Wait for maintainer confirmation before starting work

---

## Development Environment Setup

### 1. Fork and Clone the Repository

```bash
# Fork the repository on GitHub (click "Fork" button)

# Clone your fork
git clone https://github.com/YOUR-USERNAME/AIM.git
cd AIM

# Add upstream remote
git remote add upstream https://github.com/gwchristen/AIM.git
```

### 2. Open the Project in Visual Studio

1. Launch **Visual Studio 2022**
2. Click **Open a project or solution**
3. Navigate to the cloned repository
4. Open `AIM.sln`

### 3. Restore NuGet Packages

Visual Studio should automatically restore NuGet packages. If not:

```bash
# In Package Manager Console
Update-Package -reinstall
```

Or:

```bash
# In terminal
dotnet restore
```

### 4. Build the Solution

1. In Visual Studio: **Build → Build Solution** (or press `Ctrl+Shift+B`)
2. Ensure there are no build errors
3. The first build may take several minutes while downloading dependencies

### 5. Run the Application

1. Set `AIM` as the startup project (right-click project → Set as Startup Project)
2. Press **F5** to run in Debug mode
3. The application should launch and show the main window

### 6. Create a Feature Branch

```bash
# Always create a new branch for your work
git checkout -b feature/your-feature-name

# Or for bug fixes
git checkout -b fix/bug-description
```

---

## Code Style and Standards

### C# Coding Standards

AIM follows the [Microsoft C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions).

#### Naming Conventions

| Element | Convention | Example |
|---------|------------|---------|
| **Namespace** | PascalCase | `AIM.Services` |
| **Class** | PascalCase | `FileService` |
| **Interface** | PascalCase with `I` prefix | `IFileService` |
| **Method** | PascalCase | `LoadDataAsync()` |
| **Property** | PascalCase | `CurrentTheme` |
| **Private Field** | camelCase with `_` prefix | `_fileService` |
| **Parameter** | camelCase | `fileName` |
| **Local Variable** | camelCase | `fileCount` |
| **Constant** | PascalCase | `MaxRetryAttempts` |

#### Code Structure

**✅ Good Example**:
```csharp
namespace AIM.Services;

/// <summary>
/// Service for managing file operations.
/// </summary>
public class FileService : IFileService
{
    private readonly AuditLoggingService _auditService;
    private const int MaxFileSize = 10485760; // 10 MB
    
    public FileService(AuditLoggingService auditService)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }
    
    public async Task<List<FileItem>> GetFilesAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be empty", nameof(path));
        
        var files = new List<FileItem>();
        // Implementation
        return files;
    }
}
```

**❌ Bad Example**:
```csharp
namespace AIM.Services;

// No XML comments
public class fileService : IFileService  // Wrong casing
{
    AuditLoggingService auditService;  // No access modifier, no naming convention
    
    public fileService(AuditLoggingService auditService)  // Wrong casing
    {
        this.auditService = auditService;  // No null check
    }
    
    public async Task<List<FileItem>> GetFiles(string p)  // Poor parameter name
    {
        List<FileItem> f = new List<FileItem>();  // Poor variable name
        // Implementation
        return f;
    }
}
```

### MVVM Pattern Standards

#### ViewModel Guidelines

- **Inherit from `ObservableObject`** (CommunityToolkit.Mvvm)
- **Use `[ObservableProperty]`** for bindable properties
- **Use `[RelayCommand]`** for commands
- **Never reference UI elements** (Views) directly in ViewModels
- **Inject services** via constructor, not static references

**✅ Good Example**:
```csharp
public partial class MyViewModel : ObservableObject
{
    private readonly IFileService _fileService;
    private readonly IDialogService _dialogService;
    
    [ObservableProperty]
    private string _title = "Default Title";
    
    [ObservableProperty]
    private bool _isLoading;
    
    public MyViewModel(IFileService fileService, IDialogService dialogService)
    {
        _fileService = fileService;
        _dialogService = dialogService;
    }
    
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        try
        {
            IsLoading = true;
            var data = await _fileService.GetDataAsync();
            // Process data
        }
        catch (Exception ex)
        {
            await _dialogService.ShowErrorAsync("Error", ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

**❌ Bad Example**:
```csharp
public class MyViewModel : ObservableObject  // Missing 'partial'
{
    public string Title { get; set; }  // Not observable, no notification
    
    private FileService fileService = new FileService();  // Direct instantiation, not DI
    
    public void LoadData()  // Synchronous, no error handling
    {
        var data = fileService.GetData();
        // No try-catch, no loading indicator
    }
}
```

#### View Guidelines

- **Keep code-behind minimal** - Only initialization and navigation
- **Use `x:Bind`** instead of `Binding` for better performance
- **Set `DataContext`** to ViewModel in constructor
- **Use `Mode=OneWay` or `Mode=TwoWay`** explicitly

**✅ Good Example**:
```csharp
public sealed partial class MyPage : Page
{
    public MyViewModel ViewModel { get; }
    
    public MyPage()
    {
        this.InitializeComponent();
        ViewModel = Ioc.Default.GetRequiredService<MyViewModel>();
        DataContext = ViewModel;
    }
}
```

```xml
<Page x:Class="AIM.Views.MyPage">
    <TextBlock Text="{x:Bind ViewModel.Title, Mode=OneWay}" />
    <Button Command="{x:Bind ViewModel.LoadDataCommand}" />
</Page>
```

### Service Guidelines

- **Define interfaces** for all services
- **Use dependency injection** for service resolution
- **Services should be stateless** or manage their own state
- **Log important operations** via `AuditLoggingService`
- **Handle errors gracefully** and provide meaningful messages

**Service Template**:
```csharp
// Interface
public interface IMyService
{
    Task<MyData> GetDataAsync(string id);
    Task SaveDataAsync(MyData data);
}

// Implementation
public class MyService : IMyService
{
    private readonly AuditLoggingService _auditService;
    
    public MyService(AuditLoggingService auditService)
    {
        _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
    }
    
    public async Task<MyData> GetDataAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("ID cannot be empty", nameof(id));
        
        try
        {
            _auditService.LogSystemEvent("DATA_ACCESS", $"Accessed data: {id}");
            // Implementation
            return await Task.FromResult(new MyData());
        }
        catch (Exception ex)
        {
            _auditService.LogSystemEvent("DATA_ACCESS_ERROR", ex.Message);
            throw;
        }
    }
    
    public async Task SaveDataAsync(MyData data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        
        // Implementation
        _auditService.LogSystemEvent("DATA_SAVE", $"Saved data: {data.Id}");
    }
}
```

### XML Documentation

All public classes, methods, and properties should have XML documentation comments.

**✅ Good Example**:
```csharp
/// <summary>
/// Service for managing file operations with audit logging.
/// </summary>
public class FileService : IFileService
{
    /// <summary>
    /// Retrieves all files in the specified directory.
    /// </summary>
    /// <param name="path">The directory path to search.</param>
    /// <returns>A list of file items found in the directory.</returns>
    /// <exception cref="ArgumentException">Thrown when path is null or empty.</exception>
    /// <exception cref="DirectoryNotFoundException">Thrown when the directory does not exist.</exception>
    public async Task<List<FileItem>> GetFilesAsync(string path)
    {
        // Implementation
    }
}
```

### File Organization

- **One class per file** (unless nested or tightly related)
- **File name matches class name** (e.g., `FileService.cs` for `FileService` class)
- **Organize using statements** with `System` namespaces first
- **Remove unused using statements**

### Code Formatting

Visual Studio should auto-format your code. Use:
- **Ctrl+K, Ctrl+D** to format the entire document
- **Ctrl+K, Ctrl+F** to format selection

**Settings**:
- **Indentation**: 4 spaces (no tabs)
- **Braces**: Opening brace on new line (Allman style)
- **Line Length**: Aim for 120 characters max

---

## Commit Message Conventions

### Format

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Type

- **feat**: New feature
- **fix**: Bug fix
- **docs**: Documentation changes
- **style**: Code style changes (formatting, no logic change)
- **refactor**: Code refactoring (no feature or bug fix)
- **test**: Adding or updating tests
- **chore**: Maintenance tasks (dependencies, build config)
- **perf**: Performance improvements
- **security**: Security fixes or improvements

### Scope (Optional)

The scope specifies the area of the codebase:
- `services`: Services layer
- `viewmodels`: ViewModel changes
- `views`: View/UI changes
- `security`: Security-related changes
- `audit`: Audit logging changes
- `theme`: Theme/appearance changes

### Subject

- Use imperative mood ("Add feature" not "Added feature")
- No capitalization of first letter (unless proper noun)
- No period at the end
- Maximum 50 characters

### Body (Optional)

- Explain **what** and **why**, not **how**
- Wrap at 72 characters
- Separate from subject with a blank line

### Footer (Optional)

- Reference issues: `Closes #123`, `Fixes #456`
- Breaking changes: `BREAKING CHANGE: description`

### Examples

**Good Examples**:

```
feat(services): add file statistics service

Implement new FileStatisticsService to calculate file counts,
sizes, and generate reports. Includes audit logging for all
operations.

Closes #45
```

```
fix(viewmodels): resolve null reference in BrowseViewModel

Check for null directory before accessing properties in
UpdateBreadcrumbs method.

Fixes #67
```

```
docs: update ARCHITECTURE.md with service layer details

Add comprehensive documentation for all core services including
examples and usage patterns.
```

```
security: implement rate limiting for password attempts

Add 5-attempt limit with 15-minute lockout to prevent brute
force attacks on master password.

BREAKING CHANGE: SecurityService constructor signature changed
to include new rate limiting parameters.
```

**Bad Examples**:

```
Updated stuff  # Too vague
```

```
FEAT: ADDED NEW FEATURE FOR FILES  # Wrong format, all caps
```

```
Fixed bug  # No context, no issue reference
```

---

## Pull Request Process

### Before Submitting

1. **Sync with upstream**:
   ```bash
   git fetch upstream
   git rebase upstream/main
   ```

2. **Build and test**:
   - Ensure the solution builds without errors
   - Run the application and test your changes
   - Check for any console errors or warnings

3. **Code review checklist**:
   - [ ] Code follows style guidelines
   - [ ] XML documentation added for public APIs
   - [ ] No hardcoded values (use constants or configuration)
   - [ ] Error handling implemented
   - [ ] Audit logging added for important operations
   - [ ] No security vulnerabilities introduced
   - [ ] UI is responsive and accessible

4. **Commit your changes**:
   ```bash
   git add .
   git commit -m "feat(scope): description"
   ```

5. **Push to your fork**:
   ```bash
   git push origin feature/your-feature-name
   ```

### Creating the Pull Request

1. Navigate to your fork on GitHub
2. Click **"Pull Request"**
3. Select base repository: `gwchristen/AIM`, base branch: `main`
4. Select your fork and feature branch
5. Fill out the PR template:

```markdown
## Description
Brief description of what this PR does.

## Type of Change
- [ ] Bug fix
- [ ] New feature
- [ ] Breaking change
- [ ] Documentation update

## Related Issue
Closes #(issue number)

## Testing
Describe how you tested your changes:
- [ ] Manual testing performed
- [ ] All existing features still work
- [ ] New features work as expected

## Screenshots (if applicable)
Include screenshots of UI changes.

## Checklist
- [ ] Code follows project style guidelines
- [ ] Self-review completed
- [ ] XML documentation added
- [ ] No build warnings or errors
- [ ] Audit logging added (if applicable)
- [ ] Security considerations addressed
- [ ] ARCHITECTURE.md updated (if needed)
```

6. Click **"Create Pull Request"**

### Review Process

1. **Automated Checks**: Ensure all checks pass
2. **Code Review**: Maintainers will review your code
3. **Feedback**: Address any requested changes
4. **Approval**: Once approved, your PR will be merged

### Updating Your PR

If changes are requested:

```bash
# Make changes
git add .
git commit -m "fix: address review feedback"
git push origin feature/your-feature-name
```

The PR will automatically update.

---

## Testing Requirements

### Manual Testing

Since AIM is a WinUI 3 application, manual testing is the primary testing method.

#### Testing Checklist

**For All Changes**:
- [ ] Application builds without errors or warnings
- [ ] Application starts successfully
- [ ] No console errors in Debug output
- [ ] Feature works as expected in normal scenarios
- [ ] Feature handles edge cases (empty data, null values)
- [ ] Error messages are user-friendly
- [ ] UI is responsive and not frozen during operations

**For UI Changes**:
- [ ] Layout is correct at different window sizes
- [ ] Theme changes work correctly (Light, Dark, High Contrast)
- [ ] Keyboard navigation works
- [ ] Screen reader compatibility (if applicable)
- [ ] All text is readable and not truncated

**For Security Changes**:
- [ ] Authentication works correctly
- [ ] Authorization prevents unauthorized access
- [ ] Passwords are not logged or displayed
- [ ] Encryption/decryption works correctly
- [ ] Audit logs capture all security events

**For Service Changes**:
- [ ] Service methods handle errors gracefully
- [ ] Null checks implemented for parameters
- [ ] Audit logging works for all operations
- [ ] Service can be mocked/tested independently

#### Testing Scenarios

**File Operations**:
1. Browse to a directory
2. Select files
3. Perform operation (move, copy, delete)
4. Verify operation completed
5. Check audit log for entry
6. Test undo (if applicable)

**Security**:
1. Verify user is in authorized users database
2. Check user's access level is correct
3. Test Basic user cannot access Admin features
4. Test Admin user can manage other users
5. Test SuperAdmin has full access
6. Add new user through UI
7. Edit user's access level
8. Verify changes sync to database
9. Verify audit log captures security changes
10. Test user deactivation (removal)

**Theme**:
1. Change theme to Light
2. Verify UI updates correctly
3. Change theme to Dark
4. Verify UI updates correctly
5. Change theme to High Contrast
6. Verify UI updates correctly
7. Restart app and verify theme persists

### Test Documentation

Document your testing in the PR description:

```markdown
## Testing Performed

### Scenario 1: File Move Operation
- Selected 3 files in Browse page
- Moved to different directory
- ✅ Files moved successfully
- ✅ Audit log entry created
- ✅ Undo operation works

### Scenario 2: Password Validation
- Attempted weak password "test123"
- ❌ Correctly rejected (no uppercase)
- Attempted strong password "Test@123"
- ✅ Accepted and saved
- ✅ Encryption verified (file is not plain text)

### Edge Cases Tested
- Empty directory (displays empty message)
- Very long filenames (truncated correctly)
- Special characters in paths (handled correctly)
```

---

## Security Considerations

### Security Guidelines

**When implementing security-related features, ensure**:

1. **No Hardcoded Secrets**:
   - Never commit passwords, API keys, or encryption keys
   - All security managed through centralized database

2. **Database Security**:
   - Always use parameterized SQL queries (prevent SQL injection)
   - Never concatenate user input into SQL strings
   - Validate database path before use
   - Handle database connection errors gracefully

3. **Access Control**:
   - Check user access level before privileged operations
   - Use `IsCurrentUserAdmin()` or `IsCurrentUserSuperAdmin()` for checks
   - Never bypass access level checks
   - Log access denied attempts

4. **Input Validation**:
   - Validate all user input
   - Sanitize file paths to prevent directory traversal
   - Check for null/empty values
   - Validate usernames against Windows username format

5. **Error Messages**:
   - Don't expose database structure or schema in error messages
   - Don't reveal whether a user exists or not
   - Use generic messages for authorization failures

6. **Audit Logging**:
   - Log all user management operations (add/edit/remove)
   - Log all authentication attempts (success and failure)
   - Log all file operations
   - Log security configuration changes to SecurityAuditLog table
   - **Never log passwords or sensitive data**

### Security Checklist

Before submitting security-related PRs:

- [ ] No secrets in code or configuration
- [ ] Database queries use parameterized statements (prevent SQL injection)
- [ ] Input validation implemented
- [ ] Error messages don't expose system details
- [ ] Audit logging captures security events to database
- [ ] Access level checks implemented correctly (Basic/Admin/SuperAdmin)
- [ ] Security configuration is documented
- [ ] ARCHITECTURE.md security section updated (if needed)
- [ ] IMPLEMENTATION-DATABASE-SECURITY.md updated (if database changes)

### Reporting Security Vulnerabilities

**Do not open public issues for security vulnerabilities.**

Instead:
1. Email the maintainer directly (see repository owner contact)
2. Provide detailed description of the vulnerability
3. Include steps to reproduce (if applicable)
4. Allow reasonable time for fix before disclosure

---

## Documentation Requirements

### When to Update Documentation

Update documentation when:
- Adding a new feature
- Changing existing behavior
- Adding or modifying services
- Changing architecture or design patterns
- Fixing security issues
- Updating dependencies

### Documentation Files to Update

| File | When to Update |
|------|---------------|
| **README.md** | New features, installation changes, usage changes |
| **ARCHITECTURE.md** | New services, design pattern changes, workflow changes |
| **CONTRIBUTING.md** | Development process changes, new guidelines |
| **DESIGN_PATTERNS.md** | New patterns, anti-patterns, code examples |

### XML Documentation

All public APIs require XML documentation:

```csharp
/// <summary>
/// Brief description of what the method does.
/// </summary>
/// <param name="paramName">Description of the parameter.</param>
/// <returns>Description of what is returned.</returns>
/// <exception cref="ExceptionType">When this exception is thrown.</exception>
/// <remarks>
/// Additional notes about usage, caveats, or examples.
/// </remarks>
/// <example>
/// <code>
/// var result = await service.MethodAsync("parameter");
/// </code>
/// </example>
public async Task<Result> MethodAsync(string paramName)
{
    // Implementation
}
```

### README Updates

When adding a feature to README.md:

1. Add to **Features** section
2. Update **Table of Contents** if adding new section
3. Include usage example (if applicable)
4. Update screenshots (if UI changed)

### ARCHITECTURE Updates

When adding to ARCHITECTURE.md:

1. Update service layer section if adding a service
2. Add data flow diagram if implementing a workflow
3. Update extension guide if adding a new pattern
4. Add to common development tasks if relevant

---

## Issue Reporting

### Before Reporting

1. **Search existing issues** to avoid duplicates
2. **Try the latest version** of the application
3. **Reproduce the issue** consistently

### Bug Report Template

Use this template when reporting bugs:

```markdown
**Describe the Bug**
A clear description of what the bug is.

**To Reproduce**
Steps to reproduce:
1. Go to '...'
2. Click on '...'
3. See error

**Expected Behavior**
What you expected to happen.

**Actual Behavior**
What actually happened.

**Screenshots**
If applicable, add screenshots.

**Environment**
- OS: [e.g., Windows 11]
- Version: [e.g., 1.0.0]
- .NET Version: [e.g., 8.0]

**Additional Context**
Any other relevant information.

**Logs**
Paste relevant log entries from:
- Debug output
- Audit logs (%LocalAppData%\AIM\Logs\audit_log.json)
- Application logs
```

### Feature Request Template

Use this template for feature requests:

```markdown
**Feature Description**
A clear description of the feature.

**Use Case**
Why is this feature needed? What problem does it solve?

**Proposed Solution**
How should this feature work?

**Alternative Solutions**
Other approaches you've considered.

**Additional Context**
Mockups, examples, or references.
```

---

## Questions and Support

### Getting Help

- **Documentation**: Check [README.md](README.md) and [ARCHITECTURE.md](ARCHITECTURE.md)
- **Issues**: Search [existing issues](https://github.com/gwchristen/AIM/issues)
- **Discussions**: Use GitHub Discussions for questions

### Communication Channels

- **GitHub Issues**: Bug reports and feature requests
- **GitHub Discussions**: General questions and discussions
- **Pull Request Comments**: Code review and implementation discussions

---

## Development Workflow Summary

```
1. Fork and clone repository
   ↓
2. Create feature branch
   ↓
3. Make changes following style guide
   ↓
4. Add XML documentation
   ↓
5. Build and test manually
   ↓
6. Update documentation (if needed)
   ↓
7. Commit with conventional message
   ↓
8. Push to your fork
   ↓
9. Create pull request
   ↓
10. Address review feedback
   ↓
11. PR merged by maintainer
```

---

## Additional Resources

- [C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- [WinUI 3 Documentation](https://docs.microsoft.com/en-us/windows/apps/winui/)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/)
- [Dependency Injection in .NET](https://docs.microsoft.com/en-us/dotnet/core/extensions/dependency-injection)

---

## License

By contributing to AIM, you agree that your contributions will be licensed under the same license as the project.

---

Thank you for contributing to AIM! Your efforts help make this project better for everyone.
