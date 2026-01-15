using EnterpriseAutomationFramework.Services.Data;
using FluentAssertions;
using Xunit;

namespace EnterpriseAutomationFramework.Tests.Services;

/// <summary>
/// 页面元素集合测试
/// </summary>
public class PageElementCollectionTests
{
    private readonly PageElementCollection _collection;

    public PageElementCollectionTests()
    {
        _collection = new PageElementCollection();
    }

    [Fact]
    public void AddElement_WithValidParameters_ShouldAddElementSuccessfully()
    {
        // Arrange
        var element = new PageElement
        {
            Selector = "#test",
            Type = ElementType.Button,
            TimeoutMs = 5000
        };

        // Act
        _collection.AddElement("TestPage", "TestElement", element);

        // Assert
        _collection.ContainsPage("TestPage").Should().BeTrue();
        _collection.ContainsElement("TestPage", "TestElement").Should().BeTrue();
        _collection.Count.Should().Be(1);
        
        var retrievedElement = _collection.GetElement("TestPage", "TestElement");
        retrievedElement.Should().NotBeNull();
        retrievedElement.Name.Should().Be("TestElement");
        retrievedElement.Selector.Should().Be("#test");
        retrievedElement.Type.Should().Be(ElementType.Button);
        retrievedElement.TimeoutMs.Should().Be(5000);
    }

    [Fact]
    public void AddElement_WithNullPageName_ShouldThrowArgumentException()
    {
        // Arrange
        var element = new PageElement { Selector = "#test" };

        // Act & Assert
        var action = () => _collection.AddElement(null!, "TestElement", element);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*页面名称不能为空*");
    }

    [Fact]
    public void AddElement_WithEmptyPageName_ShouldThrowArgumentException()
    {
        // Arrange
        var element = new PageElement { Selector = "#test" };

        // Act & Assert
        var action = () => _collection.AddElement("", "TestElement", element);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*页面名称不能为空*");
    }

    [Fact]
    public void AddElement_WithNullElementName_ShouldThrowArgumentException()
    {
        // Arrange
        var element = new PageElement { Selector = "#test" };

        // Act & Assert
        var action = () => _collection.AddElement("TestPage", null!, element);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*元素名称不能为空*");
    }

    [Fact]
    public void AddElement_WithEmptyElementName_ShouldThrowArgumentException()
    {
        // Arrange
        var element = new PageElement { Selector = "#test" };

        // Act & Assert
        var action = () => _collection.AddElement("TestPage", "", element);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*元素名称不能为空*");
    }

    [Fact]
    public void AddElement_WithNullElement_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        var action = () => _collection.AddElement("TestPage", "TestElement", null!);
        action.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddElement_WithSamePageAndElementName_ShouldOverwriteElement()
    {
        // Arrange
        var element1 = new PageElement { Selector = "#test1", Type = ElementType.Button };
        var element2 = new PageElement { Selector = "#test2", Type = ElementType.Input };

        // Act
        _collection.AddElement("TestPage", "TestElement", element1);
        _collection.AddElement("TestPage", "TestElement", element2);

        // Assert
        _collection.Count.Should().Be(1);
        var retrievedElement = _collection.GetElement("TestPage", "TestElement");
        retrievedElement.Selector.Should().Be("#test2");
        retrievedElement.Type.Should().Be(ElementType.Input);
    }

    [Fact]
    public void AddElement_WithMultiplePagesAndElements_ShouldOrganizeCorrectly()
    {
        // Arrange
        var element1 = new PageElement { Selector = "#home-search", Type = ElementType.Input };
        var element2 = new PageElement { Selector = "#home-button", Type = ElementType.Button };
        var element3 = new PageElement { Selector = "#login-username", Type = ElementType.Input };
        var element4 = new PageElement { Selector = "#login-password", Type = ElementType.Input };

        // Act
        _collection.AddElement("HomePage", "SearchBox", element1);
        _collection.AddElement("HomePage", "SearchButton", element2);
        _collection.AddElement("LoginPage", "UsernameInput", element3);
        _collection.AddElement("LoginPage", "PasswordInput", element4);

        // Assert
        _collection.Count.Should().Be(4);
        _collection.GetPageNames().Should().HaveCount(2);
        _collection.GetPageNames().Should().Contain("HomePage");
        _collection.GetPageNames().Should().Contain("LoginPage");
        
        _collection.GetElementNames("HomePage").Should().HaveCount(2);
        _collection.GetElementNames("HomePage").Should().Contain("SearchBox");
        _collection.GetElementNames("HomePage").Should().Contain("SearchButton");
        
        _collection.GetElementNames("LoginPage").Should().HaveCount(2);
        _collection.GetElementNames("LoginPage").Should().Contain("UsernameInput");
        _collection.GetElementNames("LoginPage").Should().Contain("PasswordInput");
    }

    [Fact]
    public void GetElement_WithNonExistentPage_ShouldThrowKeyNotFoundException()
    {
        // Act & Assert
        var action = () => _collection.GetElement("NonExistentPage", "TestElement");
        action.Should().Throw<KeyNotFoundException>()
            .WithMessage("*页面 'NonExistentPage' 不存在*");
    }

    [Fact]
    public void GetElement_WithNonExistentElement_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var element = new PageElement { Selector = "#test" };
        _collection.AddElement("TestPage", "ExistingElement", element);

        // Act & Assert
        var action = () => _collection.GetElement("TestPage", "NonExistentElement");
        action.Should().Throw<KeyNotFoundException>()
            .WithMessage("*页面 'TestPage' 中的元素 'NonExistentElement' 不存在*");
    }

    [Fact]
    public void GetElement_WithNullPageName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => _collection.GetElement(null!, "TestElement");
        action.Should().Throw<ArgumentException>()
            .WithMessage("*页面名称不能为空*");
    }

    [Fact]
    public void GetElement_WithNullElementName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var action = () => _collection.GetElement("TestPage", null!);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*元素名称不能为空*");
    }

    [Fact]
    public void GetPageElements_WithValidPageName_ShouldReturnPageElements()
    {
        // Arrange
        var element1 = new PageElement { Selector = "#element1" };
        var element2 = new PageElement { Selector = "#element2" };
        _collection.AddElement("TestPage", "Element1", element1);
        _collection.AddElement("TestPage", "Element2", element2);

        // Act
        var pageElements = _collection.GetPageElements("TestPage");

        // Assert
        pageElements.Should().HaveCount(2);
        pageElements.Should().ContainKey("Element1");
        pageElements.Should().ContainKey("Element2");
        pageElements["Element1"].Selector.Should().Be("#element1");
        pageElements["Element2"].Selector.Should().Be("#element2");
    }

    [Fact]
    public void GetPageElements_WithNonExistentPage_ShouldThrowKeyNotFoundException()
    {
        // Act & Assert
        var action = () => _collection.GetPageElements("NonExistentPage");
        action.Should().Throw<KeyNotFoundException>()
            .WithMessage("*页面 'NonExistentPage' 不存在*");
    }

    [Fact]
    public void ContainsPage_WithExistingPage_ShouldReturnTrue()
    {
        // Arrange
        var element = new PageElement { Selector = "#test" };
        _collection.AddElement("TestPage", "TestElement", element);

        // Act & Assert
        _collection.ContainsPage("TestPage").Should().BeTrue();
    }

    [Fact]
    public void ContainsPage_WithNonExistentPage_ShouldReturnFalse()
    {
        // Act & Assert
        _collection.ContainsPage("NonExistentPage").Should().BeFalse();
    }

    [Fact]
    public void ContainsPage_WithNullPageName_ShouldReturnFalse()
    {
        // Act & Assert
        _collection.ContainsPage(null!).Should().BeFalse();
    }

    [Fact]
    public void ContainsElement_WithExistingElement_ShouldReturnTrue()
    {
        // Arrange
        var element = new PageElement { Selector = "#test" };
        _collection.AddElement("TestPage", "TestElement", element);

        // Act & Assert
        _collection.ContainsElement("TestPage", "TestElement").Should().BeTrue();
    }

    [Fact]
    public void ContainsElement_WithNonExistentElement_ShouldReturnFalse()
    {
        // Arrange
        var element = new PageElement { Selector = "#test" };
        _collection.AddElement("TestPage", "TestElement", element);

        // Act & Assert
        _collection.ContainsElement("TestPage", "NonExistentElement").Should().BeFalse();
    }

    [Fact]
    public void ContainsElement_WithNonExistentPage_ShouldReturnFalse()
    {
        // Act & Assert
        _collection.ContainsElement("NonExistentPage", "TestElement").Should().BeFalse();
    }

    [Fact]
    public void GetElementNames_WithValidPage_ShouldReturnElementNames()
    {
        // Arrange
        var element1 = new PageElement { Selector = "#element1" };
        var element2 = new PageElement { Selector = "#element2" };
        _collection.AddElement("TestPage", "Element1", element1);
        _collection.AddElement("TestPage", "Element2", element2);

        // Act
        var elementNames = _collection.GetElementNames("TestPage").ToList();

        // Assert
        elementNames.Should().HaveCount(2);
        elementNames.Should().Contain("Element1");
        elementNames.Should().Contain("Element2");
    }

    [Fact]
    public void GetElementNames_WithNonExistentPage_ShouldThrowKeyNotFoundException()
    {
        // Act & Assert
        var action = () => _collection.GetElementNames("NonExistentPage");
        action.Should().Throw<KeyNotFoundException>()
            .WithMessage("*页面 'NonExistentPage' 不存在*");
    }

    [Fact]
    public void Clear_ShouldRemoveAllElements()
    {
        // Arrange
        var element1 = new PageElement { Selector = "#element1" };
        var element2 = new PageElement { Selector = "#element2" };
        _collection.AddElement("TestPage1", "Element1", element1);
        _collection.AddElement("TestPage2", "Element2", element2);

        // Act
        _collection.Clear();

        // Assert
        _collection.Count.Should().Be(0);
        _collection.GetPageNames().Should().BeEmpty();
        _collection.ContainsPage("TestPage1").Should().BeFalse();
        _collection.ContainsPage("TestPage2").Should().BeFalse();
    }

    [Fact]
    public void Count_WithEmptyCollection_ShouldReturnZero()
    {
        // Act & Assert
        _collection.Count.Should().Be(0);
    }

    [Fact]
    public void Count_WithMultipleElements_ShouldReturnCorrectCount()
    {
        // Arrange
        var element1 = new PageElement { Selector = "#element1" };
        var element2 = new PageElement { Selector = "#element2" };
        var element3 = new PageElement { Selector = "#element3" };
        
        _collection.AddElement("Page1", "Element1", element1);
        _collection.AddElement("Page1", "Element2", element2);
        _collection.AddElement("Page2", "Element3", element3);

        // Act & Assert
        _collection.Count.Should().Be(3);
    }
}