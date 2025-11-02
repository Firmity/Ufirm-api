using Swashbuckle.SwaggerUi;
using System;
using System.Collections.Generic;

namespace UrestComplaintWebApi.Models
{
    public class CategoryDto
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsApproved { get; set; } = false;
        public int? ApprovedBy { get; set; }
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public int CreatedBy { get; set; }
    }

    public class ItemDto
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public int CategoryId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string MeasurementUnit { get; set; }
        public int MinStockLevel { get; set; }
        public bool IsApproved { get; set; }
        public int? ApprovedBy { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsActive { get; set; }
        public string BrandName { get; set; }
        public string HSNCode { get; set; }
    }

    public class VendorDto
    {
        public int Id { get; set; }
        public int PropertyId { get; set; }
        public string Name { get; set; }
        public string ContactPerson { get; set; }
        public string ContactNumber { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string GSTNumber { get; set; }
        public string PANNumber { get; set; }
        public bool IsApproved { get; set; }
        public int? ApprovedBy { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsActive { get; set; }
        public string PANFileUrl { get; set; }            
        public string GSTCertificateUrl { get; set; }      
        public string BrochureUrl { get; set; }            
        public string WebsiteUrl { get; set; }            
    }
    public class RateCardDto
    {
        public int Id { get; set; }
        public string CategoryName { get; set; }
        public string ItemName { get; set; }
        public string BrandName { get; set; }
        public string Description { get; set; }
        public string HSNCode { get; set; }
        public string VendorName { get; set; }
        public int ItemId { get; set; }
        public int VendorId { get; set; }
        public decimal Price { get; set; }
        public string MeasurementUnit { get; set; }
        public DateTime? ValidTill { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public bool IsApproved { get; set; }
    }

    public class PurchaseOrderDto
    {
        public int VendorId { get; set; }
        public string BillingAddress { get; set; }
        public string ShippingAddress { get; set; }
        public int CreatedBy { get; set; }
        public int PropertyId { get; set; }
        public List<PurchaseOrderItemDto> Items { get; set; }
    }

    public class PurchaseOrderItemDto
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class PurchaseOrderDetailDto
    {
        public int PurchaseOrderId { get; set; }
        public string PONumber { get; set; }
        public DateTime PODateTime { get; set; }
        public string BillingAddress { get; set; }
        public string ShippingAddress { get; set; }
        public int PropertyId { get; set; }
        public bool IsApproved { get; set; }

        public int VendorId { get; set; }
        public string VendorName { get; set; }
        public string ContactPerson { get; set; }
        public string ContactNumber { get; set; }
        public string Email { get; set; }

        public int PurchaseOrderItemId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
        public decimal LineTotal { get; set; }
    }

    public class PurchaseOrderGroupedDto
    {
        public int PurchaseOrderId { get; set; }
        public string PONumber { get; set; }
        public DateTime PODateTime { get; set; }
        public string BillingAddress { get; set; }
        public string ShippingAddress { get; set; }
        public int PropertyId { get; set; }
        public bool IsApproved { get; set; }

        public int VendorId { get; set; }
        public string VendorName { get; set; }
        public string ContactPerson { get; set; }
        public string ContactNumber { get; set; }
        public string Email { get; set; }

        public List<PurchaseOrderGroupedItemDto> Items { get; set; } = new List<PurchaseOrderGroupedItemDto>();
    }
    public class PurchaseOrderGroupedItemDto
    {
        public int PurchaseOrderItemId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public decimal Quantity { get; set; }
        public decimal Rate { get; set; }
        public decimal LineTotal { get; set; }
        public decimal? QuantityReceived { get; set; }
        public bool? IsCompleted { get; set; }
        public bool? IsRejected { get; set; }
        public string RejectionRemarks { get; set; }
    }
    public class StockViewModel
    {
        public string ItemName { get; set; }
        public string ItemDescription { get; set; }
        public string CategoryName { get; set; }
        public int? PropertyId { get; set; }
        public int? CategoryId { get; set; }
        public int? ItemId { get; set; }
        public int? CurrentQty { get; set; }
        public int StockId { get; set; }
        public int? MinStockLevel { get; set; }
    }
}
