using EnterpriseAutomationFramework.Core.Exceptions;
using EnterpriseAutomationFramework.Services.Data;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Services;

/// <summary>
/// YAML 元素读取器测试
/// </summary>
public class YamlElementReaderTests : IDisposable
{
    private readonly YamlElementReader _yamlElementReader;
    private readonly string _testDataDirectory;
    private readonly List<string> _tempFiles;

    public YamlElementReaderTests()
    {
        var logger = new LoggerFactory().CreateLogger<YamlElementReader>();
        _yamlElementReader = new YamlElementReader(logger);
        _testDataDirectory = Path.Combine(Directory.GetCurrentDirectory(), "TestData");
        _tempFiles = new List<string>();
        
        // 确保测试数据目录存在
        Directory.CreateDirectory(_testDataDirectory);
    }

    public void Dispose()
    {
        // 清理临时文件
        foreach (var tempFile in _tempFiles)
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void LoadElements_WithValidYamlFile_ShouldReturnPageElementCollection()
    {
        // Arrange
        var filePath = Path.Combine(_testDataDirectory, "valid_elements.yaml");

        // Act
        var result = _yamlElementReader.LoadElements(filePath);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(7); // 3 elements in HomePage + 4 elements in LoginPage
        
        // 验证页面存在
        result.ContainsPage("HomePage").Should().BeTrue();
        result.ContainsPage("LoginPage").Should().BeTrue();
        
        // 验证HomePage元素
        var searchBox = result.GetElement("HomePage", "SearchBox");
        searchBox.Should().NotBeNull();
        searchBox.Name.Should().Be("SearchBox");
        searchBox.Selector.Should().Be("#kw");
        searchBox.Type.Should().Be(ElementType.Input);
        searchBox.TimeoutMs.Should().Be(5000);
        searchBox.Attributes.Should().ContainKey("placeholder");
        searchBox.Attributes["placeholder"].Should().Be("请输入搜索关键词");
        searchBox.Attributes.Should().ContainKey("maxlength");
        searchBox.Attributes["maxlength"].Should().Be("100");
        
        var searchButton = result.GetElement("HomePage", "SearchButton");
        searchButton.Should().NotBeNull();
        searchButton.Name.Should().Be("SearchButton");
        searchButton.Selector.Should().Be("#su");
        searchButton.Type.Should().Be(ElementType.Button);
        searchButton.TimeoutMs.Should().Be(5000);
        
        // 验证LoginPage元素
        var usernameInput = result.GetElement("LoginPage", "UsernameInput");
        usernameInput.Should().NotBeNull();
        usernameInput.Name.Should().Be("UsernameInput");
        usernameInput.Selector.Should().Be("#username");
        usernameInput.Type.Should().Be(ElementType.Input);
        usernameInput.TimeoutMs.Should().Be(3000);
        
        var rememberCheckbox = result.GetElement("LoginPage", "RememberCheckbox");
        rememberCheckbox.Should().NotBeNull();
        rememberCheckbox.Type.Should().Be(ElementType.Checkbox);
    }

    [Fact]
    public void LoadElements_WithMinimalYamlFile_ShouldReturnPageElementCollectionWithDefaults()
    {
        // Arrange
        var filePath = Path.Combine(_testDataDirectory, "minimal_elements.yaml");

        // Act
        var result = _yamlElementReader.LoadElements(filePath);

        // Assert
        result.Should().NotBeNull();
        result.Count.Should().Be(1);
        
        var element = result.GetElement("SimplePage", "BasicElement");
        element.Should().NotBeNull();
        element.Name.Should().Be("BasicElement");
        element.Selector.Should().Be("#basic");
        element.Type.Should().Be(ElementType.Text); // 默认类型
        element.TimeoutMs.Should().Be(30000); // 默认超时时间
        element.Attributes.Should().BeEmpty();
    }

    [Fact]
    public void LoadElements_WithNullFilePath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => _yamlElementReader.LoadElements(null!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*文件路径不能为空*");
    }

    [Fact]
    public void LoadElements_WithEmptyFilePath_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => _yamlElementReader.LoadElements("");
        action.Should().Throw<ArgumentException>()
            .WithMessage("*文件路径不能为空*");
    }

    [Fact]
    public void LoadElements_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentFile = Path.Combine(_testDataDirectory, "non_existent.yaml");

        // Act & Assert
        var action = () => _yamlElementReader.LoadElements(nonExistentFile);
        action.Should().Throw<FileNotFoundException>()
            .WithMessage($"*{nonExistentFile}*");
    }

    [Fact]
    public void LoadElements_WithEmptyFile_ShouldThrowYamlDataException()
    {
        // Arrange
        var filePath = Path.Combine(_testDataDirectory, "empty_elements.yaml");

        // Act & Assert
        var action = () => _yamlElementReader.LoadElements(filePath);
        action.Should().Throw<YamlDataException>()
            .WithMessage("*YAML文件内容为空*");
    }

    [Fact]
    public void LoadElements_WithInvalidYaml_ShouldThrowYamlDataException()
    {
        // Arrange
        var invalidYaml = CreateTempYamlFile("invalid: yaml: content: [unclosed");

        // Act & Assert
        var action = () => _yamlElementReader.LoadElements(invalidYaml);
        action.Should().Throw<YamlDataException>()
            .WithMessage("*加载YAML元素文件失败*");
    }

    [Fact]
    public void LoadElements_WithMissingSelectorElement_ShouldThrowYamlDataException()
    {
        // Arrange
        var filePath = Path.Combine(_testDataDirectory, "invalid_elements.yaml");

        // Act & Assert
        var action = () => _yamlElementReader.LoadElements(filePath);
        action.Should().Throw<YamlDataException>()
            .WithMessage("*解析元素*失败*");
    }

    [Fact]
    public void LoadElements_WithInvalidElementType_ShouldUseDefaultType()
    {
        // Arrange
        var yamlContent = @"
TestPage:
  InvalidTypeElement:
    selector: ""#test""
    type: InvalidType
    timeout: 5000";
        var invalidTypeFile = CreateTempYamlFile(yamlContent);

        // Act
        var result = _yamlElementReader.LoadElements(invalidTypeFile);

        // Assert
        var element = result.GetElement("TestPage", "InvalidTypeElement");
        element.Type.Should().Be(ElementType.Text); // 应该使用默认类型
    }

    [Fact]
    public void LoadElements_WithInvalidTimeout_ShouldUseDefaultTimeout()
    {
        // Arrange
        var yamlContent = @"
TestPage:
  InvalidTimeoutElement:
    selector: ""#test""
    type: Button
    timeout: invalid";
        var invalidTimeoutFile = CreateTempYamlFile(yamlContent);

        // Act
        var result = _yamlElementReader.LoadElements(invalidTimeoutFile);

        // Assert
        var element = result.GetElement("TestPage", "InvalidTimeoutElement");
        element.TimeoutMs.Should().Be(30000); // 应该使用默认超时时间
    }

    [Fact]
    public void LoadElements_WithComplexAttributes_ShouldParseCorrectly()
    {
        // Arrange
        var yamlContent = @"
TestPage:
  ComplexElement:
    selector: ""#complex""
    type: Input
    timeout: 8000
    attributes:
      class: ""form-control input-lg""
      data-test: ""search-input""
      aria-label: ""搜索输入框""
      required: ""true""";
        var complexFile = CreateTempYamlFile(yamlContent);

        // Act
        var result = _yamlElementReader.LoadElements(complexFile);

        // Assert
        var element = result.GetElement("TestPage", "ComplexElement");
        element.Attributes.Should().HaveCount(4);
        element.Attributes["class"].Should().Be("form-control input-lg");
        element.Attributes["data-test"].Should().Be("search-input");
        element.Attributes["aria-label"].Should().Be("搜索输入框");
        element.Attributes["required"].Should().Be("true");
    }

    [Fact]
    public void GetElement_WithoutLoadingElements_ShouldThrowInvalidOperationException()
    {
        // Act & Assert
        var action = () => _yamlElementReader.GetElement("TestPage", "TestElement");
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*请先调用 LoadElements 方法*");
    }

    [Fact]
    public void GetElementFromFile_WithValidParameters_ShouldReturnElement()
    {
        // Arrange
        var filePath = Path.Combine(_testDataDirectory, "valid_elements.yaml");

        // Act
        var element = _yamlElementReader.GetElementFromFile(filePath, "HomePage", "SearchBox");

        // Assert
        element.Should().NotBeNull();
        element.Name.Should().Be("SearchBox");
        element.Selector.Should().Be("#kw");
        element.Type.Should().Be(ElementType.Input);
    }

    [Fact]
    public void GetElementFromFile_WithNonExistentPage_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_testDataDirectory, "valid_elements.yaml");

        // Act & Assert
        var action = () => _yamlElementReader.GetElementFromFile(filePath, "NonExistentPage", "TestElement");
        action.Should().Throw<KeyNotFoundException>()
            .WithMessage("*页面 'NonExistentPage' 不存在*");
    }

    [Fact]
    public void GetElementFromFile_WithNonExistentElement_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var filePath = Path.Combine(_testDataDirectory, "valid_elements.yaml");

        // Act & Assert
        var action = () => _yamlElementReader.GetElementFromFile(filePath, "HomePage", "NonExistentElement");
        action.Should().Throw<KeyNotFoundException>()
            .WithMessage("*页面 'HomePage' 中的元素 'NonExistentElement' 不存在*");
    }

    [Fact]
    public void LoadElements_WithAllElementTypes_ShouldParseCorrectly()
    {
        // Arrange
        var yamlContent = @"
TestPage:
  ButtonElement:
    selector: ""#button""
    type: Button
  InputElement:
    selector: ""#input""
    type: Input
  LinkElement:
    selector: ""#link""
    type: Link
  TextElement:
    selector: ""#text""
    type: Text
  DropdownElement:
    selector: ""#dropdown""
    type: Dropdown
  CheckboxElement:
    selector: ""#checkbox""
    type: Checkbox
  RadioElement:
    selector: ""#radio""
    type: Radio";
        var allTypesFile = CreateTempYamlFile(yamlContent);

        // Act
        var result = _yamlElementReader.LoadElements(allTypesFile);

        // Assert
        result.GetElement("TestPage", "ButtonElement").Type.Should().Be(ElementType.Button);
        result.GetElement("TestPage", "InputElement").Type.Should().Be(ElementType.Input);
        result.GetElement("TestPage", "LinkElement").Type.Should().Be(ElementType.Link);
        result.GetElement("TestPage", "TextElement").Type.Should().Be(ElementType.Text);
        result.GetElement("TestPage", "DropdownElement").Type.Should().Be(ElementType.Dropdown);
        result.GetElement("TestPage", "CheckboxElement").Type.Should().Be(ElementType.Checkbox);
        result.GetElement("TestPage", "RadioElement").Type.Should().Be(ElementType.Radio);
    }

    [Fact]
    public void LoadElements_WithCaseInsensitiveElementType_ShouldParseCorrectly()
    {
        // Arrange
        var yamlContent = @"
TestPage:
  LowerCaseElement:
    selector: ""#lower""
    type: button
  UpperCaseElement:
    selector: ""#upper""
    type: INPUT
  MixedCaseElement:
    selector: ""#mixed""
    type: ChEcKbOx";
        var caseInsensitiveFile = CreateTempYamlFile(yamlContent);

        // Act
        var result = _yamlElementReader.LoadElements(caseInsensitiveFile);

        // Assert
        result.GetElement("TestPage", "LowerCaseElement").Type.Should().Be(ElementType.Button);
        result.GetElement("TestPage", "UpperCaseElement").Type.Should().Be(ElementType.Input);
        result.GetElement("TestPage", "MixedCaseElement").Type.Should().Be(ElementType.Checkbox);
    }

    /// <summary>
    /// 创建临时YAML文件
    /// </summary>
    /// <param name="content">文件内容</param>
    /// <returns>文件路径</returns>
    private string CreateTempYamlFile(string content)
    {
        var tempFile = Path.Combine(_testDataDirectory, $"temp_{Guid.NewGuid()}.yaml");
        File.WriteAllText(tempFile, content);
        _tempFiles.Add(tempFile);
        return tempFile;
    }
}