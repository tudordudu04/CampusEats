using Xunit;
using FluentValidation.TestHelper;
using CampusEats.Api.Features.Orders;
using CampusEats.Api.Features.Inventory.AdjustStock;
using CampusEats.Api.Features.Coupons.CreateCoupon;
using CampusEats.Api.Features.Inventory.CreateIngredient;
using CampusEats.Api.Features.Kitchen.CreateKitchenTask;
using CampusEats.Api.Features.Kitchen.UpdateKitchenTask;
using CampusEats.Api.Features.Menu.CreateMenuItem;
using CampusEats.Api.Features.Menu.UpdateMenuItem;
using CampusEats.Api.Features.Orders.PlaceOrder;
using CampusEats.Api.Features.Coupons.PurchaseCoupon;
using CampusEats.Api.Features.Loyalty.RedeemPoints;
using CampusEats.Api.Enums;
using CampusEats.Api.Domain;

namespace CampusEats.Tests;

public class ExtendedValidatorTests
{
    [Fact]
    public void CancelOrderValidator_Should_Fail_When_Id_Is_Empty()
    {
        var validator = new CancelOrderValidator();
        var command = new CancelOrderCommand(Guid.Empty);
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void CancelOrderValidator_Should_Pass_When_Valid()
    {
        var validator = new CancelOrderValidator();
        var command = new CancelOrderCommand(Guid.NewGuid());
        
        var result = validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void AdjustStockValidator_Should_Fail_When_IngredientId_Is_Empty()
    {
        var validator = new AdjustStockValidator();
        var command = new AdjustStockCommand(Guid.Empty, 10, StockTransactionType.Restock, "Test");
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.IngredientId);
    }

    [Fact]
    public void AdjustStockValidator_Should_Fail_When_Quantity_Is_Zero()
    {
        var validator = new AdjustStockValidator();
        var command = new AdjustStockCommand(Guid.NewGuid(), 0, StockTransactionType.Restock, "Test");
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Quantity);
    }

    [Fact]
    public void AdjustStockValidator_Should_Fail_When_Note_Too_Long()
    {
        var validator = new AdjustStockValidator();
        var longNote = new string('a', 201);
        var command = new AdjustStockCommand(Guid.NewGuid(), 10, StockTransactionType.Restock, longNote);
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Note);
    }

    [Fact]
    public void CreateCouponValidator_Should_Fail_When_Name_Is_Empty()
    {
        var validator = new CreateCouponValidator();
        var command = new CreateCouponCommand("", "Desc", CouponType.PercentageDiscount, 10, 100, null, null, null);
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CreateCouponValidator_Should_Fail_When_Description_Is_Empty()
    {
        var validator = new CreateCouponValidator();
        var command = new CreateCouponCommand("Name", "", CouponType.PercentageDiscount, 10, 100, null, null, null);
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void CreateCouponValidator_Should_Fail_When_PointsCost_Is_Zero()
    {
        var validator = new CreateCouponValidator();
        var command = new CreateCouponCommand("Name", "Desc", CouponType.PercentageDiscount, 10, 0, null, null, null);
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.PointsCost);
    }

    [Fact]
    public void CreateIngredientValidator_Should_Fail_When_Name_Is_Empty()
    {
        var validator = new CreateIngredientValidator();
        var command = new CreateIngredientCommand("", "kg", 10);
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CreateIngredientValidator_Should_Fail_When_Unit_Is_Empty()
    {
        var validator = new CreateIngredientValidator();
        var command = new CreateIngredientCommand("Flour", "", 10);
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Unit);
    }

    [Fact]
    public void CreateKitchenTaskValidator_Should_Fail_When_OrderId_Is_Empty()
    {
        var validator = new CreateKitchenTaskValidator();
        var command = new CreateKitchenTaskCommand(Guid.Empty, Guid.NewGuid(), null);
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.OrderId);
    }

    [Fact]
    public void CreateKitchenTaskValidator_Should_Fail_When_AssignedTo_Is_Empty()
    {
        var validator = new CreateKitchenTaskValidator();
        var command = new CreateKitchenTaskCommand(Guid.NewGuid(), Guid.Empty, null);
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.AssignedTo);
    }

    [Fact]
    public void UpdateKitchenTaskValidator_Should_Fail_When_Id_Is_Empty()
    {
        var validator = new UpdateKitchenTaskValidator();
        var command = new UpdateKitchenTaskCommand(Guid.Empty, null, "Preparing", null);
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void CreateMenuItemValidator_Should_Fail_When_Name_Is_Empty()
    {
        var validator = new CreateMenuItemValidator();
        var command = new CreateMenuItemCommand("", 10, "Desc", MenuCategory.PIZZA, null, Array.Empty<string>());
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void CreateMenuItemValidator_Should_Fail_When_Price_Is_Negative()
    {
        var validator = new CreateMenuItemValidator();
        var command = new CreateMenuItemCommand("Pizza", -10, "Desc", MenuCategory.PIZZA, null, Array.Empty<string>());
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void UpdateMenuItemValidator_Should_Fail_When_Id_Is_Empty()
    {
        var validator = new UpdateMenuItemValidator();
        var command = new UpdateMenuItemCommand(Guid.Empty, "Pizza", 10, "Desc", MenuCategory.PIZZA, null, Array.Empty<string>());
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void UpdateMenuItemValidator_Should_Fail_When_Name_Is_Empty()
    {
        var validator = new UpdateMenuItemValidator();
        var command = new UpdateMenuItemCommand(Guid.NewGuid(), "", 10, "Desc", MenuCategory.PIZZA, null, Array.Empty<string>());
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void PlaceOrderValidator_Should_Fail_When_Items_Empty()
    {
        var validator = new PlaceOrderValidator();
        var dto = new OrderCreateDto { Items = new List<OrderItemCreateDto>(), Notes = null };
        var command = new PlaceOrderCommand(dto);
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor("Order.Items");
    }

    [Fact]
    public void PurchaseCouponValidator_Should_Fail_When_CouponId_Is_Empty()
    {
        var validator = new PurchaseCouponValidator();
        var command = new PurchaseCouponCommand(Guid.NewGuid(), Guid.Empty);
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.CouponId);
    }

    [Fact]
    public void PurchaseCouponValidator_Should_Pass_When_Valid()
    {
        var validator = new PurchaseCouponValidator();
        var command = new PurchaseCouponCommand(Guid.NewGuid(), Guid.NewGuid());
        
        var result = validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RedeemPointsValidator_Should_Fail_When_Points_Are_Zero()
    {
        var validator = new RedeemPointsValidator();
        var command = new RedeemPointsCommand(Guid.NewGuid(), 0, "Test");
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Points);
    }

    [Fact]
    public void RedeemPointsValidator_Should_Fail_When_Points_Are_Negative()
    {
        var validator = new RedeemPointsValidator();
        var command = new RedeemPointsCommand(Guid.NewGuid(), -10, "Test");
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Points);
    }

    [Fact]
    public void RedeemPointsValidator_Should_Fail_When_Description_Is_Empty()
    {
        var validator = new RedeemPointsValidator();
        var command = new RedeemPointsCommand(Guid.NewGuid(), 100, "");
        
        var result = validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void RedeemPointsValidator_Should_Pass_When_Valid()
    {
        var validator = new RedeemPointsValidator();
        var command = new RedeemPointsCommand(Guid.NewGuid(), 100, "Valid description");
        
        var result = validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
